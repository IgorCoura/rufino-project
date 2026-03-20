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
}
