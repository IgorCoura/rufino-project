import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart' as http_testing;
import 'package:oauth2/oauth2.dart' as oauth2;
import 'package:rufino_v2/core/errors/auth_exception.dart';
import 'package:rufino_v2/data/services/auth_api_service.dart';

import '../../../testing/fakes/fake_secure_storage.dart';
void main() {
  final tokenEndpoint = Uri.parse('https://keycloak.test/token');

  AuthApiService buildService({
    required FakeSecureStorage storage,
    http.Client? client,
    void Function()? onRefreshed,
  }) {
    return AuthApiService(
      storage: storage,
      authorizationEndpoint: tokenEndpoint,
      endSessionEndpoint: Uri.parse('https://keycloak.test/logout'),
      identifier: 'rufino-app',
      secret: 's3cret',
      httpClient: client,
      onTokenRefreshed: onRefreshed,
    );
  }

  FakeSecureStorage storageWith({
    Duration? untilExpiry,
    bool withRefreshToken = true,
  }) {
    final credentials = oauth2.Credentials(
      'old-access-token',
      refreshToken: withRefreshToken ? 'refresh-token' : null,
      tokenEndpoint: tokenEndpoint,
      expiration:
          untilExpiry == null ? null : DateTime.now().add(untilExpiry),
    );
    final storage = FakeSecureStorage();
    storage.values['credentials'] = credentials.toJson();
    return storage;
  }

  http.Client refreshOkClient() {
    return http_testing.MockClient((request) async {
      return http.Response(
        jsonEncode({
          'access_token': 'new-access-token',
          'refresh_token': 'new-refresh-token',
          'token_type': 'bearer',
          'expires_in': 300,
        }),
        200,
        headers: {'content-type': 'application/json'},
      );
    });
  }

  group('AuthApiService.getCredentials', () {
    test('refreshes the token before it expires when inside the safety margin',
        () async {
      var refreshed = false;
      final storage = storageWith(untilExpiry: const Duration(seconds: 30));
      final service = buildService(
        storage: storage,
        client: refreshOkClient(),
        onRefreshed: () => refreshed = true,
      );

      final credentials = await service.getCredentials();

      expect(credentials.accessToken, 'new-access-token');
      expect(refreshed, isTrue);
      expect(storage.values['credentials'], contains('new-access-token'));
    });

    test('throws SessionExpiredException when the token expired and there is no refresh token',
        () async {
      final storage = storageWith(
        untilExpiry: const Duration(minutes: -5),
        withRefreshToken: false,
      );
      final service = buildService(storage: storage);

      expect(
        service.getCredentials,
        throwsA(isA<SessionExpiredException>()),
      );
    });

    test('throws SessionExpiredException when the identity provider rejects the refresh token',
        () async {
      final storage = storageWith(untilExpiry: const Duration(minutes: -5));
      final service = buildService(
        storage: storage,
        client: http_testing.MockClient((request) async {
          return http.Response(
            jsonEncode({'error': 'invalid_grant'}),
            400,
            headers: {'content-type': 'application/json'},
          );
        }),
      );

      expect(
        service.getCredentials,
        throwsA(isA<SessionExpiredException>()),
      );
    });

    test('hits the token endpoint once when several callers refresh at the same time',
        () async {
      var calls = 0;
      final client = http_testing.MockClient((request) async {
        calls++;
        await Future<void>.delayed(const Duration(milliseconds: 10));
        return http.Response(
          jsonEncode({
            'access_token': 'new-access-token',
            'refresh_token': 'new-refresh-token',
            'token_type': 'bearer',
            'expires_in': 300,
          }),
          200,
          headers: {'content-type': 'application/json'},
        );
      });
      final storage = storageWith(untilExpiry: const Duration(seconds: 30));
      final service = buildService(storage: storage, client: client);

      final results = await Future.wait([
        service.getCredentials(),
        service.getCredentials(),
        service.getCredentials(),
      ]);

      expect(calls, 1);
      expect(
        results.map((c) => c.accessToken),
        everyElement('new-access-token'),
      );
    });

    test('refreshes again after a failed attempt instead of staying stuck',
        () async {
      var calls = 0;
      final client = http_testing.MockClient((request) async {
        calls++;
        throw http.ClientException('offline');
      });
      final storage = storageWith(untilExpiry: const Duration(seconds: 30));
      final service = buildService(storage: storage, client: client);

      await service.getCredentials();
      await service.getCredentials();

      expect(calls, 2);
    });
  });
}
