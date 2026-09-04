import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:rufino_core/rufino_core.dart';

import '../fakes/fakes.dart';

/// The UMA call that asks Keycloak what the current user may do on one
/// resource server.
///
/// The load-bearing rule is the 403: a user who holds no permission at all on
/// an audience is a normal user, not a failure. Turning that into an exception
/// would put everyone who uses only the other product in front of an error
/// screen.
void main() {
  final tokenEndpoint = Uri.parse(
    'https://keycloak.test/realms/rufino/protocol/openid-connect/token',
  );

  late FakeHttpClient client;

  PermissionApiService serviceReturning(
    int status, {
    String body = '[]',
    String audience = 'people-management-api',
    Future<String> Function()? getAccessToken,
  }) {
    client = FakeHttpClient((_) async => http.Response(body, status));
    return PermissionApiService(
      client: client,
      tokenEndpoint: tokenEndpoint,
      getAccessToken: getAccessToken ?? (() async => 'access-token'),
      audience: audience,
    );
  }

  group('PermissionApiService when the user has permissions', () {
    test('returns one permission per authorized resource', () async {
      final service = serviceReturning(
        200,
        body: jsonEncode([
          {
            'rsname': 'employee',
            'scopes': ['view', 'create'],
          },
          {
            'rsname': 'department',
            'scopes': ['view'],
          },
        ]),
      );

      final permissions = await service.fetchPermissions();

      expect(permissions, hasLength(2));
      expect(permissions[0].resource, 'employee');
      expect(permissions[0].scopes, ['view', 'create']);
      expect(permissions[1].resource, 'department');
    });

    test('treats a resource returned without scopes as granting none',
        () async {
      final service = serviceReturning(
        200,
        body: jsonEncode([
          {'rsname': 'employee'},
        ]),
      );

      final permissions = await service.fetchPermissions();

      expect(permissions.single.resource, 'employee');
      expect(permissions.single.scopes, isEmpty);
    });

    test('returns an empty list when Keycloak answers with no resources',
        () async {
      final service = serviceReturning(200, body: '[]');

      expect(await service.fetchPermissions(), isEmpty);
    });
  });

  group('PermissionApiService request', () {
    test('asks the configured token endpoint with the UMA ticket grant',
        () async {
      final service = serviceReturning(200, audience: 'tenant-management-api');

      await service.fetchPermissions();

      final request = client.requests.single as http.Request;
      expect(request.method, 'POST');
      expect(request.url, tokenEndpoint);
      expect(
        request.bodyFields['grant_type'],
        'urn:ietf:params:oauth:grant-type:uma-ticket',
      );
      expect(request.bodyFields['audience'], 'tenant-management-api');
      expect(request.bodyFields['response_mode'], 'permissions');
    });

    test('carries the current access token as a bearer credential', () async {
      final service = serviceReturning(
        200,
        getAccessToken: () async => 'fresh-token',
      );

      await service.fetchPermissions();

      expect(
        client.requests.single.headers['Authorization'],
        'Bearer fresh-token',
      );
    });

    test('reads the access token once per fetch, at call time', () async {
      var reads = 0;
      final service = serviceReturning(
        200,
        getAccessToken: () async {
          reads++;
          return 'token-$reads';
        },
      );

      await service.fetchPermissions();

      expect(reads, 1);
    });
  });

  group('PermissionApiService when the user has no permissions', () {
    test('reads a 403 as an empty permission set rather than a failure',
        () async {
      final service = serviceReturning(403, body: '{"error":"access_denied"}');

      final permissions = await service.fetchPermissions();

      expect(permissions, isEmpty);
    });

    test('does not raise anything at all on a 403', () async {
      final service = serviceReturning(403);

      await expectLater(service.fetchPermissions(), completes);
    });
  });

  group('PermissionApiService when the request fails', () {
    test('raises a permission fetch failure on 401', () async {
      final service = serviceReturning(401);

      expect(
        service.fetchPermissions(),
        throwsA(isA<PermissionFetchException>()),
      );
    });

    test('raises a permission fetch failure on a server error', () async {
      final service = serviceReturning(500);

      expect(
        service.fetchPermissions(),
        throwsA(isA<PermissionFetchException>()),
      );
    });

    test('names the status it failed on so the report is actionable',
        () async {
      final service = serviceReturning(500);

      try {
        await service.fetchPermissions();
        fail('expected a PermissionFetchException');
      } on PermissionFetchException catch (e) {
        expect(e.cause.toString(), contains('500'));
      }
    });

    test('lets the token lookup failure surface untouched', () async {
      final service = serviceReturning(
        200,
        getAccessToken: () async => throw const NoCredentialsException(),
      );

      expect(
        service.fetchPermissions(),
        throwsA(isA<NoCredentialsException>()),
      );
    });
  });
}
