import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/data/models/company_api_model.dart';

void main() {
  group('CompanyApiModel', () {
    final jsonFull = {
      'id': 'abc-123',
      'corporateName': 'Acme Corp S.A.',
      'fantasyName': 'Acme',
      'cnpj': '12345678000190',
      'email': 'contato@acme.com',
      'phone': '11999999999',
      'zipCode': '01310100',
      'street': 'Av. Paulista',
      'number': '1000',
      'complement': 'Sala 1',
      'neighborhood': 'Bela Vista',
      'city': 'São Paulo',
      'state': 'SP',
      'country': 'Brasil',
    };

    test('fromJson parses all fields correctly', () {
      final model = CompanyApiModel.fromJson(jsonFull);

      expect(model.id, 'abc-123');
      expect(model.corporateName, 'Acme Corp S.A.');
      expect(model.fantasyName, 'Acme');
      expect(model.cnpj, '12345678000190');
      expect(model.email, 'contato@acme.com');
      expect(model.phone, '11999999999');
    });

    test('toEntity returns Company with basic fields', () {
      final model = CompanyApiModel.fromJson(jsonFull);
      final entity = model.toEntity();

      expect(entity.id, 'abc-123');
      expect(entity.fantasyName, 'Acme');
    });

    test('toDetailEntity returns CompanyDetail with all fields', () {
      final model = CompanyApiModel.fromJson(jsonFull);
      final detail = model.toDetailEntity();

      expect(detail.email, 'contato@acme.com');
      expect(detail.city, 'São Paulo');
      expect(detail.state, 'SP');
    });

    test('toCreateJson excludes id', () {
      final model = CompanyApiModel.fromJson(jsonFull);
      final json = model.toCreateJson();

      expect(json.containsKey('id'), false);
      expect(json['corporateName'], 'Acme Corp S.A.');
    });
  });
}
