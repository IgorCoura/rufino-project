import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/data/models/workplace_api_model.dart';

void main() {
  group('AddressApiModel', () {
    const _json = {
      'zipCode': '01310100',
      'street': 'Av. Paulista',
      'number': '1000',
      'complement': 'Apto 42',
      'neighborhood': 'Bela Vista',
      'city': 'São Paulo',
      'state': 'SP',
      'country': 'Brasil',
    };

    test('fromJson parses all fields correctly', () {
      final model = AddressApiModel.fromJson(_json);

      expect(model.zipCode, '01310100');
      expect(model.street, 'Av. Paulista');
      expect(model.number, '1000');
      expect(model.complement, 'Apto 42');
      expect(model.neighborhood, 'Bela Vista');
      expect(model.city, 'São Paulo');
      expect(model.state, 'SP');
      expect(model.country, 'Brasil');
    });

    test('fromJson returns empty strings for missing fields', () {
      final model = AddressApiModel.fromJson({});

      expect(model.zipCode, '');
      expect(model.street, '');
      expect(model.number, '');
    });

    test('toJson produces the correct map', () {
      final model = AddressApiModel.fromJson(_json);
      final json = model.toJson();

      expect(json['zipCode'], '01310100');
      expect(json['street'], 'Av. Paulista');
      expect(json['number'], '1000');
      expect(json['complement'], 'Apto 42');
      expect(json['neighborhood'], 'Bela Vista');
      expect(json['city'], 'São Paulo');
      expect(json['state'], 'SP');
      expect(json['country'], 'Brasil');
    });

    test('toEntity maps to Address domain entity correctly', () {
      final model = AddressApiModel.fromJson(_json);
      final entity = model.toEntity();

      expect(entity.zipCode, '01310100');
      expect(entity.street, 'Av. Paulista');
      expect(entity.number, '1000');
      expect(entity.complement, 'Apto 42');
      expect(entity.neighborhood, 'Bela Vista');
      expect(entity.city, 'São Paulo');
      expect(entity.state, 'SP');
      expect(entity.country, 'Brasil');
    });
  });

  group('WorkplaceApiModel', () {
    const _json = {
      'id': 'wp-1',
      'name': 'Sede Principal',
      'address': {
        'zipCode': '01310100',
        'street': 'Av. Paulista',
        'number': '1000',
        'complement': '',
        'neighborhood': 'Bela Vista',
        'city': 'São Paulo',
        'state': 'SP',
        'country': 'Brasil',
      },
    };

    test('fromJson parses all fields including nested address', () {
      final model = WorkplaceApiModel.fromJson(_json);

      expect(model.id, 'wp-1');
      expect(model.name, 'Sede Principal');
      expect(model.address.zipCode, '01310100');
      expect(model.address.city, 'São Paulo');
    });

    test('fromJson returns empty strings for missing fields', () {
      final model = WorkplaceApiModel.fromJson({});

      expect(model.id, '');
      expect(model.name, '');
    });

    test('toJson includes id field', () {
      final model = WorkplaceApiModel.fromJson(_json);
      final json = model.toJson();

      expect(json['id'], 'wp-1');
      expect(json['name'], 'Sede Principal');
      expect(json.containsKey('address'), isTrue);
    });

    test('toCreateJson excludes id field', () {
      final model = WorkplaceApiModel.fromJson(_json);
      final json = model.toCreateJson();

      expect(json.containsKey('id'), isFalse);
      expect(json['name'], 'Sede Principal');
      expect(json.containsKey('address'), isTrue);
    });

    test('toEntity maps to Workplace domain entity correctly', () {
      final model = WorkplaceApiModel.fromJson(_json);
      final entity = model.toEntity();

      expect(entity.id, 'wp-1');
      expect(entity.name, 'Sede Principal');
      expect(entity.address.city, 'São Paulo');
      expect(entity.address.state, 'SP');
    });
  });
}
