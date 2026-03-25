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

  /// Total number of units across all pages (for pagination).
  final int totalUnitsCount;

  /// The loaded document units for the current page.
  final List<DocumentUnit> units;
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
}
