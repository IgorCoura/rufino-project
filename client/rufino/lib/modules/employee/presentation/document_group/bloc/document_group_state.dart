part of 'document_group_bloc.dart';

class DocumentGroupState extends Equatable {
  final bool isSavingData;
  final bool isLoading;
  final AplicationException? exception;
  final String? snackMessage;
  final List<DocumentGroup> documentGroups;

  const DocumentGroupState({
    this.isSavingData = false,
    this.isLoading = false,
    this.exception,
    this.snackMessage,
    this.documentGroups = const [],
  });

  DocumentGroupState copyWith({
    bool? isSavingData,
    bool? isLoading,
    AplicationException? exception,
    String? snackMessage,
    List<DocumentGroup>? documentGroups,
  }) {
    return DocumentGroupState(
      isSavingData: isSavingData ?? this.isSavingData,
      isLoading: isLoading ?? this.isLoading,
      exception: exception ?? this.exception,
      snackMessage: snackMessage ?? this.snackMessage,
      documentGroups: documentGroups ?? this.documentGroups,
    );
  }

  @override
  List<Object?> get props => [
        isSavingData,
        isLoading,
        exception,
        snackMessage,
        documentGroups.hashCode,
      ];
}
