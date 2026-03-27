/// A document template belonging to a company.
///
/// Templates define the structure and metadata for documents that can be
/// generated for employees, including validity period, workload, and
/// signature requirements.
class DocumentTemplate {
  const DocumentTemplate({
    required this.id,
    required this.name,
    required this.description,
    required this.validityInDays,
    required this.workload,
    required this.usePreviousPeriod,
    required this.acceptsSignature,
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

  /// Number of days the generated document remains valid.
  final int? validityInDays;

  /// Expected workload in hours associated with this template.
  final int? workload;

  /// Whether the template uses the previous period as reference.
  final bool usePreviousPeriod;

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

  /// Whether this template belongs to a document group.
  bool get hasDocumentGroup => documentGroupId.isNotEmpty;

  /// Whether this template has a validity period configured.
  bool get hasValidity => validityInDays != null && validityInDays! > 0;

  /// Whether this template has a workload configured.
  bool get hasWorkload => workload != null && workload! > 0;

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

  /// Validates the validity in days field.
  ///
  /// Optional, must be in 0–999 range when provided.
  static String? validateValidity(String? value) {
    if (value == null || value.trim().isEmpty) return null;
    final days = int.tryParse(value.trim());
    if (days == null || days < 0 || days > 999) {
      return 'Informe um valor entre 0 e 999.';
    }
    return null;
  }

  /// Validates the workload in hours field.
  ///
  /// Optional, must be in 0–999 range when provided.
  static String? validateWorkload(String? value) {
    if (value == null || value.trim().isEmpty) return null;
    final hours = int.tryParse(value.trim());
    if (hours == null || hours < 0 || hours > 999) {
      return 'Informe um valor entre 0 e 999.';
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
