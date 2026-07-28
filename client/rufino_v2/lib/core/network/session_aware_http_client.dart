import 'package:http/http.dart' as http;

/// An [http.BaseClient] that invokes [onSessionInvalid] whenever a response
/// comes back 401.
///
/// Wrapping the app's single shared client makes this the one choke point
/// where an expired session is detected, no matter which service or
/// repository issued the request.
class SessionAwareHttpClient extends http.BaseClient {
  SessionAwareHttpClient(this._inner, {required this.onSessionInvalid});

  final http.Client _inner;

  /// Called once per 401 response, before the response reaches the caller.
  final void Function() onSessionInvalid;

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    final response = await _inner.send(request);
    if (response.statusCode == 401) onSessionInvalid();
    return response;
  }

  @override
  void close() => _inner.close();
}
