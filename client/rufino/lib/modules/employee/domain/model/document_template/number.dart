import 'package:flutter/cupertino.dart';
import 'package:rufino/modules/employee/domain/model/base/text_prop_base.dart';

class Number extends TextPropBase {
  @override
  TextInputType? get inputType => TextInputType.number;
  const Number(String displayName, String value) : super(displayName, value);

  const Number.empty(String displayName) : super(displayName, "");

  @override
  String? validate(String? value) {
    if (value == null) {
      return "O $displayName é invalido.";
    }

    double? number = double.tryParse(value);

    if (number == null) {
      return "O $displayName é invalido.";
    }

    if (number < 0 || number > 100) {
      return "o $displayName não pode ser menor que 0 ou maior que 100.";
    }
    return null;
  }

  double? toDouble() {
    return double.tryParse(value);
  }

  @override
  Number copyWith({String? value}) {
    return Number(value ?? this.value, displayName);
  }
}

class RelativePositionBotton extends Number {
  static const String contDisplayName = "Posição Relativa Inferior";
  const RelativePositionBotton(String value) : super(contDisplayName, value);
  const RelativePositionBotton.empty() : super(contDisplayName, "");
  @override
  RelativePositionBotton copyWith({String? value}) {
    return RelativePositionBotton(value ?? this.value);
  }
}

class RelativePositionLeft extends Number {
  static const String contDisplayName = "Posição Relativa Esquerda";
  const RelativePositionLeft(String value) : super(contDisplayName, value);
  const RelativePositionLeft.empty() : super(contDisplayName, "");
  @override
  RelativePositionLeft copyWith({String? value}) {
    return RelativePositionLeft(value ?? this.value);
  }
}

class RelativeSizeX extends Number {
  static const String contDisplayName = "Tamanho Relativo Horizontal";
  const RelativeSizeX(String value) : super(contDisplayName, value);
  const RelativeSizeX.empty() : super(contDisplayName, "");
  @override
  RelativeSizeX copyWith({String? value}) {
    return RelativeSizeX(value ?? this.value);
  }
}

class RelativeSizeY extends Number {
  static const String contDisplayName = "Tamanho Relativo Vertical";
  const RelativeSizeY(String value) : super(contDisplayName, value);
  const RelativeSizeY.empty() : super(contDisplayName, "");
  @override
  RelativeSizeY copyWith({String? value}) {
    return RelativeSizeY(value ?? this.value);
  }
}
