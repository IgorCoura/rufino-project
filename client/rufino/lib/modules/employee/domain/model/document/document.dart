import 'package:equatable/equatable.dart';
import 'package:rufino/modules/employee/domain/model/document/document_status.dart';
import 'package:rufino/modules/employee/domain/model/document/document_unit.dart';

class Document extends Equatable {
  final String id;
  final String name;
  final String description;
  final String employeeId;
  final String companyId;
  final String requiredDocumentId;
  final String documentTemplateId;
  final bool usePreviousPeriod;
  final bool isSignable;
  final bool canGenerateDocument;
  final DocumentStatus status;
  final List<DocumentUnit> documentsUnits;
  final String createAt;
  final String updateAt;

  const Document(
      this.id,
      this.name,
      this.description,
      this.employeeId,
      this.companyId,
      this.requiredDocumentId,
      this.documentTemplateId,
      this.usePreviousPeriod,
      this.isSignable,
      this.canGenerateDocument,
      this.status,
      this.documentsUnits,
      this.createAt,
      this.updateAt);

  const Document.empty()
      : id = "",
        name = "",
        description = "",
        employeeId = "",
        companyId = "",
        requiredDocumentId = "",
        documentTemplateId = "",
        usePreviousPeriod = false,
        isSignable = false,
        canGenerateDocument = false,
        status = const DocumentStatus.empty(),
        documentsUnits = const [],
        createAt = "",
        updateAt = "";

  factory Document.fromJsonSimple(Map<String, dynamic> json) {
    return Document(
      json['id'] as String,
      json['name'] as String,
      json['description'] as String,
      json['employeeId'] as String? ?? "",
      json['companyId'] as String? ?? "",
      json['requiredDocumentId'] as String? ?? "",
      json['documentTemplateId'] as String? ?? "",
      json['usePreviousPeriod'] as bool? ?? false,
      json['isSignable'] as bool? ?? false,
      json['canGenerateDocument'] as bool? ?? false,
      DocumentStatus.fromJson(json['status'] as Map<String, dynamic>),
      List<DocumentUnit>.empty(),
      json['createAt'] as String,
      json['updateAt'] as String,
    );
  }

  factory Document.fromJson(Map<String, dynamic> json) {
    return Document(
      json['id'] as String,
      json['name'] as String,
      json['description'] as String,
      json['employeeId'] as String,
      json['companyId'] as String,
      json['requiredDocumentId'] as String,
      json['documentTemplateId'] as String,
      json['usePreviousPeriod'] as bool? ?? false,
      json['isSignable'] as bool? ?? false,
      json['canGenerateDocument'] as bool? ?? false,
      DocumentStatus.fromJson(json['status'] as Map<String, dynamic>),
      (json['documentsUnits'] as List<dynamic>)
          .map((e) => DocumentUnit.fromJson(e as Map<String, dynamic>))
          .toList(),
      json['createAt'] as String,
      json['updateAt'] as String,
    );
  }

  static List<Document> fromJsonListSimple(List<dynamic> jsonList) {
    return jsonList
        .map((json) => Document.fromJsonSimple(json as Map<String, dynamic>))
        .toList();
  }

  static List<Document> fromJsonList(List<dynamic> jsonList) {
    return jsonList
        .map((json) => Document.fromJson(json as Map<String, dynamic>))
        .toList();
  }

  @override
  List<Object?> get props => [
        id,
        name,
        description,
        employeeId,
        companyId,
        requiredDocumentId,
        documentTemplateId,
        usePreviousPeriod,
        isSignable,
        canGenerateDocument,
        status,
        documentsUnits.hashCode,
        createAt,
        updateAt,
      ];
}
