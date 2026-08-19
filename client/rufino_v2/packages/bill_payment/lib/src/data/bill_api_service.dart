import 'dart:convert';

import 'package:http/http.dart' as http;
import 'package:rufino_core/rufino_core.dart';
import 'package:uuid/uuid.dart';

import '../domain/bill.dart';
import '../domain/bill_check.dart';
import '../domain/bill_detail.dart';
import '../domain/bill_repository.dart';

const _uuid = Uuid();

/// Maps the bill read models into domain entities.
abstract final class BillMapper {
  /// Builds a [BillOrigin] from the API's JSON.
  static BillOrigin originFromJson(Map<String, dynamic> json) {
    return BillOrigin(
      sourceKind: json['sourceKind'] as String,
      sourceId: json['sourceId'] as String?,
      senderAddress: json['senderAddress'] as String?,
      receivedAt: DateTime.parse(json['receivedAt'] as String),
    );
  }

  /// Builds a [Bill] from the API's JSON.
  static Bill fromJson(Map<String, dynamic> json) {
    return Bill(
      id: json['id'] as String,
      status: json['status'] as String,
      kind: json['kind'] as String,
      rail: json['rail'] as String,
      amount: (json['amount'] as num?)?.toDouble(),
      dueDate: json['dueDate'] == null
          ? null
          : DateTime.parse(json['dueDate'] as String),
      bankCode: json['bankCode'] as String?,
      origin: originFromJson(json['origin'] as Map<String, dynamic>),
      createdAt: DateTime.parse(json['createdAt'] as String),
    );
  }

  /// Builds a [BillPage] from the API's JSON.
  static BillPage pageFromJson(Map<String, dynamic> json) {
    return BillPage(
      items: (json['items'] as List<dynamic>? ?? const [])
          .map((e) => fromJson(e as Map<String, dynamic>))
          .toList(),
      nextCursor: json['nextCursor'] as String?,
    );
  }

  /// Builds a [BillCheck] from the API's JSON.
  static BillCheck checkFromJson(Map<String, dynamic> json) {
    return BillCheck(
      type: json['type'] as String,
      outcome: json['outcome'] as String,
      severity: json['severity'] as String,
      reasonCode: json['reasonCode'] as String?,
      evidence: json['evidence'] as String?,
      isBlockingFailure: json['isBlockingFailure'] as bool? ?? false,
      evaluatedAt: DateTime.parse(json['evaluatedAt'] as String),
    );
  }

  /// Builds a [BillDetail] from the API's JSON.
  static BillDetail detailFromJson(Map<String, dynamic> json) {
    final beneficiary = json['beneficiary'] as Map<String, dynamic>?;
    final approval = json['approval'] as Map<String, dynamic>?;
    return BillDetail(
      id: json['id'] as String,
      status: json['status'] as String,
      kind: json['kind'] as String,
      rail: json['rail'] as String,
      beneficiary: beneficiary == null
          ? null
          : BillParty(
              name: beneficiary['name'] as String?,
              tradingName: beneficiary['tradingName'] as String?,
              taxId: beneficiary['taxId'] as String?,
            ),
      amount: (json['amount'] as num?)?.toDouble(),
      originalAmount: (json['originalAmount'] as num?)?.toDouble(),
      dueDate: json['dueDate'] == null
          ? null
          : DateTime.parse(json['dueDate'] as String),
      bankCode: json['bankCode'] as String?,
      minimumScheduleDate: json['minimumScheduleDate'] == null
          ? null
          : DateTime.parse(json['minimumScheduleDate'] as String),
      lastConsultedAt: json['lastConsultedAt'] == null
          ? null
          : DateTime.parse(json['lastConsultedAt'] as String),
      checks: (json['checks'] as List<dynamic>? ?? const [])
          .map((e) => checkFromJson(e as Map<String, dynamic>))
          .toList(),
      approval: approval == null
          ? null
          : BillApproval(
              decidedBy: approval['decidedBy'] as String,
              decision: approval['decision'] as String,
              decidedAt: DateTime.parse(approval['decidedAt'] as String),
              note: approval['note'] as String?,
            ),
      scheduledFor: json['scheduledFor'] == null
          ? null
          : DateTime.parse(json['scheduledFor'] as String),
      origin: originFromJson(json['origin'] as Map<String, dynamic>),
      createdAt: DateTime.parse(json['createdAt'] as String),
    );
  }
}

/// HTTP client for the bill endpoints.
class BillApiService {
  /// Creates the service against [baseUrl].
  BillApiService({
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

  /// The `yyyy-MM-dd` form the API's `DateOnly` fields expect.
  static String dateOnly(DateTime date) =>
      '${date.year.toString().padLeft(4, '0')}-'
      '${date.month.toString().padLeft(2, '0')}-'
      '${date.day.toString().padLeft(2, '0')}';

  /// Lists bills, optionally filtered by status.
  Future<BillPage> listBills({
    String? status,
    String? cursor,
    int limit = 50,
  }) async {
    final response = await client.get(
      _uri('/bills', {
        if (status != null) 'status': status,
        if (cursor != null) 'cursor': cursor,
        'limit': limit,
      }),
      headers: await _headers(),
    );
    checkApiStatus(response);
    return BillMapper.pageFromJson(
      jsonDecode(response.body) as Map<String, dynamic>,
    );
  }

  /// Returns one bill's list projection.
  Future<Bill> getBill(String id) async {
    final response = await client.get(
      _uri('/bills/$id'),
      headers: await _headers(),
    );
    checkApiStatus(response);
    return BillMapper.fromJson(
      jsonDecode(response.body) as Map<String, dynamic>,
    );
  }

  /// Returns the approval screen's detail.
  Future<BillDetail> getBillDetail(String id) async {
    final response = await client.get(
      _uri('/bills/$id/detail'),
      headers: await _headers(),
    );
    checkApiStatus(response);
    return BillMapper.detailFromJson(
      jsonDecode(response.body) as Map<String, dynamic>,
    );
  }

  /// Imports a bill typed by hand. Origin is always a manual upload here —
  /// mailbox and portal bills enter through capture, never through this
  /// screen.
  Future<ImportOutcome> importBill({
    String? digitableLine,
    String? pixPayload,
  }) async {
    final response = await client.post(
      _uri('/bills/import'),
      headers: await _headers(write: true),
      body: jsonEncode({
        'digitableLine': _clean(digitableLine),
        'pixPayload': pixPayload?.trim(),
        'sourceKind': 'ManualUpload',
        'receivedAt': DateTime.now().toUtc().toIso8601String(),
      }),
    );
    checkApiStatus(response);
    final body = jsonDecode(response.body) as Map<String, dynamic>;
    return ImportOutcome(
      id: body['id'] as String,
      kind: body['kind'] as String,
      rail: body['rail'] as String,
    );
  }

  /// Re-runs the official lookup and the twelve checks.
  Future<ValidationRunOutcome> revalidateBill(String id) async {
    final response = await client.post(
      _uri('/bills/$id/revalidate'),
      headers: await _headers(write: true),
    );
    checkApiStatus(response);
    final body = jsonDecode(response.body) as Map<String, dynamic>;
    return ValidationRunOutcome(
      id: body['id'] as String,
      status: body['status'] as String,
      blockingFailures: body['blockingFailures'] as int? ?? 0,
      attentionItems: body['attentionItems'] as int? ?? 0,
    );
  }

  /// Authorizes the payment.
  Future<void> approveBill(
    String id, {
    required DateTime scheduleFor,
    String? note,
  }) async {
    final response = await client.post(
      _uri('/bills/$id/approve'),
      headers: await _headers(write: true),
      body: jsonEncode({
        'scheduleFor': dateOnly(scheduleFor),
        'note': note?.trim(),
      }),
    );
    checkApiStatus(response);
  }

  /// Refuses the bill.
  Future<void> denyBill(String id, String reason) async {
    final response = await client.post(
      _uri('/bills/$id/deny'),
      headers: await _headers(write: true),
      body: jsonEncode({'reason': reason.trim()}),
    );
    checkApiStatus(response);
  }

  /// Removes the bill from the flow.
  Future<void> cancelBill(String id, String reason) async {
    final response = await client.post(
      _uri('/bills/$id/cancel'),
      headers: await _headers(write: true),
      body: jsonEncode({'reason': reason.trim()}),
    );
    checkApiStatus(response);
  }

  /// Strips the separators people paste along with a digitable line.
  static String? _clean(String? line) {
    if (line == null) return null;
    final digits = line.replaceAll(RegExp(r'[\s.\-]'), '');
    return digits.isEmpty ? null : digits;
  }
}
