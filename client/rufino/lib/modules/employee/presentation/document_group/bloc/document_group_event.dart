part of 'document_group_bloc.dart';

sealed class DocumentGroupEvent extends Equatable {
  const DocumentGroupEvent();

  @override
  List<Object> get props => [];
}

class LoadDocumentGroups extends DocumentGroupEvent {}

class CreateDocumentGroup extends DocumentGroupEvent {
  final String name;
  final String description;

  const CreateDocumentGroup(this.name, this.description);

  @override
  List<Object> get props => [name, description];
}

class UpdateDocumentGroup extends DocumentGroupEvent {
  final String id;
  final String name;
  final String description;

  const UpdateDocumentGroup(this.id, this.name, this.description);

  @override
  List<Object> get props => [id, name, description];
}
