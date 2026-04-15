import 'dart:convert';

import 'package:http/http.dart' as http;

import '../../core/errors/cep_exception.dart';
import '../models/cep_lookup_model.dart';

/// Queries the public ViaCEP service to resolve a Brazilian postal code (CEP)
/// into its corresponding address components.
///
/// See https://viacep.com.br for the public API contract.
class CepApiService {
  CepApiService({required this.client});

  final http.Client client;

  static final Uri _baseUri = Uri.parse('https://viacep.com.br/ws');

  /// Looks up the address associated with [cep].
  ///
  /// The [cep] may be passed with or without formatting — non-digit
  /// characters are stripped. Throws [CepNotFoundException] when ViaCEP
  /// reports the CEP does not exist, or [CepLookupException] on any other
  /// failure.
  Future<CepLookupModel> lookup(String cep) async {
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

      return CepLookupModel.fromJson(body);
    } on CepException {
      rethrow;
    } catch (e) {
      throw CepLookupException(e);
    }
  }
}
