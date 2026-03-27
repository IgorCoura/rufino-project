import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/domain/entities/document_group_with_templates.dart';

void main() {
  group('DocumentGroupWithTemplates computed properties', () {
    const group = DocumentGroupWithTemplates(
      id: '1',
      name: 'Admissão',
      description: 'Grupo de admissão',
      templates: [
        DocumentTemplateSummary(id: 't1', name: 'Contrato', description: ''),
      ],
    );

    const emptyGroup = DocumentGroupWithTemplates(
      id: '2',
      name: 'Vazio',
      description: '',
    );

    test('hasDescription returns true when description is not empty', () {
      expect(group.hasDescription, isTrue);
      expect(emptyGroup.hasDescription, isFalse);
    });

    test('hasTemplates returns true when templates list is not empty', () {
      expect(group.hasTemplates, isTrue);
      expect(emptyGroup.hasTemplates, isFalse);
    });

    test('templateCount returns the number of templates', () {
      expect(group.templateCount, 1);
      expect(emptyGroup.templateCount, 0);
    });
  });

  group('DocumentTemplateSummary computed properties', () {
    test('hasDescription returns true when description is not empty', () {
      const t = DocumentTemplateSummary(
        id: '1',
        name: 'Contrato',
        description: 'Contrato de trabalho',
      );
      expect(t.hasDescription, isTrue);
    });

    test('hasDescription returns false when description is empty', () {
      const t = DocumentTemplateSummary(
        id: '1',
        name: 'Contrato',
        description: '',
      );
      expect(t.hasDescription, isFalse);
    });
  });
}
