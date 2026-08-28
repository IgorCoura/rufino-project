namespace BillPayment.Domain.CaptureSources;

using BillPayment.Domain.SeedWork;
using BillPayment.Domain.Secrets;
using BillPayment.Domain.SharedKernel;

/// <summary>
/// Uma caixa de e-mail (ou portal) que este tenant monitora, com o cursor de onde a última
/// sincronização parou.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Uma fonte pertence a um tenant e só a ele.</strong> Quando duas contas monitoram a
/// mesma caixa, existem duas <see cref="CaptureSource"/> independentes — credenciais, cursores
/// e pipelines separados, que não se conhecem (ADR-008). É o que faz o isolamento ser por
/// construção: toda leitura filtra por <see cref="TenantId"/> como em qualquer outro Aggregate,
/// sem autorização por linha.
/// </para>
/// <para>
/// O Aggregate <strong>não emite Domain Events</strong> nesta fase. O que dispara a cascata de
/// extração é o <c>CaptureItem</c>, não a fonte; acrescentar evento aqui só faria sentido no dia
/// em que alguém precise reagir à conexão ou à desativação de uma fonte.
/// </para>
/// </remarks>
public sealed class CaptureSource : AggregateRoot<CaptureSourceId>
{
    public const int DISPLAY_NAME_MAX_LENGTH = 120;

    /// <summary>Cobre tanto o endereço de caixa (máx. 320 pelo RFC) quanto a URL de um portal.</summary>
    public const int ADDRESS_MAX_LENGTH = 500;

    /// <summary>O <c>deltaLink</c> do Graph carrega token opaco e é bem maior que uma URL comum.</summary>
    public const int SYNC_CURSOR_MAX_LENGTH = MonitoredFolder.SYNC_CURSOR_MAX_LENGTH;

    public const int SYNC_ERROR_MAX_LENGTH = MonitoredFolder.SYNC_ERROR_MAX_LENGTH;

    public const int FOLDER_PATH_MAX_LENGTH = MonitoredFolder.PATH_MAX_LENGTH;

    /// <summary>
    /// Teto de pastas acompanhadas. Cada uma custa uma chamada ao provedor por ciclo, então o
    /// limite é proteção contra limitação de taxa, não contra armazenamento.
    /// </summary>
    public const int MAX_FOLDERS = 20;

    public TenantId TenantId { get; private set; }
    public CaptureSourceKind Kind { get; private set; } = default!;

    /// <summary>Rótulo escolhido pelo usuário. Não participa de nenhuma decisão.</summary>
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>
    /// O que é monitorado, na forma canônica. Use <see cref="Normalize"/> para produzir a chave
    /// de busca — normalizar em dois lugares diferentes é como o cadastro passa a divergir da
    /// consulta.
    /// </summary>
    public string Address { get; private set; } = string.Empty;

    /// <summary>Ponteiro para o cofre. <strong>Nunca o segredo</strong> — <c>BLP.CPS01</c>.</summary>
    public CredentialRef? Credential { get; private set; }

    private readonly List<MonitoredFolder> _folders = [];

    /// <summary>
    /// As pastas acompanhadas. <strong>Sempre ao menos uma</strong> — a caixa de entrada, quando
    /// ninguém escolheu outra.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Existe por uma razão medida, não hipotética: numa caixa real de uso misto, <strong>8 de
    /// 11 anexos da primeira página não eram conta a pagar</strong> — havia CNH, contrato social
    /// e contrato de locação (medição de 2026-08-11). Apontar a fonte para pastas alimentadas
    /// por regra do cliente de e-mail é o que impede o sistema de armazenar documento pessoal
    /// junto com boleto.
    /// </para>
    /// <para>
    /// É <em>minimização de dado na origem</em>: o que não é lido não precisa ser protegido,
    /// cifrado, nem apagado depois. <strong>Mas continua não sendo pré-requisito</strong> — o
    /// padrão é a caixa de entrada e a triagem é trabalho do software, não do usuário.
    /// </para>
    /// <para>
    /// <strong>Não há varredura recursiva.</strong> A delta query do provedor é da pasta, não da
    /// árvore: subpasta que não estiver nesta lista não é lida. Decisão de 2026-08-11 — a
    /// alternativa (descobrir a árvore a cada ciclo) custaria uma chamada por pasta marcada e
    /// faria o número de cursores crescer sozinho.
    /// </para>
    /// </remarks>
    public IReadOnlyCollection<MonitoredFolder> Folders => _folders.AsReadOnly();

    /// <summary>
    /// Piso temporal da captura: nada recebido <strong>antes</strong> desta data é lido.
    /// <c>null</c> = sem limite, a caixa inteira.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>É da fonte, não da pasta.</strong> O piso descreve a caixa — pasta acrescentada
    /// depois herda o mesmo, e ter um piso por pasta só produziria fontes cujas pastas discordam
    /// sobre desde quando aquela caixa é acompanhada.
    /// </para>
    /// <para>
    /// <strong>Quem aplica o corte é o provedor, não nós.</strong> A delta query do Graph aceita
    /// exatamente <c>$filter=receivedDateTime ge {data}</c> (e <c>gt</c>) — nada mais, e nenhum
    /// teto. Por isso o modelo tem piso e não janela: um teto teria de ser filtro nosso sobre o
    /// que já trafegou, e faria a fonte parar de capturar sozinha ao vencer.
    /// </para>
    /// <para>
    /// <strong>Trocar o piso obriga a descartar os cursores</strong>, e é
    /// <see cref="ChangeCaptureSince"/> que garante isso — o provedor grava o filtro
    /// <em>dentro</em> do <c>deltaLink</c>, então um cursor velho continuaria mandando a data
    /// velha e a troca não valeria nada. Falha silenciosa, que é o que o ADR-014 existe para
    /// impedir.
    /// </para>
    /// </remarks>
    public DateOnly? CaptureSince { get; private set; }

    /// <summary>Instante da última tentativa <em>concluída</em> em qualquer pasta.</summary>
    public DateTime? LastSyncAt { get; private set; }

    /// <summary>
    /// Motivo da última falha em qualquer pasta. Nulo quando <strong>todas</strong> as pastas
    /// varreram com êxito na última tentativa de cada uma.
    /// </summary>
    /// <remarks>
    /// É um resumo para a tela de listagem; o diagnóstico por pasta vive em
    /// <see cref="MonitoredFolder.LastSyncError"/>. Colapsar as duas coisas esconderia que uma
    /// pasta está falhando enquanto as outras seguem — que é exatamente o caso de uma pasta
    /// renomeada no cliente de e-mail.
    /// </remarks>
    public string? LastSyncError { get; private set; }

    public bool IsEnabled { get; private set; }

    private CaptureSource() { }

    private CaptureSource(CaptureSourceId id) : base(id) { }

    /// <summary>
    /// Conecta a fonte. O nome é <c>Connect</c>, e não <c>Register</c>, porque o agregado só
    /// nasce depois de o acesso à caixa ter sido provado — a Application faz a leitura de teste
    /// contra o provedor antes de chamar aqui.
    /// </summary>
    public static CaptureSource Connect(
        TenantId tenantId,
        CaptureSourceKind kind,
        string displayName,
        string address,
        CredentialRef? credential,
        DateTime occurredAt,
        string? folderPath = null,
        DateOnly? captureSince = null)
    {
        var source = new CaptureSource(CaptureSourceId.New()) { TenantId = tenantId };

        source.SetKind(kind);
        source.SetDisplayName(displayName);
        source.SetAddress(address);
        source.SetCredential(credential);
        source.SetCaptureSince(captureSince, occurredAt);
        source._folders.Add(new MonitoredFolder(folderPath, occurredAt));

        source.IsEnabled = true;
        source.CreatedAt = occurredAt;
        source.UpdatedAt = occurredAt;
        return source;
    }

    public void Rename(string displayName, DateTime occurredAt)
    {
        SetDisplayName(displayName);
        UpdatedAt = occurredAt;
    }

    /// <summary>
    /// Aponta a fonte para outra credencial — rotação do segredo do registro de aplicativo, ou
    /// reconexão depois de uma revogação.
    /// </summary>
    /// <remarks>
    /// Trocar a credencial <strong>não</strong> mexe no cursor: a caixa é a mesma e reprocessar
    /// tudo do zero só geraria itens já vistos. Limpa o erro da última tentativa, porque a
    /// credencial nova é exatamente a resposta ao erro que estava registrado ali.
    /// </remarks>
    public void ReplaceCredential(CredentialRef? credential, DateTime occurredAt)
    {
        SetCredential(credential);
        LastSyncError = null;
        UpdatedAt = occurredAt;
    }

    /// <summary>
    /// Passa a acompanhar <strong>exatamente</strong> esta pasta, descartando as demais.
    /// </summary>
    /// <remarks>
    /// <strong>Cada pasta nasce sem cursor, obrigatoriamente.</strong> A varredura incremental do
    /// provedor é por pasta: um cursor obtido lendo a caixa de entrada não significa nada dentro
    /// de "Contas". Herdá-lo faria a primeira varredura da pasta nova voltar vazia e o sistema
    /// concluir que não há boleto ali — falha silenciosa, que é justamente o que o ADR-014 existe
    /// para evitar. Como a pasta é recriada, isso sai de graça.
    /// </remarks>
    public void ChangeFolder(string? folderPath, DateTime occurredAt)
    {
        _folders.Clear();
        _folders.Add(new MonitoredFolder(folderPath, occurredAt));
        UpdatedAt = occurredAt;
    }

    /// <summary>
    /// Move o piso temporal da captura. <c>null</c> devolve a fonte à caixa inteira.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Descarta o cursor de todas as pastas, sempre</strong> — e isso não é zelo: o Graph
    /// grava as opções de consulta <em>dentro</em> do <c>deltaLink</c> que devolve, então um
    /// cursor obtido com o piso velho continuaria filtrando pelo piso velho. Sem o descarte, a
    /// data nova seria decorativa e ninguém perceberia.
    /// </para>
    /// <para>
    /// Vale nas duas direções. Baixar o piso é o caso óbvio (há histórico novo a buscar), mas
    /// subi-lo também exige, porque o que manda é o filtro gravado no cursor, não a coluna.
    /// </para>
    /// <para>
    /// Reler não duplica nada: a ingestão é idempotente por <c>(tenant, fonte, mensagem, anexo)</c>.
    /// </para>
    /// </remarks>
    public void ChangeCaptureSince(DateOnly? captureSince, DateTime occurredAt)
    {
        SetCaptureSince(captureSince, occurredAt);
        ResetAllCursors(occurredAt);
    }

    /// <summary>Acrescenta uma pasta à lista acompanhada. Ela nasce sem cursor: a primeira varredura lê tudo.</summary>
    public MonitoredFolder AddFolder(string? folderPath, DateTime occurredAt)
    {
        var normalized = MonitoredFolder.NormalizePath(folderPath);

        if (_folders.Any(f => f.HasPath(normalized)))
            throw CaptureSourceErrors.FolderAlreadyMonitored(Describe(normalized));

        if (_folders.Count >= MAX_FOLDERS)
            throw CaptureSourceErrors.TooManyFolders(MAX_FOLDERS);

        var folder = new MonitoredFolder(normalized, occurredAt);
        _folders.Add(folder);
        UpdatedAt = occurredAt;

        return folder;
    }

    /// <summary>
    /// Deixa de acompanhar uma pasta. Os itens já ingeridos dela permanecem.
    /// </summary>
    /// <remarks>
    /// Recusa remover a última: uma fonte sem pasta nenhuma não varreria nada e <em>não
    /// avisaria</em> — zero pasta produz zero item exatamente como uma caixa vazia. Quem quer
    /// parar de varrer desativa a fonte, que é uma intenção explícita e reversível.
    /// </remarks>
    public void RemoveFolder(string? folderPath, DateTime occurredAt)
    {
        var normalized = MonitoredFolder.NormalizePath(folderPath);
        var folder = _folders.FirstOrDefault(f => f.HasPath(normalized))
            ?? throw CaptureSourceErrors.FolderNotMonitored(Describe(normalized));

        if (_folders.Count == 1)
            throw CaptureSourceErrors.CannotRemoveLastFolder();

        _folders.Remove(folder);
        UpdatedAt = occurredAt;
    }

    /// <summary>Liga e desliga a captura. É o botão de parada do usuário sobre esta fonte.</summary>
    public void SetEnabled(bool enabled, DateTime occurredAt)
    {
        IsEnabled = enabled;
        UpdatedAt = occurredAt;
    }

    /// <summary>
    /// Porta de entrada da sincronização: recusa a fonte desativada e devolve as pastas a varrer,
    /// cada uma com o próprio cursor.
    /// </summary>
    /// <remarks>
    /// Devolver as pastas daqui — em vez de deixar quem chama ler <see cref="Folders"/> — é o que
    /// impede a orquestração de sincronizar uma fonte desligada por esquecer a checagem.
    /// </remarks>
    public IReadOnlyList<MonitoredFolder> BeginSync()
        => IsEnabled ? _folders.AsReadOnly() : throw CaptureSourceErrors.CannotSyncDisabled(Id.Value);

    /// <summary>Registra a varredura bem-sucedida de uma pasta e avança o cursor dela.</summary>
    public void RecordSyncSuccess(MonitoredFolderId folderId, string? cursor, DateTime occurredAt)
    {
        FolderOf(folderId).RecordSyncSuccess(cursor, occurredAt);
        LastSyncAt = occurredAt;

        // O resumo da raiz só limpa quando NENHUMA pasta está falhando: uma pasta renomeada no
        // cliente de e-mail continuaria quebrada, e apagar o aviso porque as outras foram bem
        // esconderia exatamente a falha que alguém precisa ver.
        LastSyncError = _folders.Select(f => f.LastSyncError).FirstOrDefault(e => e is not null);
        UpdatedAt = occurredAt;
    }

    /// <summary>
    /// Registra a falha da tentativa numa pasta. <strong>Não toca no cursor</strong> — avançá-lo
    /// pularia mensagens que ninguém leu, e apagá-lo transformaria uma falha de rede em varredura
    /// completa da pasta.
    /// </summary>
    public void RecordSyncFailure(MonitoredFolderId folderId, string error, DateTime occurredAt)
    {
        var folder = FolderOf(folderId);
        folder.RecordSyncFailure(error, occurredAt);

        LastSyncAt = occurredAt;
        LastSyncError = folder.LastSyncError;
        UpdatedAt = occurredAt;
    }

    /// <summary>
    /// Descarta o cursor de uma pasta para que a próxima varredura dela leia tudo de novo. É a
    /// resposta ao <c>410 Gone</c> do Graph, que invalida o <c>deltaLink</c> quando ele envelhece.
    /// </summary>
    /// <remarks>
    /// Sem isto, um cursor expirado faria a pasta parar de sincronizar em silêncio — a pior falha
    /// possível num sistema cuja rede de segurança contra "a conta não chegou" ainda está por vir
    /// (ADR-014).
    /// </remarks>
    public void ResetCursor(MonitoredFolderId folderId, DateTime occurredAt)
    {
        FolderOf(folderId).ResetCursor(occurredAt);
        UpdatedAt = occurredAt;
    }

    /// <summary>
    /// Descarta o cursor de <strong>todas</strong> as pastas: a próxima varredura relê a caixa
    /// inteira. É o que sustenta a releitura deliberada, quando o cadastro muda e o usuário quer
    /// reavaliar o que já passou.
    /// </summary>
    /// <remarks>
    /// Nada é reprocessado em duplicidade por causa disto: a ingestão é idempotente por
    /// <c>(tenant, fonte, mensagem, anexo)</c>, então o que já virou item continua sendo o mesmo
    /// item. O que muda é que o que foi <em>descartado</em> volta a ser avaliado — agora com o
    /// cadastro que existe hoje.
    /// </remarks>
    public void ResetAllCursors(DateTime occurredAt)
    {
        foreach (var folder in _folders)
            folder.ResetCursor(occurredAt);

        UpdatedAt = occurredAt;
    }

    private MonitoredFolder FolderOf(MonitoredFolderId folderId)
        => _folders.FirstOrDefault(f => f.Id == folderId)
            ?? throw CaptureSourceErrors.FolderNotMonitored(folderId.ToString());

    /// <summary>
    /// Chave canônica de comparação do endereço, incluindo o índice global do ADR-008.
    /// </summary>
    /// <remarks>
    /// Caixa de e-mail vai para minúsculas; URL de portal é apenas aparada, porque o caminho de
    /// uma URL é sensível a maiúsculas e rebaixá-lo apontaria para outro recurso.
    /// </remarks>
    public static string Normalize(CaptureSourceKind kind, string? address)
    {
        ArgumentNullException.ThrowIfNull(kind);

        return kind.RequiresWebUrl
            ? address?.Trim() ?? string.Empty
            : EmailSyntax.Normalize(address);
    }

    private void SetKind(CaptureSourceKind kind)
        => Kind = kind ?? throw CaptureSourceErrors.KindRequired();

    private void SetDisplayName(string displayName)
    {
        var trimmed = displayName?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            throw CaptureSourceErrors.DisplayNameRequired();
        if (trimmed.Length > DISPLAY_NAME_MAX_LENGTH)
            throw CaptureSourceErrors.DisplayNameTooLong(DISPLAY_NAME_MAX_LENGTH);

        DisplayName = trimmed;
    }

    private void SetAddress(string address)
    {
        var normalized = Normalize(Kind, address);
        if (normalized.Length == 0)
            throw CaptureSourceErrors.AddressRequired();
        if (normalized.Length > ADDRESS_MAX_LENGTH)
            throw CaptureSourceErrors.AddressTooLong(ADDRESS_MAX_LENGTH);

        if (Kind.RequiresEmailAddress && !EmailSyntax.IsValidAddress(normalized))
            throw CaptureSourceErrors.InvalidMailboxAddress(address);

        if (Kind.RequiresWebUrl && !IsHttpsUrl(normalized))
            throw CaptureSourceErrors.InvalidPortalUrl(address);

        Address = normalized;
    }

    /// <summary>Como a pasta aparece numa mensagem de erro — a caixa de entrada não tem caminho.</summary>
    private static string Describe(string? normalizedPath) => normalizedPath ?? "Caixa de Entrada";

    /// <remarks>
    /// "Hoje" vem de <paramref name="occurredAt"/>, e não do relógio: o Domain não lê relógio. A
    /// comparação é contra a data <em>em UTC</em>, o que torna a guarda um dia permissiva para
    /// quem escolhe à noite no fuso do Brasil — e permissivo é o lado certo de errar, porque
    /// recusar seria barrar a data de hoje.
    /// </remarks>
    private void SetCaptureSince(DateOnly? captureSince, DateTime occurredAt)
    {
        if (captureSince is { } since && since > DateOnly.FromDateTime(occurredAt))
            throw CaptureSourceErrors.CaptureSinceInFuture(since);

        CaptureSince = captureSince;
        UpdatedAt = occurredAt;
    }

    private void SetCredential(CredentialRef? credential)
    {
        if (Kind.RequiresCredential && credential is null)
            throw CaptureSourceErrors.CredentialRequired();

        Credential = credential;
    }

    private static bool IsHttpsUrl(string normalized)
        => Uri.TryCreate(normalized, UriKind.Absolute, out var uri)
            && string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal);
}
