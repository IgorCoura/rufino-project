import 'package:rufino_core/rufino_core.dart';

/// Base class for failures of the tenant cadastro.
sealed class TenantException implements Exception {
  /// Const constructor for subclasses.
  const TenantException();
}

/// A business rule refused the operation.
///
/// Carries the backend's own message — written in Portuguese, by the domain —
/// and its code (`TNM.TNT20` and friends). It is an [ExpectedFailure]: a rule
/// saying no is the system working, not a bug to report.
final class TenantRuleException extends TenantException with ExpectedFailure {
  /// Creates the exception with the server's [message] and optional [code].
  const TenantRuleException(this.message, {this.code});

  /// The message the backend produced, ready to show.
  final String message;

  /// The domain error code, when the response carried one.
  final String? code;

  @override
  String toString() => 'TenantRuleException(${code ?? '-'}): $message';
}

/// The tenant service could not be reached, or answered something unexpected.
final class TenantNetworkException extends TenantException {
  /// Creates the exception wrapping the underlying [cause].
  const TenantNetworkException(this.cause);

  /// The underlying error.
  final Object cause;

  @override
  String toString() => 'TenantNetworkException: $cause';
}

/// Returns the message to show for [error], in Portuguese.
///
/// Prefers what the server said, because the domain writes better messages
/// about its own rules than any string this module could guess. [fallback] is
/// used when there is nothing to show.
String tenantErrorMessage(Object? error, {required String fallback}) {
  return switch (error) {
    TenantRuleException(:final message) => message,
    AccessDeniedException() => 'Você não tem permissão para esta ação.',
    SessionExpiredException() => 'Sua sessão expirou. Entre novamente.',
    HttpException(:final serverMessages) when serverMessages.isNotEmpty =>
      serverMessages.first,
    TenantNetworkException(:final cause) => switch (cause) {
        AccessDeniedException() => 'Você não tem permissão para esta ação.',
        SessionExpiredException() => 'Sua sessão expirou. Entre novamente.',
        HttpException(:final serverMessages) when serverMessages.isNotEmpty =>
          serverMessages.first,
        _ => fallback,
      },
    _ => fallback,
  };
}
