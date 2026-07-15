import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart' as http_testing;
import 'package:rufino_v2/data/repositories/document_template_repository_impl.dart';
import 'package:rufino_v2/data/services/document_template_api_service.dart';
import 'package:rufino_v2/domain/entities/document_template.dart';

import '../../../testing/fakes/fake_error_reporter.dart';

/// Asserts on the raw request body because that is the contract with the API:
/// a present `documentValidityDurationInDays` means "expiration rule active",
/// so sending zero for an absent rule silently creates a rule server-side.
void main() {
  late Map<String, dynamic> capturedBody;

  DocumentTemplateRepositoryImpl buildRepository() {
    final client = http_testing.MockClient((request) async {
      capturedBody = jsonDecode(request.body) as Map<String, dynamic>;
      return http.Response(jsonEncode({'id': 'tpl-1'}), 200);
    });
    return DocumentTemplateRepositoryImpl(
      apiService: DocumentTemplateApiService(
        client: client,
        baseUrl: 'api.example.com',
        getAuthHeader: () async => 'Bearer token',
      ),
      reporter: FakeErrorReporter(),
    );
  }

  Future<void> create(
    DocumentTemplateRepositoryImpl repository,
    TemplatePolicies policies,
  ) async {
    await repository.createDocumentTemplate(
      'company-1',
      name: 'NR01',
      description: 'Descrição',
      policies: policies,
      usePreviousPeriod: false,
      acceptsSignature: false,
    );
  }

  group('DocumentTemplateRepositoryImpl', () {
    test('createDocumentTemplate sends null legacy fields when no rule is active',
        () async {
      final repository = buildRepository();

      await create(repository, const TemplatePolicies());

      expect(capturedBody['documentValidityDurationInDays'], isNull);
      expect(capturedBody['workloadInHours'], isNull);
    });

    test('createDocumentTemplate mirrors the active rules into the legacy fields',
        () async {
      final repository = buildRepository();

      await create(
        repository,
        const TemplatePolicies(
          expiration: ExpirationRule(durationInDays: 365),
          workload: WorkloadRule(hours: 8),
        ),
      );

      expect(capturedBody['documentValidityDurationInDays'], 365);
      expect(capturedBody['workloadInHours'], 8);
    });

    test('createDocumentTemplate sends the policies block alongside the mirror',
        () async {
      final repository = buildRepository();

      await create(
        repository,
        const TemplatePolicies(
          expiration: ExpirationRule(durationInDays: 365),
          workload: WorkloadRule(hours: 8),
        ),
      );

      final policies = capturedBody['policies'] as Map<String, dynamic>;
      expect(policies['expiration'], {'durationInDays': 365});
      expect(policies['workload'], {'hours': 8});
    });

    test('createDocumentTemplate sends an empty policies block to clear rules',
        () async {
      final repository = buildRepository();

      await create(repository, const TemplatePolicies());

      final policies = capturedBody['policies'] as Map<String, dynamic>;
      expect(policies['expiration'], isNull);
      expect(policies['workload'], isNull);
    });

    test('createDocumentTemplate omits a rule that is not active', () async {
      final repository = buildRepository();

      await create(
        repository,
        const TemplatePolicies(workload: WorkloadRule(hours: 8)),
      );

      final policies = capturedBody['policies'] as Map<String, dynamic>;
      expect(policies['expiration'], isNull);
      expect(policies['workload'], {'hours': 8});
      expect(capturedBody['documentValidityDurationInDays'], isNull);
    });

    test('updateDocumentTemplate mirrors the active rules into the legacy fields',
        () async {
      final repository = buildRepository();

      final result = await repository.updateDocumentTemplate(
        'company-1',
        id: 'tpl-1',
        name: 'NR01',
        description: 'Descrição',
        policies: const TemplatePolicies(
          expiration: ExpirationRule(durationInDays: 30),
        ),
        usePreviousPeriod: false,
        acceptsSignature: false,
      );

      expect(result.isSuccess, isTrue);
      expect(capturedBody['documentValidityDurationInDays'], 30);
      expect(capturedBody['workloadInHours'], isNull);
      expect((capturedBody['policies'] as Map<String, dynamic>)['expiration'],
          {'durationInDays': 30});
    });

    test('getDocumentTemplateById maps the policies block to the entity',
        () async {
      final client = http_testing.MockClient((_) async {
        return http.Response(
          jsonEncode({
            'id': 'tpl-1',
            'name': 'NR01',
            'description': 'Descrição',
            'policies': {
              'expiration': {'durationInDays': 365},
              'workload': null,
            },
          }),
          200,
        );
      });
      final repository = DocumentTemplateRepositoryImpl(
        apiService: DocumentTemplateApiService(
          client: client,
          baseUrl: 'api.example.com',
          getAuthHeader: () async => 'Bearer token',
        ),
        reporter: FakeErrorReporter(),
      );

      final result = await repository.getDocumentTemplateById('company-1', 'tpl-1');

      final template = result.valueOrNull!;
      expect(template.validityInDays, 365);
      expect(template.hasWorkload, isFalse);
    });

    test('reports unexpected exception through the reporter', () async {
      final reporter = FakeErrorReporter();
      final client = http_testing.MockClient((_) async {
        return http.Response('boom', 500);
      });
      final repository = DocumentTemplateRepositoryImpl(
        apiService: DocumentTemplateApiService(
          client: client,
          baseUrl: 'api.example.com',
          getAuthHeader: () async => 'Bearer token',
        ),
        reporter: reporter,
      );

      final result = await repository.createDocumentTemplate(
        'company-1',
        name: 'NR01',
        description: 'Descrição',
        policies: const TemplatePolicies(),
        usePreviousPeriod: false,
        acceptsSignature: false,
      );

      expect(result.isError, isTrue);
      expect(reporter.capturedErrors, hasLength(1));
    });
  });
}
