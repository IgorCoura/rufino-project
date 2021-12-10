import 'package:flutter/material.dart';
import 'package:rufino_smart_app/Login/page/componentes/text_field_container.dart';
import 'package:rufino_smart_app/constants.dart';

class RoundedInputField extends StatelessWidget {
  final String hintText;
  final IconData icon;
  final ValueChanged<String> onChanged;
  const RoundedInputField(
      {Key? key,
      required this.hintText,
      this.icon = Icons.person,
      required this.onChanged})
      : super(key: key);

  @override
  Widget build(BuildContext context) {
    return TextFieldContainer(
        child: TextField(
      onChanged: onChanged,
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
