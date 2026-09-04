import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:rufino_core/rufino_core.dart';

/// The gate every call to a flat-domain-error backend (TenantManagement,
/// BillPayment) passes through.
///
/// Two things must never regress here: a 401 has to log the session out while
/// a 403 must not, and a body that is not a domain error must still produce an
/// [HttpException] instead of a parse crash.
void main() {
  /// A response with [body] and [status], optionally tagged with the request
  /// it answers so the exception can carry the method and URL.
  http.Response response(
    String body,
    int status, {
    String? reasonPhrase,
    http.BaseRequest? request,
  }) {
    return http.Response(
      body,
      status,
      reasonPhrase: reasonPhrase,
      request: request,
    );
  }

  /// Runs [checkApiStatus] and returns the [HttpException] it threw.
  HttpException captureHttpException(http.Response value) {
    try {
      checkApiStatus(value);
    } on HttpException catch (e) {
      return e;
    }
    fail('expected checkApiStatus to throw an HttpException');
  }

  group('checkApiStatus on a successful response', () {
    test('returns without throwing for every 2xx status', () {
      for (final status in [200, 201, 202, 204, 299]) {
        expect(
          () => checkApiStatus(response('', status)),
          returnsNormally,
          reason: 'status $status should be accepted',
        );
      }
    });

    test('treats 300 as a failure, since only 2xx is success', () {
      expect(
        () => checkApiStatus(response('', 300)),
        throwsA(isA<HttpException>()),
      );
    });
  });

  group('checkApiStatus on an authentication or authorization failure', () {
    test('turns 401 into a session expiry so the app can log the user out',
        () {
      expect(
        () => checkApiStatus(response('', 401)),
        throwsA(isA<SessionExpiredException>()),
      );
    });

    test('turns 403 into access denied, which must never log the user out',
        () {
      expect(
        () => checkApiStatus(response('', 403)),
        throwsA(isA<AccessDeniedException>()),
      );
    });

    test('ignores the body of a 401 or 403 and never raises HttpException',
        () {
      final body = jsonEncode({'id': 'X.Y1', 'message': 'nao autorizado'});

      expect(
        () => checkApiStatus(response(body, 401)),
        throwsA(isA<SessionExpiredException>()),
      );
      expect(
        () => checkApiStatus(response(body, 403)),
        throwsA(isA<AccessDeniedException>()),
      );
    });
  });

  group('checkApiStatus on a domain error body', () {
    test('carries the server message and the domain code to the caller', () {
      final exception = captureHttpException(
        response(
          jsonEncode({
            'id': 'TNM.TNT20',
            'message': 'O ultimo responsavel nao pode perder o acesso.',
          }),
          400,
          reasonPhrase: 'Bad Request',
        ),
      );

      expect(exception.statusCode, 400);
      expect(exception.domainErrorId, 'TNM.TNT20');
      expect(exception.serverMessages,
          ['O ultimo responsavel nao pode perder o acesso.']);
      expect(exception.message, 'HTTP 400: Bad Request');
    });

    test('carries the message even when the backend sent no domain code', () {
      final exception = captureHttpException(
        response(jsonEncode({'message': 'Valor invalido.'}), 422),
      );

      expect(exception.serverMessages, ['Valor invalido.']);
      expect(exception.domainErrorId, isNull);
    });

    test('carries the domain code even when the backend sent no message', () {
      final exception = captureHttpException(
        response(jsonEncode({'id': 'BIL.BIL35'}), 409),
      );

      expect(exception.domainErrorId, 'BIL.BIL35');
      expect(exception.serverMessages, isEmpty);
    });

    test('treats empty strings as an absent code and an absent message', () {
      final exception = captureHttpException(
        response(jsonEncode({'id': '', 'message': ''}), 400),
      );

      expect(exception.domainErrorId, isNull);
      expect(exception.serverMessages, isEmpty);
    });

    test('keeps the raw body so the error reporter can attach it', () {
      const body = '{"id":"BIL.BIL35","message":"Conta ja aprovada."}';

      final exception = captureHttpException(response(body, 409));

      expect(exception.responseBody, body);
    });

    test('records the method and URL of the request that failed', () {
      final request =
          http.Request('POST', Uri.parse('https://api.test/v1/bills'));

      final exception = captureHttpException(
        response('{}', 400, request: request),
      );

      expect(exception.requestMethod, 'POST');
      expect(exception.requestUrl, 'https://api.test/v1/bills');
    });

    test('leaves method and URL null when the response has no request', () {
      final exception = captureHttpException(response('{}', 400));

      expect(exception.requestMethod, isNull);
      expect(exception.requestUrl, isNull);
    });
  });

  group('checkApiStatus on a body that is not a domain error', () {
    test('survives an empty body', () {
      final exception = captureHttpException(response('', 500));

      expect(exception.statusCode, 500);
      expect(exception.domainErrorId, isNull);
      expect(exception.serverMessages, isEmpty);
    });

    test('survives an HTML error page from a gateway', () {
      final exception = captureHttpException(
        response('<html><body>gateway timeout</body></html>', 504),
      );

      expect(exception.statusCode, 504);
      expect(exception.serverMessages, isEmpty);
    });

    test('survives a truncated or malformed JSON body', () {
      final exception = captureHttpException(response('{"id": "BIL.', 400));

      expect(exception.domainErrorId, isNull);
      expect(exception.serverMessages, isEmpty);
    });

    test('survives a JSON body that is a list instead of an object', () {
      final exception =
          captureHttpException(response('[{"message":"nope"}]', 400));

      expect(exception.serverMessages, isEmpty);
    });

    test('survives a JSON body whose message is not a string', () {
      final exception = captureHttpException(
        response(jsonEncode({'id': 'X.Y1', 'message': 42}), 400),
      );

      expect(exception.serverMessages, isEmpty);
      expect(exception.domainErrorId, isNull);
    });
  });

  group('checkApiStatus on a rate-limited response', () {
    test('supplies a readable message for a 429 that carries no body', () {
      final exception = captureHttpException(response('', 429));

      expect(exception.statusCode, 429);
      expect(exception.serverMessages, hasLength(1));
      expect(exception.serverMessages.single, contains('Aguarde'));
    });

    test('prefers the backend message over the fallback when 429 carries one',
        () {
      final exception = captureHttpException(
        response(jsonEncode({'message': 'Cota diaria esgotada.'}), 429),
      );

      expect(exception.serverMessages, ['Cota diaria esgotada.']);
    });

    test('does not supply the rate-limit fallback to other statuses', () {
      final exception = captureHttpException(response('', 503));

      expect(exception.serverMessages, isEmpty);
    });
  });
}
