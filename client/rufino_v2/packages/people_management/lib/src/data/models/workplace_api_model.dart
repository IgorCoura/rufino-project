import '../../domain/entities/address.dart';
import '../../domain/entities/workplace.dart';

/// DTO that mirrors the JSON shape of an address returned by the
/// people-management API.
class AddressApiModel {
  const AddressApiModel({
    required this.zipCode,
    required this.street,
    required this.number,
    required this.complement,
    required this.neighborhood,
    required this.city,
    required this.state,
    required this.country,
  });

  final String zipCode;
  final String street;
  final String number;
  final String complement;
  final String neighborhood;
  final String city;
  final String state;
  final String country;

  /// Deserialises an [AddressApiModel] from a JSON map.
  factory AddressApiModel.fromJson(Map<String, dynamic> json) {
    return AddressApiModel(
      zipCode: json['zipCode'] as String? ?? '',
      street: json['street'] as String? ?? '',
      number: json['number'] as String? ?? '',
      complement: json['complement'] as String? ?? '',
      neighborhood: json['neighborhood'] as String? ?? '',
      city: json['city'] as String? ?? '',
      state: json['state'] as String? ?? '',
      country: json['country'] as String? ?? '',
    );
  }

  /// Serialises this model to a JSON map.
  Map<String, dynamic> toJson() {
    return {
      'zipCode': zipCode,
      'street': street,
      'number': number,
      'complement': complement,
      'neighborhood': neighborhood,
      'city': city,
      'state': state,
      'country': country,
    };
  }

  /// Converts this DTO to the domain [Address] entity.
  Address toEntity() {
    return Address(
      zipCode: zipCode,
      street: street,
      number: number,
      complement: complement,
      neighborhood: neighborhood,
      city: city,
      state: state,
      country: country,
    );
  }
}

/// DTO that mirrors the JSON shape of a workplace returned by the
/// people-management API.
class WorkplaceApiModel {
  const WorkplaceApiModel({
    required this.id,
    required this.name,
    required this.address,
  });

  final String id;
  final String name;
  final AddressApiModel address;

  /// Deserialises a [WorkplaceApiModel] from a JSON map.
  factory WorkplaceApiModel.fromJson(Map<String, dynamic> json) {
    return WorkplaceApiModel(
      id: json['id'] as String? ?? '',
      name: json['name'] as String? ?? '',
      address: AddressApiModel.fromJson(
          json['address'] as Map<String, dynamic>? ?? {}),
    );
  }

  /// Serialises this model to JSON including the [id] field (used for updates).
  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'name': name,
      'address': address.toJson(),
    };
  }

  /// Serialises this model to JSON without the [id] field (used for creates).
  Map<String, dynamic> toCreateJson() {
    return {
      'name': name,
      'address': address.toJson(),
    };
  }

  /// Converts this DTO to the domain [Workplace] entity.
  Workplace toEntity() {
    return Workplace(
      id: id,
      name: name,
      address: address.toEntity(),
    );
  }
}
