import 'package:flutter/material.dart';
import 'package:flutter_modular/flutter_modular.dart';

class ForgotPasswordPage extends StatelessWidget {
  const ForgotPasswordPage({Key? key}) : super(key: key);

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text("Forgot Password"),
        leading: BackButton(
          onPressed: () => Modular.to.navigate('/'),
        ),
      ),
      body: Container(
        child: const Center(
          child: Text("Page Forgot Password"),
        ),
      ),
    );
  }
}
