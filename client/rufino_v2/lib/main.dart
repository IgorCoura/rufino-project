import 'package:flutter/material.dart';

import 'app.dart';
import 'core/config/app_config.dart';
import 'core/config/dev_http_overrides_stub.dart'
    if (dart.library.io) 'core/config/dev_http_overrides.dart';

void main() {
  AppConfig.assertConfigured();

  if (AppConfig.isDevelop) {
    applyDevHttpOverrides();
  }

  runApp(const App());
}
