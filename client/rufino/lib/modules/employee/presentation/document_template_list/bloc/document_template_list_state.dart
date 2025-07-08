part of 'document_template_list_bloc.dart';

class DocumentTemplateListState extends Equatable {
  final List<DocumentTemplateSimple> documentTemplateList;
  final bool isLoading;
  final AplicationException? exception;

  const DocumentTemplateListState(
      {this.documentTemplateList = const [],
      this.isLoading = false,
      this.exception});

  DocumentTemplateListState copyWith({
    List<DocumentTemplateSimple>? documentTemplateList,
    bool? isLoading,
    AplicationException? exception,
  }) {
    return DocumentTemplateListState(
      documentTemplateList: documentTemplateList ?? this.documentTemplateList,
      isLoading: isLoading ?? this.isLoading,
      exception: exception ?? this.exception,
    );
  }

  @override
  List<Object?> get props =>
      [documentTemplateList.hashCode, isLoading, exception];
}
