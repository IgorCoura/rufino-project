import 'company.dart';

/// Extended company information including contact and address data.
class CompanyDetail extends Company {
  const CompanyDetail({
    required super.id,
    required super.corporateName,
    required super.fantasyName,
    required super.cnpj,
    required this.email,
    required this.phone,
    required this.zipCode,
    required this.street,
    required this.number,
    required this.complement,
    required this.neighborhood,
    required this.city,
    required this.state,
    required this.country,
  });

  /// The company's email address.
  final String email;

  /// The company's phone number (digits only).
  final String phone;

  final String zipCode;
  final String street;
  final String number;
  final String complement;
  final String neighborhood;
  final String city;
  final String state;
  final String country;

  /// Whether this company has an email filled in.
  bool get hasEmail => email.isNotEmpty;

  /// Whether this company has a phone number filled in.
  bool get hasPhone => phone.isNotEmpty;

  /// Whether this company has the main address fields filled in.
  bool get hasAddress =>
      zipCode.isNotEmpty &&
      street.isNotEmpty &&
      city.isNotEmpty &&
      state.isNotEmpty;

  /// Formats the phone for display as "(11) 98765-4321".
  ///
  /// Returns the raw value unchanged if it does not have 10 or 11 digits.
  String get formattedPhone {
    final digits = phone.replaceAll(RegExp(r'[^\d]'), '');
    if (digits.length == 11) {
      return '(${digits.substring(0, 2)}) ${digits.substring(2, 7)}-'
          '${digits.substring(7)}';
    }
    if (digits.length == 10) {
      return '(${digits.substring(0, 2)}) ${digits.substring(2, 6)}-'
          '${digits.substring(6)}';
    }
    return phone;
  }

  /// Formats the zip code for display as "01310-100".
  ///
  /// Returns the raw value unchanged if it does not have exactly 8 digits.
  String get formattedZipCode {
    final digits = zipCode.replaceAll(RegExp(r'[^\d]'), '');
    if (digits.length != 8) return zipCode;
    return '${digits.substring(0, 5)}-${digits.substring(5)}';
  }
}
