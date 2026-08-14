namespace TenantManagement.Application.Mediator;

/// <summary>
/// Marker for a command whose payload must NEVER be destructured into a log entry. The BaseController
/// still logs the command NAME, the route id and the request id; only the payload is replaced by a
/// redaction marker.
/// </summary>
/// <remarks>
/// Marking is opt-in and therefore easy to forget, which is deliberate: the alternative — an allowlist
/// of loggable commands — would silently stop logging anything a developer forgot to allow, and a log
/// that quietly goes missing is impossible to notice. No command carries a secret today; the marker
/// exists so that the one that eventually does has an obvious place to declare it.
/// </remarks>
public interface ISensitiveCommand;
