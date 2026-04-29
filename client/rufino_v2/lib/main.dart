import 'package:flutter/material.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'app.dart';
import 'core/config/app_config.dart';
import 'core/config/dev_http_overrides_stub.dart'
    if (dart.library.io) 'core/config/dev_http_overrides.dart';
import 'data/services/auth_code_redirect_handler.dart';

void main() async {
  WidgetsFlutterBinding.ensureInitialized();
  AppConfig.assertConfigured();

  if (AppConfig.isDevelop) {
    applyDevHttpOverrides();
  }

  final prefs = await SharedPreferences.getInstance();

  // On Web, finish any in-flight Authorization Code redirect that brought
  // us back to the app with a `?code=...` query parameter. No-op on
  // every other platform.
  PendingWebRedirectResult? pendingRedirect;
  if (AppConfig.useAuthorizationCodeFlow) {
    pendingRedirect = await completePendingAuthCodeRedirect();
  }

  runApp(App(prefs: prefs, pendingWebRedirect: pendingRedirect));
}
