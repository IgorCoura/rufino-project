import 'package:flutter/material.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'app.dart';
import 'core/config/app_config.dart';
import 'core/config/dev_http_overrides_stub.dart'
    if (dart.library.io) 'core/config/dev_http_overrides.dart';

void main() async {
  WidgetsFlutterBinding.ensureInitialized();
  AppConfig.assertConfigured();

  if (AppConfig.isDevelop) {
    applyDevHttpOverrides();
  }

  final prefs = await SharedPreferences.getInstance();

  runApp(App(prefs: prefs));
}
