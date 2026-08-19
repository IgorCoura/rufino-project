import 'bill_payment_enums.dart';

/// How much a payee is expected to charge.
class AmountPolicy {
  /// Creates the policy record.
  const AmountPolicy({
    required this.kind,
    required this.isConclusive,
    this.expectedAmount,
    this.tolerancePercent,
    this.minAmount,
    this.maxAmount,
  });

  /// One of [AmountPolicyKinds].
  final String kind;

  /// The expected amount, for a fixed policy.
  final double? expectedAmount;

  /// The tolerance around [expectedAmount], in percent.
  final double? tolerancePercent;

  /// The floor, for a range policy.
  final double? minAmount;

  /// The ceiling, for a range policy.
  final double? maxAmount;

  /// Whether this policy can conclude the amount check — an unbounded
  /// policy cannot.
  final bool isConclusive;

  /// A one-line summary of the policy, for list rows.
  String get summary => switch (kind) {
        AmountPolicyKinds.fixed => expectedAmount == null
            ? 'Valor fixo'
            : 'Valor fixo de R\$ ${expectedAmount!.toStringAsFixed(2)}',
        AmountPolicyKinds.range => 'Faixa de valores',
        AmountPolicyKinds.unbounded => 'Sem limite de valor',
        _ => kind,
      };
}

/// A registered beneficiary.
class Payee {
  /// Creates the payee record.
  const Payee({
    required this.id,
    required this.legalName,
    required this.taxId,
    required this.taxIdKind,
    required this.aliases,
    required this.acceptedBanks,
    required this.amountPolicy,
    required this.isActive,
  });

  /// The payee's id.
  final String id;

  /// The legal name.
  final String legalName;

  /// The formatted CPF/CNPJ.
  final String taxId;

  /// `CPF` or `CNPJ`.
  final String taxIdKind;

  /// Alternative names the payee appears under.
  final List<String> aliases;

  /// COMPE codes of the banks this payee is expected to charge through.
  final List<String> acceptedBanks;

  /// The expected amount policy.
  final AmountPolicy amountPolicy;

  /// Whether the payee takes part in validation.
  final bool isActive;
}

/// One page of the payee list.
class PayeePage {
  /// Creates a page with its [items] and the opaque [nextCursor].
  const PayeePage({required this.items, this.nextCursor});

  /// The payees of this page.
  final List<Payee> items;

  /// The opaque cursor of the next page, or `null` on the last one.
  final String? nextCursor;

  /// Whether another page exists.
  bool get hasMore => nextCursor != null;
}
