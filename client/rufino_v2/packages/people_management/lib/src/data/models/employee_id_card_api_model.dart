import '../../domain/entities/employee_id_card.dart';

/// Data Transfer Object for the employee ID card (Identidade) endpoint.
///
/// The API returns [dateOfBirth] in `"yyyy-MM-dd"` format, which is converted
/// to `"dd/MM/yyyy"` for display in the UI via [toEntity].
class EmployeeIdCardApiModel {
  /// Creates an [EmployeeIdCardApiModel] from the given fields.
  const EmployeeIdCardApiModel({
    required this.cpf,
    required this.motherName,
    required this.fatherName,
    required this.dateOfBirth,
    required this.birthCity,
    required this.birthState,
    required this.nationality,
  });

  /// The employee's CPF.
  final String cpf;

  /// The employee's mother's name.
  final String motherName;

  /// The employee's father's name.
  final String fatherName;

  /// Date of birth in `"yyyy-MM-dd"` format as returned by the API.
  final String dateOfBirth;

  /// The city of birth.
  final String birthCity;

  /// The state of birth.
  final String birthState;

  /// The employee's nationality.
  final String nationality;

  /// Deserialises an [EmployeeIdCardApiModel] from the API JSON map.
  ///
  /// The response nests the fields inside an `idCard` object.
  factory EmployeeIdCardApiModel.fromJson(Map<String, dynamic> json) {
    final rawIdCard = json['idCard'];
    final idCard = rawIdCard != null
        ? Map<String, dynamic>.from(rawIdCard as Map)
        : json;
    return EmployeeIdCardApiModel(
      cpf: idCard['cpf'] as String? ?? '',
      motherName: idCard['motherName'] as String? ?? '',
      fatherName: idCard['fatherName'] as String? ?? '',
      dateOfBirth: idCard['dateOfBirth'] as String? ?? '',
      birthCity: idCard['birthCity'] as String? ?? '',
      birthState: idCard['birthState'] as String? ?? '',
      nationality: idCard['nacionality'] as String? ?? '',
    );
  }

  /// Converts this model to a domain [EmployeeIdCard] entity.
  ///
  /// Converts [dateOfBirth] from `"yyyy-MM-dd"` to `"dd/MM/yyyy"` for display.
  EmployeeIdCard toEntity() {
    return EmployeeIdCard(
      cpf: cpf,
      motherName: motherName,
      fatherName: fatherName,
      dateOfBirth: _dateToDisplay(dateOfBirth),
      birthCity: birthCity,
      birthState: birthState,
      nationality: nationality,
    );
  }

  /// Converts a display date string from `"dd/MM/yyyy"` to `"yyyy-MM-dd"` for
  /// the API PUT body.
  static String dateToApi(String ddMMyyyy) {
    if (ddMMyyyy.length != 10) return ddMMyyyy;
    final parts = ddMMyyyy.split('/');
    if (parts.length != 3) return ddMMyyyy;
    return '${parts[2]}-${parts[1]}-${parts[0]}';
  }

  /// Converts an API date string from `"yyyy-MM-dd"` to `"dd/MM/yyyy"` for
  /// display.
  static String _dateToDisplay(String yyyyMMdd) {
    if (yyyyMMdd.isEmpty) return '';
    // Handle ISO format with time component e.g. "2000-01-15T00:00:00"
    final datePart = yyyyMMdd.split('T').first;
    final parts = datePart.split('-');
    if (parts.length != 3) return yyyyMMdd;
    return '${parts[2]}/${parts[1]}/${parts[0]}';
  }
}
