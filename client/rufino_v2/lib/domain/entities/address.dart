/// A physical address associated with a [Workplace].
class Address {
  const Address({
    required this.zipCode,
    required this.street,
    required this.number,
    required this.complement,
    required this.neighborhood,
    required this.city,
    required this.state,
    required this.country,
  });

  final String zipCode;
  final String street;
  final String number;
  final String complement;
  final String neighborhood;
  final String city;
  final String state;
  final String country;

  /// A short human-readable summary in the format "neighborhood, city - state".
  String get minimal => '$neighborhood, $city - $state';
}
