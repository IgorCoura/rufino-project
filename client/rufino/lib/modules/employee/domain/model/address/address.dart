import 'package:rufino/modules/employee/domain/model/address/city.dart';
import 'package:rufino/modules/employee/domain/model/address/complement.dart';
import 'package:rufino/modules/employee/domain/model/address/country.dart';
import 'package:rufino/modules/employee/domain/model/address/neighborhood.dart';
import 'package:rufino/modules/employee/domain/model/address/number.dart';
import 'package:rufino/modules/employee/domain/model/address/state.dart';
import 'package:rufino/modules/employee/domain/model/address/street.dart';
import 'package:rufino/modules/employee/domain/model/address/zipcode.dart';
import 'package:rufino/modules/employee/domain/model/enumeration.dart';
import 'package:rufino/modules/employee/domain/model/enumeration_collection.dart';
import 'package:rufino/modules/employee/domain/model/model_base.dart';
import 'package:rufino/modules/employee/domain/model/text_prop_base.dart';

class Address extends ModelBase {
  final Zipcode zipcode;
  final Street street;
  final Number number;
  final Complement complement;
  final Neighborhood neighborhood;
  final City city;
  final State state;
  final Country country;

  const Address(this.zipcode, this.street, this.number, this.complement,
      this.neighborhood, this.city, this.state, this.country,
      {super.isLoading});

  static Address get empty => Address(
      Zipcode.empty,
      Street.empty,
      Number.empty,
      Complement.empty,
      Neighborhood.empty,
      City.empty,
      State.empty,
      Country.empty);

  static Address get loading => Address(
      Zipcode.empty,
      Street.empty,
      Number.empty,
      Complement.empty,
      Neighborhood.empty,
      City.empty,
      State.empty,
      Country.empty,
      isLoading: true);

  factory Address.fromJson(Map<String, dynamic> json) {
    return Address(
        Zipcode.createFormatNumber(json["zipcode"]),
        Street(json["street"]),
        Number(json["number"]),
        Complement(json["complement"]),
        Neighborhood(json["neighborhood"]),
        City(json["city"]),
        State(json["state"]),
        Country(json["coutry"]),
        isLoading: false);
  }

  Map<String, String> toJson() {
    return {
      "zipcode": zipcode.value,
      "street": street.value,
      "number": number.value,
      "complement": complement.value,
      "neighborhood": neighborhood.value,
      "city": city.value,
      "state": state.value,
      "country": country.value
    };
  }

  @override
  List<TextPropBase> get props =>
      [zipcode, street, number, complement, neighborhood, city, state, country];

  @override
  List<ModelBase> get models => [];
  @override
  List<EnumerationCollection> get enumerationCollection => [];

  @override
  List<List<Enumeration>> get enumerations => [];
}
