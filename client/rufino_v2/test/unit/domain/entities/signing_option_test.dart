import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/domain/entities/signing_option.dart';

void main() {
  group('SigningOption.labelForId', () {
    test('returns Assinatura Física for id 1', () {
      expect(SigningOption.labelForId('1'), 'Assinatura Física');
    });

    test('returns Assinatura Digital e Whatsapp for id 2', () {
      expect(SigningOption.labelForId('2'), 'Assinatura Digital e Whatsapp');
    });

    test('returns Assinatura Digital e Selfie for id 3', () {
      expect(SigningOption.labelForId('3'), 'Assinatura Digital e Selfie');
    });

    test('returns Assinatura Digital e SMS for id 4', () {
      expect(SigningOption.labelForId('4'), 'Assinatura Digital e SMS');
    });

    test('returns Apenas SMS for id 5', () {
      expect(SigningOption.labelForId('5'), 'Apenas SMS');
    });

    test('returns Apenas Whatsapp for id 6', () {
      expect(SigningOption.labelForId('6'), 'Apenas Whatsapp');
    });

    test('returns Não informado for unknown id', () {
      expect(SigningOption.labelForId('99'), 'Não informado');
    });

    test('returns Não informado for empty id', () {
      expect(SigningOption.labelForId(''), 'Não informado');
    });
  });
}
