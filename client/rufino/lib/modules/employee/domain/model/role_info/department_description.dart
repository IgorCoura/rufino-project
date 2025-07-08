import 'package:rufino/modules/employee/domain/model/base/text_base.dart';

class DepartmentDescription extends TextBase {
  const DepartmentDescription(String value)
      : super("Descrição da Setor", value);
  const DepartmentDescription.empty() : super("Descrição da Setor", "");
}
