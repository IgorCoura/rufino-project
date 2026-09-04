import 'package:rufino_core/rufino_core.dart';

import '../domain/bill_payment_exception.dart';
import '../domain/payee.dart';
import '../domain/payee_repository.dart';
import 'payee_api_service.dart';

/// Implements [PayeeRepository] over [apiService], reporting at the catch
/// boundary.
///
/// The classification is the point of this class: a **rule refusing** is an
/// expected failure carrying the domain's own message, and never reaches the
/// error monitor; anything else is a bug or an outage and is reported with
/// its stack trace.
class PayeeRepositoryImpl implements PayeeRepository {
  /// Creates the repository.
  PayeeRepositoryImpl({required this.apiService, required this.reporter});

  /// The HTTP client for the payee endpoints.
  final PayeeApiService apiService;

  /// Where unexpected failures are reported.
  final ErrorReporter reporter;

  /// Runs [action], turning any failure into a classified [Result].
  Future<Result<T>> _guard<T>(
    Future<T> Function() action, {
    Map<String, dynamic>? context,
  }) async {
    try {
      return Result.success(await action());
    } on HttpException catch (e, st) {
      // 4xx carrying a domain message is a rule saying no — expected, shown
      // to the user, not reported. Everything else is worth a stack trace.
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
  Future<Result<PayeePage>> listPayees({String? cursor, int limit = 50}) =>
      _guard(
        () => apiService.listPayees(cursor: cursor, limit: limit),
        context: {'op': 'listPayees'},
      );

  @override
  Future<Result<Payee>> getPayee(String id) => _guard(
        () => apiService.getPayee(id),
        context: {'op': 'getPayee', 'payeeId': id},
      );

  @override
  Future<Result<Payee?>> findByTaxId(String taxId) => _guard(
        () => apiService.findByTaxId(taxId),
        context: {'op': 'findByTaxId'},
      );

  @override
  Future<Result<String>> registerPayee({
    required String legalName,
    required String taxId,
    required AmountPolicyInput amountPolicy,
  }) {
    return _guard(
      () => apiService.registerPayee(
        legalName: legalName,
        taxId: taxId,
        amountPolicy: amountPolicy,
      ),
      context: {'op': 'registerPayee'},
    );
  }

  @override
  Future<Result<void>> changeLegalName(String id, String legalName) => _guard(
        () => apiService.changeLegalName(id, legalName),
        context: {'op': 'changeLegalName', 'payeeId': id},
      );

  @override
  Future<Result<void>> changeAmountPolicy(
    String id,
    AmountPolicyInput policy,
  ) =>
      _guard(
        () => apiService.changeAmountPolicy(id, policy),
        context: {'op': 'changeAmountPolicy', 'payeeId': id},
      );

  @override
  Future<Result<void>> addAlias(String id, String alias) => _guard(
        () => apiService.addAlias(id, alias),
        context: {'op': 'addAlias', 'payeeId': id},
      );

  @override
  Future<Result<void>> removeAlias(String id, String alias) => _guard(
        () => apiService.removeAlias(id, alias),
        context: {'op': 'removeAlias', 'payeeId': id},
      );

  @override
  Future<Result<void>> addAcceptedBank(String id, String bankCode) => _guard(
        () => apiService.addAcceptedBank(id, bankCode),
        context: {'op': 'addAcceptedBank', 'payeeId': id},
      );

  @override
  Future<Result<void>> removeAcceptedBank(String id, String bankCode) =>
      _guard(
        () => apiService.removeAcceptedBank(id, bankCode),
        context: {'op': 'removeAcceptedBank', 'payeeId': id},
      );

  @override
  Future<Result<void>> setActivation(String id, {required bool isActive}) =>
      _guard(
        () => apiService.setActivation(id, isActive: isActive),
        context: {'op': 'setActivation', 'payeeId': id},
      );

  @override
  Future<Result<void>> setStanding(String id, String standing) => _guard(
        () => apiService.setStanding(id, standing),
        context: {'op': 'setStanding', 'payeeId': id},
      );

  @override
  Future<Result<void>> deletePayee(String id) => _guard(
        () => apiService.deletePayee(id),
        context: {'op': 'deletePayee', 'payeeId': id},
      );
}
