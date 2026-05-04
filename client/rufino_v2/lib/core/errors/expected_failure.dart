/// Marker mixin for exceptions that represent expected, user-actionable
/// failures rather than bugs.
///
/// An exception annotated with [ExpectedFailure] (e.g. invalid credentials,
/// duplicate entity, validation error) is filtered out by
/// [ErrorReporter.capture] so it does not pollute crash dashboards. The UI
/// still receives the error and presents a localized message to the user.
///
/// Apply per-case to the concrete exception class — never to a base class —
/// so that intent stays colocated with the exception:
///
/// ```dart
/// final class InvalidCredentialsException extends AuthException
///     with ExpectedFailure {
///   const InvalidCredentialsException();
/// }
/// ```
mixin ExpectedFailure {}
