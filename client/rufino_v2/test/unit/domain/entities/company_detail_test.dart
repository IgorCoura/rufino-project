import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/domain/entities/company_detail.dart';

void main() {
  const detail = CompanyDetail(
    id: '1',
    corporateName: 'Corp',
    fantasyName: 'Fantasy',
    cnpj: '12345678000190',
    email: 'a@b.com',
    phone: '11987654321',
    zipCode: '01310100',
    street: 'Av. Paulista',
    number: '1000',
    complement: '',
    neighborhood: 'Bela Vista',
    city: 'São Paulo',
    state: 'SP',
    country: 'Brasil',
  );

  const empty = CompanyDetail(
    id: '2',
    corporateName: '',
    fantasyName: '',
    cnpj: '',
    email: '',
    phone: '',
    zipCode: '',
    street: '',
    number: '',
    complement: '',
    neighborhood: '',
    city: '',
    state: '',
    country: '',
  );

  group('CompanyDetail computed properties', () {
    test('hasEmail returns true when email is not empty', () {
      expect(detail.hasEmail, isTrue);
      expect(empty.hasEmail, isFalse);
    });

    test('hasPhone returns true when phone is not empty', () {
      expect(detail.hasPhone, isTrue);
      expect(empty.hasPhone, isFalse);
    });

    test('hasAddress returns true when main address fields are filled', () {
      expect(detail.hasAddress, isTrue);
      expect(empty.hasAddress, isFalse);
    });
  });

  group('CompanyDetail.formattedPhone', () {
    test('formats 11-digit phone as (XX) XXXXX-XXXX', () {
      expect(detail.formattedPhone, '(11) 98765-4321');
    });

    test('formats 10-digit phone as (XX) XXXX-XXXX', () {
      const d = CompanyDetail(
        id: '1',
        corporateName: '',
        fantasyName: '',
        cnpj: '',
        email: '',
        phone: '1134567890',
        zipCode: '',
        street: '',
        number: '',
        complement: '',
        neighborhood: '',
        city: '',
        state: '',
        country: '',
      );
      expect(d.formattedPhone, '(11) 3456-7890');
    });

    test('returns raw value for unexpected length', () {
      expect(empty.formattedPhone, '');
    });
  });

  group('CompanyDetail.formattedZipCode', () {
    test('formats 8-digit zip as XXXXX-XXX', () {
      expect(detail.formattedZipCode, '01310-100');
    });

    test('returns raw value for wrong length', () {
      expect(empty.formattedZipCode, '');
    });
  });
}
