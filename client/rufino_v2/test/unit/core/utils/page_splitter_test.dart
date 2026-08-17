import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/core/utils/page_splitter.dart';

void main() {
  group('splitIntoEqualParts', () {
    test('splits ten pages every two into five parts of two', () {
      final pages = List.generate(10, (i) => 'p${i + 1}');

      final parts = splitIntoEqualParts(pages, 2);

      expect(parts, hasLength(5));
      expect(parts.every((part) => part.length == 2), isTrue);
    });

    test('keeps the original page order across the parts', () {
      final pages = List.generate(6, (i) => 'p${i + 1}');

      final parts = splitIntoEqualParts(pages, 3);

      expect(parts, [
        ['p1', 'p2', 'p3'],
        ['p4', 'p5', 'p6'],
      ]);
    });

    test('returns one part per page when the part size is one', () {
      final pages = List.generate(4, (i) => 'p$i');

      final parts = splitIntoEqualParts(pages, 1);

      expect(parts, hasLength(4));
      expect(parts.map((part) => part.single), pages);
    });

    test('returns a single part when the part size equals the total', () {
      final pages = List.generate(5, (i) => 'p$i');

      final parts = splitIntoEqualParts(pages, 5);

      expect(parts, hasLength(1));
      expect(parts.single, pages);
    });

    test('references the original pages instead of copying them', () {
      final firstPage = ['byte'];
      final pages = [
        firstPage,
        ['other'],
      ];

      final parts = splitIntoEqualParts(pages, 1);

      expect(identical(parts.first.single, firstPage), isTrue);
    });
  });

  group('validatePagesPerDocument', () {
    test('accepts a value that divides the pages evenly', () {
      expect(validatePagesPerDocument('2', totalPages: 10), isNull);
    });

    test('accepts one page per document', () {
      expect(validatePagesPerDocument('1', totalPages: 7), isNull);
    });

    test('accepts a value equal to the total page count', () {
      expect(validatePagesPerDocument('7', totalPages: 7), isNull);
    });

    test('ignores surrounding whitespace', () {
      expect(validatePagesPerDocument('  5 ', totalPages: 10), isNull);
    });

    test('rejects an empty value', () {
      expect(
        validatePagesPerDocument('', totalPages: 10),
        'Informe quantas páginas por documento.',
      );
    });

    test('rejects a null value', () {
      expect(
        validatePagesPerDocument(null, totalPages: 10),
        'Informe quantas páginas por documento.',
      );
    });

    test('rejects text that is not a whole number', () {
      expect(
        validatePagesPerDocument('2,5', totalPages: 10),
        'Informe um número inteiro.',
      );
    });

    test('rejects zero', () {
      expect(
        validatePagesPerDocument('0', totalPages: 10),
        'Mínimo de 1 página por documento.',
      );
    });

    test('rejects a negative value', () {
      expect(
        validatePagesPerDocument('-2', totalPages: 10),
        'Mínimo de 1 página por documento.',
      );
    });

    test('rejects a value larger than the number of scanned pages', () {
      expect(
        validatePagesPerDocument('11', totalPages: 10),
        'A digitalização tem apenas 10 páginas.',
      );
    });

    test('explains how many pages would be left over', () {
      expect(
        validatePagesPerDocument('3', totalPages: 10),
        '10 páginas não podem ser divididas a cada 3 (sobram 1).',
      );
    });
  });
}
