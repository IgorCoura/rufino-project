import 'package:oauth2/oauth2.dart' as oauth2;

/// Performs the user-facing portion of an OAuth2 Authorization Code (+ PKCE)
/// flow against Keycloak.
///
/// Each platform has its own implementation:
///
/// * Android / iOS — uses `flutter_appauth` to open Chrome Custom Tabs or
///   `ASWebAuthenticationSession` and listens on a custom URI scheme.
/// * Web — performs a full-page redirect, captured back inside `main()`.
/// * Windows / Linux / macOS — opens the system browser via `url_launcher`
///   and waits for the redirect on a `127.0.0.1` loopback HTTP server.
abstract class OAuthLoginStrategy {
  /// Opens the browser, lets the user authenticate, and returns the
  /// resulting [oauth2.Credentials].
  ///
  /// Throws if the user cancels or the authorization server returns an
  /// error.
  Future<oauth2.Credentials> performLogin();

  /// Terminates the user's SSO session at the identity provider.
  ///
  /// Implementations may either open the system browser at the
  /// end-session endpoint (mobile / web) or perform a silent
  /// back-channel POST when no browser is needed (desktop).
  Future<void> performLogout({
    required Uri endSessionEndpoint,
    required String? idToken,
    required String? refreshToken,
  });
}
