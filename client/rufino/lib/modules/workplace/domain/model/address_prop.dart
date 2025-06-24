import 'package:equatable/equatable.dart';

class AddressProp extends Equatable {
  final ZipCodeProp zipCode;
  final StreetProp street;
  final NumberProp number;
  final ComplementProp complement;
  final NeighborhoodProp neighborhood;
  final CityProp city;
  final StateProp state;
  final CountryProp country;

  const AddressProp(
    this.zipCode,
    this.street,
    this.number,
    this.complement,
    this.neighborhood,
    this.city,
    this.state,
    this.country,
  );

  const AddressProp.empty()
      : zipCode = const ZipCodeProp.empty(),
        street = const StreetProp.empty(),
        number = const NumberProp.empty(),
        complement = const ComplementProp.empty(),
        neighborhood = const NeighborhoodProp.empty(),
        city = const CityProp.empty(),
        state = const StateProp.empty(),
        country = const CountryProp.empty();

  AddressProp copyWith({
    ZipCodeProp? zipCode,
    StreetProp? street,
    NumberProp? number,
    ComplementProp? complement,
    NeighborhoodProp? neighborhood,
    CityProp? city,
    StateProp? state,
    CountryProp? country,
    Object? generic,
  }) {
    switch (generic.runtimeType) {
      case const (ZipCodeProp):
        zipCode = generic as ZipCodeProp?;
        break;
      case const (StreetProp):
        street = generic as StreetProp?;
        break;
      case const (NumberProp):
        number = generic as NumberProp?;
        break;
      case const (ComplementProp):
        complement = generic as ComplementProp?;
        break;
      case const (NeighborhoodProp):
        neighborhood = generic as NeighborhoodProp?;
        break;
      case const (CityProp):
        city = generic as CityProp?;
        break;
      case const (StateProp):
        state = generic as StateProp?;
        break;
      case const (CountryProp):
        country = generic as CountryProp?;
        break;
    }
    return AddressProp(
      zipCode ?? this.zipCode,
      street ?? this.street,
      number ?? this.number,
      complement ?? this.complement,
      neighborhood ?? this.neighborhood,
      city ?? this.city,
      state ?? this.state,
      country ?? this.country,
    );
  }

  factory AddressProp.fromJson(Map<String, dynamic> json) {
    return AddressProp(
      ZipCodeProp(json['zipCode']),
      StreetProp(json['street']),
      NumberProp(json['number']),
      ComplementProp(json['complement']),
      NeighborhoodProp(json['neighborhood']),
      CityProp(json['city']),
      StateProp(json['state']),
      CountryProp(json['country']),
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'zipCode': zipCode.value,
      'street': street.value,
      'number': number.value,
      'complement': complement.value,
      'neighborhood': neighborhood.value,
      'city': city.value,
      'state': state.value,
      'country': country.value,
    };
  }

  @override
  List<Object?> get props => [
        zipCode,
        street,
        number,
        complement,
        neighborhood,
        city,
        state,
        country,
      ];
}

class ZipCodeProp extends Equatable {
  final String value;

  const ZipCodeProp(this.value);

  const ZipCodeProp.empty() : value = "";

  static String? validate(String? value) {
    if (value == null || value.isEmpty) {
      return "Não pode ser vazio.";
    }

    value = value.replaceAll(RegExp(r'\D'), '');

    final pattern = RegExp(r'^\d{8}$');
    if (!pattern.hasMatch(value)) {
      return "CEP inválido.";
    }

    return null;
  }

  @override
  List<Object?> get props => [value];
}

class StreetProp extends Equatable {
  final String value;

  const StreetProp(this.value);

  const StreetProp.empty() : value = "";

  static String? validate(String? value) {
    if (value == null || value.isEmpty) {
      return "Não pode ser vazio.";
    }

    if (value.length > 100) {
      return "Não pode ser maior que 100 caracteres.";
    }
    return null;
  }

  @override
  List<Object?> get props => [value];
}

class NumberProp extends Equatable {
  final String value;

  const NumberProp(this.value);

  const NumberProp.empty() : value = "";

  static String? validate(String? value) {
    if (value == null || value.isEmpty) {
      return "Não pode ser vazio.";
    }

    value = value.replaceAll(RegExp(r'\D'), '');

    if (value.length > 10) {
      return "Não pode ser maior que 10 caracteres.";
    }

    return null;
  }

  @override
  List<Object?> get props => [value];
}

class ComplementProp extends Equatable {
  final String value;

  const ComplementProp(this.value);

  const ComplementProp.empty() : value = "";

  static String? validate(String? value) {
    if (value == null) {
      return "Não pode ser vazio.";
    }

    if (value.isEmpty) {
      return null; // Complement can be empty
    }

    if (value.length > 50) {
      return "Não pode ser maior que 50 caracteres.";
    }
    return null;
  }

  @override
  List<Object?> get props => [value];
}

class NeighborhoodProp extends Equatable {
  final String value;

  const NeighborhoodProp(this.value);

  const NeighborhoodProp.empty() : value = "";

  static String? validate(String? value) {
    if (value == null || value.isEmpty) {
      return "Não pode ser vazio.";
    }
    if (value.length > 50) {
      return "Não pode ser maior que 50 caracteres.";
    }
    return null;
  }

  @override
  List<Object?> get props => [value];
}

class CityProp extends Equatable {
  final String value;

  const CityProp(this.value);

  const CityProp.empty() : value = "";

  static String? validate(String? value) {
    if (value == null || value.isEmpty) {
      return "Não pode ser vazio.";
    }

    if (value.length > 50) {
      return "Não pode ser maior que 50 caracteres.";
    }
    return null;
  }

  @override
  List<Object?> get props => [value];
}

class StateProp extends Equatable {
  final String value;

  const StateProp(this.value);

  const StateProp.empty() : value = "";

  static String? validate(String? value) {
    if (value == null || value.isEmpty) {
      return "Não pode ser vazio.";
    }

    if (value.length > 50) {
      return "Não pode ser maior que 50 caracteres.";
    }
    return null;
  }

  @override
  List<Object?> get props => [value];
}

class CountryProp extends Equatable {
  final String value;

  const CountryProp(this.value);

  const CountryProp.empty() : value = "";

  static String? validate(String? value) {
    if (value == null || value.isEmpty) {
      return "Não pode ser vazio.";
    }

    if (value.length > 50) {
      return "Não pode ser maior que 50 caracteres.";
    }
    return null;
  }

  @override
  List<Object?> get props => [value];
}
