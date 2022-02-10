import 'package:flutter/material.dart';
import 'package:flutter_modular/flutter_modular.dart';

class AppWidget extends StatelessWidget {
  const AppWidget({Key? key}) : super(key: key);

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      debugShowCheckedModeBanner: false,
      title: "RufinoApp",
      theme: ThemeData(
        colorScheme: ColorScheme.fromSwatch(
            primarySwatch: Colors.blueGrey,
            accentColor: const Color(0xFF37474f),
            cardColor: const Color(0xFF37474f)),
        primaryColor: Colors.blueGrey[300],
        cardColor: Colors.blueGrey[400],
        listTileTheme: ListTileThemeData(
          shape: Border.symmetric(
            horizontal: BorderSide(
              color: Colors.blueGrey.shade100,
            ),
          ),
        ),
      ),
    ).modular();
  }
}
