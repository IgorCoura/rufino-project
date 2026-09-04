import 'dart:convert';

import 'package:http/http.dart' as http;

import 'cep_exception.dart';

/// The address components ViaCEP returns for a postal code.
///
/// Deliberately not an address entity: each product maps this into whatever
/// address type it owns. Putting a product's `Address` here would drag one
/// product's domain into the foundation of the other.
class CepLookup {
  /// Creates the lookup result.
  const CepLookup({
    required this.zipCode,
    required this.street,
    required this.complement,
    required this.neighborhood,
    required this.city,
    required this.state,
  });

  /// Parses a ViaCEP JSON response.
  factory CepLookup.fromJson(Map<String, dynamic> json) {
    return CepLookup(
      zipCode: (json['cep'] as String?) ?? '',
      street: (json['logradouro'] as String?) ?? '',
      complement: (json['complemento'] as String?) ?? '',
      neighborhood: (json['bairro'] as String?) ?? '',
      city: (json['localidade'] as String?) ?? '',
      state: (json['uf'] as String?) ?? '',
    );
  }

  /// The postal code, as ViaCEP formatted it.
  final String zipCode;

  /// Street name (`logradouro`).
  final String street;

  /// Complement (`complemento`), often empty.
  final String complement;

  /// Neighborhood (`bairro`).
  final String neighborhood;

  /// City (`localidade`).
  final String city;

  /// Two-letter state code (`uf`).
  final String state;
}

/// Queries the public ViaCEP service to resolve a Brazilian postal code (CEP)
/// into its corresponding address components.
///
/// See https://viacep.com.br for the public API contract.
class CepLookupService {
  /// Creates the service over [client].
  CepLookupService({required this.client});

  /// The HTTP client used for the lookup.
  final http.Client client;

  static final Uri _baseUri = Uri.parse('https://viacep.com.br/ws');

  /// Looks up the address associated with [cep].
  ///
  /// The [cep] may be passed with or without formatting — non-digit
  /// characters are stripped. Throws [CepNotFoundException] when ViaCEP
  /// reports the CEP does not exist, or [CepLookupException] on any other
  /// failure.
  Future<CepLookup> lookup(String cep) async {
    final digits = cep.replaceAll(RegExp(r'[^\d]'), '');

    final uri = _baseUri.replace(
      pathSegments: [..._baseUri.pathSegments, digits, 'json'],
    );

    try {
      final response = await client.get(uri);

      if (response.statusCode < 200 || response.statusCode >= 300) {
        throw CepLookupException(
          'HTTP ${response.statusCode}: ${response.reasonPhrase}',
        );
      }

      final body = jsonDecode(response.body);
      if (body is! Map<String, dynamic>) {
        throw const CepLookupException('Unexpected ViaCEP response shape');
      }

      if (body['erro'] == true) {
        throw const CepNotFoundException();
      }

      return CepLookup.fromJson(body);
    } on CepException {
      rethrow;
    } catch (e) {
      throw CepLookupException(e);
    }
  }
}
