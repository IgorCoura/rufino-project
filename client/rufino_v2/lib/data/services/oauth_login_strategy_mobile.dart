import 'package:flutter_appauth/flutter_appauth.dart';
import 'package:oauth2/oauth2.dart' as oauth2;
import 'package:url_launcher/url_launcher.dart';

import '../../core/config/app_config.dart';
import '../../core/errors/auth_exception.dart';
import 'oauth_login_strategy.dart';

/// Authorization Code Flow + PKCE implementation for Android and iOS.
///
/// Delegates the entire user-facing flow (browser, redirect handling,
/// PKCE, token exchange) to `flutter_appauth`, which uses Chrome Custom
/// Tabs on Android and `ASWebAuthenticationSession` on iOS.
class MobileOAuthLoginStrategy implements OAuthLoginStrategy {
  MobileOAuthLoginStrategy({
    required this.identifier,
    required this.secret,
    required this.authorizationEndpoint,
    required this.tokenEndpoint,
    required this.scopes,
    FlutterAppAuth? appAuth,
  }) : _appAuth = appAuth ?? const FlutterAppAuth();

  final String identifier;
  final String? secret;
  final Uri authorizationEndpoint;
  final Uri tokenEndpoint;
  final List<String> scopes;

  final FlutterAppAuth _appAuth;

  @override
  Future<oauth2.Credentials> performLogin() async {
    final AuthorizationTokenResponse response;
    try {
      response = await _appAuth.authorizeAndExchangeCode(
        AuthorizationTokenRequest(
          identifier,
          AppConfig.authCodeMobileRedirectUri,
          clientSecret: secret,
          serviceConfiguration: AuthorizationServiceConfiguration(
            authorizationEndpoint: authorizationEndpoint.toString(),
            tokenEndpoint: tokenEndpoint.toString(),
          ),
          scopes: scopes,
          allowInsecureConnections: AppConfig.isDevelop,
        ),
      );
    } catch (e) {
      throw NetworkAuthException(e);
    }

    if (response.accessToken == null || response.accessToken!.isEmpty) {
      throw const InvalidCredentialsException();
    }

    return oauth2.Credentials(
      response.accessToken!,
      refreshToken: response.refreshToken,
      idToken: response.idToken,
      tokenEndpoint: tokenEndpoint,
      scopes: response.scopes,
      expiration: response.accessTokenExpirationDateTime,
    );
  }

  @override
  Future<void> performLogout({
    required Uri endSessionEndpoint,
    required String? idToken,
    required String? refreshToken,
  }) async {
    try {
      await _appAuth.endSession(
        EndSessionRequest(
          idTokenHint: idToken,
          postLogoutRedirectUrl: AppConfig.authCodeMobileRedirectUri,
          serviceConfiguration: AuthorizationServiceConfiguration(
            authorizationEndpoint: authorizationEndpoint.toString(),
            tokenEndpoint: tokenEndpoint.toString(),
            endSessionEndpoint: endSessionEndpoint.toString(),
          ),
          allowInsecureConnections: AppConfig.isDevelop,
        ),
      );
    } catch (_) {
      final fallback = endSessionEndpoint.replace(
        queryParameters: {
          if (idToken != null) 'id_token_hint': idToken,
          'post_logout_redirect_uri': AppConfig.authCodeMobileRedirectUri,
        },
      );
      await launchUrl(fallback, mode: LaunchMode.externalApplication);
    }
  }
}
