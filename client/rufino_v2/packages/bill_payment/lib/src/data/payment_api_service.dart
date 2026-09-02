import 'dart:convert';

import 'package:http/http.dart' as http;
import 'package:rufino_core/rufino_core.dart';
import 'package:uuid/uuid.dart';

import '../domain/captured_artifact.dart';
import '../domain/payment_order.dart';
import 'artifact_response.dart';

const _uuid = Uuid();

/// Maps the payment order read model into the domain entity.
abstract final class PaymentOrderMapper {
  /// Builds a [PaymentOrder] from the API's JSON.
  static PaymentOrder fromJson(Map<String, dynamic> json) {
    return PaymentOrder(
      id: json['id'] as String,
      billId: json['billId'] as String,
      rail: json['rail'] as String,
      status: json['status'] as String,
      hold: json['hold'] as String,
      requestedScheduleDate:
          DateTime.parse(json['requestedScheduleDate'] as String),
      effectiveScheduleDate: json['effectiveScheduleDate'] == null
          ? null
          : DateTime.parse(json['effectiveScheduleDate'] as String),
      amount: (json['amount'] as num?)?.toDouble(),
      fee: (json['fee'] as num?)?.toDouble(),
      paidAt: json['paidAt'] == null
          ? null
          : DateTime.parse(json['paidAt'] as String),
      failReasons: (json['failReasons'] as List<dynamic>? ?? const [])
          .map((e) => e.toString())
          .toList(),
      lastError: json['lastError'] as String?,
      submissionAttempts: json['submissionAttempts'] as int? ?? 0,
      requiresConfirmation: json['requiresConfirmation'] as bool? ?? false,
      hasReceipt: json['hasReceipt'] as bool? ?? false,
      createdAt: DateTime.parse(json['createdAt'] as String),
    );
  }
}

/// HTTP client for the payment order endpoints (phase 3).
class PaymentApiService {
  /// Creates the service against [baseUrl].
  PaymentApiService({
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

  Uri _uri(String path) {
    final full = '/api/v1/${getTenantId()}$path';
    if (baseUrl.contains('://')) {
      return Uri.parse(baseUrl).replace(path: full);
    }
    return Uri.https(baseUrl, full);
  }

  Future<Map<String, String>> _headers({bool write = false}) async {
    return {
      'Authorization': await getAuthHeader(),
      'Content-Type': 'application/json',
      if (write) 'x-requestid': _uuid.v4(),
    };
  }

  /// Returns the bill's (most recent) order, or `null` when none exists yet.
  ///
  /// 404 is a normal answer here — approval creates the order through the
  /// outbox, and there is an observable window before it exists.
  Future<PaymentOrder?> getByBill(String billId) async {
    final response = await client.get(
      _uri('/payments/by-bill/$billId'),
      headers: await _headers(),
    );
    if (response.statusCode == 404) return null;
    checkApiStatus(response);
    return PaymentOrderMapper.fromJson(
      jsonDecode(response.body) as Map<String, dynamic>,
    );
  }

  /// Cancels the order.
  Future<void> cancel(String orderId) async {
    final response = await client.post(
      _uri('/payments/$orderId/cancel'),
      headers: await _headers(write: true),
    );
    checkApiStatus(response);
  }

  /// Confirms the immediate (overdue) payment of a held order.
  Future<void> confirmImmediate(String orderId) async {
    final response = await client.post(
      _uri('/payments/$orderId/confirm-immediate'),
      headers: await _headers(write: true),
    );
    checkApiStatus(response);
  }

  /// Downloads the stored receipt of [orderId].
  Future<CapturedArtifact> getReceipt(String orderId) async {
    final request = http.Request('GET', _uri('/payments/$orderId/receipt'))
      ..headers.addAll(await _headers());

    final response =
        await http.Response.fromStream(await client.send(request));
    checkApiStatus(response);

    return artifactFromResponse(response);
  }
}
