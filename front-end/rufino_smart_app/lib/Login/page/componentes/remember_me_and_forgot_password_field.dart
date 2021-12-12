import 'package:flutter/material.dart';
import 'package:rufino_smart_app/utils/constants.dart';

class RememberMeAndForgotPasswordField extends StatefulWidget {
  final Function() press;
  final Function() checkBox;
  RememberMeAndForgotPasswordField({
    Key? key,
    required this.press,
    required this.checkBox,
  }) : super(key: key);

  @override
  State<RememberMeAndForgotPasswordField> createState() =>
      _RememberMeAndForgotPasswordFieldState();
}

class _RememberMeAndForgotPasswordFieldState
    extends State<RememberMeAndForgotPasswordField> {
  bool? check = false;

  @override
  Widget build(BuildContext context) {
    Size size = MediaQuery.of(context).size;
    return Row(
      mainAxisAlignment: MainAxisAlignment.center,
      children: [
        size.width > 300 ? _rememberMe() : Container(),
        SizedBox(
          width: size.width > 300 ? size.width * 0.05 : 0,
        ),
        GestureDetector(
          onTap: widget.press,
          child: const Text(
            "Forgot Password?",
            style: TextStyle(color: kPrimaryColor, fontWeight: FontWeight.bold),
          ),
        )
      ],
    );
  }

  Widget _rememberMe() {
    return Row(
      children: [
        Checkbox(
          value: check,
          onChanged: (value) {
            setState(() {
              check = value;
            });
            widget.checkBox();
          },
          activeColor: kPrimaryColor,
        ),
        Text("Remember me"),
      ],
    );
  }
}
