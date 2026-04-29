import 'package:oauth2/oauth2.dart' as oauth2;

/// Outcome of completing an in-flight web Authorization Code redirect at
/// app startup.
class PendingWebRedirectResult {
  const PendingWebRedirectResult.none()
      : credentials = null,
        error = null;
  const PendingWebRedirectResult.success(this.credentials) : error = null;
  const PendingWebRedirectResult.failure(this.error) : credentials = null;

  final oauth2.Credentials? credentials;
  final Object? error;

  bool get hasResult => credentials != null || error != null;
}
