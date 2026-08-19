import 'package:rufino_core/rufino_core.dart';

import 'payee.dart';

/// The amount policy as the tenant informs it on register/edit.
///
/// Which fields the [kind] requires is the domain's call, on the server —
/// this input just carries what the form collected.
class AmountPolicyInput {
  /// Creates the policy input.
  const AmountPolicyInput({
    required this.kind,
    this.expectedAmount,
    this.tolerancePercent,
    this.minAmount,
    this.maxAmount,
  });

  /// One of `AmountPolicyKinds`.
  final String kind;

  /// The expected amount, for a fixed policy.
  final double? expectedAmount;

  /// The tolerance around the expected amount, in percent.
  final double? tolerancePercent;

  /// The floor, for a range policy.
  final double? minAmount;

  /// The ceiling, for a range policy.
  final double? maxAmount;

  /// The JSON body fragment the API expects.
  Map<String, dynamic> toJson() => {
        'amountPolicyKind': kind,
        'expectedAmount': expectedAmount,
        'tolerancePercent': tolerancePercent,
        'minAmount': minAmount,
        'maxAmount': maxAmount,
      };
}

/// Contract for reading and maintaining payees.
abstract class PayeeRepository {
  /// Lists payees, one cursor page at a time.
  Future<Result<PayeePage>> listPayees({String? cursor, int limit = 50});

  /// Returns one payee.
  Future<Result<Payee>> getPayee(String id);

  /// Finds a payee by its CPF/CNPJ, or `null` when none is registered —
  /// which is a state, not an error.
  Future<Result<Payee?>> findByTaxId(String taxId);

  /// Registers a payee and returns its id.
  Future<Result<String>> registerPayee({
    required String legalName,
    required String taxId,
    required AmountPolicyInput amountPolicy,
  });

  /// Renames the payee.
  Future<Result<void>> changeLegalName(String id, String legalName);

  /// Replaces the amount policy.
  Future<Result<void>> changeAmountPolicy(String id, AmountPolicyInput policy);

  /// Adds an alias.
  Future<Result<void>> addAlias(String id, String alias);

  /// Removes an alias.
  Future<Result<void>> removeAlias(String id, String alias);

  /// Adds an accepted bank (COMPE code).
  Future<Result<void>> addAcceptedBank(String id, String bankCode);

  /// Removes an accepted bank.
  Future<Result<void>> removeAcceptedBank(String id, String bankCode);

  /// Activates or deactivates the payee.
  Future<Result<void>> setActivation(String id, {required bool isActive});

  /// Removes the payee.
  Future<Result<void>> deletePayee(String id);
}
