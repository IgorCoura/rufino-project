/// Base sealed class for all document-content-snapshot errors.
///
/// Subtypes are used as typed payloads inside [Result.error] — they are
/// never thrown across layer boundaries.
sealed class DocumentContentException implements Exception {
  const DocumentContentException();
}

/// Thrown when a network or HTTP error occurs while checking or refreshing
/// the snapshot stored in a document unit.
final class DocumentContentNetworkException extends DocumentContentException {
  const DocumentContentNetworkException(this.cause);

  /// The underlying error that triggered this exception.
  final Object cause;
}
