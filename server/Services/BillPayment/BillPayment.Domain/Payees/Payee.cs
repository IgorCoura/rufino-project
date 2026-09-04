namespace BillPayment.Domain.Payees;

using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;

/// <summary>
/// O beneficiário que o tenant espera pagar. É contra este cadastro que os checks de
/// beneficiário, banco recebedor e valor comparam o que a consulta oficial devolveu —
/// sem ele, não há "condiz com o quê".
/// </summary>
public sealed class Payee : AggregateRoot<PayeeId>
{
    public const int LEGAL_NAME_MAX_LENGTH = 200;
    public const int ALIAS_MAX_LENGTH = 200;

    private readonly List<string> _aliases = [];
    private readonly List<BankCode> _acceptedBanks = [];

    public TenantId TenantId { get; private set; }
    public string LegalName { get; private set; } = string.Empty;
    public TaxId TaxId { get; private set; } = default!;
    public AmountPolicy AmountPolicy { get; private set; } = default!;
    public bool IsActive { get; private set; }

    /// <summary>Marca de confiança do tenant. Blacklist reprova o check de beneficiário.</summary>
    public PayeeStanding Standing { get; private set; } = PayeeStanding.Normal;

    /// <summary>Variações de nome já observadas em consultas. Razão social muda; o documento não.</summary>
    public IReadOnlyCollection<string> Aliases => _aliases.AsReadOnly();

    /// <summary>Vazio significa "sem expectativa" — o check de banco sai inconclusivo, não reprovado.</summary>
    public IReadOnlyCollection<BankCode> AcceptedBanks => _acceptedBanks.AsReadOnly();

    private Payee() { }

    private Payee(PayeeId id) : base(id) { }

    public static Payee Register(
        TenantId tenantId,
        string legalName,
        TaxId taxId,
        AmountPolicy amountPolicy,
        DateTime occurredAt)
    {
        var payee = new Payee(PayeeId.New()) { TenantId = tenantId, IsActive = true };

        payee.SetLegalName(legalName);
        payee.SetTaxId(taxId);
        payee.SetAmountPolicy(amountPolicy);

        payee.CreatedAt = occurredAt;
        payee.UpdatedAt = occurredAt;
        return payee;
    }

    /// <summary>
    /// Cadastro a partir dos valores crus do formulário. O <c>taxId</c> tem o tipo deduzido
    /// pelo número de dígitos e a política é montada aqui dentro — quem chama não escolhe
    /// factory nem compõe Value Object.
    /// </summary>
    public static Payee Register(
        TenantId tenantId,
        string legalName,
        string taxId,
        AmountPolicyKind amountPolicyKind,
        decimal? expectedAmount,
        decimal? tolerancePercent,
        decimal? minAmount,
        decimal? maxAmount,
        DateTime occurredAt)
        => Register(
            tenantId,
            legalName,
            TaxId.Parse(taxId),
            AmountPolicy.From(amountPolicyKind, expectedAmount, tolerancePercent, minAmount, maxAmount),
            occurredAt);

    public void Rename(string legalName, DateTime occurredAt)
    {
        EnsureActive();
        SetLegalName(legalName);
        UpdatedAt = occurredAt;
    }

    public void ChangeAmountPolicy(AmountPolicy amountPolicy, DateTime occurredAt)
    {
        EnsureActive();
        SetAmountPolicy(amountPolicy);
        UpdatedAt = occurredAt;
    }

    public void ChangeAmountPolicy(
        AmountPolicyKind amountPolicyKind,
        decimal? expectedAmount,
        decimal? tolerancePercent,
        decimal? minAmount,
        decimal? maxAmount,
        DateTime occurredAt)
        => ChangeAmountPolicy(
            AmountPolicy.From(amountPolicyKind, expectedAmount, tolerancePercent, minAmount, maxAmount),
            occurredAt);

    /// <summary>Aprende uma variação de nome vista numa consulta. Idempotente e sem distinção de caixa.</summary>
    public void LearnAlias(string alias, DateTime occurredAt)
    {
        EnsureActive();

        var normalized = NormalizeName(alias);
        if (normalized.Length == 0)
            throw PayeeErrors.AliasRequired();
        if (normalized.Length > ALIAS_MAX_LENGTH)
            throw PayeeErrors.AliasTooLong(ALIAS_MAX_LENGTH);

        if (MatchesName(normalized))
            return;

        _aliases.Add(normalized);
        UpdatedAt = occurredAt;
    }

    public void ForgetAlias(string alias, DateTime occurredAt)
    {
        EnsureActive();

        var normalized = NormalizeName(alias);
        if (_aliases.RemoveAll(a => string.Equals(a, normalized, StringComparison.OrdinalIgnoreCase)) > 0)
            UpdatedAt = occurredAt;
    }

    /// <summary>Passa a aceitar um banco recebedor. Idempotente.</summary>
    public void AllowBank(BankCode bankCode, DateTime occurredAt)
    {
        EnsureActive();

        if (bankCode is null)
            throw PayeeErrors.BankCodeRequired();
        if (_acceptedBanks.Contains(bankCode))
            return;

        _acceptedBanks.Add(bankCode);
        UpdatedAt = occurredAt;
    }

    public void DisallowBank(BankCode bankCode, DateTime occurredAt)
    {
        EnsureActive();

        if (bankCode is null)
            throw PayeeErrors.BankCodeRequired();
        if (_acceptedBanks.RemoveAll(b => b.Equals(bankCode)) > 0)
            UpdatedAt = occurredAt;
    }

    public void AllowBank(string bankCode, DateTime occurredAt)
        => AllowBank(ParseBankCode(bankCode), occurredAt);

    public void DisallowBank(string bankCode, DateTime occurredAt)
        => DisallowBank(ParseBankCode(bankCode), occurredAt);

    public void Deactivate(DateTime occurredAt)
    {
        if (!IsActive)
            return;

        IsActive = false;
        UpdatedAt = occurredAt;
    }

    public void Activate(DateTime occurredAt)
    {
        if (IsActive)
            return;

        IsActive = true;
        UpdatedAt = occurredAt;
    }

    /// <summary>
    /// Ponto único para o cadastro ligar e desligar o beneficiário. O ramo fica aqui, e não
    /// em quem chama, porque ativar e desativar são transições do próprio agregado.
    /// </summary>
    public void SetActivation(bool isActive, DateTime occurredAt)
    {
        if (isActive)
            Activate(occurredAt);
        else
            Deactivate(occurredAt);
    }

    /// <summary>
    /// Muda a marca de confiança. Fora do guard de ativação de propósito, como
    /// <see cref="SetActivation"/>: marcar um mau ator na blacklist precisa funcionar
    /// mesmo com o cadastro desativado. Idempotente.
    /// </summary>
    public void SetStanding(PayeeStanding standing, DateTime occurredAt)
    {
        if (standing is null)
            throw PayeeErrors.StandingRequired();
        if (Standing == standing)
            return;

        Standing = standing;
        UpdatedAt = occurredAt;
    }

    /// <summary>
    /// Sem banco cadastrado a resposta é <c>null</c> — ausência de expectativa é inconclusiva,
    /// e tratá-la como reprovação faria todo beneficiário novo falhar o check.
    /// </summary>
    public bool? AcceptsBank(BankCode bankCode)
    {
        if (_acceptedBanks.Count == 0)
            return null;
        return bankCode is not null && _acceptedBanks.Contains(bankCode);
    }

    /// <summary>Compara contra a razão social e todos os apelidos aprendidos, sem distinção de caixa.</summary>
    public bool MatchesName(string candidate)
    {
        var normalized = NormalizeName(candidate);
        if (normalized.Length == 0)
            return false;

        return string.Equals(LegalName, normalized, StringComparison.OrdinalIgnoreCase)
            || _aliases.Exists(a => string.Equals(a, normalized, StringComparison.OrdinalIgnoreCase));
    }

    private void EnsureActive()
    {
        if (!IsActive)
            throw PayeeErrors.InactivePayeeCannotBeChanged();
    }

    private void SetLegalName(string legalName)
    {
        var normalized = NormalizeName(legalName);
        if (normalized.Length == 0)
            throw PayeeErrors.LegalNameRequired();
        if (normalized.Length > LEGAL_NAME_MAX_LENGTH)
            throw PayeeErrors.LegalNameTooLong(LEGAL_NAME_MAX_LENGTH);

        LegalName = normalized;
    }

    private void SetTaxId(TaxId taxId)
        => TaxId = taxId ?? throw PayeeErrors.TaxIdRequired();

    private void SetAmountPolicy(AmountPolicy amountPolicy)
        => AmountPolicy = amountPolicy ?? throw PayeeErrors.AmountPolicyRequired();

    // Texto vazio vira o erro do beneficiário (BLP.PYE15), não o do VO: quem chamou
    // esqueceu de informar o banco, e essa é a mensagem que o usuário precisa ler.
    private static BankCode ParseBankCode(string bankCode)
        => string.IsNullOrWhiteSpace(bankCode)
            ? throw PayeeErrors.BankCodeRequired()
            : new BankCode(bankCode);

    private static string NormalizeName(string value)
        => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
}
