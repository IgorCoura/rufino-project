import 'package:rufino/modules/employee/domain/model/archive_category/description.dart';
import 'package:rufino/modules/employee/domain/model/base/model_base.dart';
import 'package:rufino/modules/employee/domain/model/base/text_prop_base.dart';
import 'package:rufino/modules/employee/domain/model/document_template/document_validity_duration.dart';
import 'package:rufino/modules/employee/domain/model/document_template/file_name.dart';
import 'package:rufino/modules/employee/domain/model/document_template/place_signature.dart';
import 'package:rufino/modules/employee/domain/model/document_template/recover_data_type.dart';
import 'package:rufino/modules/employee/domain/model/document_template/workload.dart';
import 'package:rufino/modules/employee/domain/model/document_template/name.dart';

class DocumentTemplate extends ModelBase {
  final String id;
  final Name name;
  final Description description;
  final BodyFileName bodyFileName;
  final HeaderFileName headerFileName;
  final FooterFileName footerFileName;
  final RecoverDataType recoverDataType;
  final DocumentValidityDuration documentValidityDuration;
  final Workload workload;
  final List<PlaceSignature> placeSignatures;

  const DocumentTemplate(
    this.id,
    this.name,
    this.description,
    this.bodyFileName,
    this.headerFileName,
    this.footerFileName,
    this.recoverDataType,
    this.documentValidityDuration,
    this.workload,
    this.placeSignatures, {
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
    this.recoverDataType = const RecoverDataType.empty(),
    this.documentValidityDuration = const DocumentValidityDuration.empty(),
    this.workload = const Workload.empty(),
    this.placeSignatures = const [],
    super.isLoading = true,
    super.isLazyLoading = true,
  });

  DocumentTemplate copyWith({
    String? id,
    Name? name,
    Description? description,
    BodyFileName? bodyFileName,
    HeaderFileName? headerFileName,
    FooterFileName? footerFileName,
    RecoverDataType? recoverDataType,
    DocumentValidityDuration? documentValidityDuration,
    Workload? workload,
    List<PlaceSignature>? placeSignatures,
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
        case const (RecoverDataType):
          recoverDataType = generic as RecoverDataType;
          break;
        case const (DocumentValidityDuration):
          documentValidityDuration = generic as DocumentValidityDuration;
          break;
        case const (Workload):
          workload = generic as Workload;
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
      recoverDataType ?? this.recoverDataType,
      documentValidityDuration ?? this.documentValidityDuration,
      workload ?? this.workload,
      placeSignatures ?? this.placeSignatures,
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
      RecoverDataType.fromJson(json['templateFileInfo']['recoverDataType']),
      DocumentValidityDuration.createFormatted(
          json['documentValidityDurationInDays'].toString()),
      Workload.createFormatted(json['workloadInHours'].toString()),
      PlaceSignature.fromListJson(json['templateFileInfo']['placeSignatures']),
    );
  }

  static List<DocumentTemplate> fromListJson(List<dynamic> jsonList) {
    return jsonList
        .map((json) => DocumentTemplate.fromJson(json as Map<String, dynamic>))
        .toList();
  }

  Map<String, dynamic> toJsonCreated() {
    return {
      'name': name.value,
      'description': description.value,
      'documentValidityDurationInDays': documentValidityDuration.toDouble(),
      'workloadInHours': workload.toDouble(),
      'templateFileInfo': {
        'bodyFileName': bodyFileName.value,
        'headerFileName': headerFileName.value,
        'footerFileName': footerFileName.value,
        'recoverDataType': recoverDataType.toInt(),
        'placeSignatures': placeSignatures.map((e) => e.toJson()).toList(),
      },
    };
  }

  Map<String, dynamic> toJson() {
    return {
      'Id': id,
      'name': name.value,
      'description': description.value,
      'documentValidityDurationInDays': documentValidityDuration.toDouble(),
      'workloadInHours': workload.toDouble(),
      'templateFileInfo': {
        'bodyFileName': bodyFileName.value,
        'headerFileName': headerFileName.value,
        'footerFileName': footerFileName.value,
        'recoverDataType': recoverDataType.toInt(),
        'placeSignatures': placeSignatures.map((e) => e.toJson()).toList(),
      },
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
        recoverDataType,
        documentValidityDuration,
        workload,
        placeSignatures.hashCode,
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
