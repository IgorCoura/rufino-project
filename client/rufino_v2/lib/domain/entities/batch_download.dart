import 'period.dart';
export 'period.dart';

/// An employee available for batch document download selection.
///
/// Used in Step 1 of the batch download wizard, enriched with role and
/// workplace names for filtering and display.
class BatchDownloadEmployee {
  const BatchDownloadEmployee({
    required this.id,
    required this.name,
    required this.statusId,
    required this.statusName,
    required this.roleName,
    required this.workplaceName,
  });

  /// The employee unique identifier.
  final String id;

  /// The employee display name.
  final String name;

  /// The employee status identifier (1-5).
  final int statusId;

  /// The employee status display name.
  final String statusName;

  /// The name of the employee's role.
  final String roleName;

  /// The name of the employee's workplace.
  final String workplaceName;

  /// Human-readable employee status label in Portuguese.
  String get statusLabel => switch (statusId) {
        1 => 'Pendente',
        2 => 'Ativo',
        3 => 'Ferias',
        4 => 'Afastado',
        5 => 'Inativo',
        _ => statusName,
      };
}

/// A document unit available for batch download.
///
/// Used in Step 2 of the batch download wizard, with employee and
/// template information for display and filtering.
class BatchDownloadUnit {
  const BatchDownloadUnit({
    required this.documentUnitId,
    required this.documentId,
    required this.employeeId,
    required this.employeeName,
    required this.documentTemplateName,
    required this.documentGroupName,
    required this.date,
    required this.statusId,
    required this.statusName,
    this.period,
    required this.hasFile,
  });

  /// The document unit unique identifier.
  final String documentUnitId;

  /// The parent document aggregate identifier.
  final String documentId;

  /// The owning employee identifier.
  final String employeeId;

  /// The owning employee display name.
  final String employeeName;

  /// The document template name.
  final String documentTemplateName;

  /// The document group name.
  final String documentGroupName;

  /// The document unit date in `dd/MM/yyyy` display format.
  final String date;

  /// The document unit status identifier (1-8).
  final int statusId;

  /// The document unit status display name.
  final String statusName;

  /// The competency period, if the document has one.
  final Period? period;

  /// Whether this unit has an uploaded file available for download.
  final bool hasFile;

  /// Human-readable status label in Portuguese.
  String get statusLabel => switch (statusId) {
        1 => 'Pendente',
        2 => 'OK',
        3 => 'Obsoleto',
        4 => 'Invalido',
        5 => 'Requer Validacao',
        6 => 'Nao Aplicavel',
        7 => 'Aguardando Assinatura',
        8 => 'Alerta',
        _ => statusName.isNotEmpty ? statusName : statusId.toString(),
      };

  /// Composite key for selection tracking.
  String get selectionKey => '$documentId:$documentUnitId';
}

/// A numbered group of document units selected in one round of the
/// combination wizard.
///
/// Each round of unit selection in step 2 produces one group. Groups are
/// merged per employee in order during the combine download.
class CombinationGroup {
  /// Creates a [CombinationGroup] with the given [groupNumber] and [units].
  const CombinationGroup({
    required this.groupNumber,
    required this.units,
  });

  /// The 1-based position of this group in the combination sequence.
  final int groupNumber;

  /// The document units selected in this round.
  final List<BatchDownloadUnit> units;
}

/// Paginated result of employees for batch download.
class BatchDownloadEmployeesPage {
  const BatchDownloadEmployeesPage({
    required this.items,
    required this.totalCount,
  });

  /// The employees on the current page.
  final List<BatchDownloadEmployee> items;

  /// The total number of matching employees across all pages.
  final int totalCount;
}

/// Paginated result of document units for batch download.
class BatchDownloadUnitsPage {
  const BatchDownloadUnitsPage({
    required this.items,
    required this.totalCount,
  });

  /// The document units on the current page.
  final List<BatchDownloadUnit> items;

  /// The total number of matching units across all pages.
  final int totalCount;
}

/// An item in the batch download request.
class BatchDownloadItem {
  const BatchDownloadItem({
    required this.documentId,
    required this.documentUnitId,
  });

  /// The parent document aggregate identifier.
  final String documentId;

  /// The document unit identifier.
  final String documentUnitId;

  /// Serializes to the JSON structure expected by the API.
  Map<String, dynamic> toJson() => {
        'documentId': documentId,
        'documentUnitId': documentUnitId,
      };
}
