import 'dart:io';

import 'package:flutter/foundation.dart';

/// Installs [HttpOverrides] that accept any TLS certificate, so local HTTPS
/// endpoints with self-signed dev certificates work without handshake errors.
///
/// **Native (Android, iOS, macOS, Windows, Linux) only.** On Flutter Web,
/// `package:http` delegates to the browser's `fetch`/`XHR` APIs, and the
/// browser validates certificates inside its own engine — there is no Dart
/// API that can intercept this. The web variant of this function lives in
/// `dev_http_overrides_stub.dart` and is a no-op.
///
/// To bypass an invalid dev certificate on web, the fix is at the source,
/// not in Dart code:
///
///   1. Open the API URL once in the same browser tab and accept the
///      certificate manually ("Advanced → Proceed"); the browser remembers
///      this for the session.
///   2. Re-issue the dev certificate with the LAN IP listed in its
///      Subject Alternative Names.
///   3. Use `mkcert` to install a locally-trusted root CA.
///
/// Only call this in development mode — never in a production build.
void applyDevHttpOverrides() {
  HttpOverrides.global = DevHttpOverrides();
}

/// Bad-certificate callback that always accepts the certificate.
///
/// Extracted as a top-level function so it can be unit-tested directly.
@visibleForTesting
bool acceptAnyCertificate(X509Certificate cert, String host, int port) => true;

/// [HttpOverrides] subclass installed by [applyDevHttpOverrides].
///
/// Wires [acceptAnyCertificate] as the `badCertificateCallback` of every
/// [HttpClient] created by the platform.
@visibleForTesting
class DevHttpOverrides extends HttpOverrides {
  @override
  HttpClient createHttpClient(SecurityContext? context) {
    return super.createHttpClient(context)
      ..badCertificateCallback = acceptAnyCertificate;
  }
}
