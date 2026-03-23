import '../../domain/entities/employee_contract.dart';

/// Data Transfer Object for the employee contract endpoints.
///
/// Dates in the API use `yyyy-MM-dd` format and are converted to
/// `dd/MM/yyyy` for UI display.
class EmployeeContractApiModel {
  const EmployeeContractApiModel({
    required this.initDate,
    required this.finalDate,
    required this.typeId,
    required this.typeName,
  });

  final String initDate;
  final String finalDate;
  final String typeId;
  final String typeName;

  /// Deserialises a single contract from the API JSON.
  factory EmployeeContractApiModel.fromJson(Map<String, dynamic> json) {
    final type = json['type'] as Map<String, dynamic>? ?? {};
    return EmployeeContractApiModel(
      initDate: json['initDate'] as String? ?? '',
      finalDate: json['finalDate'] as String? ?? '',
      typeId: (type['id'] ?? '').toString(),
      typeName: type['name'] as String? ?? '',
    );
  }

  /// Parses the GET response which wraps the list in a `contracts` key.
  static List<EmployeeContractApiModel> fromListResponse(
      Map<String, dynamic> json) {
    final list = json['contracts'] as List<dynamic>? ?? [];
    return list
        .map((e) =>
            EmployeeContractApiModel.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  /// Converts to a domain entity with display-formatted dates.
  EmployeeContractInfo toEntity() {
    return EmployeeContractInfo(
      initDate: _dateToDisplay(initDate),
      finalDate: _dateToDisplay(finalDate),
      typeId: typeId,
      typeName: typeName,
    );
  }

  /// Converts `dd/MM/yyyy` to `yyyy-MM-dd` for the API.
  static String dateToApi(String ddMMyyyy) {
    if (ddMMyyyy.isEmpty || ddMMyyyy.length != 10) return ddMMyyyy;
    final parts = ddMMyyyy.split('/');
    if (parts.length != 3) return ddMMyyyy;
    return '${parts[2]}-${parts[1]}-${parts[0]}';
  }

  static String _dateToDisplay(String yyyyMMdd) {
    if (yyyyMMdd.isEmpty) return '';
    final datePart = yyyyMMdd.split('T').first;
    final parts = datePart.split('-');
    if (parts.length != 3) return yyyyMMdd;
    return '${parts[2]}/${parts[1]}/${parts[0]}';
  }
}

/// Data Transfer Object for a contract type option.
class ContractTypeApiModel {
  const ContractTypeApiModel({required this.id, required this.name});

  final String id;
  final String name;

  factory ContractTypeApiModel.fromJson(Map<String, dynamic> json) {
    return ContractTypeApiModel(
      id: (json['id']).toString(),
      name: json['name'] as String? ?? '',
    );
  }
}
