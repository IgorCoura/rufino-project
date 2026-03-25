import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/data/models/document_group_api_model.dart';

void main() {
  group('DocumentGroupApiModel', () {
    test('fromJson parses all fields correctly', () {
      final json = {
        'id': 'grp-1',
        'name': 'Admissão',
        'description': 'Documentos de admissão',
      };

      final model = DocumentGroupApiModel.fromJson(json);

      expect(model.id, 'grp-1');
      expect(model.name, 'Admissão');
      expect(model.description, 'Documentos de admissão');
    });

    test('fromJson handles null fields with defaults', () {
      final json = <String, dynamic>{};

      final model = DocumentGroupApiModel.fromJson(json);

      expect(model.id, '');
      expect(model.name, '');
      expect(model.description, '');
    });

    test('toEntity converts to domain DocumentGroup', () {
      const model = DocumentGroupApiModel(
        id: 'grp-1',
        name: 'Periódicos',
        description: 'Documentos periódicos',
      );

      final entity = model.toEntity();

      expect(entity.id, 'grp-1');
      expect(entity.name, 'Periódicos');
      expect(entity.description, 'Documentos periódicos');
    });

    test('toCreateJson excludes id field', () {
      const model = DocumentGroupApiModel(
        id: 'grp-1',
        name: 'Admissão',
        description: 'Descrição',
      );

      final json = model.toCreateJson();

      expect(json.containsKey('id'), isFalse);
      expect(json['name'], 'Admissão');
      expect(json['description'], 'Descrição');
    });

    test('toJson includes id field', () {
      const model = DocumentGroupApiModel(
        id: 'grp-1',
        name: 'Admissão',
        description: 'Descrição',
      );

      final json = model.toJson();

      expect(json['id'], 'grp-1');
      expect(json['name'], 'Admissão');
      expect(json['description'], 'Descrição');
    });
  });
}
