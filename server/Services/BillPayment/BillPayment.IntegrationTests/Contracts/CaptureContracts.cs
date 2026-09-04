namespace BillPayment.IntegrationTests.Contracts;

// DTOs DUPLICADOS de propósito: reusar os da Application faria uma renomeação de campo passar
// despercebida, porque teste e aplicação mudariam juntos. A cópia é o que faz o teste falhar
// quando o contrato quebra para quem consome a API.

public sealed record ConnectCaptureSourceRequest(
    string Kind,
    string DisplayName,
    string Address,
    string Credential,
    string? FolderPath = null,
    DateOnly? CaptureSince = null);

/// <param name="CaptureSince">Nulo devolve a fonte à caixa inteira.</param>
public sealed record ChangeCaptureSourceSinceRequest(DateOnly? CaptureSince);

public sealed record ChangeCaptureSourceSinceResponseDto(Guid Id);

/// <summary>A fonte como a API a devolve. Só os campos que os testes de piso temporal afirmam.</summary>
public sealed record CaptureSourceResponseDto(
    Guid Id,
    string DisplayName,
    string Address,
    IReadOnlyList<MonitoredFolderResponseDto> Folders,
    bool HasCredential,
    bool IsEnabled,
    DateOnly? CaptureSince,
    DateTime? LastSyncAt,
    string? LastSyncError,
    DateTime CreatedAt);

public sealed record MonitoredFolderResponseDto(
    Guid Id,
    string? Path,
    bool HasSyncCursor,
    DateTime? LastSyncAt,
    string? LastSyncError);

public sealed record ConnectCaptureSourceResponseDto(Guid Id);

public sealed record SyncCaptureSourceResponseDto(
    Guid Id,
    string Status,
    int IngestedItems,
    int SkippedAsAlreadyIngested);

public sealed record CaptureItemResponseDto(
    Guid Id,
    Guid SourceId,
    string Sender,
    string? Subject,
    DateTime ReceivedAt,
    string Status,
    string? Reason,
    string? RoutingConfidence,
    string? ExtractionMethod,
    string? UnlockedBy,
    bool HasArtifact,
    string? SourceUrl,
    string? ContentHash,
    Guid? BillId,
    Guid? ClaimedBy,
    DateTime? ClaimedAt);

public sealed record CaptureItemPageDto(IReadOnlyList<CaptureItemResponseDto> Items, string? NextCursor);

/// <param name="FoldersReset">Quantas pastas voltarão a ser lidas por inteiro.</param>
public sealed record RescanCaptureSourceResponseDto(Guid Id, int FoldersReset);
