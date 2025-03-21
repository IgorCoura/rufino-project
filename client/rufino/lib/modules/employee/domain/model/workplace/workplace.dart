import 'package:rufino/modules/employee/domain/model/base/enumeration.dart';
import 'package:rufino/modules/employee/domain/model/workplace/address_workplace.dart';

class Workplace extends Enumeration {
  final AddressWorkplace address;
  const Workplace(String id, String name, this.address)
      : super(id, name, "Nome do local de trabalho");

  const Workplace.empty({this.address = const AddressWorkplace.empty()})
      : super.empty(displayName: "Nome do local de trabalho");

  static Workplace fromJson(Map<String, dynamic> json) {
    return Workplace(
        json["id"], json["name"], AddressWorkplace.fromJson(json["address"]));
  }

  static List<Workplace> fromListJson(List<dynamic> listJson) {
    return listJson.map((el) => fromJson(el)).toList();
  }
}
