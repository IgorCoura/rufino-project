import 'package:rufino_core/rufino_core.dart';

import '../data/tenant_api_models.dart';
import 'my_tenant.dart';
import 'tenant.dart';
import 'tenant_summary.dart';

/// Contract for reading and changing the tenant cadastro.
///
/// Every operation returns a [Result] — nothing throws across the layer, so a
/// caller cannot forget that a rule may refuse.
abstract class TenantRepository {
  /// The tenants the signed-in person has access to.
  Future<Result<List<MyTenant>>> getMyTenants();

  /// One page of the back-office listing.
  Future<Result<TenantPage>> listTenants({
    TenantListFilter filter,
    String? cursor,
    int limit,
  });

  /// The full cadastro of one tenant.
  Future<Result<Tenant>> getTenant(String id);

  /// Registers a tenant, granting the owner's access in the same act.
  ///
  /// Returns the new id. Says nothing about whether the invitation reached
  /// the identity provider — read the tenant back for that.
  Future<Result<String>> registerTenant(RegisterTenantInput input);

  /// Renames the tenant.
  Future<Result<void>> editDetails(
    String id, {
    required String legalName,
    required String tradeName,
  });

  /// Replaces the contact channel.
  Future<Result<void>> changeContact(
    String id, {
    required String email,
    required String phone,
  });

  /// Replaces the address.
  Future<Result<void>> changeAddress(String id, TenantAddress address);

  /// Freezes the cadastro.
  Future<Result<void>> suspend(String id, String reason);

  /// Lifts a suspension.
  Future<Result<void>> reactivate(String id);

  /// Turns a product on.
  Future<Result<void>> activateProduct(String id, String product);

  /// Turns a product off.
  Future<Result<void>> deactivateProduct(String id, String product);

  /// Grants somebody access.
  Future<Result<void>> grantMembership(
    String id, {
    required String email,
    required String role,
  });

  /// Revokes somebody's access.
  Future<Result<void>> revokeMembership(String id, String email);

  /// Re-sends pending grants to the identity provider.
  Future<Result<void>> reprovisionAccess(String id);
}
