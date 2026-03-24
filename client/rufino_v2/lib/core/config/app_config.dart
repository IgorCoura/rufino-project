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

  /// Validates that all required secrets were injected at compile time.
  /// Call this in main() to fail fast with a clear error.
  static void assertConfigured() {
    final missing = <String>[];
    if (authorizationEndpoint.isEmpty) missing.add('authorization_endpoint');
    if (endSessionEndpoint.isEmpty) missing.add('end_session_endpoint');
    if (identifier.isEmpty) missing.add('identifier');
    if (secret.isEmpty) missing.add('secret');
    if (peopleManagementUrl.isEmpty) missing.add('people_management_url');

    if (missing.isNotEmpty) {
      throw StateError(
        'Missing required compile-time secrets: ${missing.join(', ')}.\n'
        'Run with: flutter run --dart-define-from-file=secrets/local_config.json',
      );
    }
  }
}
