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
}
