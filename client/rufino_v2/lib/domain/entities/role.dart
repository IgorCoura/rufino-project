import 'remuneration.dart';

/// A role within a position, defining pay, CBO code, and responsibilities.
class Role {
  const Role({
    required this.id,
    required this.name,
    required this.description,
    required this.cbo,
    required this.remuneration,
  });

  final String id;
  final String name;
  final String description;

  /// Brazilian Classification of Occupations (CBO) code for this role.
  final String cbo;
  final Remuneration remuneration;

  /// Whether this role has a CBO code assigned.
  bool get hasCbo => cbo.isNotEmpty;

  /// Whether this role has a description filled in.
  bool get hasDescription => description.isNotEmpty;

  // ─── Validators ──────────────────────────────────────────────────────────

  /// Validates the role name.
  ///
  /// Required, max 100 characters.
  static String? validateName(String? value) {
    if (value == null || value.isEmpty) return 'Não pode ser vazio.';
    if (value.length > 100) return 'Não pode ser maior que 100 caracteres.';
    return null;
  }

  /// Validates the role description.
  ///
  /// Required, max 2000 characters.
  static String? validateDescription(String? value) {
    if (value == null || value.isEmpty) return 'Não pode ser vazio.';
    if (value.length > 2000) return 'Não pode ser maior que 2000 caracteres.';
    return null;
  }

  /// Validates the CBO code.
  ///
  /// Required, max 6 characters.
  static String? validateCbo(String? value) {
    if (value == null || value.isEmpty) return 'Não pode ser vazio.';
    if (value.length > 6) return 'Não pode ser maior que 6 caracteres.';
    return null;
  }
}
