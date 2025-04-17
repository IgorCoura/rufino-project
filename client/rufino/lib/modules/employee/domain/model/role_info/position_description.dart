import 'package:rufino/modules/employee/domain/model/base/text_base.dart';

class PositionDescription extends TextBase {
  const PositionDescription(String value) : super("Descrição da Cargo", value);
  const PositionDescription.empty() : super("Descrição da Cargo", "");
}
