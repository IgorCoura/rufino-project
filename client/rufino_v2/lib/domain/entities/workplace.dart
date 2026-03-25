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
}
