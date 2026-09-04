import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_core/rufino_core.dart';

void main() {
  group('HttpException', () {
    test('carries no server messages and no request context by default', () {
      const exception = HttpException(statusCode: 500, message: 'HTTP 500');

      expect(exception.serverMessages, isEmpty);
      expect(exception.domainErrorId, isNull);
      expect(exception.responseBody, isNull);
      expect(exception.requestMethod, isNull);
      expect(exception.requestUrl, isNull);
    });

    test('keeps the response body verbatim, leaving scrubbing to the reporter',
        () {
      const body = '{"cpf":"123.456.789-00"}';
      const exception = HttpException(
        statusCode: 400,
        message: 'HTTP 400: Bad Request',
        responseBody: body,
      );

      expect(exception.responseBody, body);
    });

    test('keeps the domain error code separate from the human message', () {
      const exception = HttpException(
        statusCode: 409,
        message: 'HTTP 409: Conflict',
        serverMessages: ['O ultimo responsavel nao pode perder o acesso.'],
        domainErrorId: 'TNM.TNT20',
      );

      expect(exception.domainErrorId, 'TNM.TNT20');
      expect(exception.serverMessages.single,
          'O ultimo responsavel nao pode perder o acesso.');
    });

    test('describes itself with the status code and the generic message', () {
      const exception = HttpException(
        statusCode: 404,
        message: 'HTTP 404: Not Found',
      );

      expect(exception.toString(), 'HttpException(404): HTTP 404: Not Found');
    });

    test('is an Exception, so a bare catch-on-Exception still sees it', () {
      const exception = HttpException(statusCode: 500, message: 'HTTP 500');

      expect(exception, isA<Exception>());
    });
  });
}
