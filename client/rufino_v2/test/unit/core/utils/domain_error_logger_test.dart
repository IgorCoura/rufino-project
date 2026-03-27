import 'dart:convert';
import 'dart:io';

import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/core/utils/domain_error_logger.dart';

const _logFile = 'domain_errors.json';

void main() {
  group('DomainErrorLogger', () {
    setUp(() {
      // Clean up before each test.
      final file = File(_logFile);
      if (file.existsSync()) file.deleteSync();
    });

    tearDown(() {
      final file = File(_logFile);
      if (file.existsSync()) file.deleteSync();
    });

    test('creates log file with single entry for a domain error', () {
      final body = jsonEncode({
        'errors': {
          'CPF': [
            {
              'code': 'PMD15',
              'message': 'O campo CPF, não pode ser nulo ou vazio.',
              'properties': {'NameField': 'CPF'},
            },
          ],
        },
      });

      DomainErrorLogger.log(
        method: 'PUT',
        url: Uri.parse('https://api.example.com/api/v1/employee'),
        statusCode: 400,
        responseBody: body,
      );

      final file = File(_logFile);
      expect(file.existsSync(), isTrue);

      final entries = jsonDecode(file.readAsStringSync()) as List<dynamic>;
      expect(entries, hasLength(1));

      final entry = entries.first as Map<String, dynamic>;
      expect(entry['method'], 'PUT');
      expect(entry['statusCode'], 400);
      expect(entry['url'], contains('employee'));
      expect(entry['timestamp'], isNotEmpty);

      final errors = entry['errors'] as List<dynamic>;
      expect(errors, hasLength(1));
      expect(errors.first['code'], 'PMD15');
      expect(errors.first['origin'], 'CPF');
      expect(errors.first['message'], contains('CPF'));
      expect(errors.first['properties']['NameField'], 'CPF');
    });

    test('appends multiple entries to the same file', () {
      final body = jsonEncode({
        'errors': {
          'Nome': [
            {'code': 'PMD10', 'message': 'Campo inválido.', 'properties': {}},
          ],
        },
      });

      DomainErrorLogger.log(
        method: 'POST',
        url: Uri.parse('https://api.example.com/api/v1/company'),
        statusCode: 400,
        responseBody: body,
      );

      DomainErrorLogger.log(
        method: 'PUT',
        url: Uri.parse('https://api.example.com/api/v1/company'),
        statusCode: 400,
        responseBody: body,
      );

      final entries =
          jsonDecode(File(_logFile).readAsStringSync()) as List<dynamic>;
      expect(entries, hasLength(2));
      expect(entries[0]['method'], 'POST');
      expect(entries[1]['method'], 'PUT');
    });

    test('does not create file when body has no domain errors', () {
      DomainErrorLogger.log(
        method: 'GET',
        url: Uri.parse('https://api.example.com/test'),
        statusCode: 404,
        responseBody: 'Not Found',
      );

      expect(File(_logFile).existsSync(), isFalse);
    });

    test('does not create file when errors key is missing', () {
      final body = jsonEncode({'status': 'error'});

      DomainErrorLogger.log(
        method: 'POST',
        url: null,
        statusCode: 400,
        responseBody: body,
      );

      expect(File(_logFile).existsSync(), isFalse);
    });

    test('handles multiple errors from multiple origins in one entry', () {
      final body = jsonEncode({
        'errors': {
          'Employee': [
            {'code': 'PMD10', 'message': 'Campo inválido.', 'properties': {}},
          ],
          'Address': [
            {'code': 'PMD14', 'message': 'Campo nulo.', 'properties': {}},
          ],
        },
      });

      DomainErrorLogger.log(
        method: 'PUT',
        url: Uri.parse('https://api.example.com/api/v1/employee'),
        statusCode: 400,
        responseBody: body,
      );

      final entries =
          jsonDecode(File(_logFile).readAsStringSync()) as List<dynamic>;
      expect(entries, hasLength(1));

      final errors = entries.first['errors'] as List<dynamic>;
      expect(errors, hasLength(2));
      expect(errors[0]['origin'], 'Employee');
      expect(errors[1]['origin'], 'Address');
    });
  });
}
