import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart' as http_testing;
import 'package:oauth2/oauth2.dart' as oauth2;
import 'package:rufino_v2/core/errors/auth_exception.dart';
import 'package:rufino_v2/data/services/auth_code_api_service.dart';
import 'package:rufino_v2/data/services/oauth_login_strategy.dart';

import '../../../testing/fakes/fake_secure_storage.dart';
/// Strategy stub for tests that never reach the browser dance.
class _UnusedStrategy implements OAuthLoginStrategy {
  @override
  Future<oauth2.Credentials> performLogin() => throw UnimplementedError();

  @override
  Future<void> performLogout({
    required Uri endSessionEndpoint,
    required String? idToken,
    required String? refreshToken,
  }) async {}
}

void main() {
  final tokenEndpoint = Uri.parse('https://keycloak.test/token');

  AuthCodeApiService buildService({
    required FakeSecureStorage storage,
    http.Client? client,
    void Function()? onRefreshed,
  }) {
    return AuthCodeApiService(
      storage: storage,
      strategy: _UnusedStrategy(),
      tokenEndpoint: tokenEndpoint,
      endSessionEndpoint: Uri.parse('https://keycloak.test/logout'),
      identifier: 'rufino-app',
      secret: 's3cret',
      httpClient: client,
      onTokenRefreshed: onRefreshed,
    );
  }

  oauth2.Credentials buildCredentials({
    Duration? untilExpiry,
    bool withRefreshToken = true,
  }) {
    return oauth2.Credentials(
      'old-access-token',
      refreshToken: withRefreshToken ? 'refresh-token' : null,
      tokenEndpoint: tokenEndpoint,
      expiration:
          untilExpiry == null ? null : DateTime.now().add(untilExpiry),
    );
  }

  FakeSecureStorage storageWith(oauth2.Credentials credentials) {
    final storage = FakeSecureStorage();
    storage.values['auth_code_credentials'] = credentials.toJson();
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

  http.Client refreshRejectedClient() {
    return http_testing.MockClient((request) async {
      return http.Response(
        jsonEncode({
          'error': 'invalid_grant',
          'error_description': 'Token is not active',
        }),
        400,
        headers: {'content-type': 'application/json'},
      );
    });
  }

  http.Client offlineClient() {
    return http_testing.MockClient(
      (request) async => throw http.ClientException('offline'),
    );
  }

  http.Client refusingClient() {
    return http_testing.MockClient((request) async {
      fail('The token endpoint must not be called for a healthy token.');
    });
  }

  group('AuthCodeApiService.getCredentials', () {
    test('returns the stored credentials untouched when the token is far from expiring',
        () async {
      final storage =
          storageWith(buildCredentials(untilExpiry: const Duration(minutes: 10)));
      final service = buildService(storage: storage, client: refusingClient());

      final credentials = await service.getCredentials();

      expect(credentials.accessToken, 'old-access-token');
    });

    test('refreshes the token before it expires when inside the safety margin',
        () async {
      var refreshed = false;
      final storage =
          storageWith(buildCredentials(untilExpiry: const Duration(seconds: 30)));
      final service = buildService(
        storage: storage,
        client: refreshOkClient(),
        onRefreshed: () => refreshed = true,
      );

      final credentials = await service.getCredentials();

      expect(credentials.accessToken, 'new-access-token');
      expect(refreshed, isTrue);
      expect(
        storage.values['auth_code_credentials'],
        contains('new-access-token'),
      );
    });

    test('refreshes an already expired token when a refresh token is available',
        () async {
      final storage = storageWith(
          buildCredentials(untilExpiry: const Duration(minutes: -5)));
      final service =
          buildService(storage: storage, client: refreshOkClient());

      final credentials = await service.getCredentials();

      expect(credentials.accessToken, 'new-access-token');
    });

    test('throws SessionExpiredException when the token expired and there is no refresh token',
        () async {
      final storage = storageWith(buildCredentials(
        untilExpiry: const Duration(minutes: -5),
        withRefreshToken: false,
      ));
      final service = buildService(storage: storage);

      expect(
        service.getCredentials,
        throwsA(isA<SessionExpiredException>()),
      );
    });

    test('throws SessionExpiredException when the identity provider rejects the refresh token',
        () async {
      final storage = storageWith(
          buildCredentials(untilExpiry: const Duration(minutes: -5)));
      final service =
          buildService(storage: storage, client: refreshRejectedClient());

      expect(
        service.getCredentials,
        throwsA(isA<SessionExpiredException>()),
      );
    });

    test('keeps the current token when the refresh fails at the network level and the token is still valid',
        () async {
      final storage =
          storageWith(buildCredentials(untilExpiry: const Duration(seconds: 30)));
      final service =
          buildService(storage: storage, client: offlineClient());

      final credentials = await service.getCredentials();

      expect(credentials.accessToken, 'old-access-token');
    });

    test('throws NetworkAuthException when the refresh fails at the network level and the token already expired',
        () async {
      final storage = storageWith(
          buildCredentials(untilExpiry: const Duration(minutes: -5)));
      final service =
          buildService(storage: storage, client: offlineClient());

      expect(
        service.getCredentials,
        throwsA(isA<NetworkAuthException>()),
      );
    });

    test('throws NoCredentialsException when nothing is stored', () async {
      final service = buildService(storage: FakeSecureStorage());

      expect(
        service.getCredentials,
        throwsA(isA<NoCredentialsException>()),
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
      final storage =
          storageWith(buildCredentials(untilExpiry: const Duration(seconds: 30)));
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
      final storage =
          storageWith(buildCredentials(untilExpiry: const Duration(seconds: 30)));
      final service = buildService(storage: storage, client: client);

      await service.getCredentials();
      await service.getCredentials();

      expect(calls, 2);
    });
  });

  group('AuthCodeApiService.hasValidCredentials', () {
    test('returns false when the session can no longer be refreshed', () async {
      final storage = storageWith(buildCredentials(
        untilExpiry: const Duration(minutes: -5),
        withRefreshToken: false,
      ));
      final service = buildService(storage: storage);

      expect(await service.hasValidCredentials(), isFalse);
    });

    test('returns true when the stored token is healthy', () async {
      final storage =
          storageWith(buildCredentials(untilExpiry: const Duration(minutes: 10)));
      final service = buildService(storage: storage, client: refusingClient());

      expect(await service.hasValidCredentials(), isTrue);
    });
  });
}
