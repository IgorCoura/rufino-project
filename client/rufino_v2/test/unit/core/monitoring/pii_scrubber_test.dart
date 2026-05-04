import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/core/monitoring/pii_scrubber.dart';

void main() {
  group('scrubJson', () {
    test('replaces top-level Brazilian identifiers with [Filtered]', () {
      final input = <String, Object?>{
        'cpf': '123.456.789-00',
        'rg': '12.345.678-9',
        'cnpj': '00.000.000/0001-00',
        'unrelated': 'visible',
      };

      final result = scrubJson(input);

      expect(result['cpf'], '[Filtered]');
      expect(result['rg'], '[Filtered]');
      expect(result['cnpj'], '[Filtered]');
      expect(result['unrelated'], 'visible');
    });

    test('matches sensitive keys case-insensitively', () {
      final result = scrubJson(<String, Object?>{
        'CPF': '111',
        'Salary': 5000,
        'Authorization': 'Bearer xyz',
      });

      expect(result['CPF'], '[Filtered]');
      expect(result['Salary'], '[Filtered]');
      expect(result['Authorization'], '[Filtered]');
    });

    test('scrubs values nested inside maps', () {
      final input = <String, Object?>{
        'op': 'createEmployee',
        'payload': <String, Object?>{
          'cpf': '999',
          'address': 'Rua A, 100',
          'companyId': 'co-42',
        },
      };

      final result = scrubJson(input);

      final payload = result['payload'] as Map<String, Object?>;
      expect(payload['cpf'], '[Filtered]');
      expect(payload['address'], '[Filtered]');
      expect(payload['companyId'], 'co-42');
      expect(result['op'], 'createEmployee');
    });

    test('scrubs values nested inside lists of maps', () {
      final input = <String, Object?>{
        'dependents': <Object?>[
          <String, Object?>{'name': 'Ana', 'age': 8},
          <String, Object?>{'name': 'Bruno', 'age': 12},
        ],
      };

      final result = scrubJson(input);

      final dependents = result['dependents'] as List<Object?>;
      expect((dependents[0] as Map)['name'], '[Filtered]');
      expect((dependents[0] as Map)['age'], 8);
      expect((dependents[1] as Map)['name'], '[Filtered]');
    });

    test('does not mutate the input map', () {
      final input = <String, Object?>{'cpf': 'original'};
      scrubJson(input);
      expect(input['cpf'], 'original');
    });

    test('returns the input unchanged when no sensitive keys are present', () {
      final input = <String, Object?>{
        'op': 'list',
        'count': 12,
        'flag': true,
      };

      final result = scrubJson(input);

      expect(result, equals(input));
    });

    test('terminates on deeply nested structures without throwing', () {
      Map<String, Object?> nested = <String, Object?>{'cpf': '1'};
      for (var i = 0; i < 100; i++) {
        nested = <String, Object?>{'level': nested};
      }

      expect(() => scrubJson(nested), returnsNormally);
    });
  });

  group('scrubAndTruncateBody', () {
    test('scrubs PII keys in JSON object body', () {
      const body =
          '{"cpf":"123.456.789-00","companyId":"co-1","address":"Rua A"}';

      final result = scrubAndTruncateBody(body);

      expect(result, contains('"cpf":"[Filtered]"'));
      expect(result, contains('"companyId":"co-1"'));
      expect(result, contains('"address":"[Filtered]"'));
    });

    test('scrubs PII inside JSON array body', () {
      const body =
          '[{"cpf":"111","name":"Ana"},{"cpf":"222","name":"Bruno"}]';

      final result = scrubAndTruncateBody(body);

      expect(result, contains('"cpf":"[Filtered]"'));
      expect(result, contains('"name":"[Filtered]"'));
      expect(result, isNot(contains('"Ana"')));
    });

    test('returns non-JSON body verbatim when below limit', () {
      final result = scrubAndTruncateBody('Internal Server Error');

      expect(result, 'Internal Server Error');
    });

    test('truncates long bodies and appends marker', () {
      final body = 'a' * (defaultMaxBodyChars + 200);

      final result = scrubAndTruncateBody(body);

      expect(result.length, defaultMaxBodyChars + '…[truncated]'.length);
      expect(result, endsWith('…[truncated]'));
    });

    test('respects an explicit maxChars limit', () {
      final result = scrubAndTruncateBody('abcdefghij', maxChars: 4);

      expect(result, 'abcd…[truncated]');
    });

    test('returns empty string for empty input', () {
      expect(scrubAndTruncateBody(''), '');
    });
  });
}
