import 'package:flutter/cupertino.dart';
import 'package:rufino/modules/employee/domain/model/base/text_prop_base.dart';

class Number extends TextPropBase {
  @override
  TextInputType? get inputType => TextInputType.number;
  const Number(String value, String displayName) : super(displayName, value);

  const Number.empty(String displayName) : super(displayName, "");

  @override
  String? validate(String? value) {
    if (value == null) {
      return "O $displayName é invalido.";
    }

    int? number = int.tryParse(value);

    if (number == null) {
      return "O $displayName é invalido.";
    }

    if (number < 0 || number > 100) {
      return "o $displayName não pode ser menor que 0 ou maior que 100.";
    }
    return null;
  }

  int? toInt() {
    return int.tryParse(value);
  }

  @override
  Number copyWith({String? value}) {
    return Number(value ?? this.value, displayName);
  }
}

class Page extends Number {
  static const String contDisplayName = "Página";
  const Page(String value) : super(contDisplayName, value);
  const Page.empty() : super(contDisplayName, "");
}

class RelativePositionBotton extends Number {
  static const String contDisplayName = "Posição Relativa Inferior";
  const RelativePositionBotton(String value) : super(contDisplayName, value);
  const RelativePositionBotton.empty() : super(contDisplayName, "");
}

class RelativePositionLeft extends Number {
  static const String contDisplayName = "Posição Relativa Esquerda";
  const RelativePositionLeft(String value) : super(contDisplayName, value);
  const RelativePositionLeft.empty() : super(contDisplayName, "");
}

class RelativeSizeX extends Number {
  static const String contDisplayName = "Tamanho Relativo Horizontal";
  const RelativeSizeX(String value) : super(contDisplayName, value);
  const RelativeSizeX.empty() : super(contDisplayName, "");
}

class RelativeSizeY extends Number {
  static const String contDisplayName = "Tamanho Relativo Vertical";
  const RelativeSizeY(String value) : super(contDisplayName, value);
  const RelativeSizeY.empty() : super(contDisplayName, "");
}
