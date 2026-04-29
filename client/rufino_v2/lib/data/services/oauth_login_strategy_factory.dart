import 'oauth_login_strategy.dart';
import 'oauth_login_strategy_factory_io.dart'
    if (dart.library.js_interop) 'oauth_login_strategy_factory_web.dart'
    as impl;

/// Returns the [OAuthLoginStrategy] appropriate for the current platform.
///
/// The selection happens at compile time via conditional imports — Web
/// builds get the redirect-based implementation, native builds get a
/// platform-specific implementation chosen at runtime.
OAuthLoginStrategy createOAuthLoginStrategy({
  required String identifier,
  required String? secret,
  required Uri authorizationEndpoint,
  required Uri tokenEndpoint,
  required List<String> scopes,
}) =>
    impl.createOAuthLoginStrategy(
      identifier: identifier,
      secret: secret,
      authorizationEndpoint: authorizationEndpoint,
      tokenEndpoint: tokenEndpoint,
      scopes: scopes,
    );
