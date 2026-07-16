/// A document template belonging to a company.
///
/// Templates define the structure and metadata for documents that can be
/// generated for employees. The rules that apply to a template — expiration,
/// workload — live in [policies]; see [TemplatePolicies].
class DocumentTemplate {
  const DocumentTemplate({
    required this.id,
    required this.name,
    required this.description,
    required this.acceptsSignature,
    this.policies = const TemplatePolicies(),
    this.bodyFileName = '',
    this.headerFileName = '',
    this.footerFileName = '',
    this.documentGroupId = '',
    this.documentGroupName = '',
    this.recoverDataTypeIds = const [],
    this.placeSignatures = const [],
    this.hasFile = false,
  });

  /// Unique identifier for this template.
  final String id;

  /// Human-readable name of the template.
  final String name;

  /// Detailed description of the template purpose.
  final String description;

  /// The rules this template applies. A rule is active when it is present.
  final TemplatePolicies policies;

  /// Whether the generated document accepts a digital signature.
  final bool acceptsSignature;

  /// The body file name for the template document.
  final String bodyFileName;

  /// The header file name for the template document.
  final String headerFileName;

  /// The footer file name for the template document.
  final String footerFileName;

  /// The id of the document group this template belongs to.
  final String documentGroupId;

  /// The display name of the document group.
  final String documentGroupName;

  /// The selected recover data type ids (e.g. [1, 3, 7]).
  final List<int> recoverDataTypeIds;

  /// The signature placements for this template.
  final List<PlaceSignatureData> placeSignatures;

  /// Whether the template has an uploaded file.
  final bool hasFile;

  // ─── Computed properties ─────────────────────────────────────────────────

  /// Number of days the generated document remains valid, or null when the
  /// template has no expiration rule.
  ///
  /// Derived from [policies] — the rule set is the single source of truth.
  int? get validityInDays => policies.expiration?.durationInDays;

  /// Expected workload in hours, or null when the template has no workload
  /// rule.
  ///
  /// Derived from [policies] — the rule set is the single source of truth.
  int? get workload => policies.workload?.hours;

  /// Whether this template belongs to a document group.
  bool get hasDocumentGroup => documentGroupId.isNotEmpty;

  /// Whether the template uses the previous competência as reference.
  ///
  /// Derived from [policies] — the rule set is the single source of truth.
  bool get usePreviousPeriod => policies.period?.usePreviousPeriod ?? false;

  /// Whether this template has a validity period configured.
  bool get hasValidity => policies.expiration != null;

  /// Whether this template has a workload configured.
  bool get hasWorkload => policies.workload != null;

  /// Whether this template is organized by competência.
  bool get hasPeriod => policies.period != null;

  /// Whether this template has any file name configured (body/header/footer).
  bool get hasFileConfiguration =>
      bodyFileName.isNotEmpty ||
      headerFileName.isNotEmpty ||
      footerFileName.isNotEmpty;

  /// Whether this template has signature placements configured.
  bool get hasSignaturePlacements => placeSignatures.isNotEmpty;

  /// Whether this template requires signature configuration.
  bool get requiresSignatureSetup => acceptsSignature;

  // ─── Validators ──────────────────────────────────────────────────────────

  /// Validates the template name.
  ///
  /// Required, max 100 characters.
  static String? validateName(String? value) {
    if (value == null || value.trim().isEmpty) {
      return 'O Nome não pode ser vazio.';
    }
    if (value.length > 100) {
      return 'O Nome não pode ter mais de 100 caracteres.';
    }
    return null;
  }

  /// Validates the template description.
  ///
  /// Required, max 500 characters.
  static String? validateDescription(String? value) {
    if (value == null || value.trim().isEmpty) {
      return 'A Descrição não pode ser vazia.';
    }
    if (value.length > 500) {
      return 'A Descrição não pode ter mais de 500 caracteres.';
    }
    return null;
  }

  /// Validates a file name field.
  ///
  /// Optional, max 20 characters, must end with `.html`.
  static String? validateFileName(String? value) {
    if (value == null || value.trim().isEmpty) return null;
    if (value.trim().length > 20) {
      return 'Máximo de 20 caracteres.';
    }
    if (!value.trim().toLowerCase().endsWith('.html')) {
      return 'O arquivo precisa ter extensão .html.';
    }
    return null;
  }
}

/// The set of rules a [DocumentTemplate] applies.
///
/// A rule is active when its field is non-null — absence is how "this rule does
/// not apply" is expressed. The API models the same way, and rejects a rule
/// carrying a zeroed value, so there is no such thing as an expiration of zero
/// days: that is simply no expiration.
class TemplatePolicies {
  const TemplatePolicies({this.expiration, this.workload, this.period});

  /// The expiration rule, or null when documents from this template never
  /// expire.
  final ExpirationRule? expiration;

  /// The workload rule, or null when this template carries no workload.
  final WorkloadRule? workload;

  /// The competência rule, or null when this template is not by competência.
  final PeriodRule? period;

  /// Whether no rule at all is active.
  bool get isEmpty => expiration == null && workload == null && period == null;

  /// Returns a copy with the given rules replaced.
  ///
  /// Passing `clearExpiration`, `clearWorkload` or `clearPeriod` removes the
  /// rule, which a null override cannot express.
  TemplatePolicies copyWith({
    ExpirationRule? expiration,
    WorkloadRule? workload,
    PeriodRule? period,
    bool clearExpiration = false,
    bool clearWorkload = false,
    bool clearPeriod = false,
  }) {
    return TemplatePolicies(
      expiration: clearExpiration ? null : (expiration ?? this.expiration),
      workload: clearWorkload ? null : (workload ?? this.workload),
      period: clearPeriod ? null : (period ?? this.period),
    );
  }
}

/// The granularity of a template's competência.
///
/// Ids match the backend's PeriodType smart enum (Daily=1 … Yearly=4) — they are
/// the contract; the labels are Portuguese presentation, kept here so the UI
/// does not depend on a network round-trip for four stable values.
enum PeriodGranularity {
  daily(1, 'Diário'),
  weekly(2, 'Semanal'),
  monthly(3, 'Mensal'),
  yearly(4, 'Anual');

  const PeriodGranularity(this.id, this.label);

  final int id;
  final String label;

  /// Returns the granularity with the given [id], or null when unknown.
  static PeriodGranularity? fromId(int id) {
    for (final value in values) {
      if (value.id == id) return value;
    }
    return null;
  }
}

/// The rule that organizes a template's documents by competência.
class PeriodRule {
  const PeriodRule({required this.granularity, this.usePreviousPeriod = false});

  /// How the competência is bucketed (daily, weekly, monthly, yearly).
  final PeriodGranularity granularity;

  /// Whether the document uses the previous competência instead of the current.
  final bool usePreviousPeriod;

  /// Returns a copy with the given fields replaced.
  PeriodRule copyWith({PeriodGranularity? granularity, bool? usePreviousPeriod}) {
    return PeriodRule(
      granularity: granularity ?? this.granularity,
      usePreviousPeriod: usePreviousPeriod ?? this.usePreviousPeriod,
    );
  }
}

/// The rule that makes documents from a template expire after a period.
class ExpirationRule {
  const ExpirationRule({required this.durationInDays, this.maxRenewals});

  /// How many days a generated document stays valid. Always positive.
  final int durationInDays;

  /// How many times a document renews before it stops, or null when it renews
  /// indefinitely. When set, always positive.
  final int? maxRenewals;

  /// Whether the document renews a limited number of times.
  bool get isLimited => maxRenewals != null;

  /// Returns a copy with the given fields replaced.
  ///
  /// [clearMaxRenewals] drops the limit (back to renewing forever) — needed
  /// because a null [maxRenewals] argument can't distinguish "keep" from "clear".
  ExpirationRule copyWith({int? durationInDays, int? maxRenewals, bool clearMaxRenewals = false}) {
    return ExpirationRule(
      durationInDays: durationInDays ?? this.durationInDays,
      maxRenewals: clearMaxRenewals ? null : (maxRenewals ?? this.maxRenewals),
    );
  }

  /// Validates the duration typed by the user for this rule.
  ///
  /// Required and in the 1–999 range: the API rejects a zeroed duration, since
  /// a rule that expires nothing is absence of the rule.
  static String? validateDuration(String? value) {
    if (value == null || value.trim().isEmpty) {
      return 'Informe a validade em dias.';
    }
    final days = int.tryParse(value.trim());
    if (days == null || days < 1 || days > 999) {
      return 'Informe um valor entre 1 e 999.';
    }
    return null;
  }

  /// Validates the renewal limit typed by the user.
  ///
  /// Required and in the 1–999 range: the API rejects a limit below 1, since a
  /// zero-renewal limit is not the purpose of the rule.
  static String? validateMaxRenewals(String? value) {
    if (value == null || value.trim().isEmpty) {
      return 'Informe o número de renovações.';
    }
    final renewals = int.tryParse(value.trim());
    if (renewals == null || renewals < 1 || renewals > 999) {
      return 'Informe um valor entre 1 e 999.';
    }
    return null;
  }
}

/// The rule that associates a workload with a template's documents.
class WorkloadRule {
  const WorkloadRule({required this.hours});

  /// How many hours of work the document represents. Always positive.
  final int hours;

  /// Validates the workload typed by the user for this rule.
  ///
  /// Required and in the 1–999 range, for the same reason as
  /// [ExpirationRule.validateDuration].
  static String? validateHours(String? value) {
    if (value == null || value.trim().isEmpty) {
      return 'Informe a carga horária em horas.';
    }
    final hours = int.tryParse(value.trim());
    if (hours == null || hours < 1 || hours > 999) {
      return 'Informe um valor entre 1 e 999.';
    }
    return null;
  }
}

/// A single signature placement entry for a document template.
class PlaceSignatureData {
  const PlaceSignatureData({
    this.typeSignatureId = '',
    this.page = '',
    this.positionBottom = '',
    this.positionLeft = '',
    this.sizeX = '',
    this.sizeY = '',
  });

  /// The type of signature id (1=Assinatura, 2=Visto).
  final String typeSignatureId;

  /// The page number (0–100).
  final String page;

  /// Relative position from bottom (0–100).
  final String positionBottom;

  /// Relative position from left (0–100).
  final String positionLeft;

  /// Relative horizontal size (0–100).
  final String sizeX;

  /// Relative vertical size (0–100).
  final String sizeY;

  /// Whether this placement has a signature type selected.
  bool get hasType => typeSignatureId.isNotEmpty;

  /// Whether all numeric fields have values.
  bool get isComplete =>
      hasType &&
      page.isNotEmpty &&
      positionBottom.isNotEmpty &&
      positionLeft.isNotEmpty &&
      sizeX.isNotEmpty &&
      sizeY.isNotEmpty;

  // ─── Validators ──────────────────────────────────────────────────────────

  /// Validates a numeric signature placement field.
  ///
  /// Required, must be in 0–100 range.
  static String? validateField(String? value, String label) {
    if (value == null || value.trim().isEmpty) return '$label é obrigatório.';
    final number = double.tryParse(value.trim());
    if (number == null || number < 0 || number > 100) {
      return '$label deve estar entre 0 e 100.';
    }
    return null;
  }

  /// Validates the signature type selection.
  ///
  /// Mandatory: a placement without a type is sent as `type: 0`, which the API
  /// rejects (there is no signature type 0), failing the whole save.
  static String? validateType(String? typeSignatureId) {
    if (typeSignatureId == null || typeSignatureId.trim().isEmpty) {
      return 'Selecione o tipo de assinatura.';
    }
    return null;
  }

  /// Returns a copy with the given overrides.
  PlaceSignatureData copyWith({
    String? typeSignatureId,
    String? page,
    String? positionBottom,
    String? positionLeft,
    String? sizeX,
    String? sizeY,
  }) {
    return PlaceSignatureData(
      typeSignatureId: typeSignatureId ?? this.typeSignatureId,
      page: page ?? this.page,
      positionBottom: positionBottom ?? this.positionBottom,
      positionLeft: positionLeft ?? this.positionLeft,
      sizeX: sizeX ?? this.sizeX,
      sizeY: sizeY ?? this.sizeY,
    );
  }
}
