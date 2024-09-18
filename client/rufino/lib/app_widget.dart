import 'package:flutter/material.dart';
import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino/theme/theme.dart';
import 'package:rufino/theme/util.dart';

class AppWidget extends StatelessWidget {
  const AppWidget({super.key});

  @override
  Widget build(BuildContext context) {
    //TODO: Remover comentario
    // final brightness = View.of(context).platformDispatcher.platformBrightness;
    const brightness = Brightness.light;
    TextTheme textTheme = createTextTheme(context, "Archivo", "Archivo");
    MaterialTheme theme = MaterialTheme(textTheme);
    return MaterialApp.router(
      debugShowCheckedModeBanner: false,
      title: 'Rufino',
      theme: brightness == Brightness.light ? theme.light() : theme.dark(),
      routerConfig: Modular.routerConfig,
    ); //added by extension
  }
}
