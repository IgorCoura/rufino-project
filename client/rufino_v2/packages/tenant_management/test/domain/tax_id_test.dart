import 'package:flutter_test/flutter_test.dart';
import 'package:tenant_management/tenant_management.dart';

void main() {
  group('TaxId', () {
    test('accepts a CPF whose check digits close', () {
      expect(TaxId.isValidCpf('529.982.247-25'), isTrue);
      expect(TaxId.isValidCpf('52998224725'), isTrue);
    });

    test('refuses a CPF with the wrong check digits', () {
      expect(TaxId.isValidCpf('529.982.247-24'), isFalse);
    });

    test('refuses a CPF made of one repeated digit', () {
      expect(TaxId.isValidCpf('111.111.111-11'), isFalse);
      expect(TaxId.isValidCpf('000.000.000-00'), isFalse);
    });

    test('refuses a CPF that is not eleven digits long', () {
      expect(TaxId.isValidCpf('5299822472'), isFalse);
      expect(TaxId.isValidCpf('529982247250'), isFalse);
    });

    test('accepts a CNPJ whose check digits close', () {
      expect(TaxId.isValidCnpj('11.222.333/0001-81'), isTrue);
      expect(TaxId.isValidCnpj('11222333000181'), isTrue);
    });

    test('refuses a CNPJ with the wrong check digits', () {
      expect(TaxId.isValidCnpj('11.222.333/0001-82'), isFalse);
    });

    test('refuses a CNPJ made of one repeated digit', () {
      expect(TaxId.isValidCnpj('11.111.111/1111-11'), isFalse);
    });

    test('validates against the document the tenant kind requires', () {
      expect(TaxId.isValidFor(TenantKinds.individual, '52998224725'), isTrue);
      expect(
        TaxId.isValidFor(TenantKinds.individual, '11222333000181'),
        isFalse,
      );
      expect(TaxId.isValidFor(TenantKinds.company, '11222333000181'), isTrue);
      expect(TaxId.isValidFor(TenantKinds.company, '52998224725'), isFalse);
    });

    test('formats CPF and CNPJ, and leaves a half-typed document alone', () {
      expect(TaxId.format('52998224725'), '529.982.247-25');
      expect(TaxId.format('11222333000181'), '11.222.333/0001-81');
      expect(TaxId.format('5299'), '5299');
    });
  });
}
