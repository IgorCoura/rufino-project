part of 'documents_component_bloc.dart';

class DocumentUnitPagination extends Equatable {
  final int pageNumber;
  final int pageSize;
  final int? statusId;

  const DocumentUnitPagination({
    this.pageNumber = 1,
    this.pageSize = 10,
    this.statusId,
  });

  DocumentUnitPagination copyWith({
    int? pageNumber,
    int? pageSize,
    int? statusId,
    bool clearStatusId = false,
  }) {
    return DocumentUnitPagination(
      pageNumber: pageNumber ?? this.pageNumber,
      pageSize: pageSize ?? this.pageSize,
      statusId: clearStatusId ? null : (statusId ?? this.statusId),
    );
  }

  @override
  List<Object?> get props => [pageNumber, pageSize, statusId];
}

/// Represents a selected DocumentUnit for range operations.
class SelectedDocumentUnit extends Equatable {
  final String documentId;
  final String documentUnitId;
  final String documentName;
  final String documentUnitDate;
  final bool canGenerate;
  final bool hasFile;

  const SelectedDocumentUnit({
    required this.documentId,
    required this.documentUnitId,
    required this.documentName,
    required this.documentUnitDate,
    required this.canGenerate,
    required this.hasFile,
  });

  @override
  List<Object?> get props => [
        documentId,
        documentUnitId,
        documentName,
        documentUnitDate,
        canGenerate,
        hasFile
      ];
}

class DocumentsComponentState extends Equatable {
  final String companyId;
  final String employeeId;
  final bool isLoading;
  final bool isLazyLoading;
  final bool isSavingData;
  final bool isExpanded;
  final String? snackMessage;
  final AplicationException? exception;
  final List<DocumentGroupWithDocuments> reqDocuments;
  final Map<String, DocumentUnitPagination> paginationMap;
  final bool isSelectingRange;
  final List<SelectedDocumentUnit> selectedDocumentUnits;

  const DocumentsComponentState({
    this.companyId = "",
    this.employeeId = "",
    this.isLoading = false,
    this.isLazyLoading = false,
    this.isSavingData = false,
    this.isExpanded = false,
    this.snackMessage = "",
    this.exception,
    this.reqDocuments = const [],
    this.paginationMap = const {},
    this.isSelectingRange = false,
    this.selectedDocumentUnits = const [],
  });

  DocumentUnitPagination getPagination(String documentId) {
    return paginationMap[documentId] ?? const DocumentUnitPagination();
  }

  bool isDocumentUnitSelected(String documentUnitId) {
    return selectedDocumentUnits
        .any((item) => item.documentUnitId == documentUnitId);
  }

  DocumentsComponentState copyWith({
    String? companyId,
    String? employeeId,
    bool? isLoading,
    bool? isLazyLoading,
    bool? isSavingData,
    bool? isExpanded,
    String? snackMessage,
    AplicationException? exception,
    List<DocumentGroupWithDocuments>? reqDocuments,
    Map<String, DocumentUnitPagination>? paginationMap,
    bool? isSelectingRange,
    List<SelectedDocumentUnit>? selectedDocumentUnits,
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
      paginationMap: paginationMap ?? this.paginationMap,
      isSelectingRange: isSelectingRange ?? this.isSelectingRange,
      selectedDocumentUnits:
          selectedDocumentUnits ?? this.selectedDocumentUnits,
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
        paginationMap,
        isSelectingRange,
        selectedDocumentUnits,
      ];
}
