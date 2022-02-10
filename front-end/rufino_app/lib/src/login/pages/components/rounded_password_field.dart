import 'package:flutter/material.dart';
import 'package:rufino_app/src/login/pages/components/text_field_container.dart';

class RoundedPasswordField extends StatelessWidget {
  final TextEditingController controller;
  final bool obscureText = true;
  const RoundedPasswordField({Key? key, required this.controller})
      : super(key: key);

  @override
  Widget build(BuildContext context) {
    return TextFieldContainer(
        child: TextField(
      obscureText: obscureText,
      controller: controller,
      decoration: const InputDecoration(
          hintText: "Password",
          icon: Icon(
            Icons.lock,
          ),
          suffixIcon: Icon(
            Icons.visibility,
          ),
          border: InputBorder.none),
    ));
  }
}
