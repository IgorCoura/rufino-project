import 'dart:io';

import 'package:flutter/material.dart';

import 'app.dart';
import 'core/config/app_config.dart';

void main() {
  AppConfig.assertConfigured();

  if (AppConfig.isDevelop) {
    HttpOverrides.global = _DevHttpOverrides();
  }

  runApp(const App());
}

/// Accepts all certificates so local HTTPS endpoints with self-signed
/// certificates work without handshake errors.
class _DevHttpOverrides extends HttpOverrides {
  @override
  HttpClient createHttpClient(SecurityContext? context) {
    return super.createHttpClient(context)
      ..badCertificateCallback =
          (X509Certificate cert, String host, int port) => true;
  }
}

