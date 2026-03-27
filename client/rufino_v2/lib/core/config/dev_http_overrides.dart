import 'dart:io';

/// Applies [HttpOverrides] that accept all certificates so local HTTPS
/// endpoints with self-signed certificates work without handshake errors.
///
/// This is only used in development mode and only on non-web platforms.
void applyDevHttpOverrides() {
  HttpOverrides.global = _DevHttpOverrides();
}

class _DevHttpOverrides extends HttpOverrides {
  @override
  HttpClient createHttpClient(SecurityContext? context) {
    return super.createHttpClient(context)
      ..badCertificateCallback =
          (X509Certificate cert, String host, int port) => true;
  }
}
