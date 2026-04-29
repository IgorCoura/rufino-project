import 'dart:io' show Platform;

import 'oauth_login_strategy.dart';
import 'oauth_login_strategy_desktop.dart';
import 'oauth_login_strategy_mobile.dart';

/// IO-side factory — chosen for native builds (Android, iOS, Windows,
/// Linux, macOS).
///
/// Returns [MobileOAuthLoginStrategy] on Android/iOS and
/// [DesktopOAuthLoginStrategy] on Windows/Linux/macOS.
OAuthLoginStrategy createOAuthLoginStrategy({
  required String identifier,
  required String? secret,
  required Uri authorizationEndpoint,
  required Uri tokenEndpoint,
  required List<String> scopes,
}) {
  if (Platform.isAndroid || Platform.isIOS) {
    return MobileOAuthLoginStrategy(
      identifier: identifier,
      secret: secret,
      authorizationEndpoint: authorizationEndpoint,
      tokenEndpoint: tokenEndpoint,
      scopes: scopes,
    );
  }
  return DesktopOAuthLoginStrategy(
    identifier: identifier,
    secret: secret,
    authorizationEndpoint: authorizationEndpoint,
    tokenEndpoint: tokenEndpoint,
    scopes: scopes,
  );
}
