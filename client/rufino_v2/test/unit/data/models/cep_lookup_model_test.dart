import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/data/models/cep_lookup_model.dart';

void main() {
  group('CepLookupModel', () {
    test('fromJson parses ViaCEP fields', () {
      final dto = CepLookupModel.fromJson({
        'cep': '01310-100',
        'logradouro': 'Avenida Paulista',
        'complemento': 'de 612 a 1510 - lado par',
        'bairro': 'Bela Vista',
        'localidade': 'São Paulo',
        'uf': 'SP',
      });

      expect(dto.cep, '01310-100');
      expect(dto.logradouro, 'Avenida Paulista');
      expect(dto.bairro, 'Bela Vista');
      expect(dto.localidade, 'São Paulo');
      expect(dto.uf, 'SP');
    });

    test('fromJson defaults missing fields to empty strings', () {
      final dto = CepLookupModel.fromJson(const {});
      expect(dto.cep, '');
      expect(dto.logradouro, '');
      expect(dto.uf, '');
    });

    test('toAddress maps ViaCEP fields and defaults country to Brasil', () {
      final dto = CepLookupModel.fromJson({
        'cep': '01310-100',
        'logradouro': 'Avenida Paulista',
        'complemento': 'lado par',
        'bairro': 'Bela Vista',
        'localidade': 'São Paulo',
        'uf': 'SP',
      });

      final address = dto.toAddress();
      expect(address.zipCode, '01310-100');
      expect(address.street, 'Avenida Paulista');
      expect(address.number, '');
      expect(address.complement, 'lado par');
      expect(address.neighborhood, 'Bela Vista');
      expect(address.city, 'São Paulo');
      expect(address.state, 'SP');
      expect(address.country, 'Brasil');
    });
  });
}
