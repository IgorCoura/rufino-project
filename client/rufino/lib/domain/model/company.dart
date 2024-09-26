import 'package:equatable/equatable.dart';

class Company extends Equatable {
  final String id;
  final String corporateName;
  final String fantasyName;
  final String cnpj;

  const Company(this.id, this.corporateName, this.fantasyName, this.cnpj);

  @override
  List<Object?> get props => [id, corporateName, fantasyName, cnpj];

  factory Company.fromJson(Map<String, dynamic> json) {
    return Company(
        json["id"], json["corporateName"], json["fantasyName"], json["cnpj"]);
  }

  static List<Company> fromListJson(List<dynamic> jsonList) {
    return jsonList
        .map<Company>((element) => Company.fromJson(element))
        .toList();
  }
}
