import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/data/models/employee_document_api_model.dart';

void main() {
  group('DocumentUnitApiModel.fromJson', () {
    test('parses period when present in JSON', () {
      final json = <String, dynamic>{
        'id': 'unit-1',
        'status': {'id': 2, 'name': 'OK'},
        'date': '2026-03-15',
        'validity': '2027-03-15',
        'createAt': '2026-01-10',
        'content': '',
        'name': 'holerite.pdf',
        'extension': 'pdf',
        'period': {
          'type': {'id': 3, 'name': 'Monthly'},
          'day': null,
          'week': null,
          'month': 3,
          'year': 2026,
        },
      };

      final model = DocumentUnitApiModel.fromJson(json);

      expect(model.period, isNotNull);
      expect(model.period!.typeId, 3);
      expect(model.period!.typeName, 'Monthly');
      expect(model.period!.month, 3);
      expect(model.period!.year, 2026);
    });

    test('sets period to null when absent in JSON', () {
      final json = <String, dynamic>{
        'id': 'unit-2',
        'status': {'id': 1, 'name': 'Pendente'},
        'date': '2026-03-15',
        'validity': '',
        'createAt': '2026-01-10',
        'content': '',
        'name': '',
        'extension': '',
      };

      final model = DocumentUnitApiModel.fromJson(json);

      expect(model.period, isNull);
    });

    test('toEntity converts period to domain entity', () {
      final json = <String, dynamic>{
        'id': 'unit-3',
        'status': {'id': 2, 'name': 'OK'},
        'date': '2026-06-01',
        'validity': '',
        'createAt': '2026-01-10',
        'content': '',
        'name': 'doc.pdf',
        'extension': 'pdf',
        'period': {
          'type': {'id': 1, 'name': 'Daily'},
          'day': 15,
          'week': null,
          'month': 6,
          'year': 2026,
        },
      };

      final entity = DocumentUnitApiModel.fromJson(json).toEntity();

      expect(entity.period, isNotNull);
      expect(entity.period!.isDaily, isTrue);
      expect(entity.period!.day, 15);
      expect(entity.period!.formattedPeriod, '15/06/2026');
    });

    test('toEntity sets period to null when not in JSON', () {
      final json = <String, dynamic>{
        'id': 'unit-4',
        'status': {'id': 1, 'name': 'Pendente'},
        'date': '',
        'validity': '',
        'createAt': '',
        'content': '',
        'name': '',
        'extension': '',
      };

      final entity = DocumentUnitApiModel.fromJson(json).toEntity();

      expect(entity.period, isNull);
    });
  });

  group('EmployeeDocumentApiModel.fromJson', () {
    test('parses units with period fields', () {
      final json = <String, dynamic>{
        'id': 'doc-1',
        'name': 'Holerite',
        'description': 'Monthly payslip',
        'status': {'id': 1, 'name': 'OK'},
        'isSignable': false,
        'canGenerateDocument': true,
        'usePreviousPeriod': false,
        'totalUnitsCount': 1,
        'documentsUnits': [
          {
            'id': 'unit-1',
            'status': {'id': 2, 'name': 'OK'},
            'date': '2026-03-01',
            'validity': '',
            'createAt': '2026-01-01',
            'content': '',
            'name': 'holerite.pdf',
            'extension': 'pdf',
            'period': {
              'type': {'id': 3, 'name': 'Monthly'},
              'month': 3,
              'year': 2026,
            },
          },
        ],
      };

      final entity = EmployeeDocumentApiModel.fromJson(json).toEntity();

      expect(entity.units.first.period, isNotNull);
      expect(entity.units.first.period!.isMonthly, isTrue);
      expect(entity.units.first.period!.formattedPeriod, 'Mar/2026');
    });

    test('reads the competency granularity configured in the template', () {
      final json = <String, dynamic>{
        'id': 'doc-1',
        'name': 'Holerite',
        'description': '',
        'status': {'id': 1, 'name': 'OK'},
        'isSignable': false,
        'canGenerateDocument': true,
        'usePreviousPeriod': false,
        'periodTypeId': 3,
        'totalUnitsCount': 0,
        'documentsUnits': <dynamic>[],
      };

      final entity = EmployeeDocumentApiModel.fromJson(json).toEntity();

      expect(entity.periodTypeId, 3);
      expect(entity.isByCompetency, isTrue);
    });

    test('reads a document without competency when the API omits the type', () {
      final json = <String, dynamic>{
        'id': 'doc-2',
        'name': 'Contrato',
        'description': '',
        'status': {'id': 1, 'name': 'OK'},
        'isSignable': false,
        'canGenerateDocument': true,
        'usePreviousPeriod': false,
        'totalUnitsCount': 0,
        'documentsUnits': <dynamic>[],
      };

      final entity = EmployeeDocumentApiModel.fromJson(json).toEntity();

      expect(entity.periodTypeId, isNull);
      expect(entity.isByCompetency, isFalse);
    });

    // A lista de documentos do perfil não traz a granularidade — só o detalhe
    // traz. Ler "sem competência" ali é o esperado, não uma perda de dado.
    test('a document from the list endpoint has no competency granularity', () {
      final entity = EmployeeDocumentApiModel.fromJsonSimple(<String, dynamic>{
        'id': 'doc-3',
        'name': 'Holerite',
        'description': '',
        'status': {'id': 1, 'name': 'OK'},
        'usePreviousPeriod': true,
      }).toEntity();

      expect(entity.isByCompetency, isFalse);
    });
  });

  group('EmployeeDocumentApiModel scheduled signature send', () {
    Map<String, dynamic> documentJson({
      String? suggestedDate,
      String? scheduledSendOn,
    }) =>
        <String, dynamic>{
          'id': 'doc-1',
          'name': 'Holerite',
          'description': '',
          'status': {'id': 1, 'name': 'OK'},
          'isSignable': true,
          'canGenerateDocument': true,
          'usePreviousPeriod': false,
          'totalUnitsCount': 1,
          if (suggestedDate != null)
            'suggestedSignatureScheduleDate': suggestedDate,
          'documentsUnits': [
            {
              'id': 'unit-1',
              'status': {'id': 1, 'name': 'Pendente'},
              'date': '2026-03-01',
              'validity': '',
              'createAt': '2026-01-01',
              'content': '',
              'name': '',
              'extension': '',
              if (scheduledSendOn != null)
                'scheduledSignatureSendOn': scheduledSendOn,
            },
          ],
        };

    test('converts the suggested schedule date to the display format', () {
      final entity = EmployeeDocumentApiModel
          .fromJson(documentJson(suggestedDate: '2026-06-30'))
          .toEntity();

      expect(entity.suggestedSignatureScheduleDate, '30/06/2026');
      expect(entity.hasSuggestedSignatureScheduleDate, isTrue);
    });

    test('leaves the suggested schedule date empty when the API omits it', () {
      final entity =
          EmployeeDocumentApiModel.fromJson(documentJson()).toEntity();

      expect(entity.suggestedSignatureScheduleDate, isEmpty);
      expect(entity.hasSuggestedSignatureScheduleDate, isFalse);
    });

    test('converts the unit scheduled send date to the display format', () {
      final entity = EmployeeDocumentApiModel
          .fromJson(documentJson(scheduledSendOn: '2026-06-30'))
          .toEntity();

      expect(entity.units.first.scheduledSignatureSendOn, '30/06/2026');
      expect(entity.units.first.isSignatureScheduled, isTrue);
    });

    test('reads a unit without a scheduled send as not scheduled', () {
      final entity =
          EmployeeDocumentApiModel.fromJson(documentJson()).toEntity();

      expect(entity.units.first.isSignatureScheduled, isFalse);
    });
  });
}
