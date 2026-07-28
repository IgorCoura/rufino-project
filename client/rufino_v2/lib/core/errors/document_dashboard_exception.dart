/// Base sealed class for all document-dashboard-domain errors.
///
/// Subtypes are used as typed payloads inside [Result.error] — they are
/// never thrown across layer boundaries.
sealed class DocumentDashboardException implements Exception {
  const DocumentDashboardException();
}

/// Thrown when a network or HTTP error occurs while accessing the document
/// dashboard API.
final class DocumentDashboardNetworkException
    extends DocumentDashboardException {
  const DocumentDashboardNetworkException(this.cause);

  /// The underlying error that triggered this exception.
  final Object cause;
}
