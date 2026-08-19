import 'package:rufino_core/rufino_core.dart';

import '../domain/bill.dart';
import '../domain/bill_detail.dart';
import '../domain/bill_payment_exception.dart';
import '../domain/bill_repository.dart';
import 'bill_api_service.dart';

/// Implements [BillRepository] over [apiService], reporting at the catch
/// boundary with the module's standard classification.
class BillRepositoryImpl implements BillRepository {
  /// Creates the repository.
  BillRepositoryImpl({required this.apiService, required this.reporter});

  /// The HTTP client for the bill endpoints.
  final BillApiService apiService;

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
  Future<Result<BillPage>> listBills({
    String? status,
    String? cursor,
    int limit = 50,
  }) =>
      _guard(
        () => apiService.listBills(status: status, cursor: cursor, limit: limit),
        context: {'op': 'listBills'},
      );

  @override
  Future<Result<Bill>> getBill(String id) => _guard(
        () => apiService.getBill(id),
        context: {'op': 'getBill', 'billId': id},
      );

  @override
  Future<Result<BillDetail>> getBillDetail(String id) => _guard(
        () => apiService.getBillDetail(id),
        context: {'op': 'getBillDetail', 'billId': id},
      );

  @override
  Future<Result<ImportOutcome>> importBill({
    String? digitableLine,
    String? pixPayload,
  }) =>
      _guard(
        () => apiService.importBill(
          digitableLine: digitableLine,
          pixPayload: pixPayload,
        ),
        // Never put the instruments in the context: whoever has them, pays.
        context: {'op': 'importBill'},
      );

  @override
  Future<Result<ValidationRunOutcome>> revalidateBill(String id) => _guard(
        () => apiService.revalidateBill(id),
        context: {'op': 'revalidateBill', 'billId': id},
      );

  @override
  Future<Result<void>> approveBill(
    String id, {
    required DateTime scheduleFor,
    String? note,
  }) =>
      _guard(
        () => apiService.approveBill(id, scheduleFor: scheduleFor, note: note),
        context: {'op': 'approveBill', 'billId': id},
      );

  @override
  Future<Result<void>> denyBill(String id, String reason) => _guard(
        () => apiService.denyBill(id, reason),
        context: {'op': 'denyBill', 'billId': id},
      );

  @override
  Future<Result<void>> cancelBill(String id, String reason) => _guard(
        () => apiService.cancelBill(id, reason),
        context: {'op': 'cancelBill', 'billId': id},
      );
}
