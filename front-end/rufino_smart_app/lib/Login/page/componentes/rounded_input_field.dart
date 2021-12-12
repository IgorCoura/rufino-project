import 'package:flutter/material.dart';
import 'package:rufino_smart_app/Login/page/componentes/text_field_container.dart';
import 'package:rufino_smart_app/utils/constants.dart';

class RoundedInputField extends StatelessWidget {
  final String hintText;
  final IconData icon;
  final TextEditingController controller;
  RoundedInputField(
      {Key? key,
      required this.hintText,
      this.icon = Icons.person,
      required this.controller})
      : super(key: key);

  @override
  Widget build(BuildContext context) {
    return TextFieldContainer(
        child: TextField(
      controller: controller,
      cursorColor: kPrimaryLightColor,
      decoration: InputDecoration(
        icon: Icon(
          icon,
          color: kPrimaryColor,
        ),
        hintText: hintText,
        border: InputBorder.none,
      ),
    ));
  }
}
