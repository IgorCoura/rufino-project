import 'package:rufino_core/rufino_core.dart';

/// Base class for failures of the bill payment module.
sealed class BillPaymentException implements Exception {
  /// Const constructor for subclasses.
  const BillPaymentException();
}

/// A business rule refused the operation.
///
/// Carries the backend's own message — written in Portuguese, by the domain —
/// and its code (`BLP.BIL02` and friends). It is an [ExpectedFailure]: a rule
/// saying no is the system working, not a bug to report.
final class BillPaymentRuleException extends BillPaymentException
    with ExpectedFailure {
  /// Creates the exception with the server's [message] and optional [code].
  const BillPaymentRuleException(this.message, {this.code});

  /// The message the backend produced, ready to show.
  final String message;

  /// The domain error code, when the response carried one.
  final String? code;

  @override
  String toString() => 'BillPaymentRuleException(${code ?? '-'}): $message';
}

/// The bill payment service could not be reached, or answered something
/// unexpected.
final class BillPaymentNetworkException extends BillPaymentException {
  /// Creates the exception wrapping the underlying [cause].
  const BillPaymentNetworkException(this.cause);

  /// The underlying error.
  final Object cause;

  @override
  String toString() => 'BillPaymentNetworkException: $cause';
}

/// Returns the message to show for [error], in Portuguese.
///
/// Prefers what the server said, because the domain writes better messages
/// about its own rules than any string this module could guess. [fallback] is
/// used when there is nothing to show.
String billPaymentErrorMessage(Object? error, {required String fallback}) {
  return switch (error) {
    BillPaymentRuleException(:final message) => message,
    AccessDeniedException() => 'Você não tem permissão para esta ação.',
    SessionExpiredException() => 'Sua sessão expirou. Entre novamente.',
    HttpException(:final serverMessages) when serverMessages.isNotEmpty =>
      serverMessages.first,
    BillPaymentNetworkException(:final cause) => switch (cause) {
        AccessDeniedException() => 'Você não tem permissão para esta ação.',
        SessionExpiredException() => 'Sua sessão expirou. Entre novamente.',
        HttpException(:final serverMessages) when serverMessages.isNotEmpty =>
          serverMessages.first,
        _ => fallback,
      },
    _ => fallback,
  };
}
