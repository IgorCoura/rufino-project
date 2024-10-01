import 'dart:convert';

import 'package:equatable/equatable.dart';

class Company extends Equatable {
  final String id;
  final String corporateName;
  final String fantasyName;
  final String cnpj;

  const Company(this.id, this.corporateName, this.fantasyName, this.cnpj);

  @override
  List<Object?> get props => [id, corporateName, fantasyName, cnpj];

  factory Company.fromMap(Map<String, dynamic> json) {
    return Company(
        json["id"], json["corporateName"], json["fantasyName"], json["cnpj"]);
  }

  factory Company.fromJson(String jsonString) {
    var json = jsonDecode(jsonString);
    return Company.fromMap(json);
  }

  static List<Company> fromListJson(List<dynamic> jsonList) {
    return jsonList
        .map<Company>((element) => Company.fromMap(element))
        .toList();
  }

  String toJson() {
    Map<String, dynamic> map = {
      "id": id,
      "corporateName": corporateName,
      "fantasyName": fantasyName,
      "cnpj": cnpj
    };
    return jsonEncode(map);
  }
}
