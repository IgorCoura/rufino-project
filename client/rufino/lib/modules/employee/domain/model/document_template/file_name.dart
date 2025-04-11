import 'package:rufino/modules/employee/domain/model/base/text_prop_base.dart';

class FileName extends TextPropBase {
  const FileName(String value, String displayName) : super(displayName, value);

  const FileName.empty(String displayName) : super(displayName, "");

  @override
  String? validate(String? value) {
    if (value == null || value.isEmpty) {
      return "O $displayName não pode ser vazio.";
    }
    if (value.length > 20) {
      return "o $displayName não pode ser maior que 20 caracteres.";
    }

    if (value.endsWith(".html")) {
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
}

class HeaderFileName extends FileName {
  static const String constDisplayName =
      "Nome do arquivo do cabeçalho do documento";
  const HeaderFileName(String value) : super(value, constDisplayName);

  const HeaderFileName.empty() : super.empty(constDisplayName);
}

class FooterFileName extends FileName {
  static const String constDisplayName =
      "Nome do arquivo do rodape do documento";
  const FooterFileName(String value) : super(value, constDisplayName);

  const FooterFileName.empty() : super.empty(constDisplayName);
}
