import 'package:rufino/modules/employee/domain/model/address/city.dart';
import 'package:rufino/modules/employee/domain/model/address/complement.dart';
import 'package:rufino/modules/employee/domain/model/address/country.dart';
import 'package:rufino/modules/employee/domain/model/address/neighborhood.dart';
import 'package:rufino/modules/employee/domain/model/address/number.dart';
import 'package:rufino/modules/employee/domain/model/address/state.dart';
import 'package:rufino/modules/employee/domain/model/address/street.dart';
import 'package:rufino/modules/employee/domain/model/address/zipcode.dart';
import 'package:rufino/modules/employee/domain/model/base/model_base.dart';
import 'package:rufino/modules/employee/domain/model/base/text_prop_base.dart';

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
      {super.isLoading, super.isLazyLoading});

  const Address.empty({
    this.zipcode = const Zipcode.empty(),
    this.street = const Street.empty(),
    this.number = const Number.empty(),
    this.complement = const Complement.empty(),
    this.neighborhood = const Neighborhood.empty(),
    this.city = const City.empty(),
    this.state = const State.empty(),
    this.country = const Country.empty(),
    super.isLoading,
    super.isLazyLoading,
  });

  const Address.loading({
    this.zipcode = const Zipcode.empty(),
    this.street = const Street.empty(),
    this.number = const Number.empty(),
    this.complement = const Complement.empty(),
    this.neighborhood = const Neighborhood.empty(),
    this.city = const City.empty(),
    this.state = const State.empty(),
    this.country = const Country.empty(),
    super.isLoading = true,
    super.isLazyLoading,
  });

  Address copyWith(
      {Zipcode? zipcode,
      Street? street,
      Number? number,
      Complement? complement,
      Neighborhood? neighborhood,
      City? city,
      State? state,
      Country? country,
      Object? generic,
      bool? isLoading,
      bool? isLazyLoading}) {
    if (generic != null) {
      switch (generic.runtimeType) {
        case const (Zipcode):
          zipcode = generic as Zipcode?;
        case const (Street):
          street = generic as Street?;
        case const (Number):
          number = generic as Number?;
        case const (Complement):
          complement = generic as Complement?;
        case const (Neighborhood):
          neighborhood = generic as Neighborhood?;
        case const (City):
          city = generic as City?;
        case const (State):
          state = generic as State?;
        case const (Country):
          country = generic as Country?;
      }
    }

    return Address(
      zipcode ?? this.zipcode,
      street ?? this.street,
      number ?? this.number,
      complement ?? this.complement,
      neighborhood ?? this.neighborhood,
      city ?? this.city,
      state ?? this.state,
      country ?? this.country,
      isLoading: isLoading ?? this.isLoading,
      isLazyLoading: isLazyLoading ?? this.isLazyLoading,
    );
  }

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
  List<Object?> get props => [
        zipcode,
        street,
        number,
        complement,
        neighborhood,
        city,
        state,
        country,
        isLoading,
        isLazyLoading
      ];

  List<TextPropBase> get textProps => [
        zipcode,
        street,
        number,
        complement,
        neighborhood,
        city,
        state,
        country,
      ];
}
