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
    public const int SYNC_CURSOR_MAX_LENGTH = 2000;

    public const int SYNC_ERROR_MAX_LENGTH = 500;

    public const int FOLDER_PATH_MAX_LENGTH = 250;

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

    /// <summary>
    /// Que parte da caixa monitorar. Nulo = a caixa de entrada inteira.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Existe por uma razão medida, não hipotética: numa caixa real de uso misto, <strong>8 de
    /// 11 anexos da primeira página não eram conta a pagar</strong> — havia CNH, contrato social
    /// e contrato de locação (medição de 2026-08-11). Apontar a fonte para uma pasta alimentada
    /// por regra do cliente de e-mail é o que impede o sistema de armazenar documento pessoal
    /// junto com boleto.
    /// </para>
    /// <para>
    /// É <em>minimização de dado na origem</em>: o que não é lido não precisa ser protegido,
    /// cifrado, nem apagado depois.
    /// </para>
    /// </remarks>
    public string? FolderPath { get; private set; }

    /// <summary>Onde a última sincronização bem-sucedida parou. Nulo = próxima varredura é completa.</summary>
    public string? SyncCursor { get; private set; }

    /// <summary>Instante da última tentativa <em>concluída</em>, com ou sem êxito.</summary>
    public DateTime? LastSyncAt { get; private set; }

    /// <summary>Motivo da última falha. Nulo depois de uma sincronização bem-sucedida.</summary>
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
        string? folderPath = null)
    {
        var source = new CaptureSource(CaptureSourceId.New()) { TenantId = tenantId };

        source.SetKind(kind);
        source.SetDisplayName(displayName);
        source.SetAddress(address);
        source.SetCredential(credential);
        source.SetFolderPath(folderPath);

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
    /// Aponta a fonte para outra pasta da caixa — ou para a caixa de entrada, com <c>null</c>.
    /// </summary>
    /// <remarks>
    /// <strong>Descarta o cursor, obrigatoriamente.</strong> A varredura incremental do provedor
    /// é por pasta: um cursor obtido lendo a caixa de entrada não significa nada dentro de
    /// "Contas". Mantê-lo faria a primeira varredura da pasta nova voltar vazia e o sistema
    /// concluir que não há boleto ali — falha silenciosa, que é justamente o que o ADR-014
    /// existe para evitar.
    /// </remarks>
    public void ChangeFolder(string? folderPath, DateTime occurredAt)
    {
        SetFolderPath(folderPath);
        SyncCursor = null;
        UpdatedAt = occurredAt;
    }

    /// <summary>Liga e desliga a captura. É o botão de parada do usuário sobre esta fonte.</summary>
    public void SetEnabled(bool enabled, DateTime occurredAt)
    {
        IsEnabled = enabled;
        UpdatedAt = occurredAt;
    }

    /// <summary>
    /// Porta de entrada da sincronização: recusa a fonte desativada e devolve o cursor de onde
    /// retomar. <c>null</c> significa varredura completa.
    /// </summary>
    /// <remarks>
    /// Devolver o cursor daqui — em vez de deixar quem chama ler <see cref="SyncCursor"/> — é o
    /// que impede a orquestração de sincronizar uma fonte desligada por esquecer a checagem.
    /// </remarks>
    public string? BeginSync()
        => IsEnabled ? SyncCursor : throw CaptureSourceErrors.CannotSyncDisabled(Id.Value);

    /// <summary>Registra a sincronização bem-sucedida e avança o cursor.</summary>
    public void RecordSyncSuccess(string? cursor, DateTime occurredAt)
    {
        var trimmed = cursor?.Trim();
        if (trimmed is { Length: > SYNC_CURSOR_MAX_LENGTH })
            throw CaptureSourceErrors.SyncCursorTooLong(SYNC_CURSOR_MAX_LENGTH);

        SyncCursor = string.IsNullOrEmpty(trimmed) ? null : trimmed;
        LastSyncAt = occurredAt;
        LastSyncError = null;
        UpdatedAt = occurredAt;
    }

    /// <summary>
    /// Registra a falha da tentativa. <strong>Não toca no cursor</strong> — avançá-lo pularia
    /// mensagens que ninguém leu, e apagá-lo transformaria uma falha de rede em varredura
    /// completa da caixa.
    /// </summary>
    public void RecordSyncFailure(string error, DateTime occurredAt)
    {
        var trimmed = error?.Trim();

        // Mensagem de provedor não é entrada de usuário: truncar preserva o diagnóstico, enquanto
        // recusar perderia o registro da falha justamente quando ele é a única pista.
        LastSyncError = string.IsNullOrEmpty(trimmed)
            ? "unknown_error"
            : trimmed[..Math.Min(trimmed.Length, SYNC_ERROR_MAX_LENGTH)];

        LastSyncAt = occurredAt;
        UpdatedAt = occurredAt;
    }

    /// <summary>
    /// Descarta o cursor para que a próxima sincronização varra a caixa inteira. É a resposta ao
    /// <c>410 Gone</c> do Graph, que invalida o <c>deltaLink</c> quando ele fica velho demais.
    /// </summary>
    /// <remarks>
    /// Sem isto, um cursor expirado faria a fonte parar de sincronizar em silêncio — a pior
    /// falha possível num sistema cuja rede de segurança contra "a conta não chegou" ainda está
    /// por vir (ADR-014).
    /// </remarks>
    public void ResetCursor(DateTime occurredAt)
    {
        SyncCursor = null;
        UpdatedAt = occurredAt;
    }

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

    private void SetFolderPath(string? folderPath)
    {
        var trimmed = folderPath?.Trim().Trim('/');

        if (string.IsNullOrEmpty(trimmed))
        {
            FolderPath = null;
            return;
        }

        if (trimmed.Length > FOLDER_PATH_MAX_LENGTH)
            throw CaptureSourceErrors.FolderPathTooLong(FOLDER_PATH_MAX_LENGTH);

        FolderPath = trimmed;
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
