import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/core/utils/error_messages.dart';
import 'package:rufino_v2/data/services/http_exception.dart';

/// A fake network exception with a `cause` field, mimicking the pattern
/// used by all `*NetworkException` classes in the codebase.
class _FakeNetworkException implements Exception {
  const _FakeNetworkException(this.cause);
  final Object cause;
}

/// An exception type with no `cause` field.
class _UnrelatedError implements Exception {
  const _UnrelatedError();
}

void main() {
  group('extractServerMessages', () {
    test('extracts messages from HttpException directly', () {
      const error = HttpException(
        statusCode: 400,
        message: 'Bad Request',
        serverMessages: ['O campo Nome é obrigatório.', 'Valor inválido.'],
      );

      final messages = extractServerMessages(error);
      expect(messages, hasLength(2));
      expect(messages[0], 'O campo Nome é obrigatório.');
      expect(messages[1], 'Valor inválido.');
    });

    test('extracts messages from HttpException with no server messages', () {
      const error = HttpException(
        statusCode: 500,
        message: 'Internal Server Error',
      );

      final messages = extractServerMessages(error);
      expect(messages, isEmpty);
    });

    test('extracts messages from NetworkException wrapping HttpException', () {
      const httpError = HttpException(
        statusCode: 400,
        message: 'Bad Request',
        serverMessages: ['Erro do servidor.'],
      );
      const networkError = _FakeNetworkException(httpError);

      final messages = extractServerMessages(networkError);
      expect(messages, hasLength(1));
      expect(messages.first, 'Erro do servidor.');
    });

    test('returns empty list for NetworkException wrapping non-HttpException',
        () {
      const networkError = _FakeNetworkException('some string error');

      final messages = extractServerMessages(networkError);
      expect(messages, isEmpty);
    });

    test('returns empty list for unrelated exception type', () {
      const error = _UnrelatedError();

      final messages = extractServerMessages(error);
      expect(messages, isEmpty);
    });

    test('returns empty list for a plain string error', () {
      final messages = extractServerMessages('plain string');
      expect(messages, isEmpty);
    });
  });
}
