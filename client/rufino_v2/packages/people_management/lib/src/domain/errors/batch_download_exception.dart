/// Base sealed class for all batch-download-domain errors.
///
/// Subtypes are used as typed payloads inside [Result.error] — they are
/// never thrown across layer boundaries.
sealed class BatchDownloadException implements Exception {
  const BatchDownloadException();
}

/// Thrown when a network or HTTP error occurs while accessing the batch download API.
final class BatchDownloadNetworkException extends BatchDownloadException {
  const BatchDownloadNetworkException(this.cause);

  /// The underlying error that triggered this exception.
  final Object cause;
}
