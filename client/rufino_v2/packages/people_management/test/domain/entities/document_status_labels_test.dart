import 'package:people_management/people_management.dart';
import 'package:flutter_test/flutter_test.dart';
void main() {
  group('documentUnitStatusLabel', () {
    test('translates every id the server can send for a document unit', () {
      const expected = {
        '1': 'Pendente',
        '2': 'OK',
        '3': 'Obsoleto',
        '4': 'Inválido',
        '5': 'Requer Validação',
        '6': 'Não Aplicável',
        '7': 'Aguardando Assinatura',
        '8': 'A Vencer',
        '9': 'Vencido',
      };

      expected.forEach((id, label) {
        expect(documentUnitStatusLabel(id, 'IgnoredEnglishName'), label);
      });
    });

    test('translates by the smart enum name when the id is not recognised', () {
      expect(documentUnitStatusLabel('', 'Pending'), 'Pendente');
      expect(
        documentUnitStatusLabel('99', 'AwaitingSignature'),
        'Aguardando Assinatura',
      );
      expect(documentUnitStatusLabel('', 'NotApplicable'), 'Não Aplicável');
      expect(documentUnitStatusLabel('', 'Expired'), 'Vencido');
    });

    test('matches the smart enum name regardless of case and padding', () {
      expect(documentUnitStatusLabel('', ' pending '), 'Pendente');
      expect(documentUnitStatusLabel(' 7 ', ''), 'Aguardando Assinatura');
    });

    test('falls back to the raw name when neither id nor name is known', () {
      expect(documentUnitStatusLabel('42', 'BrandNewStatus'), 'BrandNewStatus');
    });

    test('falls back to the id when there is no name either', () {
      expect(documentUnitStatusLabel('42', ''), '42');
    });
  });

  group('documentStatusLabel', () {
    test('translates every id the server can send for a document', () {
      const expected = {
        '1': 'Falta Entregar',
        '2': 'Requer Validação',
        '3': 'OK',
        '4': 'Obsoleto',
        '5': 'Aguardando Assinatura',
        '6': 'A Vencer',
        '7': 'Vencido',
      };

      expected.forEach((id, label) {
        expect(documentStatusLabel(id, 'IgnoredEnglishName'), label);
      });
    });

    test('translates by the smart enum name when the id is not recognised', () {
      expect(documentStatusLabel('', 'RequiresDocument'), 'Falta Entregar');
      expect(
        documentStatusLabel('', 'AwaitingSignature'),
        'Aguardando Assinatura',
      );
    });

    test('reads id 1 as the document scale, not as a pending unit', () {
      expect(documentStatusLabel('1', 'RequiresDocument'), 'Falta Entregar');
      expect(documentUnitStatusLabel('1', 'Pending'), 'Pendente');
    });
  });

  group('documentComplianceStatusLabel', () {
    test('translates the three-valued rollup by id', () {
      expect(documentComplianceStatusLabel('0', 'Okay'), 'OK');
      expect(documentComplianceStatusLabel('1', 'Warning'), 'A Vencer');
      expect(
        documentComplianceStatusLabel('2', 'RequiresAttention'),
        'Requer Atenção',
      );
    });

    test('translates by the smart enum name when the id is not recognised', () {
      expect(documentComplianceStatusLabel('', 'Okay'), 'OK');
      expect(
        documentComplianceStatusLabel('', 'RequiresAttention'),
        'Requer Atenção',
      );
    });
  });
}
