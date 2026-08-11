namespace BillPayment.Domain.CaptureSources;

using BillPayment.Domain.SeedWork;

/// <summary>
/// Uma pasta da caixa que a fonte acompanha, com o cursor de onde a última varredura dela parou.
/// </summary>
/// <remarks>
/// <para>
/// <strong>O cursor é da pasta, não da fonte — e isso é imposto pelo provedor.</strong> A
/// varredura incremental do Graph é por pasta: um <c>deltaLink</c> obtido lendo a caixa de
/// entrada não significa nada dentro de "Contas". Guardar um cursor só na raiz obrigaria a
/// monitorar uma pasta por fonte, duplicando credencial e cadastro para uma caixa só.
/// </para>
/// <para>
/// <strong>O erro também é por pasta.</strong> Uma pasta renomeada ou apagada no cliente de
/// e-mail registra a própria falha e <em>não impede</em> as outras de sincronizar — mesma
/// disciplina que faz uma caixa fora do ar não travar a varredura das demais.
/// </para>
/// <para>
/// Entidade interna: só a <see cref="CaptureSource"/> cria e muta. Não emite Domain Event —
/// quem emite é a raiz, e nesta fase nem ela emite.
/// </para>
/// </remarks>
public sealed class MonitoredFolder : Entity<MonitoredFolderId>
{
    public const int PATH_MAX_LENGTH = 250;

    /// <summary>O <c>deltaLink</c> do Graph carrega token opaco e é bem maior que uma URL comum.</summary>
    public const int SYNC_CURSOR_MAX_LENGTH = 2000;

    public const int SYNC_ERROR_MAX_LENGTH = 500;

    /// <summary>
    /// Caminho da pasta, com <c>/</c> separando níveis. <strong>Nulo = a caixa de entrada.</strong>
    /// </summary>
    /// <remarks>
    /// Nulo em vez de <c>"Caixa de Entrada"</c> porque o nome da caixa de entrada muda com o
    /// idioma da conta; o adapter resolve o nome bem-conhecido do provedor sem consultar nada.
    /// </remarks>
    public string? Path { get; private set; }

    /// <summary>Onde a última varredura desta pasta parou. Nulo = a próxima é completa.</summary>
    public string? SyncCursor { get; private set; }

    /// <summary>Instante da última tentativa <em>concluída</em>, com ou sem êxito.</summary>
    public DateTime? LastSyncAt { get; private set; }

    /// <summary>Motivo da última falha. Nulo depois de uma varredura bem-sucedida.</summary>
    public string? LastSyncError { get; private set; }

    private MonitoredFolder() { }

    internal MonitoredFolder(string? path, DateTime occurredAt) : base(MonitoredFolderId.New())
    {
        SetPath(path);
        CreatedAt = occurredAt;
        UpdatedAt = occurredAt;
    }

    /// <summary>Forma canônica do caminho, usada para comparar e evitar pasta repetida.</summary>
    /// <remarks>
    /// Aparar barras e espaços aqui — e só aqui — é o que impede <c>"Contas"</c>,
    /// <c>"/Contas"</c> e <c>"Contas/"</c> de virarem três pastas monitoradas que são a mesma,
    /// cada uma gastando uma chamada por ciclo. A comparação é <strong>sem</strong> distinção de
    /// maiúsculas porque o provedor também não distingue nomes de pasta.
    /// </remarks>
    public static string? NormalizePath(string? path)
    {
        var trimmed = path?.Trim().Trim('/').Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    public bool HasPath(string? path)
        => string.Equals(Path, NormalizePath(path), StringComparison.OrdinalIgnoreCase);

    /// <summary>Registra a varredura bem-sucedida desta pasta e avança o cursor dela.</summary>
    internal void RecordSyncSuccess(string? cursor, DateTime occurredAt)
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
    /// completa da pasta.
    /// </summary>
    internal void RecordSyncFailure(string error, DateTime occurredAt)
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

    /// <summary>Descarta o cursor para que a próxima varredura desta pasta leia tudo de novo.</summary>
    internal void ResetCursor(DateTime occurredAt)
    {
        SyncCursor = null;
        UpdatedAt = occurredAt;
    }

    private void SetPath(string? path)
    {
        var normalized = NormalizePath(path);

        if (normalized is { Length: > PATH_MAX_LENGTH })
            throw CaptureSourceErrors.FolderPathTooLong(PATH_MAX_LENGTH);

        Path = normalized;
    }
}
