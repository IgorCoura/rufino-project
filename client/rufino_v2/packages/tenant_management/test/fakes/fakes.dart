import 'package:rufino_core/rufino_core.dart';
import 'package:tenant_management/tenant_management.dart';

/// In-memory [TenantRepository] for widget tests.
class FakeTenantRepository implements TenantRepository {
  List<MyTenant> _myTenants = const [];
  bool _myTenantsShouldFail = false;
  Tenant? _tenant;
  TenantPage _page = TenantPage.empty;
  bool _writeShouldFail = false;
  String _ruleMessage = 'Regra recusou.';

  /// Every write the screen asked for, in order.
  final List<String> calls = [];

  /// The last input handed to [registerTenant].
  RegisterTenantInput? lastRegistered;

  void setMyTenants(List<MyTenant> tenants) => _myTenants = tenants;
  void setMyTenantsShouldFail(bool value) => _myTenantsShouldFail = value;
  void setTenant(Tenant? tenant) => _tenant = tenant;
  void setPage(TenantPage page) => _page = page;
  void setWriteShouldFail(bool value, {String message = 'Regra recusou.'}) {
    _writeShouldFail = value;
    _ruleMessage = message;
  }

  Result<T> _write<T>(String call, T value) {
    calls.add(call);
    if (_writeShouldFail) {
      return Result.error(TenantRuleException(_ruleMessage));
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
    calls.add('listTenants');
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
    return _write('registerTenant', 'new-tenant-id');
  }

  @override
  Future<Result<void>> editDetails(
    String id, {
    required String legalName,
    required String tradeName,
  }) async =>
      _write('editDetails', null);

  @override
  Future<Result<void>> changeContact(
    String id, {
    required String email,
    required String phone,
  }) async =>
      _write('changeContact', null);

  @override
  Future<Result<void>> changeAddress(String id, TenantAddress address) async =>
      _write('changeAddress', null);

  @override
  Future<Result<void>> suspend(String id, String reason) async =>
      _write('suspend', null);

  @override
  Future<Result<void>> reactivate(String id) async => _write('reactivate', null);

  @override
  Future<Result<void>> activateProduct(String id, String product) async =>
      _write('activateProduct:$product', null);

  @override
  Future<Result<void>> deactivateProduct(String id, String product) async =>
      _write('deactivateProduct:$product', null);

  @override
  Future<Result<void>> grantMembership(
    String id, {
    required String email,
    required String role,
  }) async =>
      _write('grantMembership:$email', null);

  @override
  Future<Result<void>> revokeMembership(String id, String email) async =>
      _write('revokeMembership:$email', null);

  @override
  Future<Result<void>> reprovisionAccess(String id) async =>
      _write('reprovisionAccess', null);
}

/// In-memory [PermissionRepository] for widget tests.
class FakePermissionRepository implements PermissionRepository {
  List<Permission> _permissions = const [];

  void setPermissions(List<Permission> permissions) =>
      _permissions = permissions;

  @override
  Future<Result<List<Permission>>> fetchPermissions() async =>
      Result.success(_permissions);

  @override
  Future<List<Permission>?> getCachedPermissions() async => null;

  @override
  Future<void> cachePermissions(List<Permission> permissions) async {}

  @override
  Future<void> clearCachedPermissions() async {}
}

/// In-memory [SecureStorage] for widget tests.
class FakeSecureStorage implements SecureStorage {
  final Map<String, String> values = {};

  @override
  Future<void> write({required String key, required String value}) async {
    values[key] = value;
  }

  @override
  Future<String?> read({required String key}) async => values[key];

  @override
  Future<void> delete({required String key}) async {
    values.remove(key);
  }

  @override
  Future<bool> containsKey({required String key}) async =>
      values.containsKey(key);
}

/// Builds a tenant permission notifier already loaded with [permissions].
Future<TenantPermissionNotifier> tenantPermissions(
  List<Permission> permissions,
) async {
  final repo = FakePermissionRepository()..setPermissions(permissions);
  final notifier = TenantPermissionNotifier(permissionRepository: repo);
  await notifier.loadPermissions();
  return notifier;
}

/// A full cadastro, for tests.
Tenant tenant({
  String id = 'tenant-1',
  String kind = TenantKinds.company,
  String legalName = 'Padaria do Zé LTDA',
  String tradeName = 'Pão Quente',
  String taxId = '11222333000181',
  String status = TenantStatuses.active,
  String suspensionReason = '',
  String provisioning = ProvisioningStatuses.done,
  List<TenantMembership>? memberships,
  List<TenantProductInfo>? products,
}) {
  return Tenant(
    id: id,
    kind: kind,
    legalName: legalName,
    tradeName: tradeName,
    primaryTaxId: taxId,
    status: status,
    suspensionReason: suspensionReason,
    accessProvisioning: provisioning,
    contact: const TenantContact(
      email: 'contato@paoquente.com.br',
      phone: '31999990000',
    ),
    address: const TenantAddress(
      zipCode: '30110000',
      street: 'RUA DAS FLORES',
      number: '100',
      complement: '',
      neighborhood: 'CENTRO',
      city: 'BELO HORIZONTE',
      state: 'MG',
    ),
    products: products ??
        [
          TenantProductInfo(
            product: TenantProducts.peopleManagement,
            isActive: true,
            activatedAt: DateTime(2026, 1, 1),
          ),
        ],
    memberships: memberships ??
        const [
          TenantMembership(
            email: 'dono@paoquente.com.br',
            role: MembershipRoles.owner,
            isActive: true,
            provisioning: ProvisioningStatuses.done,
          ),
        ],
    createdAt: DateTime(2026, 1, 1),
    updatedAt: DateTime(2026, 1, 2),
  );
}

/// A listing row, for tests.
TenantSummary tenantSummary({
  String id = 'tenant-1',
  String legalName = 'Padaria do Zé LTDA',
  String tradeName = 'Pão Quente',
  String kind = TenantKinds.company,
  String taxId = '11222333000181',
  String status = TenantStatuses.active,
  String provisioning = ProvisioningStatuses.done,
  List<String> products = const [TenantProducts.peopleManagement],
}) {
  return TenantSummary(
    id: id,
    kind: kind,
    legalName: legalName,
    tradeName: tradeName,
    primaryTaxId: taxId,
    status: status,
    accessProvisioning: provisioning,
    contactEmail: 'contato@paoquente.com.br',
    activeProducts: products,
    createdAt: DateTime(2026, 1, 1),
  );
}

/// A tenant the signed-in person belongs to, for tests.
MyTenant myTenant({
  String id = 'tenant-1',
  String legalName = 'Padaria do Zé LTDA',
  String tradeName = 'Pão Quente',
  String kind = TenantKinds.company,
  String status = TenantStatuses.active,
  String role = MembershipRoles.owner,
  List<String> products = const [TenantProducts.peopleManagement],
}) {
  return MyTenant(
    id: id,
    kind: kind,
    legalName: legalName,
    tradeName: tradeName,
    status: status,
    role: role,
    activeProducts: products,
  );
}
