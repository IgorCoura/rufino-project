import 'package:rufino_core/rufino_core.dart';
import 'package:tenant_management/tenant_management.dart';

/// In-memory [TenantRepository] for tests.
///
/// Only the reads the shell exercises are meaningful here; the writes answer
/// success so a screen under test can move on.
class FakeTenantRepository implements TenantRepository {
  List<MyTenant> _myTenants = const [];
  bool _myTenantsShouldFail = false;
  Tenant? _tenant;
  TenantPage _page = TenantPage.empty;
  bool _writeShouldFail = false;

  /// The last input handed to [registerTenant].
  RegisterTenantInput? lastRegistered;

  void setMyTenants(List<MyTenant> tenants) => _myTenants = tenants;
  void setMyTenantsShouldFail(bool value) => _myTenantsShouldFail = value;
  void setTenant(Tenant? tenant) => _tenant = tenant;
  void setPage(TenantPage page) => _page = page;
  void setWriteShouldFail(bool value) => _writeShouldFail = value;

  Result<T> _write<T>(T value) {
    if (_writeShouldFail) {
      return Result.error(const TenantRuleException('Regra recusou.'));
    }
    return Result.success(value);
  }

  @override
  Future<Result<List<MyTenant>>> getMyTenants() async {
    if (_myTenantsShouldFail) {
      return const Result.error(TenantNetworkException('boom'));
    }
    return Result.success(_myTenants);
  }

  @override
  Future<Result<TenantPage>> listTenants({
    TenantListFilter filter = const TenantListFilter(),
    String? cursor,
    int limit = 20,
  }) async {
    return Result.success(_page);
  }

  @override
  Future<Result<Tenant>> getTenant(String id) async {
    final tenant = _tenant;
    if (tenant == null) {
      return const Result.error(TenantNetworkException('not found'));
    }
    return Result.success(tenant);
  }

  @override
  Future<Result<String>> registerTenant(RegisterTenantInput input) async {
    lastRegistered = input;
    return _write('new-tenant-id');
  }

  @override
  Future<Result<void>> editDetails(
    String id, {
    required String legalName,
    required String tradeName,
  }) async =>
      _write(null);

  @override
  Future<Result<void>> changeContact(
    String id, {
    required String email,
    required String phone,
  }) async =>
      _write(null);

  @override
  Future<Result<void>> changeAddress(String id, TenantAddress address) async =>
      _write(null);

  @override
  Future<Result<void>> suspend(String id, String reason) async => _write(null);

  @override
  Future<Result<void>> reactivate(String id) async => _write(null);

  @override
  Future<Result<void>> activateProduct(String id, String product) async =>
      _write(null);

  @override
  Future<Result<void>> deactivateProduct(String id, String product) async =>
      _write(null);

  @override
  Future<Result<void>> grantMembership(
    String id, {
    required String email,
    required String role,
  }) async =>
      _write(null);

  @override
  Future<Result<void>> revokeMembership(String id, String email) async =>
      _write(null);

  @override
  Future<Result<void>> reprovisionAccess(String id) async => _write(null);
}
