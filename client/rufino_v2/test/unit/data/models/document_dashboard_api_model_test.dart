import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/data/models/document_dashboard_api_model.dart';

void main() {
  group('DashboardSummaryApiModel', () {
    test('parses every bucket count from the API JSON', () {
      final model = DashboardSummaryApiModel.fromJson(const {
        'expired': 3,
        'expiring': 2,
        'pending': 5,
        'awaitingSignature': 1,
        'requiresValidation': 4,
      });

      final entity = model.toEntity();
      expect(entity.expired, 3);
      expect(entity.expiring, 2);
      expect(entity.pending, 5);
      expect(entity.awaitingSignature, 1);
      expect(entity.requiresValidation, 4);
    });

    test('defaults missing counts to zero', () {
      final entity = DashboardSummaryApiModel.fromJson(const {}).toEntity();

      expect(entity.total, 0);
    });
  });

  group('DashboardUnitApiModel', () {
    final json = {
      'documentUnitId': 'unit-1',
      'documentId': 'doc-1',
      'employeeId': 'emp-1',
      'employeeName': 'Maria da Silva',
      'employeeStatus': {'id': 2, 'name': 'Active'},
      'documentTemplateName': 'ASO',
      'documentGroupName': 'Saúde',
      'date': '2026-03-01',
      'validity': '2026-06-15',
      'status': {'id': 8, 'name': 'Warning'},
      'period': {
        'type': {'id': 3, 'name': 'Monthly'},
        'month': 3,
        'year': 2026,
      },
      'hasFile': true,
    };

    test('parses the unit row and converts dates to display format', () {
      final entity = DashboardUnitApiModel.fromJson(json).toEntity();

      expect(entity.documentUnitId, 'unit-1');
      expect(entity.documentId, 'doc-1');
      expect(entity.employeeId, 'emp-1');
      expect(entity.employeeName, 'Maria da Silva');
      expect(entity.employeeStatusId, '2');
      expect(entity.documentTemplateName, 'ASO');
      expect(entity.documentGroupName, 'Saúde');
      expect(entity.date, '01/03/2026');
      expect(entity.validity, '15/06/2026');
      expect(entity.statusId, '8');
      expect(entity.statusLabel, 'A Vencer');
      expect(entity.period, isNotNull);
      expect(entity.hasFile, isTrue);
    });

    test('maps a null validity to an empty display string', () {
      final entity = DashboardUnitApiModel.fromJson({
        ...json,
        'validity': null,
      }).toEntity();

      expect(entity.validity, '');
      expect(entity.hasValidity, isFalse);
    });

    test('maps a missing period to a null entity period', () {
      final entity = DashboardUnitApiModel.fromJson({
        ...json,
        'period': null,
      }).toEntity();

      expect(entity.period, isNull);
    });
  });

  group('DashboardUnitsResponse', () {
    test('parses the paginated envelope with items and totalCount', () {
      final response = DashboardUnitsResponse.fromJson(const {
        'items': [
          {
            'documentUnitId': 'unit-1',
            'documentId': 'doc-1',
            'employeeId': 'emp-1',
            'employeeName': 'Maria',
            'employeeStatus': {'id': 2, 'name': 'Active'},
            'documentTemplateName': 'ASO',
            'documentGroupName': 'Saúde',
            'date': '2026-03-01',
            'validity': null,
            'status': {'id': 1, 'name': 'Pending'},
            'period': null,
            'hasFile': false,
          },
        ],
        'totalCount': 12,
      });

      expect(response.items, hasLength(1));
      expect(response.totalCount, 12);
    });

    test('defaults a missing items list to empty', () {
      final response = DashboardUnitsResponse.fromJson(const {});

      expect(response.items, isEmpty);
      expect(response.totalCount, 0);
    });
  });
}
