import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/domain/entities/selection_option.dart';

void main() {
  const options = [
    SelectionOption(id: '1', name: 'Masculino'),
    SelectionOption(id: '2', name: 'Feminino'),
  ];

  group('SelectionOption.labelForId', () {
    test('returns the matching name when id exists in the list', () {
      expect(SelectionOption.labelForId(options, '1'), 'Masculino');
      expect(SelectionOption.labelForId(options, '2'), 'Feminino');
    });

    test('returns Não informado when id is empty', () {
      expect(SelectionOption.labelForId(options, ''), 'Não informado');
    });

    test('returns Não informado when id is not found in the list', () {
      expect(SelectionOption.labelForId(options, '99'), 'Não informado');
    });
  });

  group('SelectionOption.safeId', () {
    test('returns the id when it exists in the list', () {
      expect(SelectionOption.safeId('1', options), '1');
    });

    test('returns null when id is not found in the list', () {
      expect(SelectionOption.safeId('99', options), isNull);
    });

    test('returns null when id is null', () {
      expect(SelectionOption.safeId(null, options), isNull);
    });

    test('returns null when id is empty', () {
      expect(SelectionOption.safeId('', options), isNull);
    });
  });
}
