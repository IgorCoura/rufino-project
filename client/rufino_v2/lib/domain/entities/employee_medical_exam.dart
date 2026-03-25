/// The employee's medical admission exam (Exame Médico Admissional / ASO).
class EmployeeMedicalExam {
  /// Creates an [EmployeeMedicalExam].
  const EmployeeMedicalExam({
    required this.dateExam,
    required this.validityExam,
  });

  /// The exam date in `dd/MM/yyyy` display format.
  final String dateExam;

  /// The exam validity/expiry date in `dd/MM/yyyy` display format.
  final String validityExam;
}
