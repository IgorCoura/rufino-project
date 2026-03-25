/// Represents the compensation structure associated with a role.
class Remuneration {
  const Remuneration({
    required this.paymentUnit,
    required this.baseSalary,
    required this.description,
  });

  final PaymentUnit paymentUnit;
  final BaseSalary baseSalary;

  /// Additional description or notes about this remuneration.
  final String description;
}

/// A payment frequency unit (e.g., per hour, per month).
class PaymentUnit {
  const PaymentUnit({required this.id, required this.name});

  final String id;
  final String name;
}

/// The base salary, combining a [SalaryType] currency and a monetary value.
class BaseSalary {
  const BaseSalary({required this.type, required this.value});

  final SalaryType type;

  /// Monetary amount as a string (e.g., "3500.00").
  final String value;
}

/// The currency type used for a [BaseSalary] (e.g., BRL, USD, EUR).
class SalaryType {
  const SalaryType({required this.id, required this.name});

  final String id;
  final String name;
}
