import 'package:rufino/modules/employee/domain/model/archive_category/description.dart';
import 'package:rufino/modules/employee/domain/model/base/model_base.dart';
import 'package:rufino/modules/employee/domain/model/base/text_prop_base.dart';
import 'package:rufino/modules/employee/domain/model/document_group/document_group.dart';
import 'package:rufino/modules/employee/domain/model/document_template/document_validity_duration.dart';
import 'package:rufino/modules/employee/domain/model/document_template/file_name.dart';
import 'package:rufino/modules/employee/domain/model/document_template/place_signature.dart';
import 'package:rufino/modules/employee/domain/model/document_template/recover_data_type.dart';
import 'package:rufino/modules/employee/domain/model/document_template/recovers_data_type.dart';
import 'package:rufino/modules/employee/domain/model/document_template/workload.dart';
import 'package:rufino/modules/employee/domain/model/document_template/name.dart';

class DocumentTemplate extends ModelBase {
  final String id;
  final Name name;
  final Description description;
  final BodyFileName bodyFileName;
  final HeaderFileName headerFileName;
  final FooterFileName footerFileName;
  final RecoversDataType recoversDataType;
  final DocumentValidityDuration documentValidityDuration;
  final Workload workload;
  final List<PlaceSignature> placeSignatures;
  final DocumentGroup documentGroup;
  final bool usePreviousPeriod;
  final bool acceptsSignature;

  const DocumentTemplate(
    this.id,
    this.name,
    this.description,
    this.bodyFileName,
    this.headerFileName,
    this.footerFileName,
    this.recoversDataType,
    this.documentValidityDuration,
    this.workload,
    this.placeSignatures,
    this.documentGroup, {
    this.usePreviousPeriod = false,
    this.acceptsSignature = false,
    super.isLoading = false,
    super.isLazyLoading = false,
  });
  const DocumentTemplate.empty({
    this.id = "",
    this.name = const Name.empty(),
    this.description = const Description.empty(),
    this.bodyFileName = const BodyFileName.empty(),
    this.headerFileName = const HeaderFileName.empty(),
    this.footerFileName = const FooterFileName.empty(),
    this.recoversDataType = const RecoversDataType.empty(),
    this.documentValidityDuration = const DocumentValidityDuration.empty(),
    this.workload = const Workload.empty(),
    this.placeSignatures = const [],
    this.documentGroup = const DocumentGroup.empty(),
    this.usePreviousPeriod = false,
    this.acceptsSignature = false,
    super.isLoading = true,
    super.isLazyLoading = true,
  });

  bool get isInvalidTemplateFileInfo {
    return isValidTemplateFileInfo == false;
  }

  bool get isValidTemplateFileInfo {
    var isNotEmpty = bodyFileName.isNotEmpty &&
        headerFileName.isNotEmpty &&
        footerFileName.isNotEmpty &&
        recoversDataType.isNotEmpty;

    var isEmpty = bodyFileName.isEmpty &&
        headerFileName.isEmpty &&
        footerFileName.isEmpty &&
        recoversDataType.isEmpty;
    return isNotEmpty || isEmpty;
  }

  bool get isEmptyTemplateFileInfo {
    return bodyFileName.isEmpty &&
        headerFileName.isEmpty &&
        footerFileName.isEmpty &&
        recoversDataType.isEmpty;
  }

  DocumentTemplate copyWith({
    String? id,
    Name? name,
    Description? description,
    BodyFileName? bodyFileName,
    HeaderFileName? headerFileName,
    FooterFileName? footerFileName,
    RecoversDataType? recoversDataType,
    DocumentValidityDuration? documentValidityDuration,
    Workload? workload,
    List<PlaceSignature>? placeSignatures,
    DocumentGroup? documentGroup,
    bool? usePreviousPeriod,
    bool? acceptsSignature,
    bool? isLoading,
    bool? isLazyLoading,
    Object? generic,
  }) {
    if (generic != null) {
      switch (generic.runtimeType) {
        case const (Name):
          name = generic as Name;
          break;
        case const (Description):
          description = generic as Description;
          break;

        case const (BodyFileName):
          bodyFileName = generic as BodyFileName;
          break;
        case const (HeaderFileName):
          headerFileName = generic as HeaderFileName;
          break;
        case const (FooterFileName):
          footerFileName = generic as FooterFileName;
          break;
        case const (RecoversDataType):
          recoversDataType = generic as RecoversDataType;
          break;
        case const (DocumentValidityDuration):
          documentValidityDuration = generic as DocumentValidityDuration;
          break;
        case const (Workload):
          workload = generic as Workload;
          break;
        case const (DocumentGroup):
          documentGroup = generic as DocumentGroup;
          break;
        default:
          break;
      }
    }

    return DocumentTemplate(
      id ?? this.id,
      name ?? this.name,
      description ?? this.description,
      bodyFileName ?? this.bodyFileName,
      headerFileName ?? this.headerFileName,
      footerFileName ?? this.footerFileName,
      recoversDataType ?? this.recoversDataType,
      documentValidityDuration ?? this.documentValidityDuration,
      workload ?? this.workload,
      placeSignatures ?? this.placeSignatures,
      documentGroup ?? this.documentGroup,
      usePreviousPeriod: usePreviousPeriod ?? this.usePreviousPeriod,
      acceptsSignature: acceptsSignature ?? this.acceptsSignature,
      isLoading: isLoading ?? this.isLoading,
      isLazyLoading: isLazyLoading ?? this.isLazyLoading,
    );
  }

  factory DocumentTemplate.fromJson(Map<String, dynamic> json) {
    return DocumentTemplate(
      json['id'],
      Name(json['name']),
      Description(json['description']),
      BodyFileName(json['templateFileInfo']['bodyFileName']),
      HeaderFileName(json['templateFileInfo']['headerFileName']),
      FooterFileName(json['templateFileInfo']['footerFileName']),
      RecoversDataType.fromJson(json['templateFileInfo']),
      DocumentValidityDuration.createFormatted(
          json['documentValidityDurationInDays'].toString()),
      Workload.createFormatted(json['workloadInHours'].toString()),
      PlaceSignature.fromListJson(json['templateFileInfo']['placeSignatures']),
      DocumentGroup.fromJson(json['documentGroup']),
      usePreviousPeriod: json['usePreviousPeriod'] as bool? ?? false,
      acceptsSignature: json['acceptsSignature'] as bool? ?? false,
    );
  }

  static List<DocumentTemplate> fromListJson(List<dynamic> jsonList) {
    return jsonList
        .map((json) => DocumentTemplate.fromJson(json as Map<String, dynamic>))
        .toList();
  }

  Map<String, dynamic> toJsonCreated() {
    Map<String, dynamic>? templateFileInfo = isEmptyTemplateFileInfo
        ? null
        : {
            'bodyFileName': bodyFileName.value,
            'headerFileName': headerFileName.value,
            'footerFileName': footerFileName.value,
            'recoversDataType': recoversDataType.toJson(),
          };

    return {
      'name': name.value,
      'description': description.value,
      'documentValidityDurationInDays': documentValidityDuration.toDouble(),
      'workloadInHours': workload.toDouble(),
      'templateFileInfo': templateFileInfo,
      'placeSignatures': placeSignatures.map((e) => e.toJson()).toList(),
      'documentGroupId': documentGroup.id,
      'usePreviousPeriod': usePreviousPeriod,
      'acceptsSignature': acceptsSignature,
    };
  }

  Map<String, dynamic> toJson() {
    Map<String, dynamic>? templateFileInfo = isEmptyTemplateFileInfo
        ? null
        : {
            'bodyFileName': bodyFileName.value,
            'headerFileName': headerFileName.value,
            'footerFileName': footerFileName.value,
            'recoversDataType': recoversDataType.toJson(),
          };

    return {
      'Id': id,
      'name': name.value,
      'description': description.value,
      'documentValidityDurationInDays': documentValidityDuration.toDouble(),
      'workloadInHours': workload.toDouble(),
      'templateFileInfo': templateFileInfo,
      'placeSignatures': placeSignatures.map((e) => e.toJson()).toList(),
      'documentGroupId': documentGroup.id,
      'usePreviousPeriod': usePreviousPeriod,
      'acceptsSignature': acceptsSignature,
    };
  }

  @override
  List<Object?> get props => [
        id,
        name,
        description,
        bodyFileName,
        headerFileName,
        footerFileName,
        recoversDataType.hashCode,
        documentValidityDuration,
        workload,
        placeSignatures.hashCode,
        documentGroup,
        usePreviousPeriod,
        acceptsSignature,
        isLoading,
        isLazyLoading,
      ];

  @override
  List<TextPropBase> get textProps => [
        name,
        description,
        bodyFileName,
        headerFileName,
        footerFileName,
        documentValidityDuration,
        workload,
      ];
}
