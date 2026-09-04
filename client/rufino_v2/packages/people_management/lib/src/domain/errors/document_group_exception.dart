/// Base sealed class for all document-group-domain errors.
///
/// Subtypes are used as typed payloads inside [Result.error] — they are
/// never thrown across layer boundaries.
sealed class DocumentGroupException implements Exception {
  const DocumentGroupException();
}

/// The requested document group could not be found.
final class DocumentGroupNotFoundException extends DocumentGroupException {
  const DocumentGroupNotFoundException(this.id);

  /// The id that was not found.
  final String id;
}

/// A network or HTTP error occurred while accessing the document group API.
final class DocumentGroupNetworkException extends DocumentGroupException {
  const DocumentGroupNetworkException(this.cause);

  /// The underlying error.
  final Object cause;
}
