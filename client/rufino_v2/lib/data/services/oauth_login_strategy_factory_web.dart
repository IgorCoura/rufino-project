import 'oauth_login_strategy.dart';
import 'oauth_login_strategy_web.dart';

/// Web-side factory — chosen when `dart.library.js_interop` is available.
OAuthLoginStrategy createOAuthLoginStrategy({
  required String identifier,
  required String? secret,
  required Uri authorizationEndpoint,
  required Uri tokenEndpoint,
  required List<String> scopes,
}) =>
    WebOAuthLoginStrategy(
      identifier: identifier,
      secret: secret,
      authorizationEndpoint: authorizationEndpoint,
      tokenEndpoint: tokenEndpoint,
      scopes: scopes,
    );
