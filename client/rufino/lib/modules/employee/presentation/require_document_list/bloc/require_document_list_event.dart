part of 'require_document_list_bloc.dart';

sealed class RequireDocumentListEvent extends Equatable {
  const RequireDocumentListEvent();

  @override
  List<Object> get props => [];
}

class InitialEvent extends RequireDocumentListEvent {
  const InitialEvent();
}
