import 'company.dart';

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

  final String email;
  final String phone;
  final String zipCode;
  final String street;
  final String number;
  final String complement;
  final String neighborhood;
  final String city;
  final String state;
  final String country;
}
