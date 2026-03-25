/// Base sealed class for all document-template-domain errors.
///
/// Subtypes are used as typed payloads inside [Result.error] — they are
/// never thrown across layer boundaries.
sealed class DocumentTemplateException implements Exception {
  const DocumentTemplateException();
}

/// Thrown when a document template with the given [id] cannot be found.
final class DocumentTemplateNotFoundException
    extends DocumentTemplateException {
  const DocumentTemplateNotFoundException(this.id);

  final String id;
}

/// Thrown when a network or HTTP error occurs while accessing the document template API.
final class DocumentTemplateNetworkException
    extends DocumentTemplateException {
  const DocumentTemplateNetworkException(this.cause);

  final Object cause;
}
