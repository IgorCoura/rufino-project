import 'package:rufino_core/rufino_core.dart';

import '../domain/bill_payment_exception.dart';
import '../domain/trusted_origin.dart';
import '../domain/trusted_origin_repository.dart';
import 'trusted_origin_api_service.dart';

/// Implements [TrustedOriginRepository] over [apiService], reporting at the
/// catch boundary with the module's standard classification.
class TrustedOriginRepositoryImpl implements TrustedOriginRepository {
  /// Creates the repository.
  TrustedOriginRepositoryImpl({
    required this.apiService,
    required this.reporter,
  });

  /// The HTTP client for the trusted origin endpoints.
  final TrustedOriginApiService apiService;

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
  Future<Result<TrustedOriginPage>> listOrigins({
    String? cursor,
    int limit = 50,
  }) =>
      _guard(
        () => apiService.listOrigins(cursor: cursor, limit: limit),
        context: {'op': 'listOrigins'},
      );

  @override
  Future<Result<TrustedOrigin>> getOrigin(String id) => _guard(
        () => apiService.getOrigin(id),
        context: {'op': 'getOrigin', 'originId': id},
      );

  @override
  Future<Result<TrustedOrigin?>> resolveSender(String sender) => _guard(
        () => apiService.resolveSender(sender),
        context: {'op': 'resolveSender'},
      );

  @override
  Future<Result<String>> registerOrigin({
    required String kind,
    required String value,
    required String decision,
    String? note,
  }) {
    return _guard(
      () => apiService.registerOrigin(
        kind: kind,
        value: value,
        decision: decision,
        note: note,
      ),
      context: {'op': 'registerOrigin'},
    );
  }

  @override
  Future<Result<void>> changeDecision(
    String id, {
    required String decision,
    String? note,
  }) =>
      _guard(
        () => apiService.changeDecision(id, decision: decision, note: note),
        context: {'op': 'changeOriginDecision', 'originId': id},
      );

  @override
  Future<Result<void>> deleteOrigin(String id) => _guard(
        () => apiService.deleteOrigin(id),
        context: {'op': 'deleteOrigin', 'originId': id},
      );
}
