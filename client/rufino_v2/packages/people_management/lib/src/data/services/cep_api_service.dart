import 'package:http/http.dart' as http;
import 'package:rufino_core/rufino_core.dart';

import '../models/cep_lookup_model.dart';

/// Resolves a Brazilian postal code (CEP) into this product's DTO.
///
/// The ViaCEP call itself lives in `rufino_core` — it is the same request for
/// every product. This adapter exists only to hand the result back in the
/// shape the People Management repositories already consume.
class CepApiService {
  /// Creates the service over [client].
  CepApiService({required this.client});

  /// The HTTP client used for the lookup.
  final http.Client client;

  /// Looks up the address associated with [cep].
  ///
  /// Throws `CepNotFoundException` when ViaCEP reports the CEP does not
  /// exist, or `CepLookupException` on any other failure.
  Future<CepLookupModel> lookup(String cep) async {
    final lookup = await CepLookupService(client: client).lookup(cep);
    return CepLookupModel.fromLookup(lookup);
  }
}
