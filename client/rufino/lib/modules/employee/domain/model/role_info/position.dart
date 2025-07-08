import 'package:rufino/modules/employee/domain/model/base/enumeration.dart';
import 'package:rufino/modules/employee/domain/model/base/text_base.dart';
import 'package:rufino/modules/employee/domain/model/role_info/position_cbo.dart';
import 'package:rufino/modules/employee/domain/model/role_info/position_description.dart';

class Position extends Enumeration {
  final PositionDescription positionDescription;
  final PositionCbo positionCbo;

  const Position(
      String id, String name, this.positionDescription, this.positionCbo)
      : super(id, name, "Cargo");
  const Position.empty(
      {this.positionDescription = const PositionDescription.empty(),
      this.positionCbo = const PositionCbo.empty()})
      : super.empty(displayName: "Cargo");

  static Position fromJson(Map<String, dynamic> json) {
    return Position(json["id"], json["name"],
        PositionDescription(json["description"]), PositionCbo(json["cbo"]));
  }

  static List<Position> fromListJson(List<dynamic> listJson) {
    return listJson.map((el) => fromJson(el)).toList();
  }

  List<TextBase> get textBase => [positionDescription, positionCbo];
}
