import 'package:flutter/material.dart';

import 'app.dart';
import 'core/config/app_config.dart';

void main() {
  AppConfig.assertConfigured();
  runApp(const App());
}
