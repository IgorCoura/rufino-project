import 'document_status_labels.dart';
import 'period.dart';

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
    this.periodTypeId,
    this.suggestedSignatureScheduleDate = '',
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

  /// The competência granularity id configured in the template (1=Diário …
  /// 4=Anual), or null when the document is not organized by competência.
  ///
  /// Only the detail endpoint sends it, so it is null until the document's units
  /// are loaded. [usePreviousPeriod] does not answer the same question: it is
  /// false both for a document without competência and for one that uses the
  /// current period.
  final int? periodTypeId;

  /// Total number of units across all pages (for pagination).
  final int totalUnitsCount;

  /// The loaded document units for the current page.
  final List<DocumentUnit> units;

  /// The date suggested when scheduling the next signature send, in
  /// `dd/MM/yyyy`, or empty when there is none to suggest.
  ///
  /// It is when the current coverage expires — the validity of the delivered
  /// unit the next one will replace. Comes from the server, not from [units]:
  /// the list is one page, so the delivered unit may not even be loaded.
  final String suggestedSignatureScheduleDate;

  /// Whether there is a date to prefill the schedule field with.
  bool get hasSuggestedSignatureScheduleDate =>
      suggestedSignatureScheduleDate.isNotEmpty;

  /// Whether this document is organized by competência.
  ///
  /// It is the only kind that can hold more than one unit in force at a time —
  /// one per period — and therefore the only one where creating a unit by hand
  /// makes sense. Everywhere else the next unit comes from deprecating,
  /// invalidating or renewing the current one.
  bool get isByCompetency => periodTypeId != null;

  /// The competência granularity, or null when the document has none.
  PeriodGranularity? get periodGranularity =>
      periodTypeId == null ? null : PeriodGranularity.fromId(periodTypeId!);

  /// Whether this document has any pending units.
  bool get hasPendingUnits => units.any((u) => u.isPending);

  /// Whether this document has any units with attached files.
  bool get hasFilledUnits => units.any((u) => u.hasFile);

  /// Returns the number of pending units.
  int get pendingUnitsCount => units.where((u) => u.isPending).length;

  /// Display label for the document status.
  ///
  /// These ids are the document-level status (1–7), not the three-valued
  /// compliance rollup used for groups and employees.
  String get statusLabel => documentStatusLabel(statusId, statusName);
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
    this.period,
    this.scheduledSignatureSendOn = '',
    this.replacesDocumentUnitId = '',
  });

  final String id;

  /// The unit status id (1=Pendente, 2=OK, 3=Obsoleto, 4=Inválido,
  /// 5=Requer Validação, 6=Não Aplicável, 7=Aguardando Assinatura,
  /// 8=A Vencer, 9=Vencido).
  ///
  /// "Obsoleto" (3) and "Vencido" (9) are both documents that left validity.
  /// The difference is whether a replacement already arrived: 3 has one and is
  /// kept as proof of the period it covered, 9 does not and is a compliance
  /// gap.
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

  /// The competency period, if the document has one.
  final Period? period;

  /// The date this unit is scheduled to be sent for signature, in `dd/MM/yyyy`,
  /// or empty when nothing is scheduled.
  ///
  /// The unit stays [isPending] while scheduled — the schedule is an intent,
  /// not a send.
  final String scheduledSignatureSendOn;

  /// Id of the unit this one renews, or empty when it is not a renewal.
  ///
  /// A renewal is the next validity cycle of a document that is expiring or has
  /// expired — not a new requirement. While the renewed unit still covers, this
  /// one is the renewal in flight and the document keeps reporting the coverage
  /// it still has.
  final String replacesDocumentUnitId;

  /// Whether this unit was created to renew another one.
  bool get isRenewal => replacesDocumentUnitId.isNotEmpty;

  /// Whether this unit has a signature send scheduled for a future date.
  bool get isSignatureScheduled => scheduledSignatureSendOn.isNotEmpty;

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

  /// Whether this unit is close to its validity date (status id 8).
  bool get isWarning => statusId == '8';

  /// Whether this unit expired and has no replacement yet (status id 9).
  bool get isExpired => statusId == '9';

  /// Whether this unit can be deprecated — only a document currently in force
  /// can become history.
  bool get canBeDeprecated => isOk;

  /// Whether this unit can be invalidated.
  ///
  /// Includes [isNotApplicable]: marking a document as not applicable is an
  /// administrative decision, not proof of coverage, so undoing it when the
  /// document becomes required again erases no period. It is also the only
  /// action such a unit has — invalidating leaves a fresh pending in its place.
  ///
  /// Deprecated and expired units are the proof that the employee was covered
  /// in that period, so they are never invalidated.
  bool get canBeInvalidated => isPending || isOk || isNotApplicable;

  /// Whether this unit can be marked as not applicable — only while nothing
  /// has been delivered for it.
  bool get canBeMarkedNotApplicable => isPending;

  /// Whether a replacement can be asked for this unit.
  ///
  /// Renewing is trading a delivery that had value for the next one, so it
  /// applies to a unit that is (or was) in force — before it expires ([isOk],
  /// [isWarning]) or after ([isExpired]).
  ///
  /// Different from deprecating: deprecating drops the coverage right away,
  /// renewing keeps it until the replacement arrives. Obsolete units already
  /// have a replacement; pending, awaiting signature and requires-validation
  /// are the delivery still in progress — what is missing there is delivering,
  /// not renewing.
  bool get canBeRenewed => isOk || isWarning || isExpired;

  /// Display label for the unit status.
  String get statusLabel => documentUnitStatusLabel(statusId, statusName);

  /// Converts the [date] from `dd/MM/yyyy` to `yyyy_MM_dd` for file names.
  ///
  /// Returns `"SEM_DATA"` when the date is empty or has an unexpected format.
  String get dateForFileName {
    if (date.isEmpty || date.length != 10) return 'SEM_DATA';
    return '${date.substring(6)}_${date.substring(3, 5)}_'
        '${date.substring(0, 2)}';
  }

  // ─── Validators ──────────────────────────────────────────────────────────

  /// Validates a document unit date in `dd/MM/yyyy` format.
  ///
  /// Only checks format — no range restriction. The backend validates
  /// `Date != MinValue && Date != MaxValue` at domain level.
  static String? validateDate(String? value) {
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
    } catch (_) {
      return 'Data inválida.';
    }
    return null;
  }

  /// Parses a `dd/MM/yyyy` string, or returns null when it is not a valid date.
  static DateTime? parseDate(String? value) {
    final stripped = (value ?? '').replaceAll(RegExp(r'[^\d]'), '');
    if (stripped.length != 8) return null;
    final parts = value!.split('/');
    if (parts.length != 3) return null;
    return DateTime.tryParse('${parts[2]}-${parts[1]}-${parts[0]}');
  }

  /// Validates the date the signature send is scheduled for.
  ///
  /// Required, and today or later — the API rejects a past date (`PMD.DOC21`),
  /// since a schedule in the past would fire at once, which is the opposite of
  /// what scheduling is for.
  static String? validateScheduleSendDate(String? value) {
    final date = parseDate(value);
    if (date == null) return 'Informe a data do envio (ex: 15/03/2026).';

    final today = DateTime.now();
    if (date.isBefore(DateTime(today.year, today.month, today.day))) {
      return 'A data do envio não pode estar no passado.';
    }
    return null;
  }

  /// Validates the signing deadline against the scheduled send date [sendOn].
  ///
  /// The deadline is counted from the send, so it must be strictly after it —
  /// same rule the API enforces (`PMD.DOC22`). With an unparseable [sendOn]
  /// only the format is checked; the send-date field reports that problem.
  static String? validateSignDeadline(String? value, String? sendOn) {
    final deadline = parseDate(value);
    if (deadline == null) return 'Informe a data limite (ex: 20/03/2026).';

    final send = parseDate(sendOn);
    if (send == null) return null;

    if (!deadline.isAfter(send)) {
      return 'A data limite precisa ser posterior à data do envio.';
    }
    return null;
  }

  /// Builds a download file name following the backend naming convention.
  ///
  /// Pattern: `{EMPLOYEE}-{YYYY_MM_DD}-{DOCUMENT}-{idSuffix}.{EXT}`,
  /// all segments UPPERCASED with spaces replaced by underscores.
  String downloadFileName(
    String docName, {
    required String employeeName,
    String extension = 'pdf',
  }) {
    final employeeSegment =
        employeeName.trim().replaceAll(' ', '_').toUpperCase();
    final documentSegment =
        docName.trim().replaceAll(' ', '_').toUpperCase();
    final idSuffix = id.length >= 4
        ? id.substring(id.length - 4).toUpperCase()
        : id.toUpperCase();
    return '$employeeSegment-$dateForFileName-$documentSegment-$idSuffix.${extension.toUpperCase()}';
  }
}
