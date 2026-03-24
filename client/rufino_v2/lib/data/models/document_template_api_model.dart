import '../../domain/entities/document_template.dart';

/// DTO that mirrors the JSON shape of a document template returned by the
/// people-management API.
class DocumentTemplateApiModel {
  const DocumentTemplateApiModel({
    required this.id,
    required this.name,
    required this.description,
    required this.validityDurationInDays,
    required this.workloadInHours,
    required this.usePreviousPeriod,
    required this.acceptsSignature,
    this.bodyFileName = '',
    this.headerFileName = '',
    this.footerFileName = '',
    this.documentGroupId = '',
    this.documentGroupName = '',
    this.recoverDataTypeIds = const [],
    this.placeSignatures = const [],
  });

  final String id;
  final String name;
  final String description;
  final double validityDurationInDays;
  final double workloadInHours;
  final bool usePreviousPeriod;
  final bool acceptsSignature;
  final String bodyFileName;
  final String headerFileName;
  final String footerFileName;
  final String documentGroupId;
  final String documentGroupName;
  final List<int> recoverDataTypeIds;
  final List<PlaceSignatureData> placeSignatures;

  /// Deserialises a [DocumentTemplateApiModel] from the simplified JSON
  /// returned by the `/documenttemplate/simple` endpoint.
  factory DocumentTemplateApiModel.fromJsonSimple(Map<String, dynamic> json) {
    return DocumentTemplateApiModel(
      id: json['id'] as String? ?? '',
      name: json['name'] as String? ?? '',
      description: json['description'] as String? ?? '',
      validityDurationInDays: 0,
      workloadInHours: 0,
      usePreviousPeriod: json['usePreviousPeriod'] as bool? ?? false,
      acceptsSignature: json['acceptsSignature'] as bool? ?? false,
    );
  }

  /// Deserialises a [DocumentTemplateApiModel] from the full JSON returned
  /// by the `/documenttemplate/{id}` endpoint.
  factory DocumentTemplateApiModel.fromJson(Map<String, dynamic> json) {
    final fileInfo =
        json['templateFileInfo'] as Map<String, dynamic>? ?? {};
    final docGroup = json['documentGroup'] as Map<String, dynamic>? ?? {};
    final rawTypes = fileInfo['recoversDataType'] as List<dynamic>? ?? [];
    final typeIds = rawTypes.map((e) {
      if (e is Map<String, dynamic>) return (e['id'] as num).toInt();
      return (e as num).toInt();
    }).toList();

    final rawSigs =
        fileInfo['placeSignatures'] as List<dynamic>? ?? [];
    final signatures = rawSigs
        .map((s) => _parsePlaceSignature(s as Map<String, dynamic>))
        .toList();

    return DocumentTemplateApiModel(
      id: json['id'] as String? ?? '',
      name: json['name'] as String? ?? '',
      description: json['description'] as String? ?? '',
      validityDurationInDays:
          (json['documentValidityDurationInDays'] as num?)?.toDouble() ?? 0,
      workloadInHours:
          (json['workloadInHours'] as num?)?.toDouble() ?? 0,
      usePreviousPeriod: json['usePreviousPeriod'] as bool? ?? false,
      acceptsSignature: json['acceptsSignature'] as bool? ?? false,
      bodyFileName: fileInfo['bodyFileName'] as String? ?? '',
      headerFileName: fileInfo['headerFileName'] as String? ?? '',
      footerFileName: fileInfo['footerFileName'] as String? ?? '',
      documentGroupId: docGroup['id'] as String? ?? '',
      documentGroupName: docGroup['name'] as String? ?? '',
      recoverDataTypeIds: typeIds,
      placeSignatures: signatures,
    );
  }

  static PlaceSignatureData _parsePlaceSignature(Map<String, dynamic> json) {
    final ts = json['typeSignature'] as Map<String, dynamic>? ?? {};
    return PlaceSignatureData(
      typeSignatureId: (ts['id'] ?? '').toString(),
      page: (json['page'] ?? '').toString(),
      positionBottom: (json['relativePositionBotton'] ?? '').toString(),
      positionLeft: (json['relativePositionLeft'] ?? '').toString(),
      sizeX: (json['relativeSizeX'] ?? '').toString(),
      sizeY: (json['relativeSizeY'] ?? '').toString(),
    );
  }

  /// Converts this DTO to the domain [DocumentTemplate] entity.
  DocumentTemplate toEntity() {
    return DocumentTemplate(
      id: id,
      name: name,
      description: description,
      validityInDays: validityDurationInDays > 0
          ? validityDurationInDays.toInt()
          : null,
      workload: workloadInHours > 0 ? workloadInHours.toInt() : null,
      usePreviousPeriod: usePreviousPeriod,
      acceptsSignature: acceptsSignature,
      bodyFileName: bodyFileName,
      headerFileName: headerFileName,
      footerFileName: footerFileName,
      documentGroupId: documentGroupId,
      documentGroupName: documentGroupName,
      recoverDataTypeIds: recoverDataTypeIds,
      placeSignatures: placeSignatures,
    );
  }

  /// Serialises this model to JSON without the [id] field (used for creates).
  Map<String, dynamic> toCreateJson() {
    final hasFileInfo =
        bodyFileName.isNotEmpty || headerFileName.isNotEmpty || footerFileName.isNotEmpty;
    return {
      'name': name,
      'description': description,
      'documentValidityDurationInDays': validityDurationInDays,
      'workloadInHours': workloadInHours,
      'usePreviousPeriod': usePreviousPeriod,
      'acceptsSignature': acceptsSignature,
      'templateFileInfo': hasFileInfo || recoverDataTypeIds.isNotEmpty
          ? {
              'bodyFileName': bodyFileName,
              'headerFileName': headerFileName,
              'footerFileName': footerFileName,
              'recoversDataType': recoverDataTypeIds,
            }
          : null,
      'placeSignatures': placeSignatures
          .map((s) => {
                'type': int.tryParse(s.typeSignatureId) ?? 0,
                'page': int.tryParse(s.page) ?? 0,
                'relativePositionBotton':
                    double.tryParse(s.positionBottom) ?? 0,
                'relativePositionLeft':
                    double.tryParse(s.positionLeft) ?? 0,
                'relativeSizeX': double.tryParse(s.sizeX) ?? 0,
                'relativeSizeY': double.tryParse(s.sizeY) ?? 0,
              })
          .toList(),
      'documentGroupId': documentGroupId,
    };
  }

  /// Serialises this model to JSON including the [id] field (used for updates).
  Map<String, dynamic> toJson() {
    final hasFileInfo =
        bodyFileName.isNotEmpty || headerFileName.isNotEmpty || footerFileName.isNotEmpty;
    return {
      'id': id,
      'name': name,
      'description': description,
      'documentValidityDurationInDays': validityDurationInDays,
      'workloadInHours': workloadInHours,
      'usePreviousPeriod': usePreviousPeriod,
      'acceptsSignature': acceptsSignature,
      'templateFileInfo': hasFileInfo || recoverDataTypeIds.isNotEmpty
          ? {
              'bodyFileName': bodyFileName,
              'headerFileName': headerFileName,
              'footerFileName': footerFileName,
              'recoversDataType': recoverDataTypeIds,
            }
          : null,
      'placeSignatures': placeSignatures
          .map((s) => {
                'type': int.tryParse(s.typeSignatureId) ?? 0,
                'page': int.tryParse(s.page) ?? 0,
                'relativePositionBotton':
                    double.tryParse(s.positionBottom) ?? 0,
                'relativePositionLeft':
                    double.tryParse(s.positionLeft) ?? 0,
                'relativeSizeX': double.tryParse(s.sizeX) ?? 0,
                'relativeSizeY': double.tryParse(s.sizeY) ?? 0,
              })
          .toList(),
      'documentGroupId': documentGroupId,
    };
  }
}
