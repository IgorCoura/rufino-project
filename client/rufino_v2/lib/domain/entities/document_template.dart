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
