import 'package:flutter/material.dart';
import 'package:rufino_smart_app/constants.dart';

class RememberMeAndForgotPasswordField extends StatelessWidget {
  final bool login;
  final Function() press;
  const RememberMeAndForgotPasswordField({
    Key? key,
    this.login = true,
    required this.press,
  }) : super(key: key);

  @override
  Widget build(BuildContext context) {
    Size size = MediaQuery.of(context).size;
    return Row(
      mainAxisAlignment: MainAxisAlignment.center,
      children: [
        Checkbox(
          value: login,
          onChanged: (value) {},
          activeColor: kPrimaryColor,
        ),
        const Text("Remember me"),
        SizedBox(
          width: size.width * 0.05,
        ),
        GestureDetector(
          onTap: press,
          child: const Text(
            "Forgot Password?",
            style: TextStyle(color: kPrimaryColor, fontWeight: FontWeight.bold),
          ),
        )
      ],
    );
  }
}
