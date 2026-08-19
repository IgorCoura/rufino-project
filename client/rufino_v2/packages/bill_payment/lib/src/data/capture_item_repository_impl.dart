import 'package:rufino_core/rufino_core.dart';

import '../domain/bill_payment_exception.dart';
import '../domain/capture_item.dart';
import '../domain/capture_item_repository.dart';
import 'capture_item_api_service.dart';

/// Implements [CaptureItemRepository] over [apiService], reporting at the
/// catch boundary with the module's standard classification.
class CaptureItemRepositoryImpl implements CaptureItemRepository {
  /// Creates the repository.
  CaptureItemRepositoryImpl({
    required this.apiService,
    required this.reporter,
  });

  /// The HTTP client for the capture item endpoints.
  final CaptureItemApiService apiService;

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
  Future<Result<CaptureItemPage>> listItems({
    String? status,
    String? cursor,
    int limit = 50,
  }) =>
      _guard(
        () => apiService.listItems(status: status, cursor: cursor, limit: limit),
        context: {'op': 'listCaptureItems'},
      );

  @override
  Future<Result<CaptureItem>> getItem(String id) => _guard(
        () => apiService.getItem(id),
        context: {'op': 'getCaptureItem', 'itemId': id},
      );

  @override
  Future<Result<void>> reprocessItem(String id) => _guard(
        () => apiService.reprocessItem(id),
        context: {'op': 'reprocessCaptureItem', 'itemId': id},
      );

  @override
  Future<Result<ClaimOutcome>> claimItem(String id) => _guard(
        () => apiService.claimItem(id),
        context: {'op': 'claimCaptureItem', 'itemId': id},
      );
}
