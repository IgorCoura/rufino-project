import '../../domain/entities/employee_social_integration_program.dart';

/// Data Transfer Object for the employee PIS/PASEP endpoint.
class EmployeeSocialIntegrationProgramApiModel {
  /// Creates a model with the given [number].
  const EmployeeSocialIntegrationProgramApiModel({required this.number});

  /// The PIS/PASEP number as returned by the API (may be empty).
  final String number;

  /// Deserialises the model from the API JSON map.
  factory EmployeeSocialIntegrationProgramApiModel.fromJson(
      Map<String, dynamic> json) {
    return EmployeeSocialIntegrationProgramApiModel(
      number: json['socialIntegrationProgramNumber'] as String? ?? '',
    );
  }

  /// Converts this model to a domain [EmployeeSocialIntegrationProgram].
  EmployeeSocialIntegrationProgram toEntity() =>
      EmployeeSocialIntegrationProgram(number: number);
}
