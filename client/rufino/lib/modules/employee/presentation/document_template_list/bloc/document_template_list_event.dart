part of 'document_template_list_bloc.dart';

sealed class DocumentTemplateListEvent extends Equatable {
  const DocumentTemplateListEvent();

  @override
  List<Object> get props => [];
}

class InitialEvent extends DocumentTemplateListEvent {
  const InitialEvent();
}
