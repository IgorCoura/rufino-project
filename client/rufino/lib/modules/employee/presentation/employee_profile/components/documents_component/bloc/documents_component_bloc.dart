import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:file_picker/file_picker.dart';
import 'package:rufino/modules/employee/domain/model/document_group/document_group_with_documents.dart';
import 'package:rufino/modules/employee/services/document_group_service.dart';
import 'package:rufino/modules/employee/services/document_service.dart';
import 'package:rufino/shared/errors/aplication_errors.dart';

part 'documents_component_event.dart';
part 'documents_component_state.dart';

class DocumentsComponentBloc
    extends Bloc<DocumentsComponentEvent, DocumentsComponentState> {
  final DocumentService _documentService;
  final DocumentGroupService _documentGroupService;

  DocumentsComponentBloc(this._documentService, this._documentGroupService)
      : super(DocumentsComponentState()) {
    on<InitialEvent>(_onInitialEvent);
    on<ExpandEvent>(_onExpandEvent);
    on<SnackMessageWasShowEvent>(_onSnackMessageWasShow);
    on<ExpandDocumentEvent>(_onExpandDocumentEvent);
    on<EditDocumentUnitEvent>(_onEditDocumentUnitEvent);
    on<CreateDocumentUnitEvent>(_onCreateDocumentUnitEvent);
    on<RefeshEvent>(_onRefeshEvent);
    on<GenerateDocumentUnitEvent>(_onGenerateDocumentUnitEvent);
    on<GenerateAndSend2SignEvent>(_onGenerateAndSend2SignEvent);
    on<LoadDocumentUnitEvent>(_onLoadDocumentUnitEvent);
    on<LoadDocumentUnitToSignEvent>(_onLoadDocumentUnitToSignEvent);
    on<DownloadDocumentUnitEvent>(_onDownloadDocumentUnitEvent);
    on<ChangeDocumentUnitPaginationEvent>(_onChangeDocumentUnitPaginationEvent);
    on<ToggleRangeSelectionModeEvent>(_onToggleRangeSelectionModeEvent);
    on<ToggleDocumentUnitSelectionEvent>(_onToggleDocumentUnitSelectionEvent);
    on<ExecuteRangeGenerateEvent>(_onExecuteRangeGenerateEvent);
    on<ExecuteRangeDownloadEvent>(_onExecuteRangeDownloadEvent);
  }

  void _onInitialEvent(
    InitialEvent event,
    Emitter<DocumentsComponentState> emit,
  ) async {
    emit(state.copyWith(
      companyId: event.companyId,
      employeeId: event.employeeId,
    ));
  }

  Future _onExpandEvent(
    ExpandEvent event,
    Emitter<DocumentsComponentState> emit,
  ) async {
    emit(state.copyWith(isExpanded: !state.isExpanded, isLoading: true));

    if (state.isExpanded == true) {
      try {
        var documentGroup = await _documentGroupService.getAllWithDocuments(
            state.companyId, state.employeeId);

        emit(state.copyWith(reqDocuments: documentGroup, isLoading: false));
      } catch (ex, stacktrace) {
        var exception = _documentService.treatErrors(ex, stacktrace);
        emit(state.copyWith(isLoading: false, exception: exception));
      }
    }
  }

  void _onSnackMessageWasShow(
    SnackMessageWasShowEvent event,
    Emitter<DocumentsComponentState> emit,
  ) {
    emit(state.copyWith(snackMessage: ""));
  }

  Future _onExpandDocumentEvent(
    ExpandDocumentEvent event,
    Emitter<DocumentsComponentState> emit,
  ) async {
    if (event.isExpanded) {
      add(RefeshEvent(event.documentId));
    }
  }

  Future _onEditDocumentUnitEvent(
    EditDocumentUnitEvent event,
    Emitter<DocumentsComponentState> emit,
  ) async {
    emit(state.copyWith(isSavingData: true));

    try {
      await _documentService.editDocumentUnit(event.date, event.documentUnitId,
          event.documentId, state.employeeId, state.companyId);
      add(RefeshEvent(event.documentId));
      emit(state.copyWith(
          isSavingData: false,
          snackMessage: "Documento atualizado com sucesso!"));
    } catch (ex, stacktrace) {
      var exception = _documentService.treatErrors(ex, stacktrace);
      emit(state.copyWith(isSavingData: false, exception: exception));
    }
  }

  Future _onCreateDocumentUnitEvent(
    CreateDocumentUnitEvent event,
    Emitter<DocumentsComponentState> emit,
  ) async {
    emit(state.copyWith(isSavingData: true));

    try {
      await _documentService.createDocumentUnit(
          event.documentId, state.employeeId, state.companyId);

      final updatedPagination =
          Map<String, DocumentUnitPagination>.from(state.paginationMap);
      final current =
          updatedPagination[event.documentId] ?? const DocumentUnitPagination();
      updatedPagination[event.documentId] = current.copyWith(pageNumber: 1);

      emit(state.copyWith(
          isSavingData: false, paginationMap: updatedPagination));
      add(RefeshEvent(event.documentId));
    } catch (ex, stacktrace) {
      var exception = _documentService.treatErrors(ex, stacktrace);
      emit(state.copyWith(isSavingData: false, exception: exception));
    }
  }

  Future _onRefeshEvent(
    RefeshEvent event,
    Emitter<DocumentsComponentState> emit,
  ) async {
    emit(state.copyWith(isLazyLoading: true));

    try {
      final pagination = state.getPagination(event.documentId);

      List<DocumentGroupWithDocuments> documentGroupCopy =
          List.from(state.reqDocuments);

      for (var documentGroup in documentGroupCopy) {
        var documentIndex = documentGroup.documents
            .indexWhere((element) => element.id == event.documentId);

        if (documentIndex != -1) {
          documentGroup.documents[documentIndex] =
              await _documentService.getByIdDocuments(state.companyId,
                  state.employeeId, documentGroup.documents[documentIndex].id,
                  pageNumber: pagination.pageNumber,
                  pageSize: pagination.pageSize,
                  statusId: pagination.statusId);
          break;
        }
      }

      emit(state.copyWith(
          reqDocuments: documentGroupCopy, isLazyLoading: false));
    } catch (ex, stacktrace) {
      var exception = _documentService.treatErrors(ex, stacktrace);
      emit(state.copyWith(
          isLoading: false, exception: exception, isLazyLoading: false));
    }
  }

  Future _onGenerateDocumentUnitEvent(
    GenerateDocumentUnitEvent event,
    Emitter<DocumentsComponentState> emit,
  ) async {
    emit(state.copyWith(isSavingData: true));

    try {
      String? savePath = await FilePicker.platform.saveFile(
          dialogTitle: 'Salvar Documento',
          fileName: "doc_${event.documentUnitId.substring(0, 10)}.pdf");

      if (savePath == null) {
        emit(state.copyWith(
            isSavingData: false,
            snackMessage: "Nenhum local de salvamento selecionado."));
        return;
      }
      await _documentService.downloadDocumentGenerated(event.documentUnitId,
          event.documentId, state.employeeId, state.companyId, savePath);
      add(RefeshEvent(event.documentId));
      emit(state.copyWith(
          isSavingData: false, snackMessage: "Documento gerado com sucesso!"));
    } catch (ex, stacktrace) {
      var exception = _documentService.treatErrors(ex, stacktrace);
      emit(state.copyWith(isSavingData: false, exception: exception));
    }
  }

  Future _onGenerateAndSend2SignEvent(
    GenerateAndSend2SignEvent event,
    Emitter<DocumentsComponentState> emit,
  ) async {
    emit(state.copyWith(isSavingData: true));

    try {
      await _documentService.generateAndSend2Sign(
          event.dateLimitToSign,
          int.parse(event.eminderEveryNDays),
          event.documentUnitId,
          event.documentId,
          state.employeeId,
          state.companyId);
      add(RefeshEvent(event.documentId));
      emit(state.copyWith(
          isSavingData: false,
          snackMessage:
              "Documento gerado e enviado para assinatura com sucesso!"));
    } catch (ex, stacktrace) {
      var exception = _documentService.treatErrors(ex, stacktrace);
      emit(state.copyWith(isSavingData: false, exception: exception));
    }
  }

  Future _onLoadDocumentUnitToSignEvent(LoadDocumentUnitToSignEvent event,
      Emitter<DocumentsComponentState> emit) async {
    emit(state.copyWith(isSavingData: true));

    try {
      FilePickerResult? result = await FilePicker.platform.pickFiles();

      if (result == null) {
        emit(state.copyWith(
            isSavingData: false,
            snackMessage: "Nenhum arquivo selecionado para upload"));
        return;
      }

      if (result.files.single.size > 10 * 1024 * 1024) {
        emit(state.copyWith(
            isSavingData: false,
            snackMessage: "O arquivo selecionado excede o limite de 10 MB."));
        return;
      }

      await _documentService.loadDocumentUnitToSign(
          event.dateLimitToSign,
          event.eminderEveryNDays,
          event.documentUnitId,
          event.documentId,
          state.employeeId,
          state.companyId,
          result.files.single.path!);

      emit(state.copyWith(
          isSavingData: false, snackMessage: "Arquivo enviado com sucesso!"));
      add(RefeshEvent(event.documentId));
    } catch (ex, stacktrace) {
      var exception = _documentService.treatErrors(ex, stacktrace);
      emit(state.copyWith(isSavingData: false, exception: exception));
    }
  }

  Future _onLoadDocumentUnitEvent(
    LoadDocumentUnitEvent event,
    Emitter<DocumentsComponentState> emit,
  ) async {
    emit(state.copyWith(isSavingData: true));
    try {
      FilePickerResult? result = await FilePicker.platform.pickFiles();

      if (result == null) {
        emit(state.copyWith(
            isSavingData: false,
            snackMessage: "Nenhum arquivo selecionado para upload"));
        return;
      }

      if (result.files.single.size > 10 * 1024 * 1024) {
        emit(state.copyWith(
            isSavingData: false,
            snackMessage: "O arquivo selecionado excede o limite de 10 MB."));
        return;
      }

      await _documentService.loadDocumentUnit(
          event.documentUnitId,
          event.documentId,
          state.employeeId,
          state.companyId,
          result.files.single.path!);

      emit(state.copyWith(
          isSavingData: false, snackMessage: "Arquivo enviado com sucesso!"));
      add(RefeshEvent(event.documentId));
    } catch (ex, stacktrace) {
      var exception = _documentService.treatErrors(ex, stacktrace);
      emit(state.copyWith(isSavingData: false, exception: exception));
    }
  }

  Future _onDownloadDocumentUnitEvent(
    DownloadDocumentUnitEvent event,
    Emitter<DocumentsComponentState> emit,
  ) async {
    emit(state.copyWith(isSavingData: true));

    try {
      String? savePath = await FilePicker.platform.saveFile(
          dialogTitle: 'Salvar Documento',
          fileName: "doc_${event.documentUnitId.substring(0, 10)}.pdf");

      if (savePath == null) {
        emit(state.copyWith(
            isSavingData: false,
            snackMessage: "Nenhum local de salvamento selecionado."));
        return;
      }
      await _documentService.downloadDocumentUnit(event.documentUnitId,
          event.documentId, state.employeeId, state.companyId, savePath);
      add(RefeshEvent(event.documentId));
      emit(state.copyWith(
          isSavingData: false, snackMessage: "Documento baixado com sucesso!"));
    } catch (ex, stacktrace) {
      var exception = _documentService.treatErrors(ex, stacktrace);
      emit(state.copyWith(isSavingData: false, exception: exception));
    }
  }

  Future _onChangeDocumentUnitPaginationEvent(
    ChangeDocumentUnitPaginationEvent event,
    Emitter<DocumentsComponentState> emit,
  ) async {
    final updatedPagination =
        Map<String, DocumentUnitPagination>.from(state.paginationMap);
    final current =
        updatedPagination[event.documentId] ?? const DocumentUnitPagination();

    updatedPagination[event.documentId] = DocumentUnitPagination(
      pageNumber: event.pageNumber ?? current.pageNumber,
      pageSize: event.pageSize ?? current.pageSize,
      statusId:
          event.clearStatusFilter ? null : (event.statusId ?? current.statusId),
    );

    emit(state.copyWith(paginationMap: updatedPagination));
    add(RefeshEvent(event.documentId));
  }

  void _onToggleRangeSelectionModeEvent(
    ToggleRangeSelectionModeEvent event,
    Emitter<DocumentsComponentState> emit,
  ) {
    final newMode = !state.isSelectingRange;
    emit(state.copyWith(
      isSelectingRange: newMode,
      selectedDocumentUnits: newMode ? state.selectedDocumentUnits : const [],
    ));
  }

  void _onToggleDocumentUnitSelectionEvent(
    ToggleDocumentUnitSelectionEvent event,
    Emitter<DocumentsComponentState> emit,
  ) {
    final currentList =
        List<SelectedDocumentUnit>.from(state.selectedDocumentUnits);
    final existingIndex = currentList
        .indexWhere((item) => item.documentUnitId == event.documentUnitId);

    if (existingIndex != -1) {
      currentList.removeAt(existingIndex);
    } else {
      currentList.add(SelectedDocumentUnit(
        documentId: event.documentId,
        documentUnitId: event.documentUnitId,
        documentName: event.documentName,
        documentUnitDate: event.documentUnitDate,
        canGenerate: event.canGenerate,
        hasFile: event.hasFile,
      ));
    }
    emit(state.copyWith(selectedDocumentUnits: currentList));
  }

  Future _onExecuteRangeGenerateEvent(
    ExecuteRangeGenerateEvent event,
    Emitter<DocumentsComponentState> emit,
  ) async {
    if (state.selectedDocumentUnits.isEmpty) {
      emit(state.copyWith(snackMessage: "Nenhum documento selecionado."));
      return;
    }

    emit(state.copyWith(isSavingData: true));

    try {
      // Separate items that can be generated from those that cannot
      final canGenerate = state.selectedDocumentUnits
          .where((item) => item.canGenerate)
          .toList();
      final cannotGenerate = state.selectedDocumentUnits
          .where((item) => !item.canGenerate)
          .toList();

      if (canGenerate.isEmpty) {
        emit(state.copyWith(
          isSavingData: false,
          snackMessage: "Nenhum dos documentos selecionados pode ser gerado.",
        ));
        return;
      }

      String? savePath = await FilePicker.platform.saveFile(
        dialogTitle: 'Salvar Documentos Gerados',
        fileName: "documentos_gerados.zip",
      );

      if (savePath == null) {
        emit(state.copyWith(
          isSavingData: false,
          snackMessage: "Nenhum local de salvamento selecionado.",
        ));
        return;
      }

      // Group by documentId
      final Map<String, List<String>> groupedItems = {};
      for (var item in canGenerate) {
        groupedItems.putIfAbsent(item.documentId, () => []);
        groupedItems[item.documentId]!.add(item.documentUnitId);
      }

      final rangeItems = groupedItems.entries
          .map((entry) => DocumentRangeItem(
                documentId: entry.key,
                documentUnitIds: entry.value,
              ))
          .toList();

      await _documentService.generatePdfRange(
        rangeItems,
        state.employeeId,
        state.companyId,
        savePath,
      );

      String message = "Documentos gerados com sucesso!";
      if (cannotGenerate.isNotEmpty) {
        final failedNames = cannotGenerate
            .map((item) => "${item.documentName} (${item.documentUnitDate})")
            .join(", ");
        message =
            "Documentos gerados com sucesso, exceto: $failedNames — estes não possuem template para geração.";
      }

      emit(state.copyWith(
        isSavingData: false,
        isSelectingRange: false,
        selectedDocumentUnits: const [],
        snackMessage: message,
      ));
    } catch (ex, stacktrace) {
      var exception = _documentService.treatErrors(ex, stacktrace);
      emit(state.copyWith(isSavingData: false, exception: exception));
    }
  }

  Future _onExecuteRangeDownloadEvent(
    ExecuteRangeDownloadEvent event,
    Emitter<DocumentsComponentState> emit,
  ) async {
    if (state.selectedDocumentUnits.isEmpty) {
      emit(state.copyWith(snackMessage: "Nenhum documento selecionado."));
      return;
    }

    emit(state.copyWith(isSavingData: true));

    try {
      // Separate items that have files from those that do not
      final canDownload =
          state.selectedDocumentUnits.where((item) => item.hasFile).toList();
      final cannotDownload =
          state.selectedDocumentUnits.where((item) => !item.hasFile).toList();

      if (canDownload.isEmpty) {
        emit(state.copyWith(
          isSavingData: false,
          snackMessage:
              "Nenhum dos documentos selecionados possui arquivo para download.",
        ));
        return;
      }

      String? savePath = await FilePicker.platform.saveFile(
        dialogTitle: 'Salvar Documentos',
        fileName: "documentos_download.zip",
      );

      if (savePath == null) {
        emit(state.copyWith(
          isSavingData: false,
          snackMessage: "Nenhum local de salvamento selecionado.",
        ));
        return;
      }

      // Group by documentId
      final Map<String, List<String>> groupedItems = {};
      for (var item in canDownload) {
        groupedItems.putIfAbsent(item.documentId, () => []);
        groupedItems[item.documentId]!.add(item.documentUnitId);
      }

      final rangeItems = groupedItems.entries
          .map((entry) => DocumentRangeItem(
                documentId: entry.key,
                documentUnitIds: entry.value,
              ))
          .toList();

      await _documentService.downloadRange(
        rangeItems,
        state.employeeId,
        state.companyId,
        savePath,
      );

      String message = "Documentos baixados com sucesso!";
      if (cannotDownload.isNotEmpty) {
        final failedNames = cannotDownload
            .map((item) => "${item.documentName} (${item.documentUnitDate})")
            .join(", ");
        message =
            "Documentos baixados com sucesso, exceto: $failedNames — estes não possuem arquivo para download.";
      }

      emit(state.copyWith(
        isSavingData: false,
        isSelectingRange: false,
        selectedDocumentUnits: const [],
        snackMessage: message,
      ));
    } catch (ex, stacktrace) {
      var exception = _documentService.treatErrors(ex, stacktrace);
      emit(state.copyWith(isSavingData: false, exception: exception));
    }
  }
}
