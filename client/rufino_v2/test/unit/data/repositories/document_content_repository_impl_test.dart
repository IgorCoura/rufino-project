import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart' as http_testing;
import 'package:rufino_v2/core/errors/auth_exception.dart';
import 'package:rufino_v2/core/errors/document_content_exception.dart';
import 'package:rufino_v2/data/models/document_content_status_api_model.dart';
import 'package:rufino_v2/data/repositories/document_content_repository_impl.dart';
import 'package:rufino_v2/data/services/document_content_api_service.dart';
import 'package:rufino_v2/domain/entities/document_content_status.dart';

import '../../../testing/fakes/fake_error_reporter.dart';

void main() {
  late FakeErrorReporter reporter;
  Uri? capturedUri;
  String? capturedBody;

  const items = [
    DocumentUnitRefApiModel(
      documentUnitId: 'unit-1',
      documentId: 'doc-1',
      employeeId: 'emp-1',
    ),
  ];

  DocumentContentRepositoryImpl buildRepository({
    int statusCode = 200,
    String body = '{}',
  }) {
    reporter = FakeErrorReporter();
    final client = http_testing.MockClient((request) async {
      capturedUri = request.url;
      capturedBody = request.body;
      return http.Response(body, statusCode,
          headers: {'content-type': 'application/json'});
    });
    return DocumentContentRepositoryImpl(
      apiService: DocumentContentApiService(
        client: client,
        baseUrl: 'api.example.com',
        getAuthHeader: () async => 'Bearer token',
      ),
      reporter: reporter,
    );
  }

  group('DocumentContentRepositoryImpl.checkOutdated', () {
    test('returns one status per unit and posts the unit references',
        () async {
      final repository = buildRepository(
        body: jsonEncode({
          'items': [
            {
              'documentUnitId': 'unit-1',
              'isOutdated': true,
              'checkFailed': false,
            },
          ],
        }),
      );

      final result = await repository.checkOutdated('company-1', items);

      List<DocumentContentStatus> statuses = const [];
      result.fold(
        onSuccess: (value) => statuses = value,
        onError: (_, __) => fail('expected success'),
      );
      expect(statuses.single.documentUnitId, 'unit-1');
      expect(statuses.single.isOutdated, isTrue);
      expect(statuses.single.needsWarning, isTrue);
      expect(capturedUri!.path,
          '/api/v1/company-1/document/content/check-outdated');
      expect(
        jsonDecode(capturedBody!),
        {
          'items': [
            {
              'documentUnitId': 'unit-1',
              'documentId': 'doc-1',
              'employeeId': 'emp-1',
            },
          ],
        },
      );
    });

    test('treats an inconclusive check as something not to warn about',
        () async {
      final repository = buildRepository(
        body: jsonEncode({
          'items': [
            {
              'documentUnitId': 'unit-1',
              'isOutdated': false,
              'checkFailed': true,
            },
          ],
        }),
      );

      final result = await repository.checkOutdated('company-1', items);

      result.fold(
        onSuccess: (statuses) {
          expect(statuses.single.checkFailed, isTrue);
          expect(statuses.single.needsWarning, isFalse);
        },
        onError: (_, __) => fail('expected success'),
      );
    });

    test('wraps unexpected errors and reports them once', () async {
      final repository = buildRepository(statusCode: 500);

      final result = await repository.checkOutdated('company-1', items);

      result.fold(
        onSuccess: (_) => fail('expected error'),
        onError: (error, _) =>
            expect(error, isA<DocumentContentNetworkException>()),
      );
      expect(reporter.capturedErrors, hasLength(1));
    });

    test('does not report a wrapped session-expired failure', () async {
      final repository = buildRepository(statusCode: 401);

      final result = await repository.checkOutdated('company-1', items);

      result.fold(
        onSuccess: (_) => fail('expected error'),
        onError: (error, _) {
          final wrapped = error as DocumentContentNetworkException;
          expect(wrapped.cause, isA<SessionExpiredException>());
        },
      );
      expect(reporter.capturedErrors, isEmpty);
    });
  });

  group('DocumentContentRepositoryImpl.refresh', () {
    test('posts the unit references to the refresh endpoint', () async {
      final repository = buildRepository(body: jsonEncode({'updatedCount': 1}));

      final result = await repository.refresh('company-1', items);

      result.fold(
        onSuccess: (_) {},
        onError: (_, __) => fail('expected success'),
      );
      expect(capturedUri!.path, '/api/v1/company-1/document/content/refresh');
      expect(reporter.capturedErrors, isEmpty);
    });

    test('wraps unexpected errors and reports them once', () async {
      final repository = buildRepository(statusCode: 500);

      final result = await repository.refresh('company-1', items);

      result.fold(
        onSuccess: (_) => fail('expected error'),
        onError: (error, _) =>
            expect(error, isA<DocumentContentNetworkException>()),
      );
      expect(reporter.capturedErrors, hasLength(1));
    });

    test('does not report a permission failure the user can act on', () async {
      final repository = buildRepository(statusCode: 403);

      final result = await repository.refresh('company-1', items);

      result.fold(
        onSuccess: (_) => fail('expected error'),
        onError: (error, _) {
          final wrapped = error as DocumentContentNetworkException;
          expect(wrapped.cause, isA<AccessDeniedException>());
        },
      );
      expect(reporter.capturedErrors, isEmpty);
    });
  });
}
