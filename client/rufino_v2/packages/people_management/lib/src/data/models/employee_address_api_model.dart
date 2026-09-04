import '../../domain/entities/address.dart';

/// Data Transfer Object for the employee address endpoint.
///
/// Note: the GET response from the API uses the misspelled key `"coutry"` for
/// the country field. The PUT body uses the correctly spelled `"country"`.
class EmployeeAddressApiModel {
  /// Creates an [EmployeeAddressApiModel] from the given fields.
  const EmployeeAddressApiModel({
    required this.zipCode,
    required this.street,
    required this.number,
    required this.complement,
    required this.neighborhood,
    required this.city,
    required this.state,
    required this.country,
  });

  /// The postal code.
  final String zipCode;

  /// The street name.
  final String street;

  /// The street number.
  final String number;

  /// The complement (apartment, suite, etc.).
  final String complement;

  /// The neighborhood name.
  final String neighborhood;

  /// The city name.
  final String city;

  /// The state abbreviation.
  final String state;

  /// The country name.
  final String country;

  /// Deserialises an [EmployeeAddressApiModel] from the API JSON map.
  ///
  /// Uses the misspelled key `"coutry"` for country as returned by the GET
  /// endpoint, falling back to `"country"` if present.
  factory EmployeeAddressApiModel.fromJson(Map<String, dynamic> json) {
    return EmployeeAddressApiModel(
      zipCode: json['zipcode'] as String? ?? '',
      street: json['street'] as String? ?? '',
      number: json['number'] as String? ?? '',
      complement: json['complement'] as String? ?? '',
      neighborhood: json['neighborhood'] as String? ?? '',
      city: json['city'] as String? ?? '',
      state: json['state'] as String? ?? '',
      // The API GET response has a typo: "coutry" instead of "country".
      country: (json['coutry'] ?? json['country']) as String? ?? '',
    );
  }

  /// Converts this model to a domain [Address] entity.
  ///
  /// The [zipCode] is normalised to digits-only so the UI can apply the
  /// `#####-###` display mask consistently regardless of API storage format.
  Address toEntity() {
    return Address(
      zipCode: zipCode.replaceAll(RegExp(r'[^\d]'), ''),
      street: street,
      number: number,
      complement: complement,
      neighborhood: neighborhood,
      city: city,
      state: state,
      country: country,
    );
  }

  /// Builds the JSON body for a PUT request to update employee address.
  ///
  /// Uses the correctly spelled `"country"` key for the PUT body.
  static Map<String, dynamic> toJsonMap(Address address, String employeeId) {
    return {
      'employeeId': employeeId,
      'zipcode': address.zipCode,
      'street': address.street,
      'number': address.number,
      'complement': address.complement,
      'neighborhood': address.neighborhood,
      'city': address.city,
      'state': address.state,
      'country': address.country,
    };
  }
}
