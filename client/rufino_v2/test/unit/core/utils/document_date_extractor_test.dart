import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/core/utils/document_date_extractor.dart';

void main() {
  group('findLastValidDate', () {
    test('returns null when no date is present', () {
      expect(findLastValidDate('contrato sem data'), isNull);
    });

    test('returns the only date when there is one', () {
      expect(
        findLastValidDate('Assinado em 15/03/2026.'),
        '15/03/2026',
      );
    });

    test('returns the last date when multiple are present', () {
      expect(
        findLastValidDate(
          'Vigência a partir de 10/01/2026. Válido até 15/03/2026.',
        ),
        '15/03/2026',
      );
    });

    test('ignores invalid dates and keeps the previous valid one', () {
      // 32/13/9999 is invalid (day, month, year out of range); the
      // function should fall back to the previous valid 10/01/2026.
      expect(
        findLastValidDate(
          'Início 10/01/2026 e referência 32/13/9999.',
        ),
        '10/01/2026',
      );
    });

    test('ignores year out of the 1900–2100 range', () {
      expect(findLastValidDate('Data: 15/03/1899.'), isNull);
      expect(findLastValidDate('Data: 15/03/2101.'), isNull);
    });

    test('accepts a date surrounded by punctuation', () {
      expect(
        findLastValidDate('(São Paulo, 15/03/2026)'),
        '15/03/2026',
      );
    });
  });
}
