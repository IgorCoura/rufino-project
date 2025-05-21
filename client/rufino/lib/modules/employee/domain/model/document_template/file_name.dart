import 'package:rufino/modules/employee/domain/model/base/text_prop_base.dart';

class FileName extends TextPropBase {
  const FileName(String value, String displayName) : super(displayName, value);

  const FileName.empty(String displayName) : super(displayName, "");

  bool get isEmpty {
    return value.isEmpty;
  }

  bool get isNotEmpty {
    return value.isNotEmpty;
  }

  @override
  String? validate(String? value) {
    if (value == null || value.isEmpty) {
      return null;
    }
    if (value.length > 20) {
      return "o $displayName não pode ser maior que 20 caracteres.";
    }

    if (!value.endsWith(".html")) {
      return "o $displayName precisa ser um arquivo .html.";
    }
    return null;
  }

  @override
  FileName copyWith({String? value}) {
    return FileName(value ?? this.value, displayName);
  }
}

class BodyFileName extends FileName {
  static const String constDisplayName =
      "Nome do arquivo do corpo do documento";
  const BodyFileName(String value) : super(value, constDisplayName);

  const BodyFileName.empty() : super.empty(constDisplayName);
  @override
  BodyFileName copyWith({String? value}) {
    return BodyFileName(value ?? this.value);
  }
}

class HeaderFileName extends FileName {
  static const String constDisplayName =
      "Nome do arquivo do cabeçalho do documento";
  const HeaderFileName(String value) : super(value, constDisplayName);

  const HeaderFileName.empty() : super.empty(constDisplayName);
  @override
  HeaderFileName copyWith({String? value}) {
    return HeaderFileName(value ?? this.value);
  }
}

class FooterFileName extends FileName {
  static const String constDisplayName =
      "Nome do arquivo do rodape do documento";
  const FooterFileName(String value) : super(value, constDisplayName);

  const FooterFileName.empty() : super.empty(constDisplayName);
  @override
  FooterFileName copyWith({String? value}) {
    return FooterFileName(value ?? this.value);
  }
}
