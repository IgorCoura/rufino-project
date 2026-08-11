namespace BillPayment.Application.Queries.CaptureSources;

/// <summary>
/// A fonte como o dono dela a vê.
/// </summary>
/// <remarks>
/// <strong>Não há campo de credencial aqui, e não é esquecimento.</strong> Nem o segredo nem o
/// ponteiro do cofre saem por API (ADR-009): o ponteiro é endereço de uma linha cifrada, e
/// expô-lo entregaria metade do caminho de graça. O que o usuário precisa saber é se a fonte
/// <em>tem</em> credencial — <see cref="HasCredential"/> — e se a última sincronização falhou.
/// </remarks>
public sealed record CaptureSourceDto(
    Guid Id,
    string Kind,
    string DisplayName,
    string Address,

    /// <summary>
    /// As pastas acompanhadas, sempre ao menos uma. Cada uma traz o próprio estado de varredura.
    /// </summary>
    IReadOnlyList<MonitoredFolderDto> Folders,

    bool HasCredential,
    bool IsEnabled,

    /// <summary>Última tentativa concluída em qualquer pasta.</summary>
    DateTime? LastSyncAt,

    /// <summary>
    /// Resumo: o motivo da falha de alguma pasta, ou nulo quando todas foram bem. O diagnóstico
    /// por pasta está em <see cref="Folders"/> — colapsar os dois esconderia uma pasta quebrada
    /// enquanto as outras rodam.
    /// </summary>
    string? LastSyncError,

    DateTime CreatedAt);

/// <summary>
/// Uma pasta acompanhada, do ponto de vista de quem opera.
/// </summary>
/// <remarks>
/// <strong>O cursor não sai daqui</strong> — só a informação de que existe. É um
/// <c>deltaLink</c> do provedor, com token opaco dentro: devolvê-lo por API entregaria um
/// ponteiro de leitura da caixa a quem só precisa saber se a varredura já rodou.
/// </remarks>
public sealed record MonitoredFolderDto(
    Guid Id,

    /// <summary>Caminho da pasta; nulo = a caixa de entrada.</summary>
    string? Path,

    bool HasSyncCursor,
    DateTime? LastSyncAt,
    string? LastSyncError);

public sealed record CaptureSourcePage(IReadOnlyList<CaptureSourceDto> Items, string? NextCursor);
