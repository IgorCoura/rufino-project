import '../../domain/entities/employee_vote_id.dart';

/// Data Transfer Object for the employee voter registration (Título de Eleitor)
/// endpoint.
class EmployeeVoteIdApiModel {
  /// Creates an [EmployeeVoteIdApiModel] with the given voter registration
  /// [number].
  const EmployeeVoteIdApiModel({required this.number});

  /// The voter registration number.
  final String number;

  /// Deserialises an [EmployeeVoteIdApiModel] from the API JSON map.
  factory EmployeeVoteIdApiModel.fromJson(Map<String, dynamic> json) {
    return EmployeeVoteIdApiModel(
      number: json['voteIdNumber'] as String? ?? '',
    );
  }

  /// Converts this model to a domain [EmployeeVoteId] entity.
  EmployeeVoteId toEntity() => EmployeeVoteId(number: number);
}
