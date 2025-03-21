import 'package:rufino/modules/employee/domain/model/base/text_base.dart';

class AddressWorkplace extends TextBase {
  static const contDisplayName = "Endereço";
  const AddressWorkplace(String value) : super(contDisplayName, value);
  const AddressWorkplace.empty() : super(contDisplayName, "");

  static AddressWorkplace fromJson(Map<String, dynamic> json) {
    var complement = json["complement"] as String;
    complement = complement.isEmpty ? "" : "$complement ,";
    var address =
        "${json["street"]}, ${json["number"]}, $complement ${json["neighborhood"]}, ${json["city"]}, ${json["state"]}, ${json["country"]}, CEP: ${json["zipCode"]}";
    return AddressWorkplace(address);
  }
}
