part of 'require_document_bloc.dart';

sealed class RequireDocumentEvent extends Equatable {
  const RequireDocumentEvent();

  @override
  List<Object> get props => [];
}

class InitialEvent extends RequireDocumentEvent {
  final String requireDocumentId;
  const InitialEvent(this.requireDocumentId);
  @override
  List<Object> get props => [requireDocumentId];
}

class SnackMessageWasShow extends RequireDocumentEvent {
  const SnackMessageWasShow();
}

class ChangeFieldValueEvent extends RequireDocumentEvent {
  final Object changeValue;

  const ChangeFieldValueEvent(this.changeValue);
  @override
  List<Object> get props => [changeValue];
}

class SaveEvent extends RequireDocumentEvent {
  const SaveEvent();
}

class CancelEditEvent extends RequireDocumentEvent {
  const CancelEditEvent();
}

class EditEvent extends RequireDocumentEvent {
  const EditEvent();
}

class AssociationTypeSelectedEvent extends RequireDocumentEvent {
  final Object associationType;

  const AssociationTypeSelectedEvent(this.associationType);
  @override
  List<Object> get props => [associationType];
}

class AddDocumentTemplateEvent extends RequireDocumentEvent {
  final DocumentTemplateSimple documentTemplate;

  const AddDocumentTemplateEvent(this.documentTemplate);
  @override
  List<Object> get props => [documentTemplate];
}

class RemoveDocumentTemplateEvent extends RequireDocumentEvent {
  final DocumentTemplateSimple documentTemplate;

  const RemoveDocumentTemplateEvent(this.documentTemplate);
  @override
  List<Object> get props => [documentTemplate];
}

class AddEventEvent extends RequireDocumentEvent {
  final Event event;

  const AddEventEvent(this.event);
  @override
  List<Object> get props => [event];
}

class RemoveEventEvent extends RequireDocumentEvent {
  final Event event;

  const RemoveEventEvent(this.event);
  @override
  List<Object> get props => [event];
}

class AddStatusEvent extends RequireDocumentEvent {
  final Status status;
  final Event event;
  const AddStatusEvent(this.status, this.event);
  @override
  List<Object> get props => [status, event];
}

class RemoveStatusEvent extends RequireDocumentEvent {
  final Status status;
  final Event event;

  const RemoveStatusEvent(this.status, this.event);
  @override
  List<Object> get props => [status, event];
}
