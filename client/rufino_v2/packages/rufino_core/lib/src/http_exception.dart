/// Thrown by API services when the server returns a non-2xx HTTP status.
///
/// Carries the raw [responseBody] (along with [requestMethod] and
/// [requestUrl]) so the error reporter can attach the failed response to
/// the captured event. The body is stored as-is — truncation and PII
/// scrubbing happen at the reporter boundary, not here.
///
/// When the response body contains structured domain errors (the `errors`
/// key), [serverMessages] is populated with the human-readable `message`
/// fields from the API.
class HttpException implements Exception {
  /// Creates an [HttpException] with the given [statusCode], [message],
  /// and optional [serverMessages], [responseBody], [requestMethod] and
  /// [requestUrl].
  const HttpException({
    required this.statusCode,
    required this.message,
    this.serverMessages = const [],
    this.responseBody,
    this.requestMethod,
    this.requestUrl,
  });

  /// The HTTP status code returned by the server.
  final int statusCode;

  /// A generic description such as `"HTTP 400: Bad Request"`.
  final String message;

  /// Human-readable error messages extracted from the API response body.
  ///
  /// Empty when the response body does not contain the expected error
  /// structure.
  final List<String> serverMessages;

  /// The raw response body returned by the server, when available.
  ///
  /// Stored verbatim; the error reporter is responsible for scrubbing PII
  /// and truncating before forwarding to the monitoring backend.
  final String? responseBody;

  /// The HTTP method (`GET`, `POST`, ...) of the failed request.
  final String? requestMethod;

  /// The absolute URL of the failed request.
  final String? requestUrl;

  @override
  String toString() => 'HttpException($statusCode): $message';
}
