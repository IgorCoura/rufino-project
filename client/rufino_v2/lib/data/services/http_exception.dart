/// Thrown by API services when the server returns a non-2xx HTTP status.
///
/// When the response body contains structured domain errors (the `errors` key),
/// [serverMessages] is populated with the human-readable `message` fields
/// from the API. Otherwise, [serverMessages] is empty and [message] contains
/// a generic status-based description.
class HttpException implements Exception {
  /// Creates an [HttpException] with the given [statusCode], [message],
  /// and optional [serverMessages] extracted from the response body.
  const HttpException({
    required this.statusCode,
    required this.message,
    this.serverMessages = const [],
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

  @override
  String toString() => 'HttpException($statusCode): $message';
}
