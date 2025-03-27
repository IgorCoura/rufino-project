import 'package:equatable/equatable.dart';

class EmployeeContractType extends Equatable {
  static const Map<String, String> conversionMapIntToString = {
    "1": "CLT",
    "2": "Aprendiz",
    "3": "Verde Amarelo",
    "4": "Verde Amarelo Com Acordo FGTS",
  };

  final String id;
  late String name;

  EmployeeContractType(this.id, String name) {
    this.name = conversionMapIntToString[id] ?? name;
  }

  EmployeeContractType.empty({this.id = "", this.name = ""});

  static EmployeeContractType fromJson(Map<String, dynamic> json) {
    return EmployeeContractType(
      (json["id"]).toString(),
      json["name"],
    );
  }

  static List<EmployeeContractType> fromListJson(List<dynamic> listJson) {
    return listJson.map((el) => fromJson(el)).toList();
  }

  @override
  List<Object?> get props => [id, name];
}
