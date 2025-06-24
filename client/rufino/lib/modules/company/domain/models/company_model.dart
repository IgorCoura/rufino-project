import 'package:equatable/equatable.dart';
import 'package:rufino/modules/company/domain/models/address_prop.dart';
import 'package:rufino/modules/company/domain/models/cnpj_prop.dart';
import 'package:rufino/modules/company/domain/models/contact_prop.dart';
import 'package:rufino/modules/company/domain/models/corporate_name_prop.dart';
import 'package:rufino/modules/company/domain/models/fantasy_name_prop.dart';

class CompanyModel extends Equatable {
  final String id;
  final CorporateNameProp corporateName;
  final FantasyNameProp fantasyName;
  final CNPJProp cnpj;
  final ContactProp contact;
  final AddressProp address;

  const CompanyModel(
    this.id,
    this.corporateName,
    this.fantasyName,
    this.cnpj,
    this.contact,
    this.address,
  );

  const CompanyModel.empty()
      : id = "",
        corporateName = const CorporateNameProp.empty(),
        fantasyName = const FantasyNameProp.empty(),
        cnpj = const CNPJProp.empty(),
        contact = const ContactProp.empty(),
        address = const AddressProp.empty();

  CompanyModel copyWith({
    String? id,
    CorporateNameProp? corporateName,
    FantasyNameProp? fantasyName,
    CNPJProp? cnpj,
    ContactProp? contact,
    AddressProp? address,
    Object? generic,
  }) {
    switch (generic.runtimeType) {
      case const (CorporateNameProp):
        corporateName = generic as CorporateNameProp?;
        break;
      case const (FantasyNameProp):
        fantasyName = generic as FantasyNameProp?;
        break;
      case const (CNPJProp):
        cnpj = generic as CNPJProp?;
        break;
      case const (ContactProp):
        contact = generic as ContactProp?;
        break;
      case const (AddressProp):
        address = generic as AddressProp?;
        break;
      default:
        contact = this.contact.copyWith(generic: generic);
        address = this.address.copyWith(generic: generic);
        break;
    }
    return CompanyModel(
      id ?? this.id,
      corporateName ?? this.corporateName,
      fantasyName ?? this.fantasyName,
      cnpj ?? this.cnpj,
      contact ?? this.contact,
      address ?? this.address,
    );
  }

  factory CompanyModel.fromJson(Map<String, dynamic> json) {
    return CompanyModel(
      json['id'],
      CorporateNameProp(json['corporateName']),
      FantasyNameProp(json['fantasyName']),
      CNPJProp(json['cnpj']),
      ContactProp(
        EmailProp(json['email']),
        PhoneProp(json['phone']),
      ),
      AddressProp(
        ZipCodeProp(json['zipCode']),
        StreetProp(json['street']),
        NumberProp(json['number']),
        ComplementProp(json['complement']),
        NeighborhoodProp(json['neighborhood']),
        CityProp(json['city']),
        StateProp(json['state']),
        CountryProp(json['country']),
      ),
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'corporateName': corporateName.value,
      'fantasyName': fantasyName.value,
      'cnpj': cnpj.value,
      'email': contact.email.value,
      'phone': contact.phone.value,
      'zipCode': address.zipCode.value,
      'street': address.street.value,
      'number': address.number.value,
      'complement': address.complement.value,
      'neighborhood': address.neighborhood.value,
      'city': address.city.value,
      'state': address.state.value,
      'country': address.country.value,
    };
  }

  Map<String, dynamic> toJsonCreate() {
    return {
      'corporateName': corporateName.value,
      'fantasyName': fantasyName.value,
      'cnpj': cnpj.value,
      'email': contact.email.value,
      'phone': contact.phone.value,
      'zipCode': address.zipCode.value,
      'street': address.street.value,
      'number': address.number.value,
      'complement': address.complement.value,
      'neighborhood': address.neighborhood.value,
      'city': address.city.value,
      'state': address.state.value,
      'country': address.country.value,
    };
  }

  @override
  List<Object?> get props =>
      [id, corporateName, fantasyName, cnpj, contact, address];
}
