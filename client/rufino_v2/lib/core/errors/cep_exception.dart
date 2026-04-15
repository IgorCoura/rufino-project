/// Base class for CEP (Brazilian postal code) lookup exceptions.
sealed class CepException implements Exception {
  const CepException();
}

/// Thrown when the ViaCEP service reports that the requested CEP does not
/// exist (response body contains `{"erro": true}`).
final class CepNotFoundException extends CepException {
  const CepNotFoundException();
}

/// Thrown when the CEP lookup request fails for any reason other than
/// "not found" — non-2xx status code, network failure, malformed response.
final class CepLookupException extends CepException {
  const CepLookupException(this.cause);

  /// The underlying error that caused the failure.
  final Object cause;
}
