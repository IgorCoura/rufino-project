import 'package:flutter/material.dart';
import 'package:rufino_smart_app/constants.dart';
import 'componentes/remember_me_and_forgot_password_field.dart';
import 'componentes/rounded_button.dart';
import 'componentes/rounded_input_field.dart';
import 'componentes/rounded_password_field.dart';

class LoginScreen extends StatelessWidget {
  const LoginScreen({Key? key}) : super(key: key);

  @override
  Widget build(BuildContext context) {
    Size size = MediaQuery.of(context).size;
    return Scaffold(
      backgroundColor: kSecondaryColor,
      body: Container(
        width: double.infinity,
        height: size.height,
        child: Stack(
          alignment: Alignment.center,
          children: [
            SingleChildScrollView(
                child: Center(
                    child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                const Text(
                  "LOGIN",
                  style: TextStyle(
                      fontWeight: FontWeight.bold,
                      fontSize: 24,
                      color: kPrimaryColor),
                ),
                SizedBox(height: size.height * 0.03),
                RoundedInputField(hintText: "Email/CPF", onChanged: (value) {}),
                RoundedPasswordField(
                  onChanged: (value) {},
                ),
                RoundedButton(text: "LOGIN", press: () {}),
                RememberMeAndForgotPasswordField(
                  press: () {},
                ),
                SizedBox(height: size.height * 0.15),
              ],
            ))),
          ],
        ),
      ),
    );
  }
}
