import 'address.dart';

/// A workplace location belonging to a company.
class Workplace {
  const Workplace({
    required this.id,
    required this.name,
    required this.address,
  });

  final String id;
  final String name;

  /// The full physical address of this workplace.
  final Address address;

  /// Whether this workplace has a name assigned.
  bool get hasName => name.isNotEmpty;

  /// Whether this workplace has address data filled in.
  bool get hasAddress => address.isComplete;

  // ─── Validators ──────────────────────────────────────────────────────────

  /// Validates the workplace name.
  ///
  /// Required, max 100 characters.
  static String? validateName(String? value) {
    if (value == null || value.isEmpty) return 'Não pode ser vazio.';
    if (value.length > 100) return 'Não pode ser maior que 100 caracteres.';
    return null;
  }
}
