import 'package:rufino_core/rufino_core.dart';

import '../domain/bill.dart';
import '../domain/bill_detail.dart';
import '../domain/bill_payment_exception.dart';
import '../domain/bill_repository.dart';
import '../domain/captured_artifact.dart';
import '../domain/email_message.dart';
import '../domain/schedule_preview.dart';
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
    List<int>? documentBytes,
    String? documentFileName,
    String? documentContentType,
  }) =>
      _guard(
        () => apiService.importBill(
          digitableLine: digitableLine,
          pixPayload: pixPayload,
          documentBytes: documentBytes,
          documentFileName: documentFileName,
          documentContentType: documentContentType,
        ),
        // Never put the instruments in the context: whoever has them, pays.
        // The file name stays out too — it routinely carries the payee and the
        // account, and the context travels to the error reporter.
        context: {'op': 'importBill', 'hasDocument': documentBytes != null},
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
    bool acknowledgeRisk = false,
    bool acknowledgeImmediateExecution = false,
  }) =>
      _guard(
        () => apiService.approveBill(
          id,
          scheduleFor: scheduleFor,
          note: note,
          acknowledgeRisk: acknowledgeRisk,
          acknowledgeImmediateExecution: acknowledgeImmediateExecution,
        ),
        context: {'op': 'approveBill', 'billId': id},
      );

  @override
  Future<Result<SchedulePreview>> previewSchedule(String id, DateTime date) =>
      _guard(
        () => apiService.previewSchedule(id, date),
        context: {'op': 'previewSchedule', 'billId': id},
      );

  @override
  Future<Result<void>> reopenBill(String id) => _guard(
        () => apiService.reopenBill(id),
        context: {'op': 'reopenBill', 'billId': id},
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

  @override
  Future<Result<EmailMessage>> getEmail(String id) => _guard(
        () => apiService.getBillEmail(id),
        context: {'billId': id},
      );

  @override
  Future<Result<CapturedArtifact>> getArtifact(String id) => _guard(
        () => apiService.getArtifact(id),
        context: {'op': 'getBillArtifact', 'billId': id},
      );
}
