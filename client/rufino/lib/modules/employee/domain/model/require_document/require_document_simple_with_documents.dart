import 'package:equatable/equatable.dart';
import 'package:rufino/modules/employee/domain/model/document/document.dart';
import 'package:rufino/modules/employee/domain/model/document/document_status.dart';

class RequireDocumentSimpleWithDocuments extends Equatable {
  final String id;
  final String name;
  final String description;
  final DocumentStatus status;
  final List<Document> documents;

  const RequireDocumentSimpleWithDocuments(
      this.id, this.name, this.description, this.documents, this.status);

  factory RequireDocumentSimpleWithDocuments.fromJson(
      Map<String, dynamic> json) {
    return RequireDocumentSimpleWithDocuments(
      json['id'] as String,
      json['name'] as String,
      json['description'] as String,
      (json['documents'] as List<dynamic>)
          .map((el) => Document.fromJsonSimple(el))
          .toList(),
      DocumentStatus.fromJson(json['documentsStatus'] as Map<String, dynamic>),
    );
  }

  static List<RequireDocumentSimpleWithDocuments> fromListJson(
      List<dynamic> listJson) {
    return listJson
        .map((el) => RequireDocumentSimpleWithDocuments.fromJson(el))
        .toList();
  }

  @override
  List<Object?> get props =>
      [id, name, description, documents.hashCode, status];
}
