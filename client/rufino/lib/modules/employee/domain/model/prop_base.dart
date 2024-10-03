import 'package:flutter/services.dart';

abstract class PropBase {
  final String displayName;
  String value;
  final TextInputFormatter? formatter;
  final TextInputType? inputType;

  PropBase(this.displayName, this.value, {this.formatter, this.inputType});

  String? validate(String? value);
}
