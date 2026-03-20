/// Base sealed class for all workplace-domain errors.
///
/// Subtypes are used as typed payloads inside [Result.error] — they are
/// never thrown across layer boundaries.
sealed class WorkplaceException implements Exception {
  const WorkplaceException();
}

/// Thrown when a workplace with the given [id] cannot be found.
final class WorkplaceNotFoundException extends WorkplaceException {
  const WorkplaceNotFoundException(this.id);

  final String id;
}

/// Thrown when a network or HTTP error occurs while accessing the workplace API.
final class WorkplaceNetworkException extends WorkplaceException {
  const WorkplaceNetworkException(this.cause);

  final Object cause;
}
