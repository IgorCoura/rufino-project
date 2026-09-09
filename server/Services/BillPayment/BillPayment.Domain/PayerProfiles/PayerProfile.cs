namespace BillPayment.Domain.PayerProfiles;

using BillPayment.Domain.Secrets;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;

/// <summary>
/// A identidade fiscal do tenant — quem ele é do ponto de vista de quem cobra.
/// É contra este cadastro que o check de pagador compara o documento extraído do
/// documento de cobrança, e é o degrau 1 da escada de roteamento entre tenants.
/// </summary>
public sealed class PayerProfile : AggregateRoot<PayerProfileId>
{
    public const int LEGAL_NAME_MAX_LENGTH = 200;

    /// <summary>Raiz do CNPJ: os 8 primeiros dígitos, comuns a matriz e filiais.</summary>
    public const int CNPJ_ROOT_LENGTH = 8;

    private readonly List<TaxId> _additionalTaxIds = [];

    public TenantId TenantId { get; private set; }
    public PayerKind Kind { get; private set; } = default!;
    public string LegalName { get; private set; } = string.Empty;
    public TaxId PrimaryTaxId { get; private set; } = default!;

    /// <summary>Filiais, CPF do titular ao lado do CNPJ do MEI, cônjuge.</summary>
    public IReadOnlyCollection<TaxId> AdditionalTaxIds => _additionalTaxIds.AsReadOnly();

    /// <summary>Quando ligado, qualquer CNPJ de mesma raiz é considerado próprio.</summary>
    public bool MatchByCnpjRoot { get; private set; }

    /// <summary>
    /// Ponteiro para a chave da subconta no cofre (<see cref="CredentialRef"/>) — nunca a chave.
    /// Nulo enquanto o tenant não configurar a própria conta; sem ele a consulta oficial
    /// degrada para indisponível e nenhum pagamento pode ser agendado.
    /// </summary>
    public CredentialRef? AsaasAccountRef { get; private set; }

    private PayerProfile() { }

    private PayerProfile(PayerProfileId id) : base(id) { }

    public static PayerProfile Register(
        TenantId tenantId,
        PayerKind kind,
        string legalName,
        TaxId primaryTaxId,
        DateTime occurredAt)
    {
        var profile = new PayerProfile(PayerProfileId.New()) { TenantId = tenantId };

        profile.SetKind(kind);
        profile.SetLegalName(legalName);
        profile.SetPrimaryTaxId(primaryTaxId);

        profile.CreatedAt = occurredAt;
        profile.UpdatedAt = occurredAt;
        return profile;
    }

    /// <summary>
    /// Cadastro a partir dos valores crus do formulário. O tipo do documento é deduzido pelo
    /// número de dígitos e conferido contra o tipo de pagador — informar CPF para pessoa
    /// jurídica continua reprovando em BLP.PRF02.
    /// </summary>
    public static PayerProfile Register(
        TenantId tenantId,
        PayerKind kind,
        string legalName,
        string primaryTaxId,
        DateTime occurredAt)
        => Register(tenantId, kind, legalName, TaxId.Parse(primaryTaxId), occurredAt);

    public void Rename(string legalName, DateTime occurredAt)
    {
        SetLegalName(legalName);
        UpdatedAt = occurredAt;
    }

    /// <summary>Acrescenta uma filial ou documento correlato. Idempotente.</summary>
    public void AddAdditionalTaxId(TaxId taxId, DateTime occurredAt)
    {
        if (taxId is null)
            throw PayerProfileErrors.AdditionalTaxIdRequired();
        if (taxId.Equals(PrimaryTaxId))
            throw PayerProfileErrors.AdditionalTaxIdCannotBePrimary();
        if (_additionalTaxIds.Contains(taxId))
            return;

        _additionalTaxIds.Add(taxId);
        UpdatedAt = occurredAt;
    }

    public void RemoveAdditionalTaxId(TaxId taxId, DateTime occurredAt)
    {
        if (taxId is null)
            throw PayerProfileErrors.AdditionalTaxIdRequired();
        if (_additionalTaxIds.RemoveAll(t => t.Equals(taxId)) > 0)
            UpdatedAt = occurredAt;
    }

    public void AddAdditionalTaxId(string taxId, DateTime occurredAt)
        => AddAdditionalTaxId(ParseAdditional(taxId), occurredAt);

    public void RemoveAdditionalTaxId(string taxId, DateTime occurredAt)
        => RemoveAdditionalTaxId(ParseAdditional(taxId), occurredAt);

    public void EnableCnpjRootMatching(DateTime occurredAt)
    {
        if (!Kind.SupportsCnpjRootMatching)
            throw PayerProfileErrors.CnpjRootMatchingRequiresCompany();
        if (MatchByCnpjRoot)
            return;

        MatchByCnpjRoot = true;
        UpdatedAt = occurredAt;
    }

    public void DisableCnpjRootMatching(DateTime occurredAt)
    {
        if (!MatchByCnpjRoot)
            return;

        MatchByCnpjRoot = false;
        UpdatedAt = occurredAt;
    }

    /// <summary>
    /// Ponto único para o cadastro ligar e desligar o casamento por raiz de CNPJ. O ramo
    /// fica aqui porque ligar exige ser pessoa jurídica (BLP.PRF07) e desligar não exige nada.
    /// </summary>
    public void SetCnpjRootMatching(bool enabled, DateTime occurredAt)
    {
        if (enabled)
            EnableCnpjRootMatching(occurredAt);
        else
            DisableCnpjRootMatching(occurredAt);
    }

    /// <summary>Vincula a subconta do provedor, concluindo o onboarding de pagamento.</summary>
    /// <summary>
    /// O <c>authToken</c> que o provedor devolve em cada webhook desta conta, guardado cifrado.
    /// </summary>
    /// <remarks>
    /// <strong>Não é a chave de API</strong>, e a diferença é de direção: a chave autentica NÓS
    /// no provedor; este token autentica O PROVEDOR em nós. Usar a chave para os dois papéis
    /// faria a credencial que paga contas trafegar em todo header de entrada.
    /// <c>GET /v3/webhooks/{id}</c> devolve apenas <c>hasAuthToken: true</c> (medido em
    /// 2026-09-08), então o valor não é recuperável do provedor — guardar aqui é a única chance.
    /// </remarks>
    public CredentialRef? AsaasWebhookRef { get; private set; }

    /// <summary>O id do webhook no provedor, para atualizar, remover e conferir saúde.</summary>
    public string? AsaasWebhookId { get; private set; }

    /// <summary>
    /// Quando chegou o último evento desta conta. Silêncio prolongado com ordens vivas significa
    /// webhook morto (fila interrompida, URL quebrada) — é o gatilho para voltar ao polling
    /// agressivo em vez de descobrir pelo boleto que não andou.
    /// </summary>
    public DateTime? LastWebhookEventAt { get; private set; }

    /// <summary>
    /// Quando o token deste webhook foi gerado pela última vez. É o relógio da rotação — e
    /// também a referência de "desde quando esperamos notícia" enquanto nenhum evento chegou.
    /// </summary>
    public DateTime? AsaasWebhookRotatedAt { get; private set; }

    /// <summary>Vincula o webhook com um token NOVO — ou seja, uma rotação.</summary>
    public void LinkAsaasWebhook(CredentialRef webhookRef, string providerWebhookId, DateTime occurredAt)
    {
        if (string.IsNullOrWhiteSpace(providerWebhookId))
            throw PayerProfileErrors.AsaasWebhookIdRequired();

        AsaasWebhookRef = webhookRef ?? throw PayerProfileErrors.AsaasWebhookTokenRequired();
        AsaasWebhookId = providerWebhookId.Trim();
        AsaasWebhookRotatedAt = occurredAt;
        UpdatedAt = occurredAt;
    }

    /// <summary>
    /// O webhook foi consertado no provedor <strong>sem trocar o token</strong> — fila reativada,
    /// reabilitado, ou URL corrigida.
    /// </summary>
    /// <remarks>
    /// Existe separado do <see cref="LinkAsaasWebhook"/> porque a varredura passa de meia em meia
    /// hora: se curar fosse rotacionar, o token trocaria o dia inteiro, e cada troca é uma janela
    /// em que uma falha entre gravar no provedor e gravar no cofre deixa o webhook mudo e
    /// irrecuperável — o <c>GET</c> do provedor não devolve o token.
    /// </remarks>
    public void ConfirmAsaasWebhook(string providerWebhookId, DateTime occurredAt)
    {
        if (string.IsNullOrWhiteSpace(providerWebhookId))
            throw PayerProfileErrors.AsaasWebhookIdRequired();
        if (AsaasWebhookRef is null)
            throw PayerProfileErrors.AsaasWebhookTokenRequired();

        AsaasWebhookId = providerWebhookId.Trim();
        UpdatedAt = occurredAt;
    }

    public void UnlinkAsaasWebhook(DateTime occurredAt)
    {
        AsaasWebhookRef = null;
        AsaasWebhookId = null;
        LastWebhookEventAt = null;
        AsaasWebhookRotatedAt = null;
        UpdatedAt = occurredAt;
    }

    /// <summary>
    /// O token deste webhook já passou da validade combinada?
    /// </summary>
    /// <remarks>
    /// Rotação por calendário é higiene, não urgência: o token do provedor não expira sozinho.
    /// Webhook sem carimbo de rotação é anterior a esta conta de tempo — rotaciona na primeira
    /// passagem, e a partir dali o relógio existe.
    /// </remarks>
    public bool IsWebhookRotationDue(DateTime nowUtc, TimeSpan interval)
    {
        if (!HasWebhook)
            return false;

        return AsaasWebhookRotatedAt is not { } rotatedAt || rotatedAt.Add(interval) <= nowUtc;
    }

    /// <summary>
    /// O webhook está mudo há tempo demais? <strong>Silêncio não é prova de defeito</strong> —
    /// quem decide se isso importa é quem sabe se há ordem viva esperando notícia.
    /// </summary>
    /// <remarks>
    /// A referência é o último evento recebido e, na falta dele, o provisionamento: um webhook
    /// recém-criado que nunca recebeu nada não está mudo, está novo.
    /// </remarks>
    public bool IsWebhookSilentSince(DateTime nowUtc, TimeSpan tolerance)
    {
        if (!HasWebhook)
            return false;

        return (LastWebhookEventAt ?? AsaasWebhookRotatedAt) is { } reference
            && reference.Add(tolerance) <= nowUtc;
    }

    /// <summary>Marca que o provedor deu sinal de vida. Monotônica: evento atrasado não regride.</summary>
    public void RecordWebhookActivity(DateTime occurredAt)
    {
        if (LastWebhookEventAt is { } last && last >= occurredAt)
            return;

        LastWebhookEventAt = occurredAt;
        UpdatedAt = occurredAt;
    }

    /// <summary>O webhook desta conta já foi provisionado no provedor?</summary>
    public bool HasWebhook => AsaasWebhookRef is not null && AsaasWebhookId is not null;

    public void LinkAsaasAccount(CredentialRef accountRef, DateTime occurredAt)
    {
        AsaasAccountRef = accountRef ?? throw PayerProfileErrors.AsaasKeyRequired();

        // O webhook daquela conta é provisionado por este evento, fora desta transação. Chave
        // nova é conta possivelmente nova, então vale também na troca.
        AddDomainEvent(new AsaasAccountLinkedDomainEvent(Id, TenantId, occurredAt));
        UpdatedAt = occurredAt;
    }

    /// <summary>Desvincula a subconta. Idempotente — o cofre é responsabilidade de quem chama.</summary>
    public void UnlinkAsaasAccount(DateTime occurredAt)
    {
        if (AsaasAccountRef is null)
            return;

        AsaasAccountRef = null;
        UpdatedAt = occurredAt;
    }

    /// <summary>Enquanto não houver subconta vinculada, o tenant usa o sistema mas não agenda pagamento.</summary>
    public bool CanSchedulePayments => AsaasAccountRef is not null;

    /// <summary>
    /// Responde a pergunta do check de pagador: este documento é do tenant?
    /// Cobre o principal, os adicionais e — quando habilitado — a raiz do CNPJ.
    /// </summary>
    public bool Owns(TaxId candidate)
    {
        if (candidate is null)
            return false;
        if (candidate.Equals(PrimaryTaxId) || _additionalTaxIds.Contains(candidate))
            return true;

        return OwnsByCnpjRoot(candidate);
    }

    /// <summary>
    /// Separado de <see cref="Owns"/> porque a evidência do check precisa distinguir
    /// "é exatamente o meu documento" de "é uma filial inferida pela raiz".
    /// </summary>
    public bool OwnsByCnpjRoot(TaxId candidate)
    {
        if (!MatchByCnpjRoot || candidate is null)
            return false;
        if (candidate.Kind != TaxIdKind.CNPJ || PrimaryTaxId.Kind != TaxIdKind.CNPJ)
            return false;

        return string.Equals(RootOf(candidate), RootOf(PrimaryTaxId), StringComparison.Ordinal);
    }

    // Texto vazio vira BLP.PRF08 em vez do erro de formato do VO — a causa é a omissão
    // do campo, e é isso que precisa chegar a quem preencheu o cadastro.
    private static TaxId ParseAdditional(string taxId)
        => string.IsNullOrWhiteSpace(taxId)
            ? throw PayerProfileErrors.AdditionalTaxIdRequired()
            : TaxId.Parse(taxId);

    private static string RootOf(TaxId taxId) => taxId.Value[..CNPJ_ROOT_LENGTH];

    private void SetKind(PayerKind kind)
        => Kind = kind ?? throw PayerProfileErrors.PrimaryTaxIdRequired();

    private void SetPrimaryTaxId(TaxId primaryTaxId)
    {
        if (primaryTaxId is null)
            throw PayerProfileErrors.PrimaryTaxIdRequired();
        if (!primaryTaxId.Kind.Equals(Kind.ExpectedPrimaryTaxIdKind))
            throw PayerProfileErrors.PrimaryTaxIdKindMismatch(
                Kind.Name, Kind.ExpectedPrimaryTaxIdKind.Name, primaryTaxId.Kind.Name);

        PrimaryTaxId = primaryTaxId;
    }

    private void SetLegalName(string legalName)
    {
        var normalized = string.IsNullOrWhiteSpace(legalName) ? string.Empty : legalName.Trim();
        if (normalized.Length == 0)
            throw PayerProfileErrors.LegalNameRequired();
        if (normalized.Length > LEGAL_NAME_MAX_LENGTH)
            throw PayerProfileErrors.LegalNameTooLong(LEGAL_NAME_MAX_LENGTH);

        LegalName = normalized;
    }
}
