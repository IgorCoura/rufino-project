import 'dart:convert';

import 'package:http/http.dart' as http;
import 'package:rufino_core/rufino_core.dart';
import 'package:uuid/uuid.dart';

import '../domain/expectation.dart';
import 'bill_api_service.dart';

const _uuid = Uuid();

/// Maps the expectation read models into domain entities.
abstract final class ExpectationMapper {
  /// Builds an [ExpectationCycle] from the API's JSON.
  static ExpectationCycle cycleFromJson(Map<String, dynamic> json) {
    return ExpectationCycle(
      id: json['id'] as String,
      competence: json['competence'].toString(),
      expectedDueDate: DateTime.parse(json['expectedDueDate'] as String),
      alertAt: DateTime.parse(json['alertAt'] as String),
      status: json['status'] as String,
      missReason: json['missReason'] as String?,
      arrived: json['arrived'] as bool?,
      fulfilledByBillId: json['fulfilledByBillId'] as String?,
      blockedByCaptureItemId: json['blockedByCaptureItemId'] as String?,
      lastAlertLevel: json['lastAlertLevel'] as String?,
    );
  }

  /// Builds an [Expectation] from the API's JSON.
  static Expectation fromJson(Map<String, dynamic> json) {
    return Expectation(
      id: json['id'] as String,
      payeeId: json['payeeId'] as String,
      accountReference: json['accountReference'] as String?,
      label: json['label'] as String,
      recurrence: json['recurrence'] as String,
      expectedDueDay: json['expectedDueDay'] as int,
      observedLeadDays: json['observedLeadDays'] as int? ?? 0,
      alertLeadDays: json['alertLeadDays'] as int? ?? 0,
      origin: json['origin'] as String? ?? 'Manual',
      observationCount: json['observationCount'] as int? ?? 0,
      isActive: json['isActive'] as bool? ?? true,
      pausedUntil: json['pausedUntil'] == null
          ? null
          : DateTime.parse(json['pausedUntil'] as String),
      cycles: (json['cycles'] as List<dynamic>? ?? const [])
          .map((e) => cycleFromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }

  /// Builds an [ExpectationPage] from the API's JSON.
  static ExpectationPage pageFromJson(Map<String, dynamic> json) {
    return ExpectationPage(
      items: (json['items'] as List<dynamic>? ?? const [])
          .map((e) => fromJson(e as Map<String, dynamic>))
          .toList(),
      nextCursor: json['nextCursor'] as String?,
    );
  }

  /// Builds a [PendingExpectation] from the API's JSON.
  static PendingExpectation pendingFromJson(Map<String, dynamic> json) {
    return PendingExpectation(
      expectationId: json['expectationId'] as String,
      cycleId: json['cycleId'] as String,
      label: json['label'] as String,
      competence: json['competence'].toString(),
      expectedDueDate: DateTime.parse(json['expectedDueDate'] as String),
      status: json['status'] as String,
      missReason: json['missReason'] as String?,
      arrived: json['arrived'] as bool?,
      blockedByCaptureItemId: json['blockedByCaptureItemId'] as String?,
      lastAlertLevel: json['lastAlertLevel'] as String?,
      isOverdue: json['isOverdue'] as bool? ?? false,
    );
  }

  /// Builds the [PendingExpectationsView] from the API's JSON.
  static PendingExpectationsView pendingViewFromJson(
    Map<String, dynamic> json,
  ) {
    List<PendingExpectation> parse(String key) =>
        (json[key] as List<dynamic>? ?? const [])
            .map((e) => pendingFromJson(e as Map<String, dynamic>))
            .toList();

    return PendingExpectationsView(
      missing: parse('missing'),
      overdue: parse('overdue'),
      captureFailed: parse('captureFailed'),
      dueSoon: parse('dueSoon'),
    );
  }
}

/// HTTP client for the expectation endpoints.
class ExpectationApiService {
  /// Creates the service against [baseUrl].
  ExpectationApiService({
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

  /// Lists expectations, one cursor page at a time.
  Future<ExpectationPage> listExpectations({
    String? cursor,
    int limit = 20,
  }) async {
    final response = await client.get(
      _uri('/expectations', {
        if (cursor != null) 'cursor': cursor,
        'limit': limit,
      }),
      headers: await _headers(),
    );
    checkApiStatus(response);
    return ExpectationMapper.pageFromJson(
      jsonDecode(response.body) as Map<String, dynamic>,
    );
  }

  /// Returns the pending panel. Not paginated — the three lists come whole.
  Future<PendingExpectationsView> getPending({
    int dueSoonWindowDays = 7,
  }) async {
    final response = await client.get(
      _uri('/expectations/pending', {'dueSoonWindowDays': dueSoonWindowDays}),
      headers: await _headers(),
    );
    checkApiStatus(response);
    return ExpectationMapper.pendingViewFromJson(
      jsonDecode(response.body) as Map<String, dynamic>,
    );
  }

  /// Returns one expectation with its cycles.
  Future<Expectation> getExpectation(String id) async {
    final response = await client.get(
      _uri('/expectations/$id'),
      headers: await _headers(),
    );
    checkApiStatus(response);
    return ExpectationMapper.fromJson(
      jsonDecode(response.body) as Map<String, dynamic>,
    );
  }

  /// Registers an expectation and returns its id.
  Future<String> registerExpectation({
    required String payeeId,
    required String label,
    required String recurrence,
    required int expectedDueDay,
    required int observedLeadDays,
    String? accountReference,
    int? alertLeadDays,
  }) async {
    final response = await client.post(
      _uri('/expectations'),
      headers: await _headers(write: true),
      body: jsonEncode({
        'payeeId': payeeId,
        'accountReference': accountReference?.trim(),
        'label': label.trim(),
        'recurrence': recurrence,
        'expectedDueDay': expectedDueDay,
        'observedLeadDays': observedLeadDays,
        'alertLeadDays': alertLeadDays,
      }),
    );
    checkApiStatus(response);
    return (jsonDecode(response.body) as Map<String, dynamic>)['id'] as String;
  }

  /// Edits an expectation. The payee is not part of the body.
  Future<void> editExpectation(
    String id, {
    required String label,
    required String recurrence,
    required int expectedDueDay,
    required int observedLeadDays,
    String? accountReference,
    int? alertLeadDays,
  }) async {
    final response = await client.put(
      _uri('/expectations/$id'),
      headers: await _headers(write: true),
      body: jsonEncode({
        'accountReference': accountReference?.trim(),
        'label': label.trim(),
        'recurrence': recurrence,
        'expectedDueDay': expectedDueDay,
        'observedLeadDays': observedLeadDays,
        'alertLeadDays': alertLeadDays,
      }),
    );
    checkApiStatus(response);
  }

  /// Deletes the expectation and its cycles.
  Future<void> deleteExpectation(String id) async {
    final response = await client.delete(
      _uri('/expectations/$id'),
      headers: await _headers(write: true),
    );
    checkApiStatus(response);
  }

  /// Pauses, resumes or deactivates the watch.
  Future<void> alterWatch(
    String id, {
    required bool isActive,
    DateTime? pausedUntil,
    String? reason,
  }) async {
    final response = await client.put(
      _uri('/expectations/$id/watch'),
      headers: await _headers(write: true),
      body: jsonEncode({
        'isActive': isActive,
        'pausedUntil':
            pausedUntil == null ? null : BillApiService.dateOnly(pausedUntil),
        'reason': reason?.trim(),
      }),
    );
    checkApiStatus(response);
  }

  /// Dismisses one cycle.
  Future<void> waiveCycle(String id, String cycleId, {String? reason}) async {
    final response = await client.post(
      _uri('/expectations/$id/cycles/$cycleId/waive'),
      headers: await _headers(write: true),
      body: jsonEncode({'reason': reason?.trim()}),
    );
    checkApiStatus(response);
  }
}
