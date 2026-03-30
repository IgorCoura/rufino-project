import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/data/models/batch_document_unit_api_model.dart';

void main() {
  group('BatchDocumentUnitApiModel', () {
    test('fromJson parses a complete JSON object correctly', () {
      final json = {
        'documentUnitId': 'unit-1',
        'documentId': 'doc-1',
        'employeeId': 'emp-1',
        'employeeName': 'João Silva',
        'employeeStatus': {'id': 2, 'name': 'Active'},
        'documentUnitDate': '2026-03-15',
        'documentUnitStatus': {'id': 1, 'name': 'Pending'},
        'period': {
          'type': {'id': 3, 'name': 'Monthly'},
          'day': null,
          'week': null,
          'month': 3,
          'year': 2026,
        },
        'isSignable': true,
        'canGenerateDocument': false,
      };

      final model = BatchDocumentUnitApiModel.fromJson(json);

      expect(model.documentUnitId, 'unit-1');
      expect(model.employeeName, 'João Silva');
      expect(model.employeeStatusId, 2);
      expect(model.documentUnitDate, '2026-03-15');
      expect(model.statusId, 1);
      expect(model.period, isNotNull);
      expect(model.period!.typeId, 3);
      expect(model.period!.month, 3);
      expect(model.period!.year, 2026);
      expect(model.isSignable, true);
      expect(model.canGenerateDocument, false);
    });

    test('toEntity converts API date format to display format', () {
      final model = BatchDocumentUnitApiModel.fromJson({
        'documentUnitId': 'unit-1',
        'documentId': 'doc-1',
        'employeeId': 'emp-1',
        'employeeName': 'Maria',
        'employeeStatus': {'id': 2, 'name': 'Active'},
        'documentUnitDate': '2026-03-15',
        'documentUnitStatus': {'id': 1, 'name': 'Pending'},
        'isSignable': false,
        'canGenerateDocument': false,
      });

      final entity = model.toEntity();

      expect(entity.date, '15/03/2026');
      expect(entity.statusId, '1');
      expect(entity.employeeStatusId, '2');
      expect(entity.isPending, true);
    });

    test('fromJson handles missing optional fields gracefully', () {
      final json = <String, dynamic>{
        'documentUnitId': 'unit-1',
        'documentId': 'doc-1',
        'employeeId': 'emp-1',
        'employeeName': 'Test',
        'documentUnitDate': '2026-01-01',
        'isSignable': false,
        'canGenerateDocument': false,
      };

      final model = BatchDocumentUnitApiModel.fromJson(json);

      expect(model.employeeStatusId, 0);
      expect(model.statusId, 0);
      expect(model.period, isNull);
    });
  });

  group('EmployeeMissingDocumentApiModel', () {
    test('fromJson parses correctly and toEntity converts types', () {
      final json = {
        'employeeId': 'emp-1',
        'employeeName': 'Carlos',
        'employeeStatus': {'id': 2, 'name': 'Active'},
      };

      final model = EmployeeMissingDocumentApiModel.fromJson(json);
      final entity = model.toEntity();

      expect(entity.employeeId, 'emp-1');
      expect(entity.employeeName, 'Carlos');
      expect(entity.employeeStatusId, '2');
    });
  });

  group('BatchDocumentUnitsResponse', () {
    test('fromJson parses items list and totalCount', () {
      final json = {
        'items': [
          {
            'documentUnitId': 'u1',
            'documentId': 'd1',
            'employeeId': 'e1',
            'employeeName': 'A',
            'documentUnitDate': '2026-01-01',
            'isSignable': false,
            'canGenerateDocument': false,
          },
        ],
        'totalCount': 42,
      };

      final response = BatchDocumentUnitsResponse.fromJson(json);

      expect(response.items.length, 1);
      expect(response.totalCount, 42);
    });

    test('fromJson returns empty list when items is null', () {
      final json = <String, dynamic>{'totalCount': 0};

      final response = BatchDocumentUnitsResponse.fromJson(json);

      expect(response.items, isEmpty);
      expect(response.totalCount, 0);
    });
  });

  group('InsertDocumentRangeResponse', () {
    test('fromJson parses results with success and failure items', () {
      final json = {
        'results': [
          {'documentUnitId': 'u1', 'success': true, 'errorMessage': null},
          {
            'documentUnitId': 'u2',
            'success': false,
            'errorMessage': 'File too large',
          },
        ],
      };

      final response = InsertDocumentRangeResponse.fromJson(json);

      expect(response.results.length, 2);
      expect(response.results[0].success, true);
      expect(response.results[1].success, false);
      expect(response.results[1].errorMessage, 'File too large');
    });
  });

  group('BatchCreateResponse', () {
    test('fromJson parses createdItems and toEntity converts them', () {
      final json = {
        'createdItems': [
          {
            'employeeId': 'e1',
            'documentId': 'd1',
            'documentUnitId': 'u1',
          },
        ],
      };

      final response = BatchCreateResponse.fromJson(json);

      expect(response.createdItems.length, 1);

      final entity = response.createdItems.first.toEntity();
      expect(entity.employeeId, 'e1');
      expect(entity.documentUnitId, 'u1');
    });
  });
}
