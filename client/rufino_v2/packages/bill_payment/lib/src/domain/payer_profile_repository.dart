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

  /// Links (or clears, with `null`) the payment provider account.
  ///
  /// Resolves to whether payments can now be scheduled.
  Future<Result<bool>> linkAsaasAccount(String? accountRef);
}
