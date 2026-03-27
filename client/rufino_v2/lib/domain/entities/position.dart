import 'role.dart';

/// A position within a department, grouping one or more [Role]s.
class Position {
  const Position({
    required this.id,
    required this.name,
    required this.description,
    required this.cbo,
    required this.roles,
  });

  final String id;
  final String name;
  final String description;

  /// Brazilian Classification of Occupations (CBO) code for this position.
  final String cbo;
  final List<Role> roles;

  /// Whether this position has a CBO code assigned.
  bool get hasCbo => cbo.isNotEmpty;

  /// Whether this position has any roles.
  bool get hasRoles => roles.isNotEmpty;

  /// Returns the total number of roles in this position.
  int get roleCount => roles.length;

  // ─── Validators ──────────────────────────────────────────────────────────

  /// Validates the position name.
  ///
  /// Required, max 100 characters.
  static String? validateName(String? value) {
    if (value == null || value.isEmpty) return 'Não pode ser vazio.';
    if (value.length > 100) return 'Não pode ser maior que 100 caracteres.';
    return null;
  }

  /// Validates the position description.
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
