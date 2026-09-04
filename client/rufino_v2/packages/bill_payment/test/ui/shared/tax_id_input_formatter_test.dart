import 'package:bill_payment/src/ui/shared/tax_id_input_formatter.dart';
import 'package:flutter/services.dart';
import 'package:flutter_test/flutter_test.dart';

/// Types [digits] one keystroke at a time, as a keyboard would.
///
/// Digit by digit and not in one paste on purpose: the bug this file guards
/// only appears at the twelfth keystroke, and a single call with the whole
/// document would never reach it.
TextEditingValue type(TaxIdInputFormatter formatter, String digits) {
  var value = TextEditingValue.empty;

  for (final digit in digits.split('')) {
    final typed = TextEditingValue(
      text: value.text + digit,
      selection: TextSelection.collapsed(offset: value.text.length + 1),
    );
    value = formatter.formatEditUpdate(value, typed);
  }

  return value;
}

void main() {
  late TaxIdInputFormatter formatter;

  setUp(() => formatter = TaxIdInputFormatter());

  group('TaxIdInputFormatter', () {
    test('starts mounted with the CPF mask', () {
      expect(formatter.currentMask, TaxIdInputFormatter.cpfMask);
    });

    test('groups a CPF as a CPF', () {
      expect(type(formatter, '12345678901').text, '123.456.789-01');
    });

    // Teste de regressão. Bug de 2026-08-19: o campo nascia com a máscara de
    // CPF, que tem 11 posições, e o MaskTextInputFormatter engolia a 12ª tecla
    // — o onChanged que trocaria para CNPJ nunca era alcançado, e cadastrar
    // empresa era impossível pela tela de novo beneficiário.
    test('grows into a CNPJ at the twelfth digit', () {
      final value = type(formatter, '11222333000181');

      expect(value.text, '11.222.333/0001-81');
      expect(formatter.currentMask, TaxIdInputFormatter.cnpjMask);
    });

    test('keeps every digit while the mask changes', () {
      final value = type(formatter, '11222333000181');

      expect(value.text.replaceAll(RegExp(r'\D'), ''), '11222333000181');
    });

    test('stops at fourteen digits', () {
      expect(type(formatter, '112223330001819999').text, '11.222.333/0001-81');
    });

    // Apagar de volta refaz o caminho: quem digitou um CNPJ por engano e
    // corrige não fica preso na máscara maior.
    test('shrinks back to a CPF when the digits no longer fill a CNPJ', () {
      var value = type(formatter, '11222333000181');

      // Três backspaces levam de 14 para 11 dígitos.
      for (var i = 0; i < 3; i++) {
        final shortened = TextEditingValue(
          text: value.text.substring(0, value.text.length - 1),
          selection: TextSelection.collapsed(offset: value.text.length - 1),
        );
        value = formatter.formatEditUpdate(value, shortened);
      }

      expect(formatter.currentMask, TaxIdInputFormatter.cpfMask);
      expect(value.text, '112.223.330-00');
    });

    test('accepts a document pasted whole', () {
      final value = formatter.formatEditUpdate(
        TextEditingValue.empty,
        const TextEditingValue(text: '11.222.333/0001-81'),
      );

      expect(value.text, '11.222.333/0001-81');
    });
  });
}
