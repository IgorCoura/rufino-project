import '../../domain/entities/address.dart';

/// DTO representing a ViaCEP API response.
///
/// Mirrors the JSON payload returned by `https://viacep.com.br/ws/{cep}/json/`.
class CepLookupModel {
  const CepLookupModel({
    required this.cep,
    required this.logradouro,
    required this.complemento,
    required this.bairro,
    required this.localidade,
    required this.uf,
  });

  final String cep;
  final String logradouro;
  final String complemento;
  final String bairro;
  final String localidade;
  final String uf;

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
