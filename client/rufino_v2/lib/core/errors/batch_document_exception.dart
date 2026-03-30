/// Base sealed class for all batch-document-domain errors.
///
/// Subtypes are used as typed payloads inside [Result.error] — they are
/// never thrown across layer boundaries.
sealed class BatchDocumentException implements Exception {
  const BatchDocumentException();
}

/// Thrown when a network or HTTP error occurs while accessing the batch document API.
final class BatchDocumentNetworkException extends BatchDocumentException {
  const BatchDocumentNetworkException(this.cause);

  /// The underlying error that triggered this exception.
  final Object cause;
}
