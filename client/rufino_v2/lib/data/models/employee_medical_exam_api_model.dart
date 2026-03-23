import '../../domain/entities/employee_medical_exam.dart';

/// Data Transfer Object for the employee medical admission exam endpoint.
///
/// The API stores dates in `yyyy-MM-dd` format. This model handles conversion
/// to and from the `dd/MM/yyyy` display format used by the UI.
class EmployeeMedicalExamApiModel {
  /// Creates an [EmployeeMedicalExamApiModel].
  const EmployeeMedicalExamApiModel({
    required this.dateExam,
    required this.validityExam,
  });

  /// The exam date in `yyyy-MM-dd` API format.
  final String dateExam;

  /// The exam validity date in `yyyy-MM-dd` API format.
  final String validityExam;

  /// Deserialises from the API JSON map.
  ///
  /// The GET response uses `dateExam` and `validityExam` keys with
  /// `yyyy-MM-dd` (or ISO 8601) formatted date strings.
  factory EmployeeMedicalExamApiModel.fromJson(Map<String, dynamic> json) {
    return EmployeeMedicalExamApiModel(
      dateExam: json['dateExam'] as String? ?? '',
      validityExam: json['validityExam'] as String? ?? '',
    );
  }

  /// Converts this model to a domain [EmployeeMedicalExam] entity.
  ///
  /// Dates are converted from `yyyy-MM-dd` to `dd/MM/yyyy` for display.
  EmployeeMedicalExam toEntity() {
    return EmployeeMedicalExam(
      dateExam: _dateToDisplay(dateExam),
      validityExam: _dateToDisplay(validityExam),
    );
  }

  /// Converts a display date `dd/MM/yyyy` to API format `yyyy-MM-dd`.
  static String dateToApi(String ddMMyyyy) {
    if (ddMMyyyy.length != 10) return ddMMyyyy;
    final parts = ddMMyyyy.split('/');
    if (parts.length != 3) return ddMMyyyy;
    return '${parts[2]}-${parts[1]}-${parts[0]}';
  }

  /// Builds the PUT request body for the medical exam endpoint.
  static Map<String, dynamic> toJsonMap(
    String employeeId,
    String dateExamDisplay,
    String validityExamDisplay,
  ) {
    return {
      'employeeId': employeeId,
      'dateExam': dateToApi(dateExamDisplay),
      'validityExam': dateToApi(validityExamDisplay),
    };
  }

  /// Converts an API date `yyyy-MM-dd` (or ISO with time) to `dd/MM/yyyy`.
  static String _dateToDisplay(String yyyyMMdd) {
    if (yyyyMMdd.isEmpty) return '';
    final datePart = yyyyMMdd.split('T').first;
    final parts = datePart.split('-');
    if (parts.length != 3) return yyyyMMdd;
    return '${parts[2]}/${parts[1]}/${parts[0]}';
  }
}
