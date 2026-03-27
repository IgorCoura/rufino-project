import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:rufino_v2/data/services/http_exception.dart';
import 'package:rufino_v2/data/services/http_status_helper.dart';

void main() {
  group('checkHttpStatus', () {
    test('does not throw for 200 status code', () {
      final response = http.Response('', 200);
      expect(() => checkHttpStatus(response), returnsNormally);
    });

    test('does not throw for 201 status code', () {
      final response = http.Response('', 201);
      expect(() => checkHttpStatus(response), returnsNormally);
    });

    test('throws HttpException for 400 status code', () {
      final response = http.Response('', 400);
      expect(() => checkHttpStatus(response), throwsA(isA<HttpException>()));
    });

    test('throws HttpException for 500 status code', () {
      final response = http.Response('', 500);
      expect(() => checkHttpStatus(response), throwsA(isA<HttpException>()));
    });

    test('parses single error message from API response body', () {
      final body = jsonEncode({
        'errors': {
          'Employee': [
            {
              'code': 'PMD10',
              'message': 'O campo Nome com o valor xyz é invalido.',
              'properties': {'NameField': 'Nome', 'Value': 'xyz'},
            },
          ],
        },
      });

      final response = http.Response(body, 400);

      try {
        checkHttpStatus(response);
        fail('Expected HttpException');
      } on HttpException catch (e) {
        expect(e.statusCode, 400);
        expect(e.serverMessages, hasLength(1));
        expect(
          e.serverMessages.first,
          'O campo Nome com o valor xyz é invalido.',
        );
      }
    });

    test('parses multiple error messages from multiple origins', () {
      final body = jsonEncode({
        'errors': {
          'Employee': [
            {
              'code': 'PMD10',
              'message': 'O campo Nome é invalido.',
              'properties': {},
            },
            {
              'code': 'PMD15',
              'message': 'O campo Email não pode ser nulo ou vazio.',
              'properties': {},
            },
          ],
          'Address': [
            {
              'code': 'PMD14',
              'message': 'O campo CEP não pode ser nulo.',
              'properties': {},
            },
          ],
        },
      });

      final response = http.Response(body, 400);

      try {
        checkHttpStatus(response);
        fail('Expected HttpException');
      } on HttpException catch (e) {
        expect(e.serverMessages, hasLength(3));
        expect(e.serverMessages[0], 'O campo Nome é invalido.');
        expect(
          e.serverMessages[1],
          'O campo Email não pode ser nulo ou vazio.',
        );
        expect(e.serverMessages[2], 'O campo CEP não pode ser nulo.');
      }
    });

    test('returns empty server messages for non-JSON body', () {
      final response = http.Response('Not Found', 404);

      try {
        checkHttpStatus(response);
        fail('Expected HttpException');
      } on HttpException catch (e) {
        expect(e.serverMessages, isEmpty);
      }
    });

    test('returns empty server messages when errors key is missing', () {
      final body = jsonEncode({'status': 'error'});
      final response = http.Response(body, 400);

      try {
        checkHttpStatus(response);
        fail('Expected HttpException');
      } on HttpException catch (e) {
        expect(e.serverMessages, isEmpty);
      }
    });

    test('returns empty server messages for malformed error objects', () {
      final body = jsonEncode({
        'errors': {
          'Employee': [
            {'code': 'PMD10'},
          ],
        },
      });

      final response = http.Response(body, 400);

      try {
        checkHttpStatus(response);
        fail('Expected HttpException');
      } on HttpException catch (e) {
        expect(e.serverMessages, isEmpty);
      }
    });

    test('returns empty server messages for empty body', () {
      final response = http.Response('', 400);

      try {
        checkHttpStatus(response);
        fail('Expected HttpException');
      } on HttpException catch (e) {
        expect(e.serverMessages, isEmpty);
      }
    });

    test('skips error items with empty message', () {
      final body = jsonEncode({
        'errors': {
          'Employee': [
            {'code': 'PMD10', 'message': ''},
            {'code': 'PMD11', 'message': 'Campo obrigatório.'},
          ],
        },
      });

      final response = http.Response(body, 400);

      try {
        checkHttpStatus(response);
        fail('Expected HttpException');
      } on HttpException catch (e) {
        expect(e.serverMessages, hasLength(1));
        expect(e.serverMessages.first, 'Campo obrigatório.');
      }
    });

    test('skips non-map items in the error list', () {
      final body = jsonEncode({
        'errors': {
          'Employee': ['just a string', 42],
        },
      });

      final response = http.Response(body, 400);

      try {
        checkHttpStatus(response);
        fail('Expected HttpException');
      } on HttpException catch (e) {
        expect(e.serverMessages, isEmpty);
      }
    });
  });
}
