import 'package:rufino/modules/employee/domain/model/archive_category/description.dart';
import 'package:rufino/modules/employee/domain/model/base/model_base.dart';
import 'package:rufino/modules/employee/domain/model/base/text_prop_base.dart';
import 'package:rufino/modules/employee/domain/model/document_template/directory_name.dart';
import 'package:rufino/modules/employee/domain/model/document_template/file_name.dart';
import 'package:rufino/modules/employee/domain/model/document_template/place_signature.dart';
import 'package:rufino/modules/employee/domain/model/document_template/recover_data_type.dart';
import 'package:rufino/modules/employee/domain/model/document_template/time_span.dart';
import 'package:rufino/modules/employee/domain/model/name.dart';

class DocumentTemplate extends ModelBase {
  final String id;
  final Name name;
  final Description description;
  final DirectoryName directoryName;
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
    this.directoryName,
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
    this.directoryName = const DirectoryName.empty(),
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
    DirectoryName? directoryName,
    BodyFileName? bodyFileName,
    HeaderFileName? headerFileName,
    FooterFileName? footerFileName,
    RecoverDataType? recoverDataType,
    DocumentValidityDuration? documentValidityDuration,
    Workload? workload,
    List<PlaceSignature>? placeSignatures,
    bool? isLoading,
    bool? isLazyLoading,
  }) {
    return DocumentTemplate(
      id ?? this.id,
      name ?? this.name,
      description ?? this.description,
      directoryName ?? this.directoryName,
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
      DirectoryName(json['directory']),
      BodyFileName(json['bodyFileName']),
      HeaderFileName(json['headerFileName']),
      FooterFileName(json['footerFileName']),
      RecoverDataType.fromJson(json['recoverDataType']),
      DocumentValidityDuration(json['documentValidityDuration']),
      Workload(json['workload']),
      PlaceSignature.fromListJson(json['placeSignatures']),
    );
  }

  static List<DocumentTemplate> fromListJson(List<dynamic> jsonList) {
    return jsonList
        .map((json) => DocumentTemplate.fromJson(json as Map<String, dynamic>))
        .toList();
  }

  @override
  List<Object?> get props => [
        id,
        name,
        description,
        directoryName,
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
        directoryName,
        bodyFileName,
        headerFileName,
        footerFileName,
        documentValidityDuration,
        workload,
      ];
}
