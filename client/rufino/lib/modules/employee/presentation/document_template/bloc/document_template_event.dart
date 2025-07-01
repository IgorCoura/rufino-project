part of 'document_template_bloc.dart';

sealed class DocumentTemplateEvent extends Equatable {
  const DocumentTemplateEvent();

  @override
  List<Object> get props => [];
}

final class InitialEvent extends DocumentTemplateEvent {
  final String documentTemplateId;
  const InitialEvent(this.documentTemplateId);
  @override
  List<Object> get props => [documentTemplateId];
}

final class EditEvent extends DocumentTemplateEvent {
  const EditEvent();
}

final class SnackMessageWasShow extends DocumentTemplateEvent {
  const SnackMessageWasShow();
}

final class ChangeFieldValueEvent extends DocumentTemplateEvent {
  final Object changeValue;

  const ChangeFieldValueEvent(this.changeValue);
  @override
  List<Object> get props => [changeValue];
}

final class ChangePlaceSignatureValuesEvent extends DocumentTemplateEvent {
  final Object changeValue;
  final int index;
  const ChangePlaceSignatureValuesEvent(this.changeValue, this.index);

  @override
  List<Object> get props => [changeValue, index];
}

final class NewPlaceSignatureEvent extends DocumentTemplateEvent {
  const NewPlaceSignatureEvent();
  @override
  List<Object> get props => [];
}

final class CancelEditEvent extends DocumentTemplateEvent {
  const CancelEditEvent();
}

final class RemovePlaceSignatureEvent extends DocumentTemplateEvent {
  final int index;
  const RemovePlaceSignatureEvent(this.index);
  @override
  List<Object> get props => [index];
}

final class SaveEvent extends DocumentTemplateEvent {
  const SaveEvent();
}

final class SendFileEvent extends DocumentTemplateEvent {
  const SendFileEvent();
}

final class DownLoadFileEvent extends DocumentTemplateEvent {
  const DownLoadFileEvent();
}

final class LoadDataModelEvent extends DocumentTemplateEvent {
  final bool isExpanded;
  const LoadDataModelEvent(this.isExpanded);

  @override
  List<Object> get props => [isExpanded];
}
