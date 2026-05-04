/// Reads all compile-time secrets injected via:
///   flutter run --dart-define-from-file=secrets/local_config.json
///   flutter build web --dart-define-from-file=secrets/prod_config.json
///
/// Keys must match those in secrets/*.json exactly.
abstract final class AppConfig {
  static const String authorizationEndpoint = String.fromEnvironment(
    'authorization_endpoint',
  );

  static const String endSessionEndpoint = String.fromEnvironment(
    'end_session_endpoint',
  );

  static const String identifier = String.fromEnvironment(
    'identifier',
  );

  static const String secret = String.fromEnvironment(
    'secret',
  );

  /// Hostname only — no scheme (e.g. "192.168.15.8:8041")
  static const String peopleManagementUrl = String.fromEnvironment(
    'people_management_url',
  );

  /// Environment mode: "develop" or "production".
  static const String environment = String.fromEnvironment(
    'environment',
    defaultValue: 'production',
  );

  /// Whether the app is running in development mode.
  static bool get isDevelop => environment == 'develop';

  /// Opts in to the legacy Resource Owner Password Credentials grant
  /// (a.k.a. Direct Access Grants) when [true]. Defaults to [false], which
  /// means the app uses the Authorization Code Flow + PKCE.
  ///
  /// Set at build time:
  ///   flutter run --dart-define=use_direct_access_grants=true
  static const bool useDirectAccessGrants = bool.fromEnvironment(
    'use_direct_access_grants',
    defaultValue: false,
  );

  /// Whether the Authorization Code Flow + PKCE is the active flow.
  ///
  /// Equivalent to `!useDirectAccessGrants`. Exposed as a named getter so
  /// call sites read top-down without inverting a negative flag.
  static bool get useAuthorizationCodeFlow => !useDirectAccessGrants;

  /// Keycloak `/auth` endpoint — only used by the Authorization Code Flow.
  ///
  /// Example: `https://keycloak.example.com/realms/rufino/protocol/openid-connect/auth`.
  static const String authCodeAuthorizationEndpoint = String.fromEnvironment(
    'auth_code_authorization_endpoint',
  );

  /// Keycloak `/token` endpoint — only used by the Authorization Code Flow.
  ///
  /// Example: `https://keycloak.example.com/realms/rufino/protocol/openid-connect/token`.
  static const String authCodeTokenEndpoint = String.fromEnvironment(
    'auth_code_token_endpoint',
  );

  /// Custom URI scheme used as the OAuth redirect on Android and iOS.
  ///
  /// Must match the scheme registered in `AndroidManifest.xml`,
  /// `Info.plist`, and the Keycloak client's "Valid Redirect URIs".
  static const String authCodeMobileRedirectScheme = String.fromEnvironment(
    'auth_code_mobile_redirect_scheme',
    defaultValue: 'br.com.couratechsafety.rufino',
  );

  /// Full redirect URI used on Android and iOS.
  static String get authCodeMobileRedirectUri =>
      '$authCodeMobileRedirectScheme://oauth/callback';

  /// Loopback path appended to `http://127.0.0.1:<port>` on
  /// Windows/Linux/macOS.
  static const String authCodeDesktopRedirectPath = '/callback';

  /// Fixed loopback port for the desktop redirect, or `0` to let the OS
  /// pick a free port at runtime (default).
  ///
  /// Use a fixed port (e.g. `8765`) when your Keycloak does not accept
  /// wildcard ports in "Valid Redirect URIs"; in that case register
  /// `http://127.0.0.1:<port>/callback` explicitly.
  static const int authCodeDesktopRedirectPort = int.fromEnvironment(
    'auth_code_desktop_redirect_port',
    defaultValue: 0,
  );

  /// Path appended to the web origin on the Web build.
  static const String authCodeWebRedirectPath = '/auth/callback';

  /// Whether device-side error monitoring is active for this build.
  ///
  /// When `false`, the app uses a no-op reporter and no events leave the
  /// device. Vendor-agnostic: the active backend is selected in `main.dart`.
  static const bool errorMonitoringEnabled = bool.fromEnvironment(
    'error_monitoring_enabled',
    defaultValue: false,
  );

  /// Vendor DSN / ingest URL used by the active error monitoring backend.
  ///
  /// Required when [errorMonitoringEnabled] is `true`.
  static const String errorMonitoringDsn = String.fromEnvironment(
    'error_monitoring_dsn',
  );

  static const String _errorMonitoringEnvironmentOverride =
      String.fromEnvironment('error_monitoring_environment');

  /// Environment label attached to every captured event (e.g. `develop`,
  /// `production`).
  ///
  /// Falls back to [environment] when not explicitly overridden so dev and
  /// prod builds are separated in the monitoring dashboard without extra
  /// configuration.
  static String get errorMonitoringEnvironment =>
      _errorMonitoringEnvironmentOverride.isEmpty
          ? environment
          : _errorMonitoringEnvironmentOverride;

  static const String _errorMonitoringTracesSampleRateRaw =
      String.fromEnvironment('error_monitoring_traces_sample_rate');

  /// Sample rate (0.0–1.0) for performance traces.
  ///
  /// Defaults to `0.0` (no tracing) so unconfigured builds do not silently
  /// ship transaction data.
  static double get errorMonitoringTracesSampleRate =>
      double.tryParse(_errorMonitoringTracesSampleRateRaw) ?? 0.0;

  /// Validates that all required secrets were injected at compile time.
  /// Call this in main() to fail fast with a clear error.
  static void assertConfigured() {
    final missing = <String>[];
    if (endSessionEndpoint.isEmpty) missing.add('end_session_endpoint');
    if (identifier.isEmpty) missing.add('identifier');
    if (peopleManagementUrl.isEmpty) missing.add('people_management_url');

    if (useDirectAccessGrants) {
      if (authorizationEndpoint.isEmpty) {
        missing.add('authorization_endpoint');
      }
      if (secret.isEmpty) missing.add('secret');
    } else {
      if (authCodeAuthorizationEndpoint.isEmpty) {
        missing.add('auth_code_authorization_endpoint');
      }
      if (authCodeTokenEndpoint.isEmpty) {
        missing.add('auth_code_token_endpoint');
      }
    }

    if (errorMonitoringEnabled && errorMonitoringDsn.isEmpty) {
      missing.add('error_monitoring_dsn');
    }

    if (missing.isNotEmpty) {
      throw StateError(
        'Missing required compile-time secrets: ${missing.join(', ')}.\n'
        'Run with: flutter run --dart-define-from-file=secrets/local_config.json',
      );
    }
  }
}
