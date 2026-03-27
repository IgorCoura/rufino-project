import 'dart:convert';

import 'package:http/http.dart' as http;

import '../../core/utils/domain_error_logger.dart';
import 'http_exception.dart';

/// Throws [HttpException] if [response] has a non-2xx status code.
///
/// When the response body contains an `errors` map matching the API's
/// domain-error structure, the error messages are extracted and included
/// in [HttpException.serverMessages]. In debug mode, domain errors are
/// also logged via [DomainErrorLogger] for later analysis.
void checkHttpStatus(http.Response response) {
  if (response.statusCode >= 200 && response.statusCode < 300) return;

  final serverMessages = _parseErrorMessages(response.body);

  DomainErrorLogger.log(
    method: response.request?.method ?? 'UNKNOWN',
    url: response.request?.url,
    statusCode: response.statusCode,
    responseBody: response.body,
  );

  throw HttpException(
    statusCode: response.statusCode,
    message: 'HTTP ${response.statusCode}: ${response.reasonPhrase}',
    serverMessages: serverMessages,
  );
}

/// Parses the API error response body and extracts all `message` fields.
///
/// Returns an empty list if the body is not valid JSON or does not match the
/// expected `{ "errors": { "Origin": [ { "message": "..." } ] } }` structure.
List<String> _parseErrorMessages(String body) {
  try {
    final json = jsonDecode(body) as Map<String, dynamic>;
    final errors = json['errors'] as Map<String, dynamic>?;
    if (errors == null) return const [];

    final messages = <String>[];
    for (final entry in errors.values) {
      if (entry is! List<dynamic>) continue;
      for (final item in entry) {
        if (item is Map<String, dynamic>) {
          final message = item['message'] as String?;
          if (message != null && message.isNotEmpty) {
            messages.add(message);
          }
        }
      }
    }
    return messages;
  } catch (_) {
    return const [];
  }
}
