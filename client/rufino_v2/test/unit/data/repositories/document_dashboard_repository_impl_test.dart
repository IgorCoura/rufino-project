import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart' as http_testing;
import 'package:rufino_v2/core/errors/auth_exception.dart';
import 'package:rufino_v2/core/errors/document_dashboard_exception.dart';
import 'package:rufino_v2/data/repositories/document_dashboard_repository_impl.dart';
import 'package:rufino_v2/data/services/document_dashboard_api_service.dart';
import 'package:rufino_v2/domain/entities/document_dashboard.dart';

import '../../../testing/fakes/fake_error_reporter.dart';

void main() {
  late FakeErrorReporter reporter;
  Uri? capturedUri;

  DocumentDashboardRepositoryImpl buildRepository({
    int statusCode = 200,
    String body = '{}',
  }) {
    reporter = FakeErrorReporter();
    final client = http_testing.MockClient((request) async {
      capturedUri = request.url;
      return http.Response(body, statusCode,
          headers: {'content-type': 'application/json'});
    });
    return DocumentDashboardRepositoryImpl(
      apiService: DocumentDashboardApiService(
        client: client,
        baseUrl: 'api.example.com',
        getAuthHeader: () async => 'Bearer token',
      ),
      reporter: reporter,
    );
  }

  group('DocumentDashboardRepositoryImpl.getSummary', () {
    test('returns the parsed summary and sends the horizon and filters',
        () async {
      final repository = buildRepository(
        body: jsonEncode({
          'expired': 1,
          'expiring': 2,
          'pending': 3,
          'awaitingSignature': 0,
          'requiresValidation': 0,
        }),
      );

      final result = await repository.getSummary(
        'company-1',
        expiringInDays: 60,
        employeeStatusId: 2,
        documentGroupId: 'group-1',
      );

      var summary = DashboardSummary.empty;
      result.fold(
        onSuccess: (value) => summary = value,
        onError: (_, __) => fail('expected success'),
      );
      expect(summary.expired, 1);
      expect(summary.expiring, 2);
      expect(summary.pending, 3);
      expect(capturedUri!.path,
          '/api/v1/company-1/document-dashboard/summary');
      expect(capturedUri!.queryParameters['ExpiringInDays'], '60');
      expect(capturedUri!.queryParameters['EmployeeStatusId'], '2');
      expect(capturedUri!.queryParameters['DocumentGroupId'], 'group-1');
    });

    test('wraps unexpected errors and reports them once', () async {
      final repository = buildRepository(statusCode: 500);

      final result = await repository.getSummary(
        'company-1',
        expiringInDays: 30,
      );

      result.fold(
        onSuccess: (_) => fail('expected error'),
        onError: (error, _) =>
            expect(error, isA<DocumentDashboardNetworkException>()),
      );
      expect(reporter.capturedErrors, hasLength(1));
    });

    test('does not report a wrapped session-expired failure', () async {
      final repository = buildRepository(statusCode: 401);

      final result = await repository.getSummary(
        'company-1',
        expiringInDays: 30,
      );

      result.fold(
        onSuccess: (_) => fail('expected error'),
        onError: (error, _) {
          final wrapped = error as DocumentDashboardNetworkException;
          expect(wrapped.cause, isA<SessionExpiredException>());
        },
      );
      expect(reporter.capturedErrors, isEmpty);
    });
  });

  group('DocumentDashboardRepositoryImpl.getUnits', () {
    test('returns the mapped page and sends the bucket and pagination',
        () async {
      final repository = buildRepository(
        body: jsonEncode({
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
              'validity': '2026-06-15',
              'status': {'id': 2, 'name': 'OK'},
              'period': null,
              'hasFile': true,
            },
          ],
          'totalCount': 7,
        }),
      );

      final result = await repository.getUnits(
        'company-1',
        bucket: DashboardBucket.expiring,
        expiringInDays: 30,
        pageSize: 10,
        pageNumber: 2,
      );

      DashboardUnitsPage? page;
      result.fold(
        onSuccess: (value) => page = value,
        onError: (_, __) => fail('expected success'),
      );
      expect(page!.totalCount, 7);
      expect(page!.items.single.validity, '15/06/2026');
      expect(capturedUri!.path, '/api/v1/company-1/document-dashboard/units');
      expect(capturedUri!.queryParameters['Bucket'], 'Expiring');
      expect(capturedUri!.queryParameters['PageSize'], '10');
      expect(capturedUri!.queryParameters['PageNumber'], '2');
    });

    test('wraps unexpected errors and reports them once', () async {
      final repository = buildRepository(statusCode: 500);

      final result = await repository.getUnits(
        'company-1',
        bucket: DashboardBucket.expired,
        expiringInDays: 30,
      );

      result.fold(
        onSuccess: (_) => fail('expected error'),
        onError: (error, _) =>
            expect(error, isA<DocumentDashboardNetworkException>()),
      );
      expect(reporter.capturedErrors, hasLength(1));
    });
  });
}
