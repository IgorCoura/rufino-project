import 'dart:ui';

import 'package:http/http.dart' as http;
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
    this.httpClient,
    this.onTokenRefreshed,
  });

  final SecureStorage storage;
  final OAuthLoginStrategy strategy;
  final Uri tokenEndpoint;
  final Uri endSessionEndpoint;
  final String identifier;
  final String? secret;

  /// Client used for the silent token refresh; when provided by the app it
  /// carries the monitoring breadcrumbs wrapper.
  final http.Client? httpClient;

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

  /// Safety margin subtracted from the token expiry: the API validates
  /// lifetimes with near-zero clock skew, so a token about to die must be
  /// refreshed before it is attached to a request.
  static const tokenExpiryMargin = Duration(seconds: 60);

  Future<oauth2.Credentials> getCredentials() async {
    await _recoverCredentials();
    final credentials = _credentials;
    if (credentials == null) throw const NoCredentialsException();

    if (!_shouldRefresh(credentials)) {
      if (credentials.isExpired) throw const SessionExpiredException();
      return credentials;
    }

    final oauth2.Credentials refreshed;
    try {
      refreshed = await credentials.refresh(
        identifier: identifier,
        secret: secret,
        httpClient: httpClient,
      );
    } on oauth2.AuthorizationException {
      throw const SessionExpiredException();
    } catch (e) {
      // A transient failure (network, CORS) must not kill a still-valid
      // session; only give up when the token is truly gone.
      if (credentials.isExpired) throw NetworkAuthException(e);
      return credentials;
    }

    _credentials = refreshed;
    await storage.write(key: _credentialsKey, value: refreshed.toJson());
    onTokenRefreshed?.call();
    return refreshed;
  }

  /// Whether [credentials] should be refreshed now: already expired or
  /// inside [tokenExpiryMargin] of expiring, and refreshable at all.
  bool _shouldRefresh(oauth2.Credentials credentials) {
    if (!credentials.canRefresh) return false;
    final expiration = credentials.expiration;
    if (expiration == null) return false;
    return DateTime.now()
        .isAfter(expiration.subtract(tokenExpiryMargin));
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

  /// Drops the locally stored credentials without contacting the identity
  /// provider — used when the session is already known to be dead.
  Future<void> clearLocalSession() async {
    await storage.delete(key: _credentialsKey);
    _credentials = null;
  }

  Future<void> _recoverCredentials() async {
    if (_credentials != null) return;
    final json = await storage.read(key: _credentialsKey);
    if (json == null) throw const NoCredentialsException();
    _credentials = oauth2.Credentials.fromJson(json);
  }
}
