import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_core/rufino_core.dart';

/// The last thing that runs before a payload leaves for the crash dashboard.
///
/// Anything it misses is personal data sitting in a third party's database, so
/// the tests below go key by key over the categories the scrubber claims to
/// cover, and then over the shapes a real response body arrives in.
void main() {
  group('scrubJson on top-level keys', () {
    test('filters Brazilian personal identifiers', () {
      final result = scrubJson(<String, Object?>{
        'cpf': '123.456.789-00',
        'rg': '12.345.678-9',
        'cnpj': '00.000.000/0001-00',
        'pis': '111',
        'tituloEleitor': '222',
      });

      expect(result['cpf'], '[Filtered]');
      expect(result['rg'], '[Filtered]');
      expect(result['cnpj'], '[Filtered]');
      expect(result['pis'], '[Filtered]');
      expect(result['tituloEleitor'], '[Filtered]');
    });

    test('filters names, contact data and addresses', () {
      final result = scrubJson(<String, Object?>{
        'name': 'Ana',
        'nomeCompleto': 'Ana Souza',
        'email': 'ana@test.com',
        'telefone': '11999999999',
        'endereco': 'Rua A, 100',
        'cep': '01000-000',
      });

      expect(result.values, everyElement('[Filtered]'));
    });

    test('filters payroll amounts', () {
      final result = scrubJson(<String, Object?>{
        'salary': 5000,
        'remuneracao': 7000,
        'wage': 1,
      });

      expect(result.values, everyElement('[Filtered]'));
    });

    test('filters authentication secrets', () {
      final result = scrubJson(<String, Object?>{
        'password': 'hunter2',
        'accessToken': 'eyJ...',
        'refreshToken': 'eyJ...',
        'authorization': 'Bearer eyJ...',
        'apiKey': 'k-1',
      });

      expect(result.values, everyElement('[Filtered]'));
    });

    test('leaves keys that carry no personal data untouched', () {
      final input = <String, Object?>{
        'operation': 'listEmployees',
        'companyId': 'co-42',
        'count': 12,
        'flag': true,
      };

      expect(scrubJson(input), equals(input));
    });

    test('matches sensitive keys regardless of how they were capitalized', () {
      final result = scrubJson(<String, Object?>{
        'CPF': '111',
        'Salary': 5000,
        'Authorization': 'Bearer xyz',
        'ACCESSTOKEN': 'eyJ...',
      });

      expect(result.values, everyElement('[Filtered]'));
    });

    test('filters the mother and father name keys spelled in Portuguese', () {
      final result = scrubJson(<String, Object?>{
        'nomeMae': 'Maria',
        'nomePai': 'Jose',
      });

      expect(result['nomeMae'], '[Filtered]');
      expect(result['nomePai'], '[Filtered]');
    });

    test('filters the mother and father name keys spelled in English', () {
      final result = scrubJson(<String, Object?>{
        'motherName': 'Maria',
        'fatherName': 'Jose',
      });

      expect(result['motherName'], '[Filtered]');
      expect(result['fatherName'], '[Filtered]');
    });
  });

  group('scrubJson on nested structures', () {
    test('filters personal data nested inside a map', () {
      final result = scrubJson(<String, Object?>{
        'operation': 'createEmployee',
        'payload': <String, Object?>{
          'cpf': '999',
          'address': 'Rua A, 100',
          'companyId': 'co-42',
        },
      });

      final payload = result['payload'] as Map<String, Object?>;
      expect(payload['cpf'], '[Filtered]');
      expect(payload['address'], '[Filtered]');
      expect(payload['companyId'], 'co-42');
      expect(result['operation'], 'createEmployee');
    });

    test('filters personal data inside every element of a list', () {
      final result = scrubJson(<String, Object?>{
        'dependents': <Object?>[
          <String, Object?>{'name': 'Ana', 'age': 8},
          <String, Object?>{'name': 'Bruno', 'age': 12},
        ],
      });

      final dependents = result['dependents'] as List<Object?>;
      expect((dependents[0] as Map)['name'], '[Filtered]');
      expect((dependents[0] as Map)['age'], 8);
      expect((dependents[1] as Map)['name'], '[Filtered]');
    });

    test('filters personal data several levels down', () {
      final result = scrubJson(<String, Object?>{
        'a': <String, Object?>{
          'b': <String, Object?>{
            'c': <String, Object?>{'cpf': '111', 'id': 'x'},
          },
        },
      });

      final c = ((result['a'] as Map)['b'] as Map)['c'] as Map;
      expect(c['cpf'], '[Filtered]');
      expect(c['id'], 'x');
    });

    test('filters a whole sensitive subtree by its key, without descending',
        () {
      final result = scrubJson(<String, Object?>{
        'address': <String, Object?>{'street': 'Rua A', 'number': 100},
      });

      expect(result['address'], '[Filtered]');
    });

    test('walks a map that is not typed as a JSON map', () {
      final result = scrubJson(<String, Object?>{
        'context': <dynamic, dynamic>{'cpf': '111', 'id': 'x'},
      });

      final context = result['context'] as Map;
      expect(context['cpf'], '[Filtered]');
      expect(context['id'], 'x');
    });

    test('terminates instead of overflowing on an absurdly nested payload',
        () {
      Map<String, Object?> nested = <String, Object?>{'cpf': '1'};
      for (var i = 0; i < 200; i++) {
        nested = <String, Object?>{'level': nested};
      }

      expect(() => scrubJson(nested), returnsNormally);
    });

    test('does not mutate the map it was given', () {
      final input = <String, Object?>{
        'cpf': 'original',
        'nested': <String, Object?>{'email': 'ana@test.com'},
      };

      scrubJson(input);

      expect(input['cpf'], 'original');
      expect((input['nested'] as Map)['email'], 'ana@test.com');
    });
  });

  group('scrubAndTruncateBody', () {
    test('filters personal data in a JSON object body', () {
      final result = scrubAndTruncateBody(
        '{"cpf":"123.456.789-00","companyId":"co-1","address":"Rua A"}',
      );

      expect(result, contains('"cpf":"[Filtered]"'));
      expect(result, contains('"companyId":"co-1"'));
      expect(result, isNot(contains('123.456.789-00')));
    });

    test('filters personal data in a body that is a bare JSON array', () {
      final result = scrubAndTruncateBody(
        '[{"cpf":"111","name":"Ana"},{"cpf":"222","name":"Bruno"}]',
      );

      expect(result, isNot(contains('Ana')));
      expect(result, isNot(contains('Bruno')));
    });

    test('returns a body that is not JSON unchanged', () {
      expect(
        scrubAndTruncateBody('Internal Server Error'),
        'Internal Server Error',
      );
    });

    test('returns an empty body unchanged', () {
      expect(scrubAndTruncateBody(''), '');
    });

    test('truncates a body longer than the default limit and says so', () {
      final result = scrubAndTruncateBody('a' * (defaultMaxBodyChars + 200));

      expect(result, endsWith('…[truncated]'));
      expect(result.length, defaultMaxBodyChars + '…[truncated]'.length);
    });

    test('honours a caller-supplied limit', () {
      expect(scrubAndTruncateBody('abcdefghij', maxChars: 4), 'abcd…[truncated]');
    });

    test('leaves a body shorter than the limit whole', () {
      expect(scrubAndTruncateBody('short', maxChars: 100), 'short');
    });

    test('measures the limit against the filtered body, not the original', () {
      final result = scrubAndTruncateBody(
        '{"cpf":"a very long identifier that would blow the limit"}',
        maxChars: 30,
      );

      expect(result, '{"cpf":"[Filtered]"}');
    });
  });
}
