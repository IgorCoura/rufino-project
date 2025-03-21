import 'package:rufino/modules/employee/domain/model/base/text_base.dart';

class SalaryDescription extends TextBase {
  const SalaryDescription(String value) : super("Descrição do Salario", value);
  const SalaryDescription.empty() : super("Descrição da Salario", "");
}
