import 'dart:ui';

import 'package:jwt_decoder/jwt_decoder.dart';
import 'package:oauth2/oauth2.dart' as oauth2;

import '../../core/errors/auth_exception.dart';
import '../../core/storage/secure_storage.dart';
import 'oauth_login_strategy.dart';

/// Authorization Code Flow + PKCE counterpart to [AuthApiService].
///
/// Holds the same public surface — `login`, `getCredentials`,
/// `getAuthorizationHeader`, `getCompanyIds`, `hasValidCredentials`,
/// `logout`, plus the `onTokenRefreshed` callback — so the rest of the
/// app does not need to know which flow is active. The actual browser
/// dance is delegated to an [OAuthLoginStrategy].
class AuthCodeApiService {
  AuthCodeApiService({
    required this.storage,
    required this.strategy,
    required this.tokenEndpoint,
    required this.endSessionEndpoint,
    required this.identifier,
    required this.secret,
    this.onTokenRefreshed,
  });

  final SecureStorage storage;
  final OAuthLoginStrategy strategy;
  final Uri tokenEndpoint;
  final Uri endSessionEndpoint;
  final String identifier;
  final String? secret;

  /// Called after a successful silent token refresh inside [getCredentials].
  VoidCallback? onTokenRefreshed;

  static const _credentialsKey = 'auth_code_credentials';

  oauth2.Credentials? _credentials;

  /// Seeds the in-memory credentials from a previously-completed
  /// redirect (Web only). Persists them so the next app launch picks
  /// them up via [_recoverCredentials].
  Future<void> primeCredentials(oauth2.Credentials credentials) async {
    _credentials = credentials;
    await storage.write(key: _credentialsKey, value: credentials.toJson());
  }

  Future<void> login() async {
    final oauth2.Credentials credentials;
    try {
      credentials = await strategy.performLogin();
    } on AuthException {
      rethrow;
    } catch (e) {
      throw NetworkAuthException(e);
    }

    _credentials = credentials;
    await storage.write(key: _credentialsKey, value: credentials.toJson());
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
    final idToken = _credentials?.idToken;
    final refreshToken = _credentials?.refreshToken;
    try {
      await strategy.performLogout(
        endSessionEndpoint: endSessionEndpoint,
        idToken: idToken,
        refreshToken: refreshToken,
      );
    } catch (_) {
      // Best-effort: even if the SSO end-session call fails, drop the
      // local credentials so the user is logged out of this app.
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
