import '../../domain/entities/employee_military_document.dart';

/// Data Transfer Object for the employee military document endpoint.
class EmployeeMilitaryDocumentApiModel {
  /// Creates an [EmployeeMilitaryDocumentApiModel].
  const EmployeeMilitaryDocumentApiModel({
    required this.number,
    required this.type,
    required this.isRequired,
  });

  /// The document number.
  final String number;

  /// The document type (e.g. "Reservista").
  final String type;

  /// Whether the military document is required for this employee.
  final bool isRequired;

  /// Deserialises an [EmployeeMilitaryDocumentApiModel] from the API JSON map.
  ///
  /// The GET response uses `number` and `type` keys, which differ from the PUT
  /// body keys (`documentNumber` / `documentType`).
  factory EmployeeMilitaryDocumentApiModel.fromJson(
      Map<String, dynamic> json) {
    return EmployeeMilitaryDocumentApiModel(
      number: json['number'] as String? ?? '',
      type: json['type'] as String? ?? '',
      isRequired: json['isRequired'] as bool? ?? false,
    );
  }

  /// Converts this model to a domain [EmployeeMilitaryDocument] entity.
  EmployeeMilitaryDocument toEntity() {
    return EmployeeMilitaryDocument(
      number: number,
      type: type,
      isRequired: isRequired,
    );
  }

  /// Builds the PUT request body for the military document endpoint.
  static Map<String, dynamic> toJsonMap(
    String employeeId,
    String number,
    String type,
  ) {
    return {
      'employeeId': employeeId,
      'documentNumber': number,
      'documentType': type,
    };
  }
}
