part of 'require_document_bloc.dart';

class RequireDocumentState extends Equatable {
  final bool isSavingData;
  final bool isLoading;
  final bool isEditing;
  final AplicationException? exception;
  final String? snackMessage;
  final RequireDocument requireDocument;
  final List<AssociationType> associationTypes;
  final List<Association> associations;
  final List<DocumentTemplateSimple> documentTemplates;
  final List<Event> events;
  final List<Status> listStatus;

  const RequireDocumentState({
    this.isSavingData = false,
    this.isEditing = false,
    this.isLoading = false,
    this.exception,
    this.snackMessage,
    this.requireDocument = const RequireDocument.empty(),
    this.associationTypes = const [],
    this.associations = const [],
    this.documentTemplates = const [],
    this.events = const [],
    this.listStatus = const [],
  });

  RequireDocumentState copyWith({
    bool? isSavingData,
    bool? isEditing,
    bool? isLoading,
    AplicationException? exception,
    String? snackMessage,
    RequireDocument? requireDocument,
    List<AssociationType>? associationTypes,
    List<Association>? associations,
    List<DocumentTemplateSimple>? documentTemplates,
    List<Event>? events,
    List<Status>? listStatus,
  }) {
    return RequireDocumentState(
      isSavingData: isSavingData ?? this.isSavingData,
      isEditing: isEditing ?? this.isEditing,
      isLoading: isLoading ?? this.isLoading,
      exception: exception ?? this.exception,
      snackMessage: snackMessage ?? this.snackMessage,
      requireDocument: requireDocument ?? this.requireDocument,
      associationTypes: associationTypes ?? this.associationTypes,
      associations: associations ?? this.associations,
      documentTemplates: documentTemplates ?? this.documentTemplates,
      events: events ?? this.events,
      listStatus: listStatus ?? this.listStatus,
    );
  }

  @override
  List<Object?> get props => [
        isSavingData,
        isEditing,
        isLoading,
        exception,
        snackMessage,
        requireDocument,
        associationTypes.hashCode,
        associations.hashCode,
        documentTemplates.hashCode,
        events.hashCode,
        listStatus.hashCode,
      ];
}
