import 'package:rufino_core/rufino_core.dart';

import '../domain/bill_payment_exception.dart';
import '../domain/expectation.dart';
import '../domain/expectation_repository.dart';
import 'expectation_api_service.dart';

/// Implements [ExpectationRepository] over [apiService], reporting at the
/// catch boundary with the module's standard classification.
class ExpectationRepositoryImpl implements ExpectationRepository {
  /// Creates the repository.
  ExpectationRepositoryImpl({
    required this.apiService,
    required this.reporter,
  });

  /// The HTTP client for the expectation endpoints.
  final ExpectationApiService apiService;

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
  Future<Result<ExpectationPage>> listExpectations({
    String? cursor,
    int limit = 20,
  }) =>
      _guard(
        () => apiService.listExpectations(cursor: cursor, limit: limit),
        context: {'op': 'listExpectations'},
      );

  @override
  Future<Result<PendingExpectationsView>> getPending({
    int dueSoonWindowDays = 7,
  }) =>
      _guard(
        () => apiService.getPending(dueSoonWindowDays: dueSoonWindowDays),
        context: {'op': 'getPendingExpectations'},
      );

  @override
  Future<Result<Expectation>> getExpectation(String id) => _guard(
        () => apiService.getExpectation(id),
        context: {'op': 'getExpectation', 'expectationId': id},
      );

  @override
  Future<Result<String>> registerExpectation({
    required String payeeId,
    required String label,
    required String recurrence,
    required int expectedDueDay,
    required int observedLeadDays,
    String? accountReference,
    int? alertLeadDays,
  }) {
    return _guard(
      () => apiService.registerExpectation(
        payeeId: payeeId,
        label: label,
        recurrence: recurrence,
        expectedDueDay: expectedDueDay,
        observedLeadDays: observedLeadDays,
        accountReference: accountReference,
        alertLeadDays: alertLeadDays,
      ),
      context: {'op': 'registerExpectation', 'payeeId': payeeId},
    );
  }

  @override
  Future<Result<void>> editExpectation(
    String id, {
    required String label,
    required String recurrence,
    required int expectedDueDay,
    required int observedLeadDays,
    String? accountReference,
    int? alertLeadDays,
  }) {
    return _guard(
      () => apiService.editExpectation(
        id,
        label: label,
        recurrence: recurrence,
        expectedDueDay: expectedDueDay,
        observedLeadDays: observedLeadDays,
        accountReference: accountReference,
        alertLeadDays: alertLeadDays,
      ),
      context: {'op': 'editExpectation', 'expectationId': id},
    );
  }

  @override
  Future<Result<void>> deleteExpectation(String id) => _guard(
        () => apiService.deleteExpectation(id),
        context: {'op': 'deleteExpectation', 'expectationId': id},
      );

  @override
  Future<Result<void>> alterWatch(
    String id, {
    required bool isActive,
    DateTime? pausedUntil,
    String? reason,
  }) =>
      _guard(
        () => apiService.alterWatch(
          id,
          isActive: isActive,
          pausedUntil: pausedUntil,
          reason: reason,
        ),
        context: {'op': 'alterExpectationWatch', 'expectationId': id},
      );

  @override
  Future<Result<void>> waiveCycle(
    String id,
    String cycleId, {
    String? reason,
  }) =>
      _guard(
        () => apiService.waiveCycle(id, cycleId, reason: reason),
        context: {
          'op': 'waiveExpectationCycle',
          'expectationId': id,
          'cycleId': cycleId,
        },
      );
}
