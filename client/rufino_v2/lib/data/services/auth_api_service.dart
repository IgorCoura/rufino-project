import 'dart:ui';

import 'package:jwt_decoder/jwt_decoder.dart';
import 'package:oauth2/oauth2.dart' as oauth2;

import '../../core/errors/auth_exception.dart';
import '../../core/storage/secure_storage.dart';

/// Handles OAuth2 authentication against Keycloak.
///
/// Manages login, token refresh, and credential persistence via
/// [SecureStorage]. When a silent token refresh occurs, [onTokenRefreshed]
/// is called so that dependents (e.g. permission reload) can react.
class AuthApiService {
  AuthApiService({
    required this.storage,
    required this.authorizationEndpoint,
    required this.endSessionEndpoint,
    required this.identifier,
    required this.secret,
    this.onTokenRefreshed,
  });

  final SecureStorage storage;
  final Uri authorizationEndpoint;
  final Uri endSessionEndpoint;
  final String identifier;
  final String secret;

  /// Called after a successful silent token refresh inside [getCredentials].
  VoidCallback? onTokenRefreshed;

  static const _credentialsKey = 'credentials';

  oauth2.Credentials? _credentials;

  Future<void> login({
    required String username,
    required String password,
  }) async {
    final client = await oauth2.resourceOwnerPasswordGrant(
      authorizationEndpoint,
      username,
      password,
      identifier: identifier,
      secret: secret,
    );
    _credentials = client.credentials;

    await storage.write(
        key: _credentialsKey, value: client.credentials.toJson());
    client.close();
  }

  Future<oauth2.Credentials> getCredentials() async {
    await _recoverCredentials();
    var credentials = _credentials;
    if (credentials == null) throw const NoCredentialsException();

    if (credentials.isExpired) {
      if (!credentials.canRefresh) {
        throw const SessionExpiredException();
      }
      credentials = await credentials.refresh(
        identifier: identifier,
        secret: secret,
      );
      _credentials = credentials;
      await storage.write(key: _credentialsKey, value: credentials.toJson());
      onTokenRefreshed?.call();
    }
    return credentials;
  }

  Future<String> getAuthorizationHeader() async {
    final credentials = await getCredentials();
    return 'Bearer ${credentials.accessToken}';
  }

  Future<List<String>> getCompanyIds() async {
    final credentials = await getCredentials();
    try {
      final decoded = JwtDecoder.decode(credentials.accessToken);
      final raw = decoded['companies'];
      if (raw == null) return [];
      return (raw as List<dynamic>).map((e) => e.toString()).toList();
    } catch (_) {
      throw const SessionExpiredException();
    }
  }

  Future<bool> hasValidCredentials() async {
    try {
      await getCredentials();
      return true;
    } catch (_) {
      return false;
    }
  }

  Future<void> logout() async {
    try {
      final credentials = _credentials;
      if (credentials != null) {
        final client = oauth2.Client(credentials);
        await client.post(endSessionEndpoint, body: {
          'client_id': identifier,
          'client_secret': secret,
          'refresh_token': credentials.refreshToken ?? '',
        });
        client.close();
      }
    } catch (_) {
      // Ignore errors on logout
    } finally {
      await storage.delete(key: _credentialsKey);
      _credentials = null;
    }
  }

  Future<void> _recoverCredentials() async {
    if (_credentials != null) return;
    final json = await storage.read(key: _credentialsKey);
    if (json == null) throw const NoCredentialsException();
    _credentials = oauth2.Credentials.fromJson(json);
  }
}
