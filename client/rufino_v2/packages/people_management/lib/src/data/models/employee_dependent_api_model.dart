import '../../domain/entities/employee_dependent.dart';

/// Data Transfer Object for the employee dependent endpoints.
///
/// The API nests identity fields inside an `idCard` object and uses
/// `yyyy-MM-dd` dates that are converted to `dd/MM/yyyy` for display.
class EmployeeDependentApiModel {
  /// Creates an [EmployeeDependentApiModel].
  const EmployeeDependentApiModel({
    required this.name,
    required this.genderId,
    required this.dependencyTypeId,
    required this.cpf,
    required this.motherName,
    required this.fatherName,
    required this.dateOfBirth,
    required this.birthCity,
    required this.birthState,
    required this.nationality,
  });

  final String name;
  final String genderId;
  final String dependencyTypeId;
  final String cpf;
  final String motherName;
  final String fatherName;

  /// Date of birth in `yyyy-MM-dd` API format.
  final String dateOfBirth;

  final String birthCity;
  final String birthState;
  final String nationality;

  /// Deserialises a single dependent from the API JSON.
  factory EmployeeDependentApiModel.fromJson(Map<String, dynamic> json) {
    final idCard = json['idCard'] as Map<String, dynamic>? ?? {};
    final gender = json['gender'];
    final depType = json['dependencyType'];
    return EmployeeDependentApiModel(
      name: json['name'] as String? ?? '',
      genderId: gender is Map ? (gender['id']).toString() : '$gender',
      dependencyTypeId: depType is Map ? (depType['id']).toString() : '$depType',
      cpf: idCard['cpf'] as String? ?? '',
      motherName: idCard['motherName'] as String? ?? '',
      fatherName: idCard['fatherName'] as String? ?? '',
      dateOfBirth: idCard['dateOfBirth'] as String? ?? '',
      birthCity: idCard['birthCity'] as String? ?? '',
      birthState: idCard['birthState'] as String? ?? '',
      nationality: idCard['nacionality'] as String? ?? '',
    );
  }

  /// Parses the GET response which wraps the list in a `dependents` key.
  static List<EmployeeDependentApiModel> fromListResponse(
      Map<String, dynamic> json) {
    final list = json['dependents'] as List<dynamic>? ?? [];
    return list
        .map((e) =>
            EmployeeDependentApiModel.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  /// Converts this model to a domain [EmployeeDependent] entity.
  EmployeeDependent toEntity() {
    return EmployeeDependent(
      originalName: name,
      name: name,
      genderId: genderId,
      dependencyTypeId: dependencyTypeId,
      cpf: cpf,
      motherName: motherName,
      fatherName: fatherName,
      dateOfBirth: _dateToDisplay(dateOfBirth),
      birthCity: birthCity,
      birthState: birthState,
      nationality: nationality,
    );
  }

  /// Builds the PUT body for creating a new dependent.
  static Map<String, dynamic> toCreateJson(
    String employeeId,
    EmployeeDependent dep,
  ) {
    return {
      'employeeId': employeeId,
      'name': dep.name,
      'idCard': _idCardJson(dep),
      'gender': int.tryParse(dep.genderId) ?? dep.genderId,
      'dependencyType':
          int.tryParse(dep.dependencyTypeId) ?? dep.dependencyTypeId,
    };
  }

  /// Builds the PUT body for updating an existing dependent.
  static Map<String, dynamic> toUpdateJson(
    String employeeId,
    EmployeeDependent dep,
  ) {
    return {
      'employeeId': employeeId,
      'oldName': dep.originalName,
      'currentDependent': {
        'name': dep.name,
        'idCard': _idCardJson(dep),
        'gender': int.tryParse(dep.genderId) ?? dep.genderId,
        'dependencyType':
            int.tryParse(dep.dependencyTypeId) ?? dep.dependencyTypeId,
      },
    };
  }

  /// Builds the PUT body for removing a dependent.
  static Map<String, dynamic> toRemoveJson(
      String employeeId, String dependentName) {
    return {
      'employeeId': employeeId,
      'nameDepedent': dependentName,
    };
  }

  static Map<String, dynamic> _idCardJson(EmployeeDependent dep) {
    return {
      'cpf': dep.cpf,
      'motherName': dep.motherName,
      'fatherName': dep.fatherName,
      'dateOfBirth': _dateToApi(dep.dateOfBirth),
      'birthCity': dep.birthCity,
      'birthState': dep.birthState,
      'nacionality': dep.nationality,
    };
  }

  static String _dateToDisplay(String yyyyMMdd) {
    if (yyyyMMdd.isEmpty) return '';
    final datePart = yyyyMMdd.split('T').first;
    final parts = datePart.split('-');
    if (parts.length != 3) return yyyyMMdd;
    return '${parts[2]}/${parts[1]}/${parts[0]}';
  }

  static String _dateToApi(String ddMMyyyy) {
    if (ddMMyyyy.isEmpty || ddMMyyyy.length != 10) return ddMMyyyy;
    final parts = ddMMyyyy.split('/');
    if (parts.length != 3) return ddMMyyyy;
    return '${parts[2]}-${parts[1]}-${parts[0]}';
  }
}
