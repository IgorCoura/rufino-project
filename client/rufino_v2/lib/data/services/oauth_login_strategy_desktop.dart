import 'dart:async';
import 'dart:convert';
import 'dart:io';
import 'dart:math';

import 'package:http/http.dart' as http;
import 'package:oauth2/oauth2.dart' as oauth2;
import 'package:url_launcher/url_launcher.dart';

import '../../core/config/app_config.dart';
import '../../core/errors/auth_exception.dart';
import 'oauth_login_strategy.dart';

/// Authorization Code Flow + PKCE implementation for desktop platforms
/// (Windows, Linux, macOS).
///
/// Spawns a short-lived `127.0.0.1` HTTP server to capture the redirect,
/// opens the system browser via `url_launcher`, and completes the token
/// exchange using the `oauth2` package.
class DesktopOAuthLoginStrategy implements OAuthLoginStrategy {
  DesktopOAuthLoginStrategy({
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

  /// How long to wait for the user to finish the browser flow.
  static const _redirectTimeout = Duration(minutes: 5);

  @override
  Future<oauth2.Credentials> performLogin() async {
    final server = await HttpServer.bind(
      InternetAddress.loopbackIPv4,
      AppConfig.authCodeDesktopRedirectPort,
    );
    final redirectUri = Uri.parse(
      'http://127.0.0.1:${server.port}${AppConfig.authCodeDesktopRedirectPath}',
    );

    final codeVerifier = _generateCodeVerifier();
    final grant = oauth2.AuthorizationCodeGrant(
      identifier,
      authorizationEndpoint,
      tokenEndpoint,
      secret: secret,
      codeVerifier: codeVerifier,
    );

    final authUrl = grant.getAuthorizationUrl(redirectUri, scopes: scopes);

    if (!await launchUrl(authUrl, mode: LaunchMode.externalApplication)) {
      await server.close(force: true);
      throw const NetworkAuthException('Failed to launch system browser.');
    }

    try {
      final request = await server.first.timeout(_redirectTimeout);
      final params = request.uri.queryParameters;

      request.response
        ..statusCode = HttpStatus.ok
        ..headers.contentType = ContentType.html
        ..write(_completionHtml());
      await request.response.close();

      final client = await grant.handleAuthorizationResponse(params);
      return client.credentials;
    } on TimeoutException {
      throw const NetworkAuthException('Login timed out.');
    } on oauth2.AuthorizationException {
      throw const InvalidCredentialsException();
    } catch (e) {
      throw NetworkAuthException(e);
    } finally {
      await server.close(force: true);
    }
  }

  /// Silent back-channel logout — no browser is opened.
  ///
  /// Posts directly to the Keycloak end-session endpoint with the
  /// `refresh_token` so the SSO session is terminated server-side. Falls
  /// back to a GET with `id_token_hint` if the refresh token is missing.
  @override
  Future<void> performLogout({
    required Uri endSessionEndpoint,
    required String? idToken,
    required String? refreshToken,
  }) async {
    final client = http.Client();
    try {
      if (refreshToken != null) {
        await client.post(
          endSessionEndpoint,
          headers: const {'Content-Type': 'application/x-www-form-urlencoded'},
          body: {
            'client_id': identifier,
            if (secret != null) 'client_secret': secret!,
            'refresh_token': refreshToken,
          },
        );
        return;
      }
      if (idToken != null) {
        await client.get(
          endSessionEndpoint.replace(queryParameters: {
            'id_token_hint': idToken,
            'client_id': identifier,
          }),
        );
      }
    } finally {
      client.close();
    }
  }

  static String _generateCodeVerifier() {
    final rand = Random.secure();
    final bytes = List<int>.generate(64, (_) => rand.nextInt(256));
    return base64UrlEncode(bytes).replaceAll('=', '');
  }

  static String _completionHtml() => '''
<!DOCTYPE html>
<html lang="pt-br"><head><meta charset="utf-8"><title>Rufino</title>
<style>
  body{font-family:system-ui,-apple-system,Segoe UI,Roboto,sans-serif;
       display:flex;align-items:center;justify-content:center;
       height:100vh;margin:0;background:#f5f5f5;color:#202020}
  .card{background:#fff;padding:32px 48px;border-radius:12px;
        box-shadow:0 4px 12px rgba(0,0,0,.08);text-align:center}
</style></head><body>
<div class="card">
  <h1>Login concluído</h1>
  <p>Você já pode fechar esta janela e voltar ao Rufino.</p>
</div></body></html>
''';
}
