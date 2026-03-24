import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/data/models/document_group_with_templates_api_model.dart';

void main() {
  group('DocumentGroupWithTemplatesApiModel', () {
    test('fromJson parses all fields correctly', () {
      final json = {
        'id': 'grp-1',
        'name': 'Admissão',
        'description': 'Documentos de admissão',
        'companyId': 'company-1',
        'documentTemplates': [
          {
            'id': 'tpl-1',
            'name': 'Contrato de Trabalho',
            'description': 'Modelo padrão de contrato',
          },
          {
            'id': 'tpl-2',
            'name': 'Ficha de Registro',
            'description': 'Ficha de registro do funcionário',
          },
        ],
      };

      final model = DocumentGroupWithTemplatesApiModel.fromJson(json);

      expect(model.id, 'grp-1');
      expect(model.name, 'Admissão');
      expect(model.description, 'Documentos de admissão');
      expect(model.companyId, 'company-1');
      expect(model.documentTemplates, hasLength(2));
      expect(model.documentTemplates.first.id, 'tpl-1');
      expect(model.documentTemplates.first.name, 'Contrato de Trabalho');
      expect(model.documentTemplates.last.id, 'tpl-2');
    });

    test('fromJson handles missing documentTemplates', () {
      final json = {
        'id': 'grp-1',
        'name': 'Admissão',
        'description': 'Documentos de admissão',
        'companyId': 'company-1',
      };

      final model = DocumentGroupWithTemplatesApiModel.fromJson(json);

      expect(model.id, 'grp-1');
      expect(model.name, 'Admissão');
      expect(model.documentTemplates, isEmpty);
    });

    test('fromJson handles empty documentTemplates list', () {
      final json = {
        'id': 'grp-1',
        'name': 'Admissão',
        'description': 'Documentos de admissão',
        'companyId': 'company-1',
        'documentTemplates': <dynamic>[],
      };

      final model = DocumentGroupWithTemplatesApiModel.fromJson(json);

      expect(model.documentTemplates, isEmpty);
    });

    test('toEntity converts to domain entity correctly', () {
      const model = DocumentGroupWithTemplatesApiModel(
        id: 'grp-1',
        name: 'Periódicos',
        description: 'Documentos periódicos',
        companyId: 'company-1',
        documentTemplates: [],
      );

      final entity = model.toEntity();

      expect(entity.id, 'grp-1');
      expect(entity.name, 'Periódicos');
      expect(entity.description, 'Documentos periódicos');
      expect(entity.templates, isEmpty);
    });

    test('toEntity converts nested templates to DocumentTemplateSummary',
        () {
      const model = DocumentGroupWithTemplatesApiModel(
        id: 'grp-1',
        name: 'Admissão',
        description: 'Documentos de admissão',
        companyId: 'company-1',
        documentTemplates: [
          DocumentTemplateSummaryApiModel(
            id: 'tpl-1',
            name: 'Contrato de Trabalho',
            description: 'Modelo padrão de contrato',
          ),
          DocumentTemplateSummaryApiModel(
            id: 'tpl-2',
            name: 'Ficha de Registro',
            description: 'Ficha de registro do funcionário',
          ),
        ],
      );

      final entity = model.toEntity();

      expect(entity.templates, hasLength(2));
      expect(entity.templates.first.id, 'tpl-1');
      expect(entity.templates.first.name, 'Contrato de Trabalho');
      expect(entity.templates.first.description, 'Modelo padrão de contrato');
      expect(entity.templates.last.id, 'tpl-2');
      expect(entity.templates.last.name, 'Ficha de Registro');
    });
  });
}
