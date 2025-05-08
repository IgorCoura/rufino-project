import 'package:rufino/modules/employee/domain/model/base/model_base.dart';
import 'package:rufino/modules/employee/domain/model/document_template/description.dart';
import 'package:rufino/modules/employee/domain/model/document_template/document_template_simple.dart';
import 'package:rufino/modules/employee/domain/model/require_document/association.dart';
import 'package:rufino/modules/employee/domain/model/require_document/association_type.dart';
import 'package:rufino/modules/employee/domain/model/require_document/listen_event.dart';
import 'package:rufino/modules/employee/domain/model/require_document/name.dart';

class RequireDocument extends ModelBase {
  final String id;
  final String companyId;
  final Name name;
  final Description description;
  final Association association;
  final AssociationType associationType;
  final List<DocumentTemplateSimple> documentTemplates;
  final List<ListenEvent> listenEvents;

  const RequireDocument(
    this.id,
    this.companyId,
    this.name,
    this.description,
    this.association,
    this.associationType,
    this.documentTemplates,
    this.listenEvents, {
    super.isLoading = false,
    super.isLazyLoading = false,
  });

  const RequireDocument.empty({
    this.id = "",
    this.companyId = "",
    this.name = const Name.empty(),
    this.description = const Description.empty(),
    this.association = const Association.empty(),
    this.associationType = const AssociationType.empty(),
    this.documentTemplates = const [],
    this.listenEvents = const [],
    super.isLoading = false,
    super.isLazyLoading = false,
  });

  RequireDocument copyWith({
    String? id,
    String? companyId,
    Name? name,
    Description? description,
    Association? association,
    AssociationType? associationType,
    List<DocumentTemplateSimple>? documentTemplates,
    List<ListenEvent>? listenEvents,
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
        case const (Association):
          association = generic as Association;
          break;
        case const (AssociationType):
          associationType = generic as AssociationType;
          break;
      }
    }

    return RequireDocument(
      id ?? this.id,
      companyId ?? this.companyId,
      name ?? this.name,
      description ?? this.description,
      association ?? this.association,
      associationType ?? this.associationType,
      documentTemplates ?? this.documentTemplates,
      listenEvents ?? this.listenEvents,
      isLoading: isLoading ?? this.isLoading,
      isLazyLoading: isLazyLoading ?? this.isLazyLoading,
    );
  }

  factory RequireDocument.fromJson(Map<String, dynamic> json) {
    return RequireDocument(
      json['id'] as String,
      json['companyId'] as String,
      Name(json['name'] as String),
      Description(json['description'] as String),
      Association.fromJson(json['association']),
      AssociationType.fromJson(json['associationType'] as Map<String, dynamic>),
      DocumentTemplateSimple.fromListJson(json['documentsTemplates']),
      (json['listenEvents'] as List)
          .map((e) => ListenEvent.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }

  static List<RequireDocument> fromListJson(List<dynamic> jsonList) {
    return jsonList
        .map((json) => RequireDocument.fromJson(json as Map<String, dynamic>))
        .toList();
  }

  Map<String, dynamic> toJsonCreate() {
    return {
      'name': name.value,
      'description': description.value,
      'associationId': association.id,
      'associationType': associationType.toInt(),
      'documentTemplateIds': documentTemplates.map((e) => e.id).toList(),
      'listenEvents': listenEvents.map((e) => e.toJson()).toList(),
    };
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'name': name.value,
      'description': description.value,
      'associationId': association.id,
      'associationType': associationType.toInt(),
      'documentTemplateIds': documentTemplates.map((e) => e.id).toList(),
      'listenEvents': listenEvents.map((e) => e.toJson()).toList(),
    };
  }

  @override
  List<Object?> get props => [
        id,
        companyId,
        name,
        description,
        association,
        associationType,
        documentTemplates,
        listenEvents.hashCode,
        isLoading,
        isLazyLoading,
      ];
}
