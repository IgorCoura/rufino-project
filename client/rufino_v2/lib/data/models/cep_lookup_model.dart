import 'package:rufino_core/rufino_core.dart';

import '../../domain/entities/address.dart';

/// DTO representing a ViaCEP API response.
///
/// Mirrors the JSON payload returned by `https://viacep.com.br/ws/{cep}/json/`.
///
/// The HTTP call itself lives in `rufino_core` so both products can look a
/// CEP up; what stays here is the mapping to this product's [Address], which
/// is the part the foundation must not know about.
class CepLookupModel {
  /// Creates the DTO with the raw ViaCEP field names.
  const CepLookupModel({
    required this.cep,
    required this.logradouro,
    required this.complemento,
    required this.bairro,
    required this.localidade,
    required this.uf,
  });

  /// Parses a ViaCEP JSON response into a [CepLookupModel].
  factory CepLookupModel.fromJson(Map<String, dynamic> json) {
    return CepLookupModel(
      cep: (json['cep'] as String?) ?? '',
      logradouro: (json['logradouro'] as String?) ?? '',
      complemento: (json['complemento'] as String?) ?? '',
      bairro: (json['bairro'] as String?) ?? '',
      localidade: (json['localidade'] as String?) ?? '',
      uf: (json['uf'] as String?) ?? '',
    );
  }

  /// Adapts the shared [CepLookup] result into this product's DTO.
  factory CepLookupModel.fromLookup(CepLookup lookup) {
    return CepLookupModel(
      cep: lookup.zipCode,
      logradouro: lookup.street,
      complemento: lookup.complement,
      bairro: lookup.neighborhood,
      localidade: lookup.city,
      uf: lookup.state,
    );
  }

  /// The postal code as ViaCEP formatted it.
  final String cep;

  /// Street name.
  final String logradouro;

  /// Complement, often empty.
  final String complemento;

  /// Neighborhood.
  final String bairro;

  /// City.
  final String localidade;

  /// Two-letter state code.
  final String uf;

  /// Converts this DTO into an [Address] domain entity.
  ///
  /// The returned address leaves [Address.number] empty (ViaCEP does not
  /// return street numbers) and defaults [Address.country] to `'Brasil'`.
  Address toAddress() {
    return Address(
      zipCode: cep,
      street: logradouro,
      number: '',
      complement: complemento,
      neighborhood: bairro,
      city: localidade,
      state: uf,
      country: 'Brasil',
    );
  }
}
