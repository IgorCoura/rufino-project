import '../../domain/entities/company.dart';
import '../../domain/entities/company_detail.dart';

class CompanyApiModel {
  const CompanyApiModel({
    required this.id,
    required this.corporateName,
    required this.fantasyName,
    required this.cnpj,
    this.email,
    this.phone,
    this.zipCode,
    this.street,
    this.number,
    this.complement,
    this.neighborhood,
    this.city,
    this.state,
    this.country,
  });

  final String id;
  final String corporateName;
  final String fantasyName;
  final String cnpj;
  final String? email;
  final String? phone;
  final String? zipCode;
  final String? street;
  final String? number;
  final String? complement;
  final String? neighborhood;
  final String? city;
  final String? state;
  final String? country;

  factory CompanyApiModel.fromJson(Map<String, dynamic> json) {
    return CompanyApiModel(
      id: json['id'] as String,
      corporateName: json['corporateName'] as String,
      fantasyName: json['fantasyName'] as String,
      cnpj: json['cnpj'] as String,
      email: json['email'] as String?,
      phone: json['phone'] as String?,
      zipCode: json['zipCode'] as String?,
      street: json['street'] as String?,
      number: json['number'] as String?,
      complement: json['complement'] as String?,
      neighborhood: json['neighborhood'] as String?,
      city: json['city'] as String?,
      state: json['state'] as String?,
      country: json['country'] as String?,
    );
  }

  Map<String, dynamic> toJson() => {
        'id': id,
        'corporateName': corporateName,
        'fantasyName': fantasyName,
        'cnpj': cnpj,
        if (email != null) 'email': email,
        if (phone != null) 'phone': phone,
        if (zipCode != null) 'zipCode': zipCode,
        if (street != null) 'street': street,
        if (number != null) 'number': number,
        if (complement != null) 'complement': complement,
        if (neighborhood != null) 'neighborhood': neighborhood,
        if (city != null) 'city': city,
        if (state != null) 'state': state,
        if (country != null) 'country': country,
      };

  Map<String, dynamic> toCreateJson() => {
        'corporateName': corporateName,
        'fantasyName': fantasyName,
        'cnpj': cnpj,
        if (email != null) 'email': email,
        if (phone != null) 'phone': phone,
        if (zipCode != null) 'zipCode': zipCode,
        if (street != null) 'street': street,
        if (number != null) 'number': number,
        if (complement != null) 'complement': complement,
        if (neighborhood != null) 'neighborhood': neighborhood,
        if (city != null) 'city': city,
        if (state != null) 'state': state,
        if (country != null) 'country': country,
      };

  Company toEntity() => Company(
        id: id,
        corporateName: corporateName,
        fantasyName: fantasyName,
        cnpj: cnpj,
      );

  CompanyDetail toDetailEntity() => CompanyDetail(
        id: id,
        corporateName: corporateName,
        fantasyName: fantasyName,
        cnpj: cnpj,
        email: email ?? '',
        phone: phone ?? '',
        zipCode: zipCode ?? '',
        street: street ?? '',
        number: number ?? '',
        complement: complement ?? '',
        neighborhood: neighborhood ?? '',
        city: city ?? '',
        state: state ?? '',
        country: country ?? '',
      );
}
