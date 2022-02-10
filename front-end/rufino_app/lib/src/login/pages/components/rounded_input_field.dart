import 'package:flutter/material.dart';
import 'package:rufino_app/src/login/pages/components/text_field_container.dart';

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
      decoration: InputDecoration(
        icon: Icon(
          icon,
        ),
        hintText: hintText,
        border: InputBorder.none,
      ),
    ));
  }
}
