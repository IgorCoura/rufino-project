namespace BillPayment.Application.Mediator;

/// <summary>
/// Marker for a command whose payload must NEVER be destructured into a log entry — the ADR-009 rule
/// ("segredo nunca entra no repositório nem no log") expressed as a type the controller can check.
/// The BaseController still logs the command NAME, the route id and the request id; only the payload
/// is replaced by a redaction marker.
/// </summary>
/// <remarks>
/// Marking is opt-in and therefore easy to forget, which is deliberate: the alternative — an allowlist
/// of loggable commands — would silently stop logging anything a developer forgot to allow, and a log
/// that quietly goes missing is worse than one that quietly says too much is impossible to notice.
/// The guard against forgetting is the integration test that asserts a credential never reaches the
/// log, not the compiler. Add the marker to any command carrying a secret (vault credential, PDF
/// password) or a payment instrument (linha digitável, BR Code) — the latter because it is enough to
/// pay the bill, and the read API already refuses to expose it.
/// </remarks>
public interface ISensitiveCommand;
