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
      add(RefeshEvent(event.documentId));
      emit(state.copyWith(isSavingData: false));
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
      List<DocumentGroupWithDocuments> documentGroupCopy =
          List.from(state.reqDocuments);

      for (var documentGroup in documentGroupCopy) {
        var documentIndex = documentGroup.documents
            .indexWhere((element) => element.id == event.documentId);

        if (documentIndex != -1) {
          documentGroup.documents[documentIndex] =
              await _documentService.getByIdDocuments(state.companyId,
                  state.employeeId, documentGroup.documents[documentIndex].id);
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
}
