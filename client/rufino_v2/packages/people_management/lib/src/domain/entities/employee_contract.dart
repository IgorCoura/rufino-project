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

  /// Whether this contract has been terminated (has an end date).
  bool get isTerminated => finalDate.isNotEmpty;

  // ─── Validators ──────────────────────────────────────────────────────────

  /// Validates a contract init date in `dd/MM/yyyy` format.
  ///
  /// Must be a valid date between 12 years in the past and 1 year in the
  /// future, matching backend `EmploymentContract.InitDate` rules.
  static String? validateInitDate(String? value) {
    final stripped = (value ?? '').replaceAll(RegExp(r'[^\d]'), '');
    if (stripped.isEmpty) {
      return 'A data não pode ser vazia.';
    }
    if (stripped.length != 8) {
      return 'Data inválida (ex: 15/03/2026).';
    }
    try {
      final parts = value!.split('/');
      final date =
          DateTime.tryParse('${parts[2]}-${parts[1]}-${parts[0]}');
      if (date == null) return 'Data inválida.';

      final now = DateTime.now();
      final minDate = DateTime(now.year - 12, now.month, now.day);
      final maxDate = DateTime(now.year + 1, now.month, now.day);
      if (date.isBefore(minDate) || date.isAfter(maxDate)) {
        return 'A data deve estar entre ${_formatDate(minDate)} e ${_formatDate(maxDate)}.';
      }
    } catch (_) {
      return 'Data inválida.';
    }
    return null;
  }

  /// Validates a contract final date in `dd/MM/yyyy` format.
  ///
  /// Must be a valid date within ±1 year of today, matching backend
  /// `EmploymentContract.FinalDate` rules.
  static String? validateFinalDate(String? value) {
    final stripped = (value ?? '').replaceAll(RegExp(r'[^\d]'), '');
    if (stripped.isEmpty) {
      return 'A data não pode ser vazia.';
    }
    if (stripped.length != 8) {
      return 'Data inválida (ex: 15/03/2026).';
    }
    try {
      final parts = value!.split('/');
      final date =
          DateTime.tryParse('${parts[2]}-${parts[1]}-${parts[0]}');
      if (date == null) return 'Data inválida.';

      final now = DateTime.now();
      final minDate = DateTime(now.year - 1, now.month, now.day);
      final maxDate = DateTime(now.year + 1, now.month, now.day);
      if (date.isBefore(minDate) || date.isAfter(maxDate)) {
        return 'A data deve estar entre ${_formatDate(minDate)} e ${_formatDate(maxDate)}.';
      }
    } catch (_) {
      return 'Data inválida.';
    }
    return null;
  }

  static String _formatDate(DateTime d) =>
      '${d.day.toString().padLeft(2, '0')}/${d.month.toString().padLeft(2, '0')}/${d.year}';
}
