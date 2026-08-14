import 'dart:convert';

import 'package:http/http.dart' as http;
import 'package:rufino_core/rufino_core.dart';
import 'package:uuid/uuid.dart';

import '../domain/my_tenant.dart';
import '../domain/tenant.dart';
import '../domain/tenant_summary.dart';
import 'tenant_api_models.dart';

const _uuid = Uuid();

/// HTTP client for the TenantManagement bounded context.
///
/// Holds no state. The [client] arrives from the shell already wrapped, which
/// is what makes a 401 anywhere here reach the app's session listener without
/// this module knowing anything about sessions.
class TenantApiService {
  /// Creates the service against [baseUrl].
  TenantApiService({
    required this.client,
    required this.baseUrl,
    required this.getAuthHeader,
  });

  /// The shared HTTP client.
  final http.Client client;

  /// Host (`host:port`) or full origin (`http://host:port`) of the service.
  ///
  /// Both forms are accepted on purpose: this BC is served over plain HTTP in
  /// development and over HTTPS in production, and hard-coding either would
  /// make one of the two environments unusable.
  final String baseUrl;

  /// Supplies the `Authorization` header value.
  final Future<String> Function() getAuthHeader;

  Uri _uri(String path, [Map<String, dynamic>? query]) {
    final params = query?.map((k, v) => MapEntry(k, v.toString()));
    if (baseUrl.contains('://')) {
      final base = Uri.parse(baseUrl);
      return base.replace(path: path, queryParameters: params);
    }
    return Uri.https(baseUrl, path, params);
  }

  Future<Map<String, String>> _headers({bool write = false}) async {
    return {
      'Authorization': await getAuthHeader(),
      'Content-Type': 'application/json',
      // Idempotency: the backend deduplicates a repeated command by this id,
      // so a double tap cannot register the same tenant twice.
      if (write) 'x-requestid': _uuid.v4(),
    };
  }

  /// Returns the tenants the signed-in person has access to.
  Future<List<MyTenant>> getMyTenants() async {
    final response = await client.get(
      _uri('/api/v1/me/tenants'),
      headers: await _headers(),
    );
    checkApiStatus(response);
    return (jsonDecode(response.body) as List<dynamic>)
        .map((e) => MyTenantMapper.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  /// Lists tenants for the back-office, one cursor page at a time.
  Future<TenantPage> listTenants({
    TenantListFilter filter = const TenantListFilter(),
    String? cursor,
    int limit = 20,
  }) async {
    final response = await client.get(
      _uri('/api/v1/tenants', {
        if (filter.kind != null) 'kind': filter.kind,
        if (filter.status != null) 'status': filter.status,
        if (filter.product != null) 'product': filter.product,
        if (filter.search != null && filter.search!.isNotEmpty)
          'search': filter.search,
        if (cursor != null) 'cursor': cursor,
        'limit': limit,
      }),
      headers: await _headers(),
    );
    checkApiStatus(response);
    return TenantPageMapper.fromJson(
      jsonDecode(response.body) as Map<String, dynamic>,
    );
  }

  /// Returns the full cadastro of one tenant.
  Future<Tenant> getTenant(String id) async {
    final response = await client.get(
      _uri('/api/v1/tenants/$id'),
      headers: await _headers(),
    );
    checkApiStatus(response);
    return TenantMapper.fromJson(
      jsonDecode(response.body) as Map<String, dynamic>,
    );
  }

  /// Registers a tenant and grants the owner's access in the same act.
  ///
  /// Returns the new tenant id. **A success here says nothing about the
  /// access grant** — the identity provider does not take part in the
  /// transaction, and its failure is swallowed on purpose so the caller does
  /// not resend a cadastro that already exists. Read the tenant back to learn
  /// what happened to the invitation.
  Future<String> registerTenant(RegisterTenantInput input) async {
    final response = await client.post(
      _uri('/api/v1/tenants'),
      headers: await _headers(write: true),
      body: jsonEncode(input.toJson()),
    );
    checkApiStatus(response);
    return (jsonDecode(response.body) as Map<String, dynamic>)['id'] as String;
  }

  /// Renames the tenant.
  Future<void> editDetails(
    String id, {
    required String legalName,
    required String tradeName,
  }) async {
    final response = await client.put(
      _uri('/api/v1/tenants/$id'),
      headers: await _headers(write: true),
      body: jsonEncode({
        'legalName': legalName.trim(),
        'tradeName': tradeName.trim().isEmpty ? null : tradeName.trim(),
      }),
    );
    checkApiStatus(response);
  }

  /// Replaces the tenant's contact channel.
  Future<void> changeContact(
    String id, {
    required String email,
    required String phone,
  }) async {
    final response = await client.put(
      _uri('/api/v1/tenants/$id/contact'),
      headers: await _headers(write: true),
      body: jsonEncode({
        'email': email.trim(),
        'phone': phone.trim().isEmpty ? null : phone.trim(),
      }),
    );
    checkApiStatus(response);
  }

  /// Replaces the tenant's address.
  Future<void> changeAddress(String id, TenantAddress address) async {
    final response = await client.put(
      _uri('/api/v1/tenants/$id/address'),
      headers: await _headers(write: true),
      body: jsonEncode({'address': addressToJson(address)}),
    );
    checkApiStatus(response);
  }

  /// Freezes the cadastro, keeping everything in place.
  Future<void> suspend(String id, String reason) async {
    final response = await client.post(
      _uri('/api/v1/tenants/$id/suspend'),
      headers: await _headers(write: true),
      body: jsonEncode({'reason': reason.trim()}),
    );
    checkApiStatus(response);
  }

  /// Lifts a suspension.
  Future<void> reactivate(String id) async {
    final response = await client.post(
      _uri('/api/v1/tenants/$id/reactivate'),
      headers: await _headers(write: true),
    );
    checkApiStatus(response);
  }

  /// Turns a product on for this tenant.
  Future<void> activateProduct(String id, String product) async {
    final response = await client.post(
      _uri('/api/v1/tenants/$id/products/$product'),
      headers: await _headers(write: true),
    );
    checkApiStatus(response);
  }

  /// Turns a product off. The record is kept as history.
  Future<void> deactivateProduct(String id, String product) async {
    final response = await client.delete(
      _uri('/api/v1/tenants/$id/products/$product'),
      headers: await _headers(write: true),
    );
    checkApiStatus(response);
  }

  /// Grants somebody access to this tenant.
  Future<void> grantMembership(
    String id, {
    required String email,
    required String role,
  }) async {
    final response = await client.post(
      _uri('/api/v1/tenants/$id/members'),
      headers: await _headers(write: true),
      body: jsonEncode({'email': email.trim(), 'role': role}),
    );
    checkApiStatus(response);
  }

  /// Revokes somebody's access.
  ///
  /// The e-mail goes in the query string, not the path: an address contains
  /// `.` and `@`, and ASP.NET routing would read the suffix as a file
  /// extension.
  Future<void> revokeMembership(String id, String email) async {
    final response = await client.delete(
      _uri('/api/v1/tenants/$id/members', {'email': email}),
      headers: await _headers(write: true),
    );
    checkApiStatus(response);
  }

  /// Re-sends to the identity provider whatever never arrived.
  ///
  /// Idempotent — which is what makes it a button somebody actually presses.
  Future<void> reprovisionAccess(String id) async {
    final response = await client.post(
      _uri('/api/v1/tenants/$id/access/reprovision'),
      headers: await _headers(write: true),
    );
    checkApiStatus(response);
  }
}
