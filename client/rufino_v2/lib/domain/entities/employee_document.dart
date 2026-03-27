/// A required document (Documento) for an employee, containing a list of
/// document units (instances).
class EmployeeDocument {
  const EmployeeDocument({
    required this.id,
    required this.name,
    required this.description,
    required this.statusId,
    required this.statusName,
    required this.isSignable,
    required this.canGenerateDocument,
    required this.usePreviousPeriod,
    required this.totalUnitsCount,
    required this.units,
  });

  final String id;
  final String name;
  final String description;

  /// The document group status id.
  final String statusId;

  /// The document group status display name.
  final String statusName;

  /// Whether this document supports digital signing.
  final bool isSignable;

  /// Whether a PDF can be generated for this document.
  final bool canGenerateDocument;

  /// Whether this document uses a previous period reference.
  final bool usePreviousPeriod;

  /// Total number of units across all pages (for pagination).
  final int totalUnitsCount;

  /// The loaded document units for the current page.
  final List<DocumentUnit> units;

  /// Whether this document has any pending units.
  bool get hasPendingUnits => units.any((u) => u.isPending);

  /// Whether this document has any units with attached files.
  bool get hasFilledUnits => units.any((u) => u.hasFile);

  /// Returns the number of pending units.
  int get pendingUnitsCount => units.where((u) => u.isPending).length;

  /// Display label for the document group status.
  String get groupStatusLabel => switch (statusId) {
        '1' => 'OK',
        '2' => 'Pendente',
        '3' => 'Inválido',
        _ => statusName.isNotEmpty ? statusName : statusId,
      };
}

/// A single instance (unit) of a document.
class DocumentUnit {
  const DocumentUnit({
    required this.id,
    required this.statusId,
    required this.statusName,
    required this.date,
    required this.validity,
    required this.createdAt,
    required this.hasFile,
    required this.name,
  });

  final String id;

  /// The unit status id (1=Pendente, 2=OK, 3=Obsoleto, 4=Inválido,
  /// 5=Requer Validação, 6=Não Aplicável, 7=Aguardando Assinatura).
  final String statusId;

  /// The unit status display name.
  final String statusName;

  /// The document date in `dd/MM/yyyy` display format.
  final String date;

  /// The validity/expiry date in `dd/MM/yyyy` display format.
  final String validity;

  /// The creation timestamp in `dd/MM/yyyy` display format.
  final String createdAt;

  /// Whether this unit has a file attached.
  final bool hasFile;

  /// The file name (including extension) when a file is attached.
  final String name;

  /// Whether this unit is pending (status id 1).
  bool get isPending => statusId == '1';

  /// Whether this unit is OK (status id 2).
  bool get isOk => statusId == '2';

  /// Whether this unit is obsolete (status id 3).
  bool get isObsolete => statusId == '3';

  /// Whether this unit is invalid (status id 4).
  bool get isInvalid => statusId == '4';

  /// Whether this unit requires validation (status id 5).
  bool get requiresValidation => statusId == '5';

  /// Whether this unit is not applicable (status id 6).
  bool get isNotApplicable => statusId == '6';

  /// Whether this unit is waiting for a digital signature (status id 7).
  bool get isWaitingSignature => statusId == '7';

  /// Display label for the unit status.
  String get statusLabel => switch (statusId) {
        '1' => 'Pendente',
        '2' => 'OK',
        '3' => 'Obsoleto',
        '4' => 'Inválido',
        '5' => 'Requer Validação',
        '6' => 'Não Aplicável',
        '7' => 'Aguardando Assinatura',
        _ => statusName.isNotEmpty ? statusName : statusId,
      };

  /// Converts the [date] from `dd/MM/yyyy` to `yyyy-MM-dd` for file names.
  ///
  /// Returns `"sem-data"` when the date is empty or has an unexpected format.
  String get dateForFileName {
    if (date.isEmpty || date.length != 10) return 'sem-data';
    return '${date.substring(6)}-${date.substring(3, 5)}-'
        '${date.substring(0, 2)}';
  }

  /// Builds a download file name from this unit's date and the parent
  /// [docName], e.g. `"2026-03-01-contrato-de-trabalho.pdf"`.
  String downloadFileName(String docName, {String extension = 'pdf'}) {
    final namePart = docName
        .toLowerCase()
        .replaceAll(RegExp(r'[^\w\s-]'), '')
        .replaceAll(RegExp(r'\s+'), '-');
    return '$dateForFileName-$namePart.$extension';
  }
}
