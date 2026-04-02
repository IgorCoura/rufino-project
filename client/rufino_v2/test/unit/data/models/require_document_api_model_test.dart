import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/data/models/require_document_api_model.dart';

void main() {
  group('RequireDocumentApiModel', () {
    test('fromJsonSimple parses id, name, and description', () {
      final json = {
        'id': 'req-1',
        'name': 'Admissão CLT',
        'description': 'Documentos de admissão',
      };

      final model = RequireDocumentApiModel.fromJsonSimple(json);

      expect(model.id, 'req-1');
      expect(model.name, 'Admissão CLT');
      expect(model.description, 'Documentos de admissão');
    });

    test('fromJsonSimple handles missing fields gracefully', () {
      final json = <String, dynamic>{};

      final model = RequireDocumentApiModel.fromJsonSimple(json);

      expect(model.id, '');
      expect(model.name, '');
      expect(model.description, '');
    });

    test('fromJson parses full structure with associations and templates', () {
      final json = {
        'id': 'req-1',
        'name': 'Admissão CLT',
        'description': 'Documentos de admissão',
        'companyId': 'company-1',
        'associationType': {'id': 1, 'name': 'Role'},
        'associations': [
          {'id': 'assoc-1', 'name': 'Desenvolvedor'},
          {'id': 'assoc-2', 'name': 'Analista'},
        ],
        'documentsTemplates': [
          {'id': 'tpl-1', 'name': 'Contrato', 'description': 'Contrato CLT'},
        ],
        'listenEvents': [
          {
            'event': {'id': 1, 'name': 'CreatedEvent'},
            'status': [1, 2],
          },
        ],
      };

      final model = RequireDocumentApiModel.fromJson(json);

      expect(model.id, 'req-1');
      expect(model.name, 'Admissão CLT');
      expect(model.associationTypeId, 1);
      expect(model.associationTypeName, 'Função');
      expect(model.associationIds, ['assoc-1', 'assoc-2']);
      expect(model.associations.length, 2);
      expect(model.associations[0].name, 'Desenvolvedor');
      expect(model.associations[1].name, 'Analista');
      expect(model.documentTemplates.length, 1);
      expect(model.listenEvents.length, 1);
    });

    test('toEntity converts all fields to domain entity', () {
      final json = {
        'id': 'req-1',
        'name': 'Admissão CLT',
        'description': 'Documentos de admissão',
        'companyId': 'company-1',
        'associationType': {'id': '1', 'name': 'Role'},
        'associations': [
          {'id': 'assoc-1', 'name': 'Desenvolvedor'},
        ],
        'documentsTemplates': [
          {'id': 'tpl-1', 'name': 'Contrato', 'description': 'Contrato CLT'},
        ],
        'listenEvents': [
          {
            'event': {'id': 1, 'name': 'CreatedEvent'},
            'status': [1, 2],
          },
        ],
      };

      final entity = RequireDocumentApiModel.fromJson(json).toEntity();

      expect(entity.id, 'req-1');
      expect(entity.name, 'Admissão CLT');
      expect(entity.associationTypeId, 1);
      expect(entity.associationTypeName, 'Função');
      expect(entity.associationIds, ['assoc-1']);
      expect(entity.associations.length, 1);
      expect(entity.associations.first.name, 'Desenvolvedor');
      expect(entity.documentTemplates.length, 1);
      expect(entity.documentTemplates.first.name, 'Contrato');
      expect(entity.listenEvents.length, 1);
      expect(entity.listenEvents.first.eventName, 'Evento Criado');
      expect(entity.listenEvents.first.statuses.length, 2);
      expect(entity.listenEvents.first.statuses[0].name, 'Pendente');
      expect(entity.listenEvents.first.statuses[1].name, 'Ativo');
    });

    test('toCreateJson serializes without id', () {
      const model = RequireDocumentApiModel(
        id: '',
        name: 'Test',
        description: 'Test desc',
        associationIds: ['assoc-1', 'assoc-2'],
        associationTypeId: 1,
      );

      final json = model.toCreateJson();

      expect(json.containsKey('id'), isFalse);
      expect(json['name'], 'Test');
      expect(json['associationType'], 1);
      expect(json['associationIds'], ['assoc-1', 'assoc-2']);
    });

    test('toJson serializes with id', () {
      const model = RequireDocumentApiModel(
        id: 'req-1',
        name: 'Test',
        description: 'Test desc',
        associationIds: ['assoc-1'],
        associationTypeId: 1,
      );

      final json = model.toJson();

      expect(json['id'], 'req-1');
      expect(json['name'], 'Test');
      expect(json['associationIds'], ['assoc-1']);
    });

    test('fromJson handles null associationType and associations gracefully',
        () {
      final json = {
        'id': 'req-1',
        'name': 'Test',
        'description': 'desc',
      };

      final model = RequireDocumentApiModel.fromJson(json);

      expect(model.associationTypeId, 0);
      expect(model.associationTypeName, '');
      expect(model.associationIds, isEmpty);
      expect(model.associations, isEmpty);
      expect(model.documentTemplates, isEmpty);
      expect(model.listenEvents, isEmpty);
    });

    test('fromJson translates event names to Portuguese', () {
      final json = {
        'id': 'req-1',
        'name': 'Test',
        'description': 'desc',
        'listenEvents': [
          {
            'event': {'id': 5, 'name': 'WorkplaceChangeEvent'},
            'status': [],
          },
          {
            'event': {'id': 10, 'name': 'JanuaryEvent'},
            'status': [],
          },
        ],
      };

      final entity = RequireDocumentApiModel.fromJson(json).toEntity();

      expect(entity.listenEvents[0].eventName,
          'Evento de Alteração de Local de Trabalho');
      expect(
          entity.listenEvents[1].eventName, 'Evento Recorrente de Janeiro');
    });
  });
}
