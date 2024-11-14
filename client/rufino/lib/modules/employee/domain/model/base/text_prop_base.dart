import 'package:equatable/equatable.dart';
import 'package:flutter/services.dart';

abstract class TextPropBase extends Equatable {
  final String displayName;
  final String value;
  TextInputFormatter? get formatter => null;
  TextInputType? get inputType => null;

  const TextPropBase(this.displayName, this.value);

  TextPropBase copyWith({String? value});

  String? validate(String? value);

  @override
  List<Object?> get props => [value];
}
