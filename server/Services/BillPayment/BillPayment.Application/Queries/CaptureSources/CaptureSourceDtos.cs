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

    /// <summary>Pasta monitorada; nulo = a caixa de entrada inteira.</summary>
    string? FolderPath,

    bool HasCredential,
    bool IsEnabled,
    DateTime? LastSyncAt,
    string? LastSyncError,
    bool HasSyncCursor,
    DateTime CreatedAt);

public sealed record CaptureSourcePage(IReadOnlyList<CaptureSourceDto> Items, string? NextCursor);
