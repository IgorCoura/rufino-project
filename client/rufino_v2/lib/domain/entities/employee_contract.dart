/// A single employment contract (Contrato) for an employee.
class EmployeeContractInfo {
  /// Creates an [EmployeeContractInfo].
  const EmployeeContractInfo({
    required this.initDate,
    required this.finalDate,
    required this.typeId,
    required this.typeName,
  });

  /// The contract start date in `dd/MM/yyyy` display format.
  final String initDate;

  /// The contract end date in `dd/MM/yyyy` display format, or empty when the
  /// contract is still active.
  final String finalDate;

  /// The contract type id.
  final String typeId;

  /// The contract type display name (e.g. "CLT", "Aprendiz").
  final String typeName;

  /// Whether this contract is still active (no end date).
  bool get isActive => finalDate.isEmpty;
}
