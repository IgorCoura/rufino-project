import 'package:rufino_core/rufino_core.dart';

import '../domain/bill_payment_exception.dart';
import '../domain/captured_artifact.dart';
import '../domain/payment_order.dart';
import '../domain/payment_repository.dart';
import 'payment_api_service.dart';

/// Implements [PaymentRepository] over [apiService], reporting at the catch
/// boundary with the module's standard classification.
class PaymentRepositoryImpl implements PaymentRepository {
  /// Creates the repository.
  PaymentRepositoryImpl({required this.apiService, required this.reporter});

  /// The HTTP client for the payment endpoints.
  final PaymentApiService apiService;

  /// Where unexpected failures are reported.
  final ErrorReporter reporter;

  Future<Result<T>> _guard<T>(
    Future<T> Function() action, {
    Map<String, dynamic>? context,
  }) async {
    try {
      return Result.success(await action());
    } on HttpException catch (e, st) {
      if (e.statusCode < 500 && e.serverMessages.isNotEmpty) {
        return reporter.failure(
          BillPaymentRuleException(
            e.serverMessages.first,
            code: e.domainErrorId,
          ),
          st,
          context: context,
        );
      }
      return reporter.failure(
        BillPaymentNetworkException(e),
        st,
        context: context,
      );
    } on BillPaymentException catch (e, st) {
      return reporter.failure(e, st, context: context);
    } catch (e, st) {
      return reporter.failure(
        BillPaymentNetworkException(e),
        st,
        context: context,
      );
    }
  }

  @override
  Future<Result<PaymentOrder?>> getForBill(String billId) => _guard(
        () => apiService.getByBill(billId),
        context: {'op': 'getPaymentForBill', 'billId': billId},
      );

  @override
  Future<Result<void>> cancel(String orderId) => _guard(
        () => apiService.cancel(orderId),
        context: {'op': 'cancelPayment', 'paymentOrderId': orderId},
      );

  @override
  Future<Result<void>> confirmImmediate(String orderId) => _guard(
        () => apiService.confirmImmediate(orderId),
        context: {'op': 'confirmImmediatePayment', 'paymentOrderId': orderId},
      );

  @override
  Future<Result<CapturedArtifact>> getReceiptForBill(String billId) => _guard(
        () async {
          final order = await apiService.getByBill(billId);
          if (order == null || !order.hasReceipt) {
            throw const BillPaymentRuleException(
              'Este boleto ainda não tem comprovante disponível.',
            );
          }
          return apiService.getReceipt(order.id);
        },
        context: {'op': 'getPaymentReceipt', 'billId': billId},
      );
}
