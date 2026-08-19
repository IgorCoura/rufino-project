import 'dart:convert';

import 'package:http/http.dart' as http;
import 'package:rufino_core/rufino_core.dart';
import 'package:uuid/uuid.dart';

import '../domain/payer_profile.dart';
import 'payer_profile_api_models.dart';

const _uuid = Uuid();

/// HTTP client for the payer profile endpoints — singular routes, because
/// the profile is one per tenant.
class PayerProfileApiService {
  /// Creates the service against [baseUrl].
  PayerProfileApiService({
    required this.client,
    required this.baseUrl,
    required this.getAuthHeader,
    required this.getTenantId,
  });

  /// The shared HTTP client.
  final http.Client client;

  /// Host (`host:port`) or full origin (`http://host:port`) of the service.
  final String baseUrl;

  /// Supplies the `Authorization` header value.
  final Future<String> Function() getAuthHeader;

  /// Supplies the selected tenant's id.
  final String Function() getTenantId;

  Uri _uri(String path, [Map<String, dynamic>? query]) {
    final full = '/api/v1/${getTenantId()}$path';
    final params = query?.map((k, v) => MapEntry(k, v.toString()));
    if (baseUrl.contains('://')) {
      final base = Uri.parse(baseUrl);
      return base.replace(path: full, queryParameters: params);
    }
    return Uri.https(baseUrl, full, params);
  }

  Future<Map<String, String>> _headers({bool write = false}) async {
    return {
      'Authorization': await getAuthHeader(),
      'Content-Type': 'application/json',
      if (write) 'x-requestid': _uuid.v4(),
    };
  }

  /// Returns the profile, or `null` when none was registered yet.
  Future<PayerProfile?> getProfile() async {
    final response = await client.get(
      _uri('/payer-profile'),
      headers: await _headers(),
    );
    // A 404 here is the onboarding state, not a failure: the cadastro simply
    // does not exist yet.
    if (response.statusCode == 404) return null;
    checkApiStatus(response);
    return PayerProfileMapper.fromJson(
      jsonDecode(response.body) as Map<String, dynamic>,
    );
  }

  /// Registers the profile and returns its id.
  Future<String> registerProfile({
    required String kind,
    required String legalName,
    required String primaryTaxId,
  }) async {
    final response = await client.post(
      _uri('/payer-profile'),
      headers: await _headers(write: true),
      body: jsonEncode({
        'kind': kind,
        'legalName': legalName.trim(),
        'primaryTaxId': primaryTaxId.trim(),
      }),
    );
    checkApiStatus(response);
    return (jsonDecode(response.body) as Map<String, dynamic>)['id'] as String;
  }

  /// Renames the payer.
  Future<void> changeLegalName(String legalName) async {
    final response = await client.put(
      _uri('/payer-profile/legal-name'),
      headers: await _headers(write: true),
      body: jsonEncode({'legalName': legalName.trim()}),
    );
    checkApiStatus(response);
  }

  /// Adds an extra fiscal document.
  Future<void> addTaxId(String taxId) async {
    final response = await client.post(
      _uri('/payer-profile/tax-ids'),
      headers: await _headers(write: true),
      body: jsonEncode({'taxId': taxId.trim()}),
    );
    checkApiStatus(response);
  }

  /// Removes an extra fiscal document. The value goes in the query string —
  /// a formatted CNPJ contains `/` and would die in routing.
  Future<void> removeTaxId(String taxId) async {
    final response = await client.delete(
      _uri('/payer-profile/tax-ids', {'taxId': taxId}),
      headers: await _headers(write: true),
    );
    checkApiStatus(response);
  }

  /// Turns CNPJ-root matching on or off.
  Future<void> setCnpjRootMatching({required bool enabled}) async {
    final response = await client.put(
      _uri('/payer-profile/cnpj-root-matching'),
      headers: await _headers(write: true),
      body: jsonEncode({'enabled': enabled}),
    );
    checkApiStatus(response);
  }

  /// Links (or clears) the payment provider account. Returns whether
  /// payments can now be scheduled — the reference itself never comes back.
  Future<bool> linkAsaasAccount(String? accountRef) async {
    final response = await client.put(
      _uri('/payer-profile/asaas-account'),
      headers: await _headers(write: true),
      body: jsonEncode({'accountRef': accountRef?.trim()}),
    );
    checkApiStatus(response);
    final body = jsonDecode(response.body) as Map<String, dynamic>;
    return body['canSchedulePayments'] as bool? ?? false;
  }
}
