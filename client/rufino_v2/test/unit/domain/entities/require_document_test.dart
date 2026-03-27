import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/domain/entities/require_document.dart';

void main() {
  group('RequireDocument computed properties', () {
    const doc = RequireDocument(
      id: '1',
      name: 'Admissão',
      description: 'Documentos de admissão',
      associationId: 'role-1',
      associationName: 'Analista',
      associationTypeId: 1,
      associationTypeName: 'Função',
      documentTemplates: [
        RequireDocumentTemplate(id: 't1', name: 'Contrato'),
      ],
      listenEvents: [
        ListenEvent(eventId: 1, eventName: 'Admissão'),
      ],
    );

    const emptyDoc = RequireDocument(
      id: '2',
      name: 'Vazio',
      description: '',
    );

    test('hasAssociation returns true when associationId is not empty', () {
      expect(doc.hasAssociation, isTrue);
      expect(emptyDoc.hasAssociation, isFalse);
    });

    test('isRoleAssociation returns true for type 1', () {
      expect(doc.isRoleAssociation, isTrue);
      expect(doc.isWorkplaceAssociation, isFalse);
    });

    test('isWorkplaceAssociation returns true for type 2', () {
      const wpDoc = RequireDocument(
        id: '3',
        name: 'WP',
        description: '',
        associationTypeId: 2,
      );
      expect(wpDoc.isWorkplaceAssociation, isTrue);
      expect(wpDoc.isRoleAssociation, isFalse);
    });

    test('hasTemplates returns true when templates list is not empty', () {
      expect(doc.hasTemplates, isTrue);
      expect(emptyDoc.hasTemplates, isFalse);
    });

    test('hasEvents returns true when events list is not empty', () {
      expect(doc.hasEvents, isTrue);
      expect(emptyDoc.hasEvents, isFalse);
    });

    test('templateCount returns the number of templates', () {
      expect(doc.templateCount, 1);
      expect(emptyDoc.templateCount, 0);
    });

    test('eventCount returns the number of events', () {
      expect(doc.eventCount, 1);
      expect(emptyDoc.eventCount, 0);
    });
  });

  group('RequireDocument.validateName', () {
    test('returns error when null or empty', () {
      expect(RequireDocument.validateName(null), isNotNull);
      expect(RequireDocument.validateName(''), isNotNull);
      expect(RequireDocument.validateName('   '), isNotNull);
    });

    test('returns error when exceeds 100 characters', () {
      expect(RequireDocument.validateName('A' * 101), isNotNull);
    });

    test('returns null for valid name', () {
      expect(RequireDocument.validateName('Admissão CLT'), isNull);
    });
  });

  group('RequireDocument.validateDescription', () {
    test('returns error when null or empty', () {
      expect(RequireDocument.validateDescription(null), isNotNull);
      expect(RequireDocument.validateDescription(''), isNotNull);
      expect(RequireDocument.validateDescription('   '), isNotNull);
    });

    test('returns error when exceeds 500 characters', () {
      expect(RequireDocument.validateDescription('A' * 501), isNotNull);
    });

    test('returns null for valid description', () {
      expect(
        RequireDocument.validateDescription('Documentos necessários para admissão.'),
        isNull,
      );
    });
  });

  group('RequireDocumentTemplate computed properties', () {
    test('hasDescription returns true when description is not empty', () {
      const t = RequireDocumentTemplate(
        id: '1',
        name: 'Contrato',
        description: 'Contrato de trabalho',
      );
      expect(t.hasDescription, isTrue);
    });

    test('hasDescription returns false when description is empty', () {
      const t = RequireDocumentTemplate(id: '1', name: 'Contrato');
      expect(t.hasDescription, isFalse);
    });
  });

  group('ListenEvent computed properties', () {
    const event = ListenEvent(
      eventId: 1,
      eventName: 'Admissão',
      statuses: [
        EventStatus(id: 1, name: 'Pendente'),
        EventStatus(id: 2, name: 'Ativo'),
      ],
    );

    const emptyEvent = ListenEvent(eventId: 2, eventName: 'Demissão');

    test('hasStatuses returns true when statuses list is not empty', () {
      expect(event.hasStatuses, isTrue);
      expect(emptyEvent.hasStatuses, isFalse);
    });

    test('statusCount returns the number of statuses', () {
      expect(event.statusCount, 2);
      expect(emptyEvent.statusCount, 0);
    });

    test('hasStatus returns true when status id is in list', () {
      expect(event.hasStatus(1), isTrue);
      expect(event.hasStatus(2), isTrue);
      expect(event.hasStatus(99), isFalse);
    });
  });
}
