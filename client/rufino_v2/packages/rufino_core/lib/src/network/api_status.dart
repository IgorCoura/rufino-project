import 'dart:convert';

import 'package:http/http.dart' as http;

import '../auth/auth_exception.dart';
import '../http_exception.dart';

/// Throws a typed exception if [response] has a non-2xx status code.
///
/// This is the checker for the services whose backend answers a **flat**
/// domain error — `{"id": "TNM.TNT20", "message": "..."}` — which is what
/// `TenantManagement` and `BillPayment` return. The older PeopleManagement
/// shape (`{"errors": {...}}`) has its own checker in the app; reusing that
/// one here would swallow every message and leave the user with a bare
/// "HTTP 400".
///
/// A **401** means the API no longer accepts the session's token and becomes
/// [SessionExpiredException]; a **403** means the session is valid but lacks
/// permission and becomes [AccessDeniedException] — which must never log the
/// user out. Every other failure status raises [HttpException] carrying the
/// server's message in [HttpException.serverMessages] and its domain code in
/// [HttpException.domainErrorId].
void checkApiStatus(http.Response response) {
  if (response.statusCode >= 200 && response.statusCode < 300) return;

  if (response.statusCode == 401) throw const SessionExpiredException();
  if (response.statusCode == 403) throw const AccessDeniedException();

  final (id, message) = _parseDomainError(response.body);

  throw HttpException(
    statusCode: response.statusCode,
    message: 'HTTP ${response.statusCode}: ${response.reasonPhrase}',
    serverMessages: message == null ? const [] : [message],
    domainErrorId: id,
    responseBody: response.body,
    requestMethod: response.request?.method,
    requestUrl: response.request?.url.toString(),
  );
}

/// Extracts `(id, message)` from a flat domain-error body.
///
/// Returns `(null, null)` when the body is not JSON or does not carry the
/// expected keys — a gateway timeout page is not a domain error.
(String?, String?) _parseDomainError(String body) {
  try {
    final json = jsonDecode(body);
    if (json is! Map<String, dynamic>) return (null, null);

    final message = json['message'] as String?;
    final id = json['id'] as String?;
    return (
      id != null && id.isNotEmpty ? id : null,
      message != null && message.isNotEmpty ? message : null,
    );
  } catch (_) {
    return (null, null);
  }
}
