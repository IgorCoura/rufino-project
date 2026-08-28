namespace BillPayment.Domain.CaptureItems;

using BillPayment.Domain.Bills;
using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;

/// <summary>
/// O registro bruto de um artefato ingerido — um anexo, um link ou o corpo de uma mensagem —
/// incluindo os que nunca viraram boleto.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Um item por artefato, não por mensagem.</strong> Um e-mail com três boletos anexos
/// produz três itens, porque cada um tem seu próprio destino: um pode virar <c>Bill</c>, outro
/// ser de outro pagador e o terceiro nem ser boleto. Com um item por mensagem, o estado seria
/// misto e a projeção de visibilidade do ADR-008 teria de existir por anexo — dentro de um
/// agregado cujo status diz outra coisa. A idempotência da ingestão passa a ser
/// <c>(TenantId, SourceId, ExternalMessageId, ArtifactKey)</c>.
/// </para>
/// <para>
/// A quarentena tem <strong>dois níveis de visibilidade</strong>, e quem os decide é o
/// <see cref="CaptureItemStatus"/>, não a tela: ver <c>ExposesFinancialDetail</c>.
/// </para>
/// </remarks>
public sealed class CaptureItem : AggregateRoot<CaptureItemId>
{
    public const int EXTERNAL_MESSAGE_ID_MAX_LENGTH = 512;
    public const int ARTIFACT_KEY_MAX_LENGTH = 512;
    public const int SENDER_MAX_LENGTH = 320;
    public const int SUBJECT_MAX_LENGTH = 500;
    public const int CONTENT_TYPE_MAX_LENGTH = 150;
    public const int FILE_NAME_MAX_LENGTH = 255;
    public const int CONTENT_HASH_MAX_LENGTH = 100;
    /// <summary>O <c>Message-ID</c> do cabeçalho RFC-822 cabe folgado em 512.</summary>
    public const int INTERNET_MESSAGE_ID_MAX_LENGTH = 512;

    public const int STORAGE_KEY_MAX_LENGTH = 512;
    public const int SOURCE_URL_MAX_LENGTH = 2000;

    /// <summary>
    /// Sentinela de <see cref="StorageKey"/> para o item que ficou protegido por senha: existe
    /// item, não existe arquivo guardado.
    /// </summary>
    /// <remarks>
    /// É constante, e não string solta no handler, porque <see cref="HasStoredArtifact"/> é a
    /// única coisa entre um GET de documento e um 500 tentando buscar a chave "pending-unlock"
    /// no balde.
    /// </remarks>
    public const string PENDING_UNLOCK = "pending-unlock";

    /// <summary>Sentinela de <see cref="StorageKey"/> para o item sem instrumento reconhecido.</summary>
    public const string PENDING_REVIEW = "pending-review";

    public const int REASON_MAX_LENGTH = 200;
    public const int UNLOCKED_BY_MAX_LENGTH = 100;

    /// <summary>
    /// Espaço para a mensagem do erro que impediu o processamento — diagnóstico, não payload.
    /// </summary>
    /// <remarks>
    /// Cortado, e não recusado, quando estoura: perder o item por causa do tamanho da explicação
    /// da falha inverteria a razão de guardá-la.
    /// </remarks>
    public const int LAST_ERROR_MAX_LENGTH = 500;

    /// <summary>Motivo de quem foi para <c>Failed</c> por uma regra que vai recusar sempre.</summary>
    public const string REASON_PROCESSING_REJECTED = "processing_rejected";

    /// <summary>Motivo de quem foi para <c>Failed</c> por esgotar as tentativas.</summary>
    public const string REASON_ATTEMPTS_EXHAUSTED = "processing_attempts_exhausted";

    /// <summary>Motivo padrão de quem foi reprovado sem observação escrita.</summary>
    public const string REASON_DISMISSED = "dismissed_by_user";

    /// <summary>
    /// Teto da espera entre tentativas. A espera dobra a cada falha e para de dobrar aqui.
    /// </summary>
    /// <remarks>
    /// Sem teto, a oitava tentativa de um item com espera-base de um minuto cairia daqui a duas
    /// horas — mais do que o prazo que um boleto costuma ter de folga.
    /// </remarks>
    private static readonly TimeSpan MAX_RETRY_DELAY = TimeSpan.FromMinutes(30);

    /// <summary>Quantas duplicações de espera antes de o expoente parar de crescer.</summary>
    private const int MAX_BACKOFF_SHIFT = 10;

    public TenantId TenantId { get; private set; }
    public CaptureSourceId SourceId { get; private set; }

    /// <summary>Id da mensagem no provedor. Estável entre sincronizações — é o que dá idempotência.</summary>
    public string ExternalMessageId { get; private set; } = string.Empty;

    /// <summary>
    /// O <c>Message-ID</c> do cabeçalho RFC-822 — <strong>do e-mail, não da cópia</strong>.
    /// </summary>
    /// <remarks>
    /// <see cref="ExternalMessageId"/> endereça o item onde ele está guardado, e a pasta faz
    /// parte desse endereço: mover a mensagem o mata, e o download passa a devolver 404 para
    /// sempre. Este aqui é escrito pelo remetente e não muda nunca — é por ele que a mensagem é
    /// reencontrada quando o endereço morre. Nulo quando o provedor não informa.
    /// </remarks>
    public string? InternetMessageId { get; private set; }

    /// <summary>
    /// Qual artefato daquela mensagem é este: nome do anexo, hash da URL, ou a marca do corpo.
    /// Distingue os irmãos que compartilham o mesmo <see cref="ExternalMessageId"/>.
    /// </summary>
    public string ArtifactKey { get; private set; } = string.Empty;

    /// <summary>
    /// Tipo de mídia declarado pelo provedor na ingestão.
    /// </summary>
    /// <remarks>
    /// <strong>Guardado porque o <see cref="ArtifactKey"/> não é nome de arquivo.</strong> No
    /// Microsoft Graph ele é um identificador opaco, sem extensão nenhuma — deduzir o tipo dali
    /// faz todo anexo parecer PDF. Medido em 2026-08-11: o extrator de visão recebia imagem
    /// rotulada como <c>application/pdf</c> e o provedor recusava, então os anexos que não eram
    /// PDF continuavam inalcançáveis mesmo depois de a visão existir.
    /// </remarks>
    public string? ContentType { get; private set; }

    /// <summary>Nome do arquivo, quando o provedor informa. Diagnóstico e triagem — nunca chave.</summary>
    public string? FileName { get; private set; }

    public string Sender { get; private set; } = string.Empty;
    public string? Subject { get; private set; }
    public DateTime ReceivedAt { get; private set; }

    /// <summary>SHA-256 do artefato, para detectar o mesmo documento reenviado noutra mensagem.</summary>
    public string? ContentHash { get; private set; }

    /// <summary>Onde os bytes originais estão, cifrados. Nulo enquanto o download não aconteceu.</summary>
    public string? StorageKey { get; private set; }

    /// <summary>Se existe arquivo recuperável no armazenamento.</summary>
    /// <remarks>
    /// <strong>Ter <see cref="StorageKey"/> preenchido não é ter arquivo.</strong> A retenção é
    /// por desfecho: só o caminho que reconheceu boleto grava os bytes, e os desfechos que
    /// mantêm o item para uma pessoa resolver carimbam uma sentinela
    /// (<see cref="PENDING_UNLOCK"/>, <see cref="PENDING_REVIEW"/>) para preservar o histórico
    /// sem prometer conteúdo. Quem for buscar o documento pergunta aqui, não ao campo.
    /// </remarks>
    public bool HasStoredArtifact =>
        !string.IsNullOrEmpty(StorageKey)
        && StorageKey != PENDING_UNLOCK
        && StorageKey != PENDING_REVIEW;

    public CaptureItemStatus Status { get; private set; } = default!;

    /// <summary>Por qual degrau da escada este item foi atribuído. Nulo antes do roteamento.</summary>
    public RoutingConfidence? Routing { get; private set; }

    /// <summary>URL de origem quando o artefato veio de link — evidência e reprocesso.</summary>
    public string? SourceUrl { get; private set; }

    /// <summary>
    /// <strong>Qual campo</strong> do <c>PayerProfile</c> derivou a senha do PDF — jamais a
    /// senha. É o que permite auditar a decisão sem transformar o banco num depósito de segredos.
    /// </summary>
    public string? UnlockedBy { get; private set; }

    public ExtractionMethod? Extraction { get; private set; }

    /// <summary>Motivo legível do desfecho de quarentena, em código estável.</summary>
    public string? Reason { get; private set; }

    public BillId? BillId { get; private set; }

    /// <summary>O item original, quando este foi descartado por duplicidade de conteúdo.</summary>
    public CaptureItemId? DiscardedOf { get; private set; }

    public UserId? ClaimedBy { get; private set; }
    public DateTime? ClaimedAt { get; private set; }

    /// <summary>Quem reprovou o item, quando alguém reprovou.</summary>
    /// <remarks>
    /// Reprovar tira trabalho da vista sem que ninguém tenha verificado o documento — é a única
    /// operação da quarentena com essa propriedade. Por isso registra autor, no mesmo molde da
    /// reivindicação: sem ele a fila deixa de ser auditável.
    /// </remarks>
    public UserId? DismissedBy { get; private set; }
    public DateTime? DismissedAt { get; private set; }

    /// <summary>
    /// Se o arquivo guardado foi anexado por uma pessoa, e não baixado do provedor.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Muda o caminho do processamento: o artefato é lido do balde em vez de rebaixado do e-mail,
    /// e a escada de link não roda — o documento já está em mãos, e buscá-lo de novo gastaria
    /// rede para descobrir o que já se tem.
    /// </para>
    /// <para>
    /// <strong>Também muda a retenção.</strong> A retenção por desfecho existe para o balde não
    /// virar depósito do que foi capturado automaticamente; aqui alguém escolheu subir o arquivo,
    /// e apagá-lo jogaria fora o trabalho dela. Anexo manual é sempre guardado.
    /// </para>
    /// </remarks>
    public bool ManuallySupplied { get; private set; }

    /// <summary>Quantas vezes um worker já pegou este item para processar.</summary>
    /// <remarks>
    /// <para>
    /// <strong>Cresce na reivindicação, não no fim.</strong> Contar só as falhas registradas
    /// deixaria de fora justamente a pior delas — a que derruba o processo antes de escrever
    /// coisa alguma —, e um item que mata o worker voltaria à fila para sempre. Contando na
    /// saída da fila, toda passagem custa uma tentativa, inclusive a que não voltou.
    /// </para>
    /// <para>
    /// Zerado por <see cref="Reopen"/> e por <see cref="MarkVisionPending"/>: os dois significam
    /// que o item avançou, e o orçamento da fila seguinte é outro.
    /// </para>
    /// </remarks>
    public int ProcessingAttempts { get; private set; }

    /// <summary>A mensagem da última falha de processamento. Diagnóstico para uma pessoa.</summary>
    public string? LastError { get; private set; }

    /// <summary>
    /// Até quando este item é de quem o reivindicou — e, depois de uma falha, quando ele volta.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Uma coluna, dois papéis, de propósito.</strong> Enquanto um worker processa, ela
    /// é o aluguel: a consulta da fila pula quem tem aluguel vivo, então dois workers não pegam
    /// o mesmo item — e um worker que morre não o segura para sempre, porque o aluguel vence
    /// sozinho. Depois de uma falha transitória, ela é a espera antes da próxima tentativa. Nos
    /// dois casos a pergunta da fila é a mesma: <em>já posso mexer neste item?</em>
    /// </para>
    /// <para>
    /// Limpa em toda transição (ver <c>Transition</c>): o aluguel era da fila anterior.
    /// </para>
    /// </remarks>
    public DateTime? LeaseExpiresAt { get; private set; }

    private CaptureItem() { }

    private CaptureItem(CaptureItemId id) : base(id) { }

    /// <summary>Registra o artefato como recebido, antes de qualquer tentativa de leitura.</summary>
    public static CaptureItem Ingest(
        TenantId tenantId,
        CaptureSourceId sourceId,
        string externalMessageId,
        string artifactKey,
        string sender,
        string? subject,
        DateTime receivedAt,
        DateTime occurredAt,
        string? contentType = null,
        string? fileName = null,
        string? internetMessageId = null)
    {
        if (sourceId.Equals(CaptureSourceId.Empty))
            throw CaptureItemErrors.SourceRequired();

        var item = new CaptureItem(CaptureItemId.New())
        {
            TenantId = tenantId,
            SourceId = sourceId,
            ReceivedAt = receivedAt,
            Status = CaptureItemStatus.Received,
        };

        item.SetExternalMessageId(externalMessageId);
        item.SetInternetMessageId(internetMessageId);
        item.SetArtifactKey(artifactKey);
        item.SetSender(sender);
        item.SetSubject(subject);
        item.SetContentType(contentType);
        item.SetFileName(fileName);

        item.CreatedAt = occurredAt;
        item.UpdatedAt = occurredAt;
        return item;
    }

    /// <summary>
    /// Guarda onde os bytes ficaram e o hash do conteúdo. Não muda o status — armazenar é
    /// pré-requisito do processamento, não um desfecho dele.
    /// </summary>
    public void StoreArtifact(string contentHash, string storageKey, DateTime occurredAt)
    {
        var hash = Require(contentHash, nameof(ContentHash), CONTENT_HASH_MAX_LENGTH);
        var key = Require(storageKey, nameof(StorageKey), STORAGE_KEY_MAX_LENGTH);

        ContentHash = hash;
        StorageKey = key;
        UpdatedAt = occurredAt;
    }

    /// <summary>O artefato precisa ser baixado de uma URL antes de poder ser lido.</summary>
    public void MarkLinkPending(string sourceUrl, DateTime occurredAt)
    {
        var url = sourceUrl?.Trim();
        if (string.IsNullOrEmpty(url))
            throw CaptureItemErrors.SourceUrlRequired();
        if (url.Length > SOURCE_URL_MAX_LENGTH)
            throw CaptureItemErrors.TextTooLong(nameof(SourceUrl), SOURCE_URL_MAX_LENGTH);

        Transition(CaptureItemStatus.LinkPending, occurredAt);
        SourceUrl = url;
    }

    /// <summary>
    /// Registra de qual endereço o documento foi trazido, sem mexer no status.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Não é transição, é procedência.</strong> A resolução do link acontece dentro do
    /// mesmo processamento que já vai decidir o destino do item — passar por <c>LinkPending</c> só
    /// para voltar no instante seguinte inventaria um estado intermediário que ninguém observa e
    /// que a máquina de estados teria de admitir em todas as direções.
    /// </para>
    /// <para>
    /// <strong>A URL é tão sigilosa quanto o documento.</strong> Medido em 2026-08-11: os endereços
    /// de boleto respondem <c>200</c> sem autenticação nenhuma — quem tem o link tem o boleto. Ele
    /// sai por API só sob o mesmo portão do ADR-008 que já cobre o <see cref="StorageKey"/>.
    /// </para>
    /// </remarks>
    public void RecordResolvedLink(string sourceUrl, DateTime occurredAt)
    {
        var url = sourceUrl?.Trim();
        if (string.IsNullOrEmpty(url))
            throw CaptureItemErrors.SourceUrlRequired();
        if (url.Length > SOURCE_URL_MAX_LENGTH)
            throw CaptureItemErrors.TextTooLong(nameof(SourceUrl), SOURCE_URL_MAX_LENGTH);

        SourceUrl = url;
        UpdatedAt = occurredAt;
    }

    /// <summary>
    /// Registra o endereço onde o documento <em>estaria</em>, quando não há receita para buscá-lo.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>É o que transforma "não consegui" em fila de trabalho.</strong> A escada de link
    /// só alcança host com receita configurada, então emissor novo cai na quarentena sem dizer
    /// de onde veio — e a informação que faltava para cadastrar a receita era exatamente essa.
    /// Guardando o endereço tentado, a quarentena passa a responder "quais emissores mandam
    /// boleto por link que ainda não sabemos buscar".
    /// </para>
    /// <para>
    /// Grava no mesmo campo do link resolvido, e sob o mesmo portão do ADR-008: a URL continua
    /// sendo credencial ao portador, resolvida ou não. Quem sai sem portão é só o
    /// <see cref="LinkHost"/>, que identifica o emissor sem levar ao documento.
    /// </para>
    /// </remarks>
    public void RecordAttemptedLink(string sourceUrl, DateTime occurredAt)
        => RecordResolvedLink(sourceUrl, occurredAt);

    /// <summary>
    /// Só o host do <see cref="SourceUrl"/> — quem hospeda, sem o caminho que abre o documento.
    /// </summary>
    /// <remarks>
    /// Existe para a tela poder dizer <c>www.asaas.com</c> sem entregar
    /// <c>www.asaas.com/i/55p08…</c>, que é a credencial ao portador. É o dado que decide qual
    /// receita cadastrar, e o único da URL que pode sair sem o portão do ADR-008.
    /// </remarks>
    public string? LinkHost =>
        Uri.TryCreate(SourceUrl, UriKind.Absolute, out var uri) ? uri.Host : null;

    /// <summary>A escada de resolução esgotou sem trazer o documento.</summary>
    public void MarkLinkFailed(string reason, DateTime occurredAt)
    {
        Transition(CaptureItemStatus.LinkFailed, occurredAt);
        Reason = RequireReason(reason);
    }

    /// <summary>
    /// Põe o artefato na fila do extrator de IA e devolve o worker para os outros itens.
    /// </summary>
    /// <remarks>
    /// <strong>Sair da frente é o ponto.</strong> A cota de IA é escassa e a chamada leva de 3 a 5
    /// segundos; segurar o processamento esperando a vez fazia os itens comuns — mediana de 150 ms
    /// — ficarem atrás de um que levava 30. Medido em 2026-08-26: 27% dos itens consumindo 86% do
    /// tempo. Aqui o item cede o lugar e é retomado pelo worker de visão, que respeita a cota sem
    /// travar ninguém.
    /// </remarks>
    public void MarkVisionPending(string reason, DateTime occurredAt)
    {
        Transition(CaptureItemStatus.VisionPending, occurredAt);
        Reason = RequireReason(reason);

        // Chegar aqui é ter atravessado os degraus 0 a 2 com êxito — o item avançou, e a fila
        // da IA tem orçamento próprio. Carregar o contador faria as tentativas gastas na faixa
        // rápida descontarem das chamadas ao extrator, que são o recurso escasso de verdade.
        ProcessingAttempts = 0;
        LastError = null;
    }

    /// <summary>PDF cifrado que nenhum candidato de senha abriu.</summary>
    public void MarkLocked(DateTime occurredAt)
    {
        EnsureArtifactStored();
        Transition(CaptureItemStatus.Locked, occurredAt);
        Reason = "pdf_locked";
    }

    /// <summary>
    /// Um instrumento de pagamento válido foi extraído. <paramref name="unlockedBy"/> registra
    /// qual campo do perfil abriu o PDF, quando foi o caso.
    /// </summary>
    public void MarkParsed(ExtractionMethod method, string? unlockedBy, DateTime occurredAt)
    {
        ArgumentNullException.ThrowIfNull(method);
        EnsureArtifactStored();

        var derivedFrom = unlockedBy?.Trim();
        if (derivedFrom is { Length: > UNLOCKED_BY_MAX_LENGTH })
            throw CaptureItemErrors.TextTooLong(nameof(UnlockedBy), UNLOCKED_BY_MAX_LENGTH);

        Transition(CaptureItemStatus.Parsed, occurredAt);
        Extraction = method;
        UnlockedBy = string.IsNullOrEmpty(derivedFrom) ? null : derivedFrom;
        Reason = null;
    }

    /// <summary>Nenhum boleto válido no artefato.</summary>
    public void MarkUnrecognized(string reason, DateTime occurredAt)
    {
        Transition(CaptureItemStatus.Unrecognized, occurredAt);
        Reason = RequireReason(reason);
    }

    /// <summary>
    /// Devolve o artefato à fila para ser avaliado de novo pela cascata de hoje.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>O desfecho de um artefato é do dia em que ele passou, não para sempre.</strong> A
    /// cascata muda — degrau novo, prompt novo, modelo novo — e o cadastro também: sem
    /// <c>PayerProfile</c> não há senha derivada, e sem <c>Payee</c> nem <c>TrustedOrigin</c> o
    /// que o parser erra é descartado. Um item julgado antes disso ficaria congelado num veredito
    /// que a versão atual do sistema não daria.
    /// </para>
    /// <para>
    /// <strong>Volta para <c>Received</c> em vez de reprocessar aqui</strong>: assim ele atravessa
    /// exatamente o mesmo caminho do primeiro processamento, pelo mesmo worker, com a mesma
    /// transação e a mesma retenção por desfecho. Um segundo caminho de processamento seria um
    /// segundo lugar para as regras envelhecerem.
    /// </para>
    /// <para>
    /// <strong>Nada é apagado.</strong> O artefato continua no armazenamento e a chave continua
    /// no item — é justamente o que permite reavaliar sem baixar de novo do provedor.
    /// </para>
    /// </remarks>
    public void Reopen(DateTime occurredAt)
    {
        Transition(CaptureItemStatus.Received, occurredAt);
        Reason = null;
        Extraction = null;
        UnlockedBy = null;

        // Reabrir é dar orçamento novo. Sem zerar, um item que veio de `Failed` gastaria a
        // primeira tentativa já no teto e voltaria para lá na primeira falha — inclusive quando
        // a reabertura foi motivada justamente pela correção que faz o processamento funcionar.
        ProcessingAttempts = 0;
        LastError = null;

        // Reabrir desfaz a reprovação: o item volta a ser trabalho pendente, e manter o autor
        // descreveria uma decisão que deixou de valer.
        DismissedBy = null;
        DismissedAt = null;

        // A procedência também é do dia em que o item passou: a escada de link vai ser percorrida
        // de novo, com as receitas de hoje, e pode trazer o documento de outro endereço — ou de
        // nenhum. Manter o anterior descreveria uma busca que não foi a que aconteceu.
        SourceUrl = null;
    }

    /// <summary>Roteou para este tenant e virou boleto.</summary>
    public void Promote(BillId billId, RoutingConfidence confidence, DateTime occurredAt)
    {
        if (billId.Equals(Bills.BillId.Empty))
            throw CaptureItemErrors.BillRequired();
        if (confidence is null)
            throw CaptureItemErrors.RoutingConfidenceRequired();

        Transition(CaptureItemStatus.Promoted, occurredAt);
        BillId = billId;
        Routing = confidence;
        Reason = null;
    }

    /// <summary>O pagador foi identificado e não é deste tenant.</summary>
    public void MarkForeign(string reason, DateTime occurredAt)
    {
        Transition(CaptureItemStatus.ForeignPayer, occurredAt);
        Reason = RequireReason(reason);
    }

    /// <summary>Nada resolveu de quem é. Vai para a fila de reivindicação do dono da fonte.</summary>
    public void MarkUnrouted(string reason, DateTime occurredAt)
    {
        Transition(CaptureItemStatus.Unrouted, occurredAt);
        Reason = RequireReason(reason);
    }

    /// <summary>
    /// Uma pessoa assumiu que o item é desta conta, e o item vira boleto por essa decisão.
    /// </summary>
    /// <remarks>
    /// Recusado quando a escada já concluiu que o pagador é outro (<c>BLP.CPI04</c>): a
    /// reivindicação é o degrau mais fraco de todos, e deixá-la sobrepor a única evidência
    /// <em>constatada</em> de propriedade inverteria a ordem de confiança do doc 07.
    /// </remarks>
    public void Claim(UserId claimedBy, BillId billId, DateTime occurredAt)
    {
        if (claimedBy.Equals(UserId.Empty))
            throw CaptureItemErrors.ClaimedByRequired();
        if (Status == CaptureItemStatus.ForeignPayer)
            throw CaptureItemErrors.ClaimContradictsExtractedPayer(Id.Value);

        Promote(billId, RoutingConfidence.Claimed, occurredAt);
        ClaimedBy = claimedBy;
        ClaimedAt = occurredAt;
    }

    /// <summary>
    /// Uma pessoa olhou e disse que não reconhece: o item sai da fila de pendências.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>É decisão humana, e por isso registra autor</strong> — ao contrário de
    /// <see cref="Discard"/>, que é o sistema reconhecendo duplicata. A máquina de estados
    /// recusa reprovar o que já virou boleto ou o que já foi atribuído a outro pagador: no
    /// primeiro o dinheiro está em jogo, no segundo a decisão não é deste tenant.
    /// </para>
    /// <para>
    /// <strong>Nada é apagado.</strong> O artefato continua guardado — é o que permite desfazer,
    /// e o que permite auditar depois o que foi reprovado.
    /// </para>
    /// </remarks>
    public void Dismiss(UserId dismissedBy, string? note, DateTime occurredAt)
    {
        if (dismissedBy.Equals(UserId.Empty))
            throw CaptureItemErrors.DismissedByRequired();

        Transition(CaptureItemStatus.Dismissed, occurredAt);
        DismissedBy = dismissedBy;
        DismissedAt = occurredAt;

        var trimmed = note?.Trim();
        Reason = string.IsNullOrEmpty(trimmed)
            ? REASON_DISMISSED
            : trimmed[..Math.Min(trimmed.Length, REASON_MAX_LENGTH)];
    }

    /// <summary>
    /// Uma pessoa buscou o documento à mão e o anexou; o item volta à fila para ser lido.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Preserva a <see cref="SourceUrl"/>, ao contrário de <see cref="Reopen"/>.</strong>
    /// O reprocessamento comum apaga a procedência porque a escada de link vai ser percorrida de
    /// novo e pode trazer o documento de outro endereço. Aqui não vai percorrer nada: o arquivo
    /// já está em mãos, e o endereço é a prova de onde a pessoa o tirou.
    /// </para>
    /// <para>
    /// Zera o orçamento de tentativas pelo mesmo motivo do <see cref="Reopen"/>: o item mudou, e
    /// julgar o conteúdo novo com o saldo gasto no antigo o mandaria para <c>Failed</c> na
    /// primeira falha.
    /// </para>
    /// </remarks>
    public void AttachManualArtifact(
        string contentHash,
        string storageKey,
        string? contentType,
        string? fileName,
        DateTime occurredAt)
    {
        Transition(CaptureItemStatus.Received, occurredAt);
        StoreArtifact(contentHash, storageKey, occurredAt);
        SetContentType(contentType);
        SetFileName(fileName);

        ManuallySupplied = true;

        // NÃO se marca `ExtractionMethod.Manual` aqui: esse campo diz COMO o instrumento foi
        // lido, e quem o preenche é a cascata logo adiante (texto embutido, QR, visão). Quem
        // diz que uma PESSOA trouxe o arquivo é `ManuallySupplied` — os dois fatos são
        // diferentes, e colapsá-los faria o item mentir sobre o degrau que o resolveu.
        Extraction = null;

        Reason = null;
        UnlockedBy = null;
        DismissedBy = null;
        DismissedAt = null;
        ProcessingAttempts = 0;
        LastError = null;
    }

    /// <summary>Duplicata de um artefato já processado, com o ponteiro para o original.</summary>
    public void Discard(CaptureItemId originalItemId, DateTime occurredAt)
    {
        Transition(CaptureItemStatus.Discarded, occurredAt);
        DiscardedOf = originalItemId.Equals(CaptureItemId.Empty) ? null : originalItemId;
        Reason = "duplicate_content";
    }

    /// <summary>
    /// Uma tentativa de processamento estourou. Decide entre tentar de novo e desistir.
    /// </summary>
    /// <param name="permanent">
    /// Se a falha é uma recusa determinística — regra do domínio dizendo não. Repetir devolve a
    /// mesma resposta, então a decisão de desistir é imediata e não espera esgotar tentativas.
    /// </param>
    /// <param name="maxAttempts">Teto de tentativas para a falha que <em>pode</em> ser passageira.</param>
    /// <param name="baseRetryDelay">Espera após a primeira falha; dobra a cada uma seguinte.</param>
    /// <returns><c>true</c> quando o item desistiu e foi para <c>Failed</c>.</returns>
    /// <remarks>
    /// <para>
    /// <strong>É a regra que faltava, e a ausência dela foi medida.</strong> Até 2026-08-26 o
    /// worker tratava toda falha como passageira e devolvia o item à fila indefinidamente:
    /// quatro itens somaram 1.709 tentativas do mesmo <c>BLP.BIL15</c> — um PDF com dois boletos
    /// de naturezas diferentes, que ia recusar igual para sempre. Pior que o desperdício era o
    /// bloqueio: a fila é <c>ORDER BY received_at LIMIT 10</c>, então cada item envenenado
    /// ocupava uma vaga em caráter permanente, e dez deles parariam a captura inteira sem um
    /// único alerta.
    /// </para>
    /// <para>
    /// <strong>Distinguir permanente de passageiro é o ponto.</strong> Retentar existe para
    /// contornar rede instável e provedor fora do ar; a quarentena existe para conter o que
    /// nenhuma repetição conserta. Sem a segunda, a primeira vira fonte permanente de carga.
    /// </para>
    /// <para>
    /// A espera dobra a cada falha (até <see cref="MAX_RETRY_DELAY"/>) e mora no mesmo campo do
    /// aluguel, porque a pergunta da fila é uma só: já posso mexer neste item?
    /// </para>
    /// </remarks>
    public bool RecordProcessingFailure(
        string error,
        bool permanent,
        int maxAttempts,
        TimeSpan baseRetryDelay,
        DateTime occurredAt)
    {
        // Item que já concluiu não volta a falhar. A exceção que chega aqui nesse caso é de uma
        // execução que perdeu a corrida para outra que terminou — e arrastar um `Promoted` para
        // `Failed` destruiria o desfecho bom por causa do eco do perdedor.
        if (Status.IsTerminal)
            return false;

        SetLastError(error);

        if (!permanent && ProcessingAttempts < maxAttempts)
        {
            // Continua na fila. O aluguel vira a espera: enquanto ele estiver vivo a consulta
            // pula o item, e é isso que espaça as tentativas sem nenhum agendador à parte.
            LeaseExpiresAt = NextAttemptAt(ProcessingAttempts, baseRetryDelay, occurredAt);
            UpdatedAt = occurredAt;
            return false;
        }

        Transition(CaptureItemStatus.Failed, occurredAt);
        Reason = permanent ? REASON_PROCESSING_REJECTED : REASON_ATTEMPTS_EXHAUSTED;
        return true;
    }

    /// <summary>Marca que um worker assumiu este item até o instante informado.</summary>
    /// <remarks>
    /// Existe para a fila em memória dos testes e para quem reivindicar fora do SQL de claim.
    /// No caminho de produção quem carimba é o próprio <c>UPDATE ... SKIP LOCKED</c>, porque
    /// escolher e marcar precisam ser o mesmo passo — em dois passos, dois workers escolhem o
    /// mesmo item antes de qualquer um marcar.
    /// </remarks>
    public void Lease(DateTime expiresAt, DateTime occurredAt)
    {
        ProcessingAttempts++;
        LeaseExpiresAt = expiresAt;
        UpdatedAt = occurredAt;
    }

    /// <summary>Quando a próxima tentativa fica liberada, dobrando a espera a cada falha.</summary>
    private static DateTime NextAttemptAt(int attempts, TimeSpan baseRetryDelay, DateTime occurredAt)
    {
        var shift = Math.Min(Math.Max(attempts - 1, 0), MAX_BACKOFF_SHIFT);
        var scaled = baseRetryDelay.Ticks * (1L << shift);

        return occurredAt.AddTicks(Math.Min(scaled, MAX_RETRY_DELAY.Ticks));
    }

    private void SetLastError(string? value)
    {
        var trimmed = value?.Trim();

        LastError = string.IsNullOrEmpty(trimmed)
            ? null
            : trimmed[..Math.Min(trimmed.Length, LAST_ERROR_MAX_LENGTH)];
    }

    /// <remarks>
    /// <para>
    /// <strong>O aviso de "travou" e o de "destravou" saem daqui, e de mais lugar nenhum.</strong>
    /// Emiti-los dentro de cada <c>MarkXxx</c> deixaria buraco por degrau — foi assim que
    /// <c>VisionPending → LinkFailed</c> ficou de fora da matriz de transições e prendeu item na
    /// fila da IA para sempre. Um gancho só, na única porta por onde todo estado passa.
    /// </para>
    /// <para>
    /// O evento carrega o <c>Status</c> alvo, não o <c>Reason</c>: os <c>MarkXxx</c> escrevem o
    /// motivo <em>depois</em> de transicionar, e ler o campo aqui devolveria o motivo anterior.
    /// Quem traduz estado em causa é o consumidor, contra o Smart Enum.
    /// </para>
    /// </remarks>
    private void Transition(CaptureItemStatus target, DateTime occurredAt)
    {
        if (!Status.CanTransitionTo(target))
            throw CaptureItemErrors.InvalidTransition(Status.Name, target.Name);

        var previous = Status;
        Status = target;

        // O aluguel era da fila de onde o item saiu. Carregá-lo para a fila seguinte faria o
        // item nascer lá já bloqueado, esperando um prazo que não diz respeito a ela.
        LeaseExpiresAt = null;
        UpdatedAt = occurredAt;

        if (target.AwaitsRescue)
        {
            AddDomainEvent(new CaptureItemStuckDomainEvent(
                Id, TenantId, SourceId, target.Name, ReceivedAt, occurredAt));
        }
        else if (previous.AwaitsRescue)
        {
            AddDomainEvent(new CaptureItemUnstuckDomainEvent(Id, TenantId, target.Name, occurredAt));
        }
    }

    private void EnsureArtifactStored()
    {
        if (string.IsNullOrEmpty(StorageKey))
            throw CaptureItemErrors.ArtifactRequired();
    }

    /// <summary>
    /// Reaponta o item para onde a mensagem está agora.
    /// </summary>
    /// <remarks>
    /// <strong>Não é mutação de negócio — é correção de endereço.</strong> O e-mail é o mesmo
    /// (prova disso é o <see cref="InternetMessageId"/>, que não muda); o que mudou foi o lugar
    /// onde o provedor o guarda. Sem isto, um download que se recuperou pela busca por
    /// <c>Message-ID</c> voltaria a falhar na chamada seguinte, porque os ids velhos
    /// continuariam gravados.
    /// </remarks>
    public void Relocate(string externalMessageId, string artifactKey, DateTime occurredAt)
    {
        SetExternalMessageId(externalMessageId);
        SetArtifactKey(artifactKey);
        UpdatedAt = occurredAt;
    }

    private void SetInternetMessageId(string? value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            InternetMessageId = null;
            return;
        }

        if (trimmed.Length > INTERNET_MESSAGE_ID_MAX_LENGTH)
            throw CaptureItemErrors.TextTooLong(nameof(InternetMessageId), INTERNET_MESSAGE_ID_MAX_LENGTH);

        InternetMessageId = trimmed;
    }

    private void SetExternalMessageId(string value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            throw CaptureItemErrors.ExternalMessageIdRequired();
        if (trimmed.Length > EXTERNAL_MESSAGE_ID_MAX_LENGTH)
            throw CaptureItemErrors.TextTooLong(nameof(ExternalMessageId), EXTERNAL_MESSAGE_ID_MAX_LENGTH);

        ExternalMessageId = trimmed;
    }

    private void SetArtifactKey(string value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            throw CaptureItemErrors.ArtifactKeyRequired();
        if (trimmed.Length > ARTIFACT_KEY_MAX_LENGTH)
            throw CaptureItemErrors.TextTooLong(nameof(ArtifactKey), ARTIFACT_KEY_MAX_LENGTH);

        ArtifactKey = trimmed;
    }

    /// <summary>
    /// Guarda o tipo declarado, aparado — nunca deduzido.
    /// </summary>
    /// <remarks>
    /// Truncar em vez de recusar: o tipo vem do provedor e um valor esquisito não pode impedir a
    /// ingestão de um boleto. Quem decide se o extrator sabe abrir aquilo é <c>DocumentPayload</c>.
    /// </remarks>
    private void SetContentType(string? value)
    {
        var trimmed = value?.Trim();
        ContentType = string.IsNullOrEmpty(trimmed)
            ? null
            : trimmed[..Math.Min(trimmed.Length, CONTENT_TYPE_MAX_LENGTH)];
    }

    private void SetFileName(string? value)
    {
        var trimmed = value?.Trim();
        FileName = string.IsNullOrEmpty(trimmed)
            ? null
            : trimmed[..Math.Min(trimmed.Length, FILE_NAME_MAX_LENGTH)];
    }

    private void SetSender(string value)
    {
        // Remetente vem de fora e não decide nada sozinho — quem decide confiança é o
        // TrustedOrigin. Normalizar aqui é o que faz a resolução casar com o que foi cadastrado.
        var normalized = EmailSyntax.Normalize(value);
        if (normalized.Length > SENDER_MAX_LENGTH)
            throw CaptureItemErrors.TextTooLong(nameof(Sender), SENDER_MAX_LENGTH);

        Sender = normalized;
    }

    private void SetSubject(string? value)
    {
        var trimmed = value?.Trim();
        if (trimmed is { Length: > SUBJECT_MAX_LENGTH })
            trimmed = trimmed[..SUBJECT_MAX_LENGTH];

        Subject = string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    private static string RequireReason(string reason)
    {
        var trimmed = reason?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            throw CaptureItemErrors.ReasonRequired();
        if (trimmed.Length > REASON_MAX_LENGTH)
            throw CaptureItemErrors.TextTooLong(nameof(Reason), REASON_MAX_LENGTH);

        return trimmed;
    }

    private static string Require(string value, string field, int maxLength)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            throw CaptureItemErrors.ArtifactRequired();
        if (trimmed.Length > maxLength)
            throw CaptureItemErrors.TextTooLong(field, maxLength);

        return trimmed;
    }
}
