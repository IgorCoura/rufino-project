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
        scaffoldBackgroundColor: Colors.white,
        backgroundColor: Colors.white,
        listTileTheme: ListTileThemeData(
          shape: Border.symmetric(
            horizontal: BorderSide(
              color: Colors.blueGrey.shade100,
            ),
          ),
        ),
        textTheme: const TextTheme(
            titleLarge: TextStyle(fontSize: 26, fontWeight: FontWeight.bold),
            titleMedium: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
            bodyLarge: TextStyle(
              fontSize: 18,
            )),
      ),
    ).modular();
  }
}
