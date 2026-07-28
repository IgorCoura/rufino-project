import 'period.dart';

/// Operational bucket of the company-wide document dashboard.
///
/// Mirrors the backend `DashboardBucket` enum — [apiValue] is the exact
/// string sent as the `bucket` query parameter, and summary counts and the
/// unit list share the same server-side predicate per bucket.
enum DashboardBucket {
  expired('Expired', 'Vencidos'),
  expiring('Expiring', 'A Vencer'),
  pending('Pending', 'Pendentes'),
  awaitingSignature('AwaitingSignature', 'Aguardando Assinatura'),
  requiresValidation('RequiresValidation', 'Requer Validação');

  const DashboardBucket(this.apiValue, this.label);

  /// The value sent to the API's `bucket` query parameter.
  final String apiValue;

  /// The Brazilian Portuguese display label.
  final String label;
}

/// Company-wide counts of document units per dashboard bucket.
class DashboardSummary {
  const DashboardSummary({
    required this.expired,
    required this.expiring,
    required this.pending,
    required this.awaitingSignature,
    required this.requiresValidation,
  });

  /// An empty summary with all counts zeroed.
  static const empty = DashboardSummary(
    expired: 0,
    expiring: 0,
    pending: 0,
    awaitingSignature: 0,
    requiresValidation: 0,
  );

  final int expired;
  final int expiring;
  final int pending;
  final int awaitingSignature;
  final int requiresValidation;

  /// Returns the count for [bucket].
  int countFor(DashboardBucket bucket) => switch (bucket) {
        DashboardBucket.expired => expired,
        DashboardBucket.expiring => expiring,
        DashboardBucket.pending => pending,
        DashboardBucket.awaitingSignature => awaitingSignature,
        DashboardBucket.requiresValidation => requiresValidation,
      };

  /// Total number of units across all buckets.
  int get total =>
      expired + expiring + pending + awaitingSignature + requiresValidation;
}

/// A document unit row of the dashboard list, enriched with the owning
/// employee, template and group for display.
class DashboardUnitItem {
  const DashboardUnitItem({
    required this.documentUnitId,
    required this.documentId,
    required this.employeeId,
    required this.employeeName,
    required this.employeeStatusId,
    required this.employeeStatusName,
    required this.documentTemplateName,
    required this.documentGroupName,
    required this.date,
    required this.validity,
    required this.statusId,
    required this.statusName,
    this.period,
    required this.hasFile,
  });

  final String documentUnitId;
  final String documentId;
  final String employeeId;
  final String employeeName;

  /// The employee status identifier (1–5).
  final String employeeStatusId;

  /// The employee status display name.
  final String employeeStatusName;

  final String documentTemplateName;
  final String documentGroupName;

  /// The document unit date in `dd/MM/yyyy` display format.
  final String date;

  /// The validity/expiry date in `dd/MM/yyyy` format, empty when the unit
  /// has no expiration.
  final String validity;

  /// The unit status id (1=Pendente … 8=A Vencer).
  final String statusId;

  /// The unit status display name.
  final String statusName;

  /// The competency period, if the document has one.
  final Period? period;

  /// Whether this unit has a file attached.
  final bool hasFile;

  /// Whether this unit carries a validity date.
  bool get hasValidity => validity.isNotEmpty;

  /// Display label for the unit status.
  String get statusLabel => switch (statusId) {
        '1' => 'Pendente',
        '2' => 'OK',
        '3' => 'Obsoleto',
        '4' => 'Inválido',
        '5' => 'Requer Validação',
        '6' => 'Não Aplicável',
        '7' => 'Aguardando Assinatura',
        '8' => 'A Vencer',
        _ => statusName.isNotEmpty ? statusName : statusId,
      };

  /// Human-readable employee status label in Portuguese.
  String get employeeStatusLabel => switch (employeeStatusId) {
        '1' => 'Pendente',
        '2' => 'Ativo',
        '3' => 'Férias',
        '4' => 'Afastado',
        '5' => 'Inativo',
        _ => employeeStatusName,
      };

  /// Signed number of days between [reference] and the validity date.
  ///
  /// Positive means the unit still has days to live, zero means it expires
  /// on [reference]'s date, negative means it is already overdue. Returns
  /// `null` when the unit has no validity date.
  int? daysUntilValidity(DateTime reference) {
    final parsed = _parseValidity();
    if (parsed == null) return null;
    final refDay = DateTime(reference.year, reference.month, reference.day);
    return parsed.difference(refDay).inDays;
  }

  /// Urgency phrase for the list row ("vence em X dias" / "vencido há X
  /// dias"), or `null` when the unit has no validity date.
  String? urgencyLabel(DateTime reference) {
    final days = daysUntilValidity(reference);
    if (days == null) return null;
    if (days > 1) return 'Vence em $days dias';
    if (days == 1) return 'Vence amanhã';
    if (days == 0) return 'Vence hoje';
    if (days == -1) return 'Vencido há 1 dia';
    return 'Vencido há ${-days} dias';
  }

  DateTime? _parseValidity() {
    if (validity.isEmpty) return null;
    final parts = validity.split('/');
    if (parts.length != 3) return null;
    final day = int.tryParse(parts[0]);
    final month = int.tryParse(parts[1]);
    final year = int.tryParse(parts[2]);
    if (day == null || month == null || year == null) return null;
    return DateTime(year, month, day);
  }
}

/// Paginated result of dashboard document units.
class DashboardUnitsPage {
  const DashboardUnitsPage({required this.items, required this.totalCount});

  /// The dashboard units on the current page.
  final List<DashboardUnitItem> items;

  /// The total number of matching units across all pages.
  final int totalCount;
}
