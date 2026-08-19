import 'dart:convert';

import 'package:http/http.dart' as http;
import 'package:rufino_core/rufino_core.dart';
import 'package:uuid/uuid.dart';

import '../domain/payee.dart';
import '../domain/payee_repository.dart';
import 'payee_api_models.dart';

const _uuid = Uuid();

/// HTTP client for the payee endpoints of the BillPayment bounded context.
///
/// Holds no state. The [client] arrives from the shell already wrapped, which
/// is what makes a 401 anywhere here reach the app's session listener without
/// this module knowing anything about sessions. Every route carries the
/// current tenant, supplied by [getTenantId].
class PayeeApiService {
  /// Creates the service against [baseUrl].
  PayeeApiService({
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

  /// Supplies the selected tenant's id — every route of this BC is scoped by
  /// it, and the server validates it against the token's `bp_tenants` claim.
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
      // Idempotency: the backend deduplicates a repeated command by this id,
      // so a double tap cannot register the same payee twice.
      if (write) 'x-requestid': _uuid.v4(),
    };
  }

  /// Lists payees, one cursor page at a time.
  Future<PayeePage> listPayees({String? cursor, int limit = 50}) async {
    final response = await client.get(
      _uri('/payees', {
        if (cursor != null) 'cursor': cursor,
        'limit': limit,
      }),
      headers: await _headers(),
    );
    checkApiStatus(response);
    return PayeePageMapper.fromJson(
      jsonDecode(response.body) as Map<String, dynamic>,
    );
  }

  /// Returns one payee.
  Future<Payee> getPayee(String id) async {
    final response = await client.get(
      _uri('/payees/$id'),
      headers: await _headers(),
    );
    checkApiStatus(response);
    return PayeeMapper.fromJson(
      jsonDecode(response.body) as Map<String, dynamic>,
    );
  }

  /// Finds a payee by CPF/CNPJ, or `null` when none is registered.
  ///
  /// The document goes in the query string, never the path — a formatted
  /// CNPJ contains `/` and would die in routing before the controller. A 204
  /// means "not registered", which is a state, not an error.
  Future<Payee?> findByTaxId(String taxId) async {
    final response = await client.get(
      _uri('/payees/by-tax-id', {'taxId': taxId}),
      headers: await _headers(),
    );
    if (response.statusCode == 204) return null;
    checkApiStatus(response);
    return PayeeMapper.fromJson(
      jsonDecode(response.body) as Map<String, dynamic>,
    );
  }

  /// Registers a payee and returns its id.
  Future<String> registerPayee({
    required String legalName,
    required String taxId,
    required AmountPolicyInput amountPolicy,
  }) async {
    final response = await client.post(
      _uri('/payees'),
      headers: await _headers(write: true),
      body: jsonEncode({
        'legalName': legalName.trim(),
        'taxId': taxId.trim(),
        ...amountPolicy.toJson(),
      }),
    );
    checkApiStatus(response);
    return (jsonDecode(response.body) as Map<String, dynamic>)['id'] as String;
  }

  /// Renames the payee.
  Future<void> changeLegalName(String id, String legalName) async {
    final response = await client.put(
      _uri('/payees/$id/legal-name'),
      headers: await _headers(write: true),
      body: jsonEncode({'legalName': legalName.trim()}),
    );
    checkApiStatus(response);
  }

  /// Replaces the amount policy.
  Future<void> changeAmountPolicy(String id, AmountPolicyInput policy) async {
    final response = await client.put(
      _uri('/payees/$id/amount-policy'),
      headers: await _headers(write: true),
      body: jsonEncode(policy.toJson()),
    );
    checkApiStatus(response);
  }

  /// Adds an alias.
  Future<void> addAlias(String id, String alias) async {
    final response = await client.post(
      _uri('/payees/$id/aliases'),
      headers: await _headers(write: true),
      body: jsonEncode({'alias': alias.trim()}),
    );
    checkApiStatus(response);
  }

  /// Removes an alias. The value goes in the query string — an alias is free
  /// text and may contain `/`.
  Future<void> removeAlias(String id, String alias) async {
    final response = await client.delete(
      _uri('/payees/$id/aliases', {'alias': alias}),
      headers: await _headers(write: true),
    );
    checkApiStatus(response);
  }

  /// Adds an accepted bank.
  Future<void> addAcceptedBank(String id, String bankCode) async {
    final response = await client.post(
      _uri('/payees/$id/accepted-banks'),
      headers: await _headers(write: true),
      body: jsonEncode({'bankCode': bankCode.trim()}),
    );
    checkApiStatus(response);
  }

  /// Removes an accepted bank. A COMPE code is three digits — safe in the
  /// path.
  Future<void> removeAcceptedBank(String id, String bankCode) async {
    final response = await client.delete(
      _uri('/payees/$id/accepted-banks/$bankCode'),
      headers: await _headers(write: true),
    );
    checkApiStatus(response);
  }

  /// Activates or deactivates the payee.
  Future<void> setActivation(String id, {required bool isActive}) async {
    final response = await client.put(
      _uri('/payees/$id/activation'),
      headers: await _headers(write: true),
      body: jsonEncode({'isActive': isActive}),
    );
    checkApiStatus(response);
  }

  /// Removes the payee.
  Future<void> deletePayee(String id) async {
    final response = await client.delete(
      _uri('/payees/$id'),
      headers: await _headers(write: true),
    );
    checkApiStatus(response);
  }
}
