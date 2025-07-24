import 'package:equatable/equatable.dart';
import 'package:rufino/modules/employee/domain/model/document/document.dart';
import 'package:rufino/modules/employee/domain/model/document/document_status.dart';

class DocumentGroupWithDocuments extends Equatable {
  final String id;
  final String name;
  final String description;
  final DocumentStatus status;
  final List<Document> documents;

  const DocumentGroupWithDocuments(
      this.id, this.name, this.description, this.documents, this.status);

  factory DocumentGroupWithDocuments.fromJson(Map<String, dynamic> json) {
    return DocumentGroupWithDocuments(
      json['id'] as String,
      json['name'] as String,
      json['description'] as String,
      (json['documents'] as List<dynamic>)
          .map((el) => Document.fromJsonSimple(el))
          .toList(),
      DocumentStatus.fromJson(json['documentsStatus'] as Map<String, dynamic>),
    );
  }

  static List<DocumentGroupWithDocuments> fromListJson(List<dynamic> listJson) {
    return listJson
        .map((el) => DocumentGroupWithDocuments.fromJson(el))
        .toList();
  }

  @override
  List<Object?> get props =>
      [id, name, description, documents.hashCode, status];
}
