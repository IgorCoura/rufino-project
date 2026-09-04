import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:rufino_core/rufino_core.dart';

void main() {
  group('checkApiStatus', () {
    test('accepts any 2xx response without throwing', () {
      expect(() => checkApiStatus(http.Response('{}', 200)), returnsNormally);
      expect(() => checkApiStatus(http.Response('', 204)), returnsNormally);
    });

    test('turns 401 into a session expiry, never a permission problem', () {
      expect(
        () => checkApiStatus(http.Response('', 401)),
        throwsA(isA<SessionExpiredException>()),
      );
    });

    test('turns 403 into access denied so the user is not logged out', () {
      expect(
        () => checkApiStatus(http.Response('', 403)),
        throwsA(isA<AccessDeniedException>()),
      );
    });

    test('carries the flat domain error message and code to the caller', () {
      final response = http.Response(
        jsonEncode({
          'id': 'TNM.TNT20',
          'message': 'O último responsável não pode perder o acesso.',
        }),
        400,
      );

      try {
        checkApiStatus(response);
        fail('expected an HttpException');
      } on HttpException catch (e) {
        expect(e.statusCode, 400);
        expect(e.domainErrorId, 'TNM.TNT20');
        expect(
          e.serverMessages,
          ['O último responsável não pode perder o acesso.'],
        );
      }
    });

    test('survives a body that is not a domain error at all', () {
      try {
        checkApiStatus(http.Response('<html>gateway timeout</html>', 504));
        fail('expected an HttpException');
      } on HttpException catch (e) {
        expect(e.statusCode, 504);
        expect(e.domainErrorId, isNull);
        expect(e.serverMessages, isEmpty);
      }
    });
  });
}
