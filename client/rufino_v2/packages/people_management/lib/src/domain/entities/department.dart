import 'position.dart';

/// A department within a company, containing a hierarchical structure of positions and roles.
class Department {
  const Department({
    required this.id,
    required this.name,
    required this.description,
    required this.positions,
  });

  final String id;
  final String name;
  final String description;
  final List<Position> positions;

  /// Whether this department has a description filled in.
  bool get hasDescription => description.isNotEmpty;

  /// Whether this department has any positions.
  bool get hasPositions => positions.isNotEmpty;

  /// Returns the total number of positions in this department.
  int get positionCount => positions.length;

  // ─── Validators ──────────────────────────────────────────────────────────

  /// Validates the department name.
  ///
  /// Required, max 100 characters.
  static String? validateName(String? value) {
    if (value == null || value.isEmpty) return 'Não pode ser vazio.';
    if (value.length > 100) return 'Não pode ser maior que 100 caracteres.';
    return null;
  }

  /// Validates the department description.
  ///
  /// Required, max 2000 characters.
  static String? validateDescription(String? value) {
    if (value == null || value.isEmpty) return 'Não pode ser vazio.';
    if (value.length > 2000) return 'Não pode ser maior que 2000 caracteres.';
    return null;
  }
}
