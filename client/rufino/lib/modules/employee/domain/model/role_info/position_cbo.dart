import 'package:rufino/modules/employee/domain/model/base/text_base.dart';

class PositionCbo extends TextBase {
  const PositionCbo(String value) : super("CBO do Cargo", value);
  const PositionCbo.empty() : super("CBO da Cargo", "");
}
