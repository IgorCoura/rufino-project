import 'dart:convert';
import 'dart:developer' as developer;

import 'package:flutter/foundation.dart';

import 'domain_error_logger_writer_stub.dart'
    if (dart.library.io) 'domain_error_logger_writer.dart';

/// Logs domain errors returned by the API to a JSON file in debug mode only.
///
/// Each entry includes the timestamp, HTTP method, endpoint, status code,
/// and the full list of errors with code, message, origin, and properties.
///
/// This logger exists because domain errors from the server should be
/// exceptional — the frontend should validate all data before submitting.
/// The JSON log file helps identify missing frontend validations.
abstract final class DomainErrorLogger {
  /// Appends all domain errors found in [responseBody] to the log file.
  ///
  /// [method] and [url] identify the HTTP request that triggered the error.
  /// [statusCode] is the HTTP response status code.
  /// Does nothing in release builds or on platforms without file I/O.
  static void log({
    required String method,
    required Uri? url,
    required int statusCode,
    required String responseBody,
  }) {
    if (!kDebugMode) return;

    final errors = _parseDetailedErrors(responseBody);
    if (errors.isEmpty) return;

    final entry = {
      'timestamp': DateTime.now().toIso8601String(),
      'method': method,
      'url': url?.toString() ?? 'unknown',
      'statusCode': statusCode,
      'errors': errors,
    };

    _printToConsole(method, url, statusCode, errors);
    appendLogEntry(entry);
  }

  static void _printToConsole(
    String method,
    Uri? url,
    int statusCode,
    List<Map<String, dynamic>> errors,
  ) {
    final buffer = StringBuffer()
      ..writeln('╔══════════════════════════════════════════════════════')
      ..writeln('║ DOMAIN ERROR FROM SERVER')
      ..writeln('║ ${DateTime.now().toIso8601String()}')
      ..writeln('║ $method ${url ?? 'unknown'} → $statusCode')
      ..writeln('╠──────────────────────────────────────────────────────');

    for (final error in errors) {
      buffer
        ..writeln('║ Origin:     ${error['origin']}')
        ..writeln('║ Code:       ${error['code']}')
        ..writeln('║ Message:    ${error['message']}');

      final props = error['properties'];
      if (props is Map && props.isNotEmpty) {
        buffer.writeln('║ Properties: $props');
      }

      buffer.writeln('╠──────────────────────────────────────────────────────');
    }

    buffer.write('╚══════════════════════════════════════════════════════');

    developer.log(buffer.toString(), name: 'DomainError', level: 900);
  }

  static List<Map<String, dynamic>> _parseDetailedErrors(String body) {
    try {
      final json = jsonDecode(body) as Map<String, dynamic>;
      final errors = json['errors'] as Map<String, dynamic>?;
      if (errors == null) return const [];

      final result = <Map<String, dynamic>>[];
      for (final entry in errors.entries) {
        final origin = entry.key;
        final list = entry.value;
        if (list is! List<dynamic>) continue;
        for (final item in list) {
          if (item is Map<String, dynamic>) {
            result.add({
              'origin': origin,
              'code': item['code'] ?? '',
              'message': item['message'] ?? '',
              'properties': item['properties'] ?? {},
            });
          }
        }
      }
      return result;
    } catch (_) {
      return const [];
    }
  }
}
