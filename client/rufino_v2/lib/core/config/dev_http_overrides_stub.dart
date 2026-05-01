import 'dart:developer' as developer;

/// No-op stub used on Flutter Web, where `dart:io`'s [HttpOverrides] is
/// unavailable.
///
/// On the web, all HTTP traffic flows through the browser's `fetch`/`XHR`
/// stack and the browser validates TLS certificates inside its own engine
/// — Dart code cannot intercept this. So this function cannot bypass a
/// self-signed or hostname-mismatched dev certificate; it only logs a
/// developer-facing warning explaining the workaround.
///
/// The native counterpart lives in `dev_http_overrides.dart` and is
/// selected automatically by the conditional import in `main.dart`.
void applyDevHttpOverrides() {
  developer.log(
    'applyDevHttpOverrides() is a no-op on Flutter Web. '
    'The browser controls TLS validation; Dart cannot bypass it. '
    'If the dev API is rejected with a certificate error, fix it at the '
    'source: (1) open the API URL once and accept the cert manually, '
    '(2) re-issue the cert with the LAN IP in the Subject Alternative '
    'Names, or (3) use mkcert to install a locally-trusted root CA.',
    name: 'dev_http_overrides',
    level: 900,
  );
}
