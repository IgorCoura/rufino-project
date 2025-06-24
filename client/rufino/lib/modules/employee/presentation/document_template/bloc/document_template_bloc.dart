import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:file_picker/file_picker.dart';
import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino/domain/services/company_global_service.dart';
import 'package:rufino/modules/employee/domain/model/document_template/document_template.dart';
import 'package:rufino/modules/employee/domain/model/document_template/place_signature.dart';
import 'package:rufino/modules/employee/domain/model/document_template/recover_data_type.dart';
import 'package:rufino/modules/employee/domain/model/document_template/type_signature.dart';
import 'package:rufino/modules/employee/services/document_template_service.dart';
import 'package:rufino/shared/errors/aplication_errors.dart';

part 'document_template_event.dart';
part 'document_template_state.dart';

class DocumentTemplateBloc
    extends Bloc<DocumentTemplateEvent, DocumentTemplateState> {
  final DocumentTemplateService _documentTemplateService;
  final CompanyGlobalService _companyService;

  DocumentTemplateBloc(this._documentTemplateService, this._companyService)
      : super(DocumentTemplateState()) {
    on<SnackMessageWasShow>(_onSnackMessageWasShow);
    on<InitialEvent>(_onInitialEvent);
    on<EditEvent>(_onEditEvent);
    on<ChangeFieldValueEvent>(_onChangeFieldValueEvent);
    on<ChangePlaceSignatureValuesEvent>(_onChangePlaceSignatureValuesEvent);
    on<NewPlaceSignatureEvent>(_onNewPlaceSignatureEvent);
    on<CancelEditEvent>(_onCancelEditEvent);
    on<RemovePlaceSignatureEvent>(_onRemovePlaceSignatureEvent);
    on<SaveEvent>(_onSaveEvent);
    on<SendFileEvent>(_onSendFileEvent);
    on<DownLoadFileEvent>(_onDownLoadFileEvent);
  }

  void _onSnackMessageWasShow(
      SnackMessageWasShow event, Emitter<DocumentTemplateState> emit) {
    emit(state.copyWith(snackMessage: ""));
  }

  Future _onInitialEvent(
      InitialEvent event, Emitter<DocumentTemplateState> emit) async {
    emit(state.copyWith(isLoading: true));

    try {
      final company = await _companyService.getSelectedCompany();

      final recoverDataType =
          await _documentTemplateService.getAllRecoverDataType(company.id);
      final typeSignature =
          await _documentTemplateService.getAllTypeSignature(company.id);
      var documentTemplate = DocumentTemplate.empty();
      var hasFile = false;
      if (event.documentTemplateId != "new") {
        documentTemplate = await _documentTemplateService
            .getByIdDocumentTemplates(event.documentTemplateId, company.id);
        hasFile = await _documentTemplateService.hasFileInDocumentTemplate(
            company.id, documentTemplate.id);
      }
      emit(state.copyWith(
          isLoading: false,
          hasFile: hasFile,
          documentTemplate: documentTemplate,
          placeSignatures: documentTemplate.placeSignatures,
          recoverDataType: recoverDataType,
          typeSignature: typeSignature));

      if (event.documentTemplateId == "new") {
        emit(state.copyWith(isEditing: true));
      }
    } catch (ex, stacktrace) {
      var exception = _documentTemplateService.treatErrors(ex, stacktrace);
      emit(state.copyWith(isLoading: false, exception: exception));
    }
  }

  void _onEditEvent(EditEvent event, Emitter<DocumentTemplateState> emit) {
    emit(state.copyWith(isEditing: true));
  }

  void _onChangeFieldValueEvent(
      ChangeFieldValueEvent event, Emitter<DocumentTemplateState> emit) {
    var newDocumentTemplate =
        state.documentTemplate.copyWith(generic: event.changeValue);
    emit(state.copyWith(documentTemplate: newDocumentTemplate));
  }

  void _onChangePlaceSignatureValuesEvent(ChangePlaceSignatureValuesEvent event,
      Emitter<DocumentTemplateState> emit) {
    var index = event.index;
    var newPlaceSignature =
        state.placeSignatures[index].copyWith(generic: event.changeValue);
    var newListPlaceSignature =
        List<PlaceSignature>.from(state.placeSignatures);
    newListPlaceSignature[index] = newPlaceSignature;

    emit(state.copyWith(placeSignatures: newListPlaceSignature));
  }

  void _onNewPlaceSignatureEvent(
      NewPlaceSignatureEvent event, Emitter<DocumentTemplateState> emit) {
    var newPlaceSignature = PlaceSignature.empty();
    var newListPlaceSignature =
        List<PlaceSignature>.from(state.placeSignatures);
    newListPlaceSignature.add(newPlaceSignature);
    emit(state.copyWith(placeSignatures: newListPlaceSignature));
  }

  void _onCancelEditEvent(
      CancelEditEvent event, Emitter<DocumentTemplateState> emit) {
    emit(state.copyWith(isEditing: false));
    if (state.documentTemplate == DocumentTemplate.empty()) {
      Modular.to.navigate("/employee/document-template");
    } else {
      add(InitialEvent(state.documentTemplate.id));
    }
  }

  void _onRemovePlaceSignatureEvent(
      RemovePlaceSignatureEvent event, Emitter<DocumentTemplateState> emit) {
    var newListPlaceSignature =
        List<PlaceSignature>.from(state.placeSignatures);
    newListPlaceSignature.removeAt(event.index);
    emit(state.copyWith(placeSignatures: newListPlaceSignature));
  }

  Future _onSaveEvent(
      SaveEvent event, Emitter<DocumentTemplateState> emit) async {
    emit(state.copyWith(isSavingData: true));
    try {
      final company = await _companyService.getSelectedCompany();
      var newDocumentTemplate = state.documentTemplate
          .copyWith(placeSignatures: state.placeSignatures);

      if (newDocumentTemplate.isInvalidTemplateFileInfo) {
        throw AplicationErrors.emplyee.errorInvalidTemplateFileInfo;
      }

      if (newDocumentTemplate.id == "") {
        newDocumentTemplate = newDocumentTemplate.copyWith(
            id: await _documentTemplateService.createDocumentTemplate(
                company.id, newDocumentTemplate));
        emit(state.copyWith(documentTemplate: newDocumentTemplate));
      } else {
        await _documentTemplateService.editDocumentTemplate(
            company.id, newDocumentTemplate);
      }

      emit(state.copyWith(
          isSavingData: false,
          isEditing: false,
          snackMessage: "Template de documento salvo com sucesso!"));
    } catch (ex, stacktrace) {
      var exception = _documentTemplateService.treatErrors(ex, stacktrace);
      emit(state.copyWith(isSavingData: false, exception: exception));
    }
  }

  Future _onSendFileEvent(
      SendFileEvent event, Emitter<DocumentTemplateState> emit) async {
    emit(state.copyWith(isLoading: true));
    try {
      if (state.documentTemplate.isEmptyTemplateFileInfo) {
        throw AplicationErrors.emplyee.errorInvalidTemplateFileInfo;
      }
      FilePickerResult? result = await FilePicker.platform.pickFiles();
      if (result != null) {
        if (result.files.single.size > 10 * 1024 * 1024) {
          emit(state.copyWith(
              isSavingData: false,
              snackMessage: "O arquivo selecionado excede o limite de 10 MB."));
          return;
        }

        final company = await _companyService.getSelectedCompany();
        await _documentTemplateService.loadFileToDocumentTemplate(
            company.id, state.documentTemplate.id, result.files.single.path!);
      } else {
        emit(state.copyWith(
            isLoading: false,
            snackMessage: "Nenhum arquivo selecionado para upload"));
      }

      emit(state.copyWith(
          isLoading: false,
          snackMessage: "Arquivo enviado com sucesso!",
          hasFile: true));
    } catch (ex, stacktrace) {
      var exception = _documentTemplateService.treatErrors(ex, stacktrace);
      emit(state.copyWith(isLoading: false, exception: exception));
    }
  }

  Future _onDownLoadFileEvent(
      DownLoadFileEvent event, Emitter<DocumentTemplateState> emit) async {
    try {
      String? savePath = await FilePicker.platform.saveFile(
        dialogTitle: 'Save File',
        fileName: 'document_template.zip',
      );

      if (savePath == null) {
        emit(state.copyWith(
            isLoading: false,
            snackMessage: "Nenhum arquivo selecionado para download"));
        return;
      }

      final company = await _companyService.getSelectedCompany();
      _documentTemplateService.downloadFileToDocumentTemplate(
          company.id, state.documentTemplate.id, savePath);
    } catch (ex, stacktrace) {
      var exception = _documentTemplateService.treatErrors(ex, stacktrace);
      emit(state.copyWith(isLoading: false, exception: exception));
    }
  }
}
