import 'package:equatable/equatable.dart';
import 'package:rufino/modules/workplace/domain/model/address_prop.dart';

class Workplace extends Equatable {
  final String id;
  final String name;
  final AddressProp address;

  const Workplace({
    required this.id,
    required this.name,
    required this.address,
  });

  const Workplace.empty()
      : id = '',
        name = '',
        address = const AddressProp.empty();

  Workplace copyWith({
    String? name,
    AddressProp? address,
    Object? generic,
  }) {
    switch (generic.runtimeType) {
      case const (String):
        name = generic as String?;
        break;
      case const (AddressProp):
        address = generic as AddressProp?;
        break;
      default:
        address = this.address.copyWith(generic: generic);
        break;
    }
    return Workplace(
      id: id,
      name: name ?? this.name,
      address: address ?? this.address,
    );
  }

  factory Workplace.fromJson(Map<String, dynamic> json) {
    return Workplace(
      id: json['id'] as String,
      name: json['name'] as String,
      address: AddressProp(
        ZipCodeProp(json['address']['zipCode']),
        StreetProp(json['address']['street']),
        NumberProp(json['address']['number']),
        ComplementProp(json['address']['complement']),
        NeighborhoodProp(json['address']['neighborhood']),
        CityProp(json['address']['city']),
        StateProp(json['address']['state']),
        CountryProp(json['address']['country']),
      ),
    );
  }

  String get minimalAddress {
    return '${address.neighborhood.value}, ${address.city.value} - ${address.state.value}';
  }

  static List<Workplace> fromJsonList(List<dynamic> jsonList) {
    return jsonList
        .map((json) => Workplace.fromJson(json as Map<String, dynamic>))
        .toList();
  }

  Map<String, dynamic> toJsonCreate() {
    return {
      'name': name,
      'address': {
        'zipCode': address.zipCode.value,
        'street': address.street.value,
        'number': address.number.value,
        'complement': address.complement.value,
        'neighborhood': address.neighborhood.value,
        'city': address.city.value,
        'state': address.state.value,
        'country': address.country.value,
      }
    };
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'name': name,
      'address': {
        'zipCode': address.zipCode.value,
        'street': address.street.value,
        'number': address.number.value,
        'complement': address.complement.value,
        'neighborhood': address.neighborhood.value,
        'city': address.city.value,
        'state': address.state.value,
        'country': address.country.value,
      }
    };
  }

  static String? validateName(String? value) {
    if (value == null || value.isEmpty) {
      return "Não pode ser vazio.";
    }
    if (value.length > 100) {
      return "Não pode ser maior que 100 caracteres.";
    }
    return null;
  }

  @override
  List<Object?> get props => [id, name, address];
}
