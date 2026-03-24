/// Base sealed class for all require-document-domain errors.
///
/// Subtypes are used as typed payloads inside [Result.error] — they are
/// never thrown across layer boundaries.
sealed class RequireDocumentException implements Exception {
  const RequireDocumentException();
}

/// Thrown when a require document with the given [id] cannot be found.
final class RequireDocumentNotFoundException extends RequireDocumentException {
  const RequireDocumentNotFoundException(this.id);

  final String id;
}

/// Thrown when a network or HTTP error occurs while accessing the require document API.
final class RequireDocumentNetworkException extends RequireDocumentException {
  const RequireDocumentNetworkException(this.cause);

  final Object cause;
}
