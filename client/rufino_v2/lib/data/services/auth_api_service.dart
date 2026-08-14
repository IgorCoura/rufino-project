import 'dart:ui';

import 'package:http/http.dart' as http;
import 'package:jwt_decoder/jwt_decoder.dart';
import 'package:oauth2/oauth2.dart' as oauth2;

import 'package:rufino_core/rufino_core.dart';
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
    this.httpClient,
    this.onTokenRefreshed,
  });

  final SecureStorage storage;
  final Uri authorizationEndpoint;
  final Uri endSessionEndpoint;
  final String identifier;
  final String secret;

  /// Client used for the silent token refresh; when provided by the app it
  /// carries the monitoring breadcrumbs wrapper.
  final http.Client? httpClient;

  /// Called after a successful silent token refresh inside [getCredentials].
  VoidCallback? onTokenRefreshed;

  static const _credentialsKey = 'credentials';

  oauth2.Credentials? _credentials;

  Future<void> login({
    required String username,
    required String password,
  }) async {
    final oauth2.Client client;
    try {
      client = await oauth2.resourceOwnerPasswordGrant(
        authorizationEndpoint,
        username,
        password,
        identifier: identifier,
        secret: secret,
      );
    } on oauth2.AuthorizationException {
      throw const InvalidCredentialsException();
    }
    _credentials = client.credentials;

    await storage.write(
        key: _credentialsKey, value: client.credentials.toJson());
    client.close();
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
