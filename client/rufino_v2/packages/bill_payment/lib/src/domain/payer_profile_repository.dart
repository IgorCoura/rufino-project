import 'package:rufino_core/rufino_core.dart';

import 'payer_profile.dart';

/// Contract for reading and maintaining the tenant's payer profile.
abstract class PayerProfileRepository {
  /// Returns the profile, or `null` when none was registered yet — the
  /// onboarding state, not an error.
  Future<Result<PayerProfile?>> getProfile();

  /// Registers the profile and returns its id.
  Future<Result<String>> registerProfile({
    required String kind,
    required String legalName,
    required String primaryTaxId,
  });

  /// Renames the payer.
  Future<Result<void>> changeLegalName(String legalName);

  /// Adds an extra fiscal document.
  Future<Result<void>> addTaxId(String taxId);

  /// Removes an extra fiscal document.
  Future<Result<void>> removeTaxId(String taxId);

  /// Turns CNPJ-root matching on or off — companies only.
  Future<Result<void>> setCnpjRootMatching({required bool enabled});

  /// Sends the tenant's Asaas API key to be proven and stored server-side.
  ///
  /// Resolves to whether payments can now be scheduled. The key is never
  /// echoed back.
  Future<Result<bool>> linkAsaasAccount(String apiKey);

  /// Unlinks the account, removing the key from the server vault.
  Future<Result<bool>> unlinkAsaasAccount();
}
