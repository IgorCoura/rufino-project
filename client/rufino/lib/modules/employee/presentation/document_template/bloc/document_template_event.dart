part of 'document_template_bloc.dart';

sealed class DocumentTemplateEvent extends Equatable {
  const DocumentTemplateEvent();

  @override
  List<Object> get props => [];
}

final class InitialEvent extends DocumentTemplateEvent {
  const InitialEvent();
}

final class SnackMessageWasShow extends DocumentTemplateEvent {
  const SnackMessageWasShow();
}
