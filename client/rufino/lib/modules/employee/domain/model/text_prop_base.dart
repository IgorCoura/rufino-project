import 'package:equatable/equatable.dart';
import 'package:flutter/services.dart';

abstract class TextPropBase extends Equatable {
  final String displayName;
  final String value;
  final TextInputFormatter? formatter;
  final TextInputType? inputType;

  const TextPropBase(this.displayName, this.value,
      {this.formatter, this.inputType});

  TextPropBase copyWith({String? value});

  String? validate(String? value);

  @override
  List<Object?> get props => [value];
}
