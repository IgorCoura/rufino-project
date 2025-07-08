import 'package:rufino/modules/employee/domain/model/base/text_base.dart';

class RoleDescription extends TextBase {
  const RoleDescription(String value) : super("Descrição da Função", value);
  const RoleDescription.empty() : super("Descrição da Função", "");
}
