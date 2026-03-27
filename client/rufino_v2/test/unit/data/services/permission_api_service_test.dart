import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart' as http_testing;
import 'package:rufino_v2/core/errors/permission_exception.dart';
import 'package:rufino_v2/data/services/permission_api_service.dart';

void main() {
  const tokenEndpoint = 'https://keycloak.example.com/token';
  const audience = 'people-management-api';
  const fakeAccessToken = 'fake-access-token';

  late PermissionApiService service;

  PermissionApiService buildService(http.Client client) {
    return PermissionApiService(
      client: client,
      tokenEndpoint: Uri.parse(tokenEndpoint),
      getAccessToken: () async => fakeAccessToken,
      audience: audience,
    );
  }

  group('PermissionApiService', () {
    test('fetchPermissions returns parsed permissions on success', () async {
      final mockClient = http_testing.MockClient((request) async {
        expect(request.url.toString(), tokenEndpoint);
        expect(request.method, 'POST');
        expect(
          request.headers['Authorization'],
          'Bearer $fakeAccessToken',
        );
        expect(request.bodyFields['grant_type'],
            'urn:ietf:params:oauth:grant-type:uma-ticket');
        expect(request.bodyFields['audience'], audience);
        expect(request.bodyFields['response_mode'], 'permissions');

        return http.Response(
          jsonEncode([
            {
              'rsname': 'employee',
              'scopes': ['create', 'view', 'edit'],
            },
            {
              'rsname': 'Document',
              'scopes': ['view'],
            },
          ]),
          200,
        );
      });

      service = buildService(mockClient);
      final permissions = await service.fetchPermissions();

      expect(permissions, hasLength(2));
      expect(permissions[0].resource, 'employee');
      expect(permissions[0].scopes, ['create', 'view', 'edit']);
      expect(permissions[1].resource, 'Document');
      expect(permissions[1].scopes, ['view']);
    });

    test('fetchPermissions handles permissions without scopes', () async {
      final mockClient = http_testing.MockClient((request) async {
        return http.Response(
          jsonEncode([
            {'rsname': 'employee'},
          ]),
          200,
        );
      });

      service = buildService(mockClient);
      final permissions = await service.fetchPermissions();

      expect(permissions, hasLength(1));
      expect(permissions[0].resource, 'employee');
      expect(permissions[0].scopes, isEmpty);
    });

    test('fetchPermissions throws PermissionFetchException on non-2xx',
        () async {
      final mockClient = http_testing.MockClient((request) async {
        return http.Response('Forbidden', 403);
      });

      service = buildService(mockClient);

      expect(
        () => service.fetchPermissions(),
        throwsA(isA<PermissionFetchException>()),
      );
    });

    test('fetchPermissions returns empty list on empty response', () async {
      final mockClient = http_testing.MockClient((request) async {
        return http.Response(jsonEncode([]), 200);
      });

      service = buildService(mockClient);
      final permissions = await service.fetchPermissions();

      expect(permissions, isEmpty);
    });
  });
}
