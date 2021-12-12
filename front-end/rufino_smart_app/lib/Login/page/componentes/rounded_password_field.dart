import 'package:flutter/material.dart';
import 'package:rufino_smart_app/Login/page/componentes/text_field_container.dart';
import '../../../utils/constants.dart';

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
      cursorColor: kPrimaryColor,
      decoration: const InputDecoration(
          hintText: "Password",
          icon: Icon(
            Icons.lock,
            color: kPrimaryColor,
          ),
          suffixIcon: Icon(
            Icons.visibility,
            color: kPrimaryColor,
          ),
          border: InputBorder.none),
    ));
  }
}
