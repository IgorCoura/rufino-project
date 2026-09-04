import 'package:rufino_core/rufino_core.dart';

import '../domain/bill_payment_exception.dart';
import '../domain/payer_profile.dart';
import '../domain/payer_profile_repository.dart';
import 'payer_profile_api_service.dart';

/// Implements [PayerProfileRepository] over [apiService], reporting at the
/// catch boundary with the module's standard classification.
class PayerProfileRepositoryImpl implements PayerProfileRepository {
  /// Creates the repository.
  PayerProfileRepositoryImpl({
    required this.apiService,
    required this.reporter,
  });

  /// The HTTP client for the payer profile endpoints.
  final PayerProfileApiService apiService;

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
  Future<Result<PayerProfile?>> getProfile() =>
      _guard(apiService.getProfile, context: {'op': 'getPayerProfile'});

  @override
  Future<Result<String>> registerProfile({
    required String kind,
    required String legalName,
    required String primaryTaxId,
  }) {
    return _guard(
      () => apiService.registerProfile(
        kind: kind,
        legalName: legalName,
        primaryTaxId: primaryTaxId,
      ),
      context: {'op': 'registerPayerProfile'},
    );
  }

  @override
  Future<Result<void>> changeLegalName(String legalName) => _guard(
        () => apiService.changeLegalName(legalName),
        context: {'op': 'changePayerLegalName'},
      );

  @override
  Future<Result<void>> addTaxId(String taxId) => _guard(
        () => apiService.addTaxId(taxId),
        context: {'op': 'addPayerTaxId'},
      );

  @override
  Future<Result<void>> removeTaxId(String taxId) => _guard(
        () => apiService.removeTaxId(taxId),
        context: {'op': 'removePayerTaxId'},
      );

  @override
  Future<Result<void>> setCnpjRootMatching({required bool enabled}) => _guard(
        () => apiService.setCnpjRootMatching(enabled: enabled),
        context: {'op': 'setCnpjRootMatching'},
      );

  @override
  Future<Result<bool>> linkAsaasAccount(String apiKey) => _guard(
        () => apiService.linkAsaasAccount(apiKey),
        context: {'op': 'linkAsaasAccount'},
      );

  @override
  Future<Result<bool>> unlinkAsaasAccount() => _guard(
        () => apiService.unlinkAsaasAccount(),
        context: {'op': 'unlinkAsaasAccount'},
      );
}
