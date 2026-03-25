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
}
