import 'package:rufino_core/rufino_core.dart';

import '../domain/my_tenant.dart';
import '../domain/tenant.dart';
import '../domain/tenant_exception.dart';
import '../domain/tenant_repository.dart';
import '../domain/tenant_summary.dart';
import 'tenant_api_models.dart';
import 'tenant_api_service.dart';

/// Implements [TenantRepository] over [apiService], reporting at the catch
/// boundary.
///
/// The classification is the point of this class: a **rule refusing** is an
/// expected failure carrying the domain's own message, and never reaches the
/// error monitor; anything else is a bug or an outage and is reported with
/// its stack trace.
class TenantRepositoryImpl implements TenantRepository {
  /// Creates the repository.
  TenantRepositoryImpl({required this.apiService, required this.reporter});

  /// The HTTP client for the bounded context.
  final TenantApiService apiService;

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
          TenantRuleException(e.serverMessages.first, code: e.domainErrorId),
          st,
          context: context,
        );
      }
      return reporter.failure(TenantNetworkException(e), st, context: context);
    } on TenantException catch (e, st) {
      return reporter.failure(e, st, context: context);
    } catch (e, st) {
      return reporter.failure(TenantNetworkException(e), st, context: context);
    }
  }

  @override
  Future<Result<List<MyTenant>>> getMyTenants() =>
      _guard(apiService.getMyTenants, context: {'op': 'getMyTenants'});

  @override
  Future<Result<TenantPage>> listTenants({
    TenantListFilter filter = const TenantListFilter(),
    String? cursor,
    int limit = 20,
  }) {
    return _guard(
      () => apiService.listTenants(
        filter: filter,
        cursor: cursor,
        limit: limit,
      ),
      context: {'op': 'listTenants'},
    );
  }

  @override
  Future<Result<Tenant>> getTenant(String id) => _guard(
        () => apiService.getTenant(id),
        context: {'op': 'getTenant', 'tenantId': id},
      );

  @override
  Future<Result<String>> registerTenant(RegisterTenantInput input) => _guard(
        () => apiService.registerTenant(input),
        context: {'op': 'registerTenant'},
      );

  @override
  Future<Result<void>> editDetails(
    String id, {
    required String legalName,
    required String tradeName,
  }) {
    return _guard(
      () => apiService.editDetails(
        id,
        legalName: legalName,
        tradeName: tradeName,
      ),
      context: {'op': 'editDetails', 'tenantId': id},
    );
  }

  @override
  Future<Result<void>> changeContact(
    String id, {
    required String email,
    required String phone,
  }) {
    return _guard(
      () => apiService.changeContact(id, email: email, phone: phone),
      context: {'op': 'changeContact', 'tenantId': id},
    );
  }

  @override
  Future<Result<void>> changeAddress(String id, TenantAddress address) =>
      _guard(
        () => apiService.changeAddress(id, address),
        context: {'op': 'changeAddress', 'tenantId': id},
      );

  @override
  Future<Result<void>> suspend(String id, String reason) => _guard(
        () => apiService.suspend(id, reason),
        context: {'op': 'suspend', 'tenantId': id},
      );

  @override
  Future<Result<void>> reactivate(String id) => _guard(
        () => apiService.reactivate(id),
        context: {'op': 'reactivate', 'tenantId': id},
      );

  @override
  Future<Result<void>> activateProduct(String id, String product) => _guard(
        () => apiService.activateProduct(id, product),
        context: {'op': 'activateProduct', 'tenantId': id, 'product': product},
      );

  @override
  Future<Result<void>> deactivateProduct(String id, String product) => _guard(
        () => apiService.deactivateProduct(id, product),
        context: {
          'op': 'deactivateProduct',
          'tenantId': id,
          'product': product,
        },
      );

  @override
  Future<Result<void>> grantMembership(
    String id, {
    required String email,
    required String role,
  }) {
    return _guard(
      () => apiService.grantMembership(id, email: email, role: role),
      context: {'op': 'grantMembership', 'tenantId': id},
    );
  }

  @override
  Future<Result<void>> revokeMembership(String id, String email) => _guard(
        () => apiService.revokeMembership(id, email),
        context: {'op': 'revokeMembership', 'tenantId': id},
      );

  @override
  Future<Result<void>> reprovisionAccess(String id) => _guard(
        () => apiService.reprovisionAccess(id),
        context: {'op': 'reprovisionAccess', 'tenantId': id},
      );
}
