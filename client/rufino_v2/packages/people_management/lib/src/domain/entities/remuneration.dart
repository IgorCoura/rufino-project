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

  /// Whether this remuneration has a salary value configured.
  bool get hasSalary => baseSalary.value.isNotEmpty;

  /// Whether this remuneration has a description filled in.
  bool get hasDescription => description.isNotEmpty;

  // ─── Validators ──────────────────────────────────────────────────────────

  static final _salaryRegex = RegExp(r'^\d+(\.\d{1,2})?$');

  /// Validates the base salary value.
  ///
  /// Required, must be a valid decimal number (e.g. "3500" or "3500.00").
  /// Accepts commas as decimal separator (normalized to dot).
  static String? validateSalaryValue(String? value) {
    if (value == null || value.isEmpty) return 'Não pode ser vazio.';
    final normalized = value.replaceAll(',', '.');
    if (!_salaryRegex.hasMatch(normalized)) return 'Valor inválido.';
    return null;
  }

  /// Validates the remuneration description.
  ///
  /// Required, max 2000 characters.
  static String? validateDescription(String? value) {
    if (value == null || value.isEmpty) return 'Não pode ser vazio.';
    if (value.length > 2000) return 'Não pode ser maior que 2000 caracteres.';
    return null;
  }

  /// Validates a dropdown selection (payment unit or salary type).
  ///
  /// Required — must not be null or empty.
  static String? validateDropdownSelection(String? value) =>
      (value == null || value.isEmpty) ? 'Selecione uma opção.' : null;
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

  /// Whether this salary has a value set.
  bool get hasValue => value.isNotEmpty;
}

/// The currency type used for a [BaseSalary] (e.g., BRL, USD, EUR).
class SalaryType {
  const SalaryType({required this.id, required this.name});

  final String id;
  final String name;
}
