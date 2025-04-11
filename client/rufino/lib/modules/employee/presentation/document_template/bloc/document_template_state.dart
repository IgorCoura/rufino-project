part of 'document_template_bloc.dart';

class DocumentTemplateState extends Equatable {
  final bool isSavingData;
  final bool isLoading;
  final AplicationException? exception;
  final String? snackMessage;
  final List<DocumentTemplate> documentTemplates;

  const DocumentTemplateState({
    this.isLoading = false,
    this.isSavingData = false,
    this.exception,
    this.snackMessage,
    this.documentTemplates = const [],
  });

  DocumentTemplateState copyWith({
    bool? isLoading,
    bool? isSavingData,
    AplicationException? exception,
    String? snackMessage,
    List<DocumentTemplate>? documentTemplates,
  }) {
    return DocumentTemplateState(
      isLoading: isLoading ?? this.isLoading,
      isSavingData: isSavingData ?? this.isSavingData,
      exception: exception ?? this.exception,
      snackMessage: snackMessage ?? this.snackMessage,
      documentTemplates: documentTemplates ?? this.documentTemplates,
    );
  }

  @override
  List<Object?> get props => [
        isSavingData,
        isLoading,
        exception,
        snackMessage,
        documentTemplates,
      ];
}
