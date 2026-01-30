import 'package:flutter/services.dart';
import 'package:rufino/modules/employee/domain/model/base/text_prop_base.dart';

class Email extends TextPropBase {
  static const emailInputType = TextInputType.emailAddress;

  const Email(String value) : super("Email", value);
  const Email.empty() : super("Email", "");

  @override
  String? validate(value) {
    if (value == null || value.isEmpty) {
      return null; // Permite campo vazio
    }
    if (value.length > 100) {
      return "o $displayName não pode ser maior que 100 caracteres.";
    }
    var regex = RegExp(
        r'^(([^<>()[\]\\.,;:\s@\"]+(\.[^<>()[\]\\.,;:\s@\"]+)*)|(\".+\"))@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\])|(([a-zA-Z\-0-9]+\.)+[a-zA-Z]{2,}))$');
    if (regex.hasMatch(value) == false) {
      return "Formato invalido";
    }
    return null;
  }

  @override
  Email copyWith({String? value}) {
    return Email(value ?? this.value);
  }
}
