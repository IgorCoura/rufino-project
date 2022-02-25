import 'dart:io';

import 'package:flutter/material.dart';
import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino_app/src/app_module.dart';
import 'package:rufino_app/src/app_widget.dart';
import 'package:intl/date_symbol_data_local.dart';
import 'package:rufino_app/src/my_http_overrides.dart';

Future<void> main() async {
  initializeDateFormatting('pt_Br', null).then(
      (_) => runApp(ModularApp(module: AppModule(), child: const AppWidget())));
  HttpOverrides.global = MyHttpOverrides();
}
