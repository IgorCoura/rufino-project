import '../../domain/entities/employee_contact.dart';

/// Data Transfer Object for the employee contact endpoint.
class EmployeeContactApiModel {
  /// Creates an [EmployeeContactApiModel] from the given fields.
  const EmployeeContactApiModel({
    required this.cellphone,
    required this.email,
  });

  /// The employee's mobile phone number.
  final String cellphone;

  /// The employee's email address.
  final String email;

  /// Deserialises an [EmployeeContactApiModel] from the API JSON map.
  factory EmployeeContactApiModel.fromJson(Map<String, dynamic> json) {
    return EmployeeContactApiModel(
      cellphone: json['cellphone'] as String? ?? '',
      email: json['email'] as String? ?? '',
    );
  }

  /// Converts this model to a domain [EmployeeContact] entity.
  ///
  /// The cellphone is normalised to digits-only so the UI can apply any
  /// display mask consistently regardless of how the API stored the value.
  EmployeeContact toEntity() {
    return EmployeeContact(
      cellphone: cellphone.replaceAll(RegExp(r'[^\d]'), ''),
      email: email,
    );
  }
}
