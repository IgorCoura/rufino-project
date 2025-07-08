part of 'documents_component_bloc.dart';

class DocumentsComponentState extends Equatable {
  final String companyId;
  final String employeeId;
  final bool isLoading;
  final bool isLazyLoading;
  final bool isSavingData;
  final bool isExpanded;
  final String? snackMessage;
  final AplicationException? exception;
  final List<RequireDocumentSimpleWithDocuments> reqDocuments;

  const DocumentsComponentState(
      {this.companyId = "",
      this.employeeId = "",
      this.isLoading = false,
      this.isLazyLoading = false,
      this.isSavingData = false,
      this.isExpanded = false,
      this.snackMessage = "",
      this.exception,
      this.reqDocuments = const []});

  DocumentsComponentState copyWith({
    String? companyId,
    String? employeeId,
    bool? isLoading,
    bool? isLazyLoading,
    bool? isSavingData,
    bool? isExpanded,
    String? snackMessage,
    AplicationException? exception,
    List<RequireDocumentSimpleWithDocuments>? reqDocuments,
  }) {
    return DocumentsComponentState(
      companyId: companyId ?? this.companyId,
      employeeId: employeeId ?? this.employeeId,
      isLoading: isLoading ?? this.isLoading,
      isLazyLoading: isLazyLoading ?? this.isLazyLoading,
      isSavingData: isSavingData ?? this.isSavingData,
      isExpanded: isExpanded ?? this.isExpanded,
      snackMessage: snackMessage ?? this.snackMessage,
      exception: exception ?? this.exception,
      reqDocuments: reqDocuments ?? this.reqDocuments,
    );
  }

  @override
  List<Object?> get props => [
        companyId,
        employeeId,
        isLoading,
        isLazyLoading,
        isSavingData,
        isExpanded,
        snackMessage,
        exception,
        reqDocuments.hashCode,
      ];
}
