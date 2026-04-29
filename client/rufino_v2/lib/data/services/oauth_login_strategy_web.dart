import 'dart:async';
import 'dart:convert';
import 'dart:math';

import 'package:oauth2/oauth2.dart' as oauth2;
import 'package:shared_preferences/shared_preferences.dart';
import 'package:web/web.dart' as web;

import '../../core/config/app_config.dart';
import '../../core/errors/auth_exception.dart';
import 'oauth_login_strategy.dart';
import 'pending_web_redirect_result.dart';

export 'pending_web_redirect_result.dart';

/// Authorization Code Flow + PKCE implementation for the Web platform.
///
/// Performs a full-page redirect to Keycloak. The pending flow state
/// (code verifier, scopes, token endpoint) is persisted in
/// `SharedPreferences` and consumed by [completePendingWebRedirect] at
/// app startup, before the rest of the UI loads.
class WebOAuthLoginStrategy implements OAuthLoginStrategy {
  WebOAuthLoginStrategy({
    required this.identifier,
    required this.secret,
    required this.authorizationEndpoint,
    required this.tokenEndpoint,
    required this.scopes,
  });

  final String identifier;
  final String? secret;
  final Uri authorizationEndpoint;
  final Uri tokenEndpoint;
  final List<String> scopes;

  @override
  Future<oauth2.Credentials> performLogin() async {
    final prefs = await SharedPreferences.getInstance();

    final codeVerifier = _generateCodeVerifier();
    final state = _generateState();
    final redirectUri = _redirectUri();

    await prefs.setString(
      _pendingFlowKey,
      jsonEncode({
        'code_verifier': codeVerifier,
        'state': state,
        'redirect_uri': redirectUri.toString(),
        'identifier': identifier,
        'secret': secret,
        'token_endpoint': tokenEndpoint.toString(),
        'authorization_endpoint': authorizationEndpoint.toString(),
        'scopes': scopes,
      }),
    );

    final grant = oauth2.AuthorizationCodeGrant(
      identifier,
      authorizationEndpoint,
      tokenEndpoint,
      secret: secret,
      codeVerifier: codeVerifier,
    );

    final url = grant.getAuthorizationUrl(
      redirectUri,
      scopes: scopes,
      state: state,
    );

    web.window.location.assign(url.toString());

    // The browser is now navigating away; the returned Future is never
    // resolved in the current page lifetime.
    return Completer<oauth2.Credentials>().future;
  }

  @override
  Future<void> performLogout({
    required Uri endSessionEndpoint,
    required String? idToken,
    required String? refreshToken,
  }) async {
    final logoutUrl = endSessionEndpoint.replace(
      queryParameters: {
        if (idToken != null) 'id_token_hint': idToken,
        'post_logout_redirect_uri': _redirectUri().toString(),
        'client_id': identifier,
      },
    );
    web.window.location.assign(logoutUrl.toString());
  }

  static const _pendingFlowKey = 'auth_code_pending_flow';

  static Uri _redirectUri() {
    final origin = _origin();
    return Uri.parse('$origin${AppConfig.authCodeWebRedirectPath}');
  }

  static String _origin() => web.window.location.origin;

  static String _generateCodeVerifier() {
    final rand = Random.secure();
    final bytes = List<int>.generate(64, (_) => rand.nextInt(256));
    return base64UrlEncode(bytes).replaceAll('=', '');
  }

  static String _generateState() {
    final rand = Random.secure();
    final bytes = List<int>.generate(16, (_) => rand.nextInt(256));
    return base64UrlEncode(bytes).replaceAll('=', '');
  }
}

/// Inspects `window.location` for an OAuth callback left over from a
/// previous full-page redirect. Three cases:
///
/// 1. `?code=...` from a successful login — completes the token exchange.
/// 2. `?error=...` from a failed login — surfaces the error.
/// 3. Path matches the redirect path with no query — likely the return
///    leg of a logout. Just rewrites the URL back to the origin so the
///    GoRouter can resolve the home route.
///
/// Must be called from `main()` *before* `runApp`, on Web only.
Future<PendingWebRedirectResult> completePendingWebRedirect() async {
  final url = Uri.parse(web.window.location.href);
  final isCallbackPath = url.path == AppConfig.authCodeWebRedirectPath;
  final hasCode = url.queryParameters.containsKey('code');
  final hasError = url.queryParameters.containsKey('error');

  if (!hasCode && !hasError) {
    if (isCallbackPath) _resetUrlToOrigin();
    return const PendingWebRedirectResult.none();
  }

  final prefs = await SharedPreferences.getInstance();
  final raw = prefs.getString(WebOAuthLoginStrategy._pendingFlowKey);
  if (raw == null) {
    _resetUrlToOrigin();
    return const PendingWebRedirectResult.none();
  }
  await prefs.remove(WebOAuthLoginStrategy._pendingFlowKey);

  final pending = jsonDecode(raw) as Map<String, dynamic>;

  final grant = oauth2.AuthorizationCodeGrant(
    pending['identifier'] as String,
    Uri.parse(pending['authorization_endpoint'] as String),
    Uri.parse(pending['token_endpoint'] as String),
    secret: pending['secret'] as String?,
    codeVerifier: pending['code_verifier'] as String,
  );

  // Re-priming the grant — the package requires us to call
  // `getAuthorizationUrl` once before `handleAuthorizationResponse`,
  // so it knows which redirect URI to validate against.
  grant.getAuthorizationUrl(
    Uri.parse(pending['redirect_uri'] as String),
    scopes: (pending['scopes'] as List).cast<String>(),
    state: pending['state'] as String,
  );

  try {
    final client = await grant.handleAuthorizationResponse(url.queryParameters);
    _resetUrlToOrigin();
    return PendingWebRedirectResult.success(client.credentials);
  } on oauth2.AuthorizationException {
    _resetUrlToOrigin();
    return const PendingWebRedirectResult.failure(InvalidCredentialsException());
  } catch (e) {
    _resetUrlToOrigin();
    return PendingWebRedirectResult.failure(NetworkAuthException(e));
  }
}

void _resetUrlToOrigin() {
  web.window.history.replaceState(null, '', '/');
}
