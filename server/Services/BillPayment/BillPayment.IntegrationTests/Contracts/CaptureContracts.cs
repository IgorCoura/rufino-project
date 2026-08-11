namespace BillPayment.IntegrationTests.Contracts;

// DTOs DUPLICADOS de propósito: reusar os da Application faria uma renomeação de campo passar
// despercebida, porque teste e aplicação mudariam juntos. A cópia é o que faz o teste falhar
// quando o contrato quebra para quem consome a API.

public sealed record ConnectCaptureSourceRequest(
    string Kind,
    string DisplayName,
    string Address,
    string Credential);

public sealed record ConnectCaptureSourceResponseDto(Guid Id, bool AlreadyMonitoredByAnotherAccount);

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
    string? StorageKey,
    string? SourceUrl,
    string? ContentHash,
    Guid? BillId,
    Guid? ClaimedBy,
    DateTime? ClaimedAt);

public sealed record CaptureItemPageDto(IReadOnlyList<CaptureItemResponseDto> Items, string? NextCursor);
