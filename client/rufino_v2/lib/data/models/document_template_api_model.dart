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
    this.policies,
  });

  final String id;
  final String name;
  final String description;

  /// Validity in days, or null when the template has no expiration rule.
  ///
  /// Null and zero are not interchangeable: the API reads a present value as
  /// "rule active", so a template without expiration must send null.
  ///
  /// This mirrors [policies] on writes — it is the legacy shape, kept so an API
  /// that predates the policies block still gets the rule.
  final double? validityDurationInDays;

  /// Workload in hours, or null when the template has no workload rule.
  ///
  /// Mirrors [policies], same as [validityDurationInDays].
  final double? workloadInHours;

  /// The rule set, or null when the API did not send the `policies` block.
  ///
  /// Null means "the server never mentioned policies" — the rules are then
  /// derived from the legacy fields. It does not mean "no rules"; an empty
  /// [TemplatePolicies] means that.
  final TemplatePolicies? policies;
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
      validityDurationInDays: null,
      workloadInHours: null,
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
          (json['documentValidityDurationInDays'] as num?)?.toDouble(),
      workloadInHours: (json['workloadInHours'] as num?)?.toDouble(),
      usePreviousPeriod: json['usePreviousPeriod'] as bool? ?? false,
      acceptsSignature: json['acceptsSignature'] as bool? ?? false,
      bodyFileName: fileInfo['bodyFileName'] as String? ?? '',
      headerFileName: fileInfo['headerFileName'] as String? ?? '',
      footerFileName: fileInfo['footerFileName'] as String? ?? '',
      documentGroupId: docGroup['id'] as String? ?? '',
      documentGroupName: docGroup['name'] as String? ?? '',
      recoverDataTypeIds: typeIds,
      placeSignatures: signatures,
      policies: _parsePolicies(json['policies'] as Map<String, dynamic>?),
    );
  }

  static TemplatePolicies? _parsePolicies(Map<String, dynamic>? json) {
    if (json == null) return null;

    final expiration = json['expiration'] as Map<String, dynamic>?;
    final workload = json['workload'] as Map<String, dynamic>?;

    return TemplatePolicies(
      expiration: expiration == null
          ? null
          : ExpirationRule(
              durationInDays:
                  (expiration['durationInDays'] as num?)?.toInt() ?? 0),
      workload: workload == null
          ? null
          : WorkloadRule(hours: (workload['hours'] as num?)?.toInt() ?? 0),
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

  /// Returns [value] as an int, or null when it is absent or non-positive.
  ///
  /// Legacy templates persisted a zeroed duration instead of null, and the API
  /// still echoes that zero back. Both mean "no rule".
  static int? _positiveOrNull(double? value) =>
      value != null && value > 0 ? value.toInt() : null;

  /// The rule set for this template, falling back to the legacy fields.
  ///
  /// An API that predates the policies block sends only the legacy fields; the
  /// rules are then whatever those fields describe.
  TemplatePolicies _resolvePolicies() {
    if (policies != null) return policies!;

    final days = _positiveOrNull(validityDurationInDays);
    final hours = _positiveOrNull(workloadInHours);
    return TemplatePolicies(
      expiration: days == null ? null : ExpirationRule(durationInDays: days),
      workload: hours == null ? null : WorkloadRule(hours: hours),
    );
  }

  /// Converts this DTO to the domain [DocumentTemplate] entity.
  DocumentTemplate toEntity() {
    return DocumentTemplate(
      id: id,
      name: name,
      description: description,
      policies: _resolvePolicies(),
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

  /// Serialises [policies] to the API's `policies` block.
  ///
  /// Returns null when there is no rule set to send, which tells the API to
  /// fall back to the legacy fields. A non-null block wins over them.
  Map<String, dynamic>? _policiesToJson() {
    final rules = policies;
    if (rules == null) return null;

    return {
      'expiration': rules.expiration == null
          ? null
          : {'durationInDays': rules.expiration!.durationInDays},
      'workload':
          rules.workload == null ? null : {'hours': rules.workload!.hours},
    };
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
      'policies': _policiesToJson(),
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
      'policies': _policiesToJson(),
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
