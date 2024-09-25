import 'dart:io';

import 'package:flutter/material.dart';
import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino/app_module.dart';
import 'package:rufino/app_widget.dart';
import 'package:rufino/configurations/config_http_overrides.dart';

void main() {
  HttpOverrides.global = ConfigHttpOverrides();
  runApp(ModularApp(
    module: AppModule(),
    child: const AppWidget(),
  ));
}
