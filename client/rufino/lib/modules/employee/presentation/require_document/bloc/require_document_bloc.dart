import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino/domain/services/company_global_service.dart';
import 'package:rufino/modules/employee/domain/model/document_template/document_template_simple.dart';
import 'package:rufino/modules/employee/domain/model/require_document/association.dart';
import 'package:rufino/modules/employee/domain/model/require_document/association_type.dart';
import 'package:rufino/modules/employee/domain/model/require_document/event.dart';
import 'package:rufino/modules/employee/domain/model/require_document/listen_event.dart';
import 'package:rufino/modules/employee/domain/model/require_document/require_document.dart';
import 'package:rufino/modules/employee/domain/model/require_document/status.dart';
import 'package:rufino/modules/employee/services/document_template_service.dart';
import 'package:rufino/modules/employee/services/people_management_service.dart';
import 'package:rufino/shared/errors/aplication_errors.dart';

part 'require_document_event.dart';
part 'require_document_state.dart';

class RequireDocumentBloc
    extends Bloc<RequireDocumentEvent, RequireDocumentState> {
  final PeopleManagementService _peopleManagementService;
  final DocumentTemplateService _documentTemplateService;
  final CompanyGlobalService _companyService;

  RequireDocumentBloc(this._companyService, this._peopleManagementService,
      this._documentTemplateService)
      : super(RequireDocumentState()) {
    on<InitialEvent>(_onInitialEvent);
    on<EditEvent>(_onEditEvent);
    on<CancelEditEvent>(_onCancelEditEvent);
    on<AssociationTypeSelectedEvent>(_onAssociationTypeSelectedEvent);
    on<AddDocumentTemplateEvent>(_onAddDocumentTemplateEvent);
    on<RemoveDocumentTemplateEvent>(_onRemoveDocumentTemplateEvent);
    on<AddEventEvent>(_onAddEventEvent);
    on<RemoveEventEvent>(_onRemoveEventEvent);
    on<AddStatusEvent>(_onAddStatusEvent);
    on<RemoveStatusEvent>(_onRemoveStatusEvent);
    on<ChangeFieldValueEvent>(_onChangeFieldValueEvent);
    on<SaveEvent>(_onSaveEvent);
    on<SnackMessageWasShow>(_onSnackMessageWasShow);
  }

  void _onSnackMessageWasShow(
      SnackMessageWasShow event, Emitter<RequireDocumentState> emit) {
    emit(state.copyWith(snackMessage: ""));
  }

  Future _onInitialEvent(
      InitialEvent event, Emitter<RequireDocumentState> emit) async {
    try {
      emit(state.copyWith(isLoading: true));
      var company = await _companyService.getSelectedCompany();

      var associationTypes =
          await _peopleManagementService.getAllAssociationTypes(company.id);
      var requireDocument = RequireDocument.empty(companyId: company.id);
      var associations = <Association>[];
      if (event.requireDocumentId != "new") {
        requireDocument = await _peopleManagementService
            .getByIdRequireDocuments(event.requireDocumentId, company.id);
        associations = await _peopleManagementService.getAllAssociation(
            company.id, requireDocument.associationType.id);
      } else {
        add(EditEvent());
      }
      emit(state.copyWith(
          requireDocument: requireDocument,
          associationTypes: associationTypes,
          associations: associations,
          isLoading: false));
    } catch (ex, stacktrace) {
      var exception = _peopleManagementService.treatErrors(ex, stacktrace);
      emit(state.copyWith(isLoading: false, exception: exception));
    }
  }

  Future _onEditEvent(
      EditEvent event, Emitter<RequireDocumentState> emit) async {
    try {
      emit(state.copyWith(isLoading: true));
      var status = await _peopleManagementService
          .getStatus(state.requireDocument.companyId);
      var reqDocStatus = status.map((el) => Status(el.id, el.name)).toList();
      var events = await _peopleManagementService
          .getEvents(state.requireDocument.companyId);
      var eventsRequireDocuments = await _peopleManagementService
          .getRequireDocumentEvents(state.requireDocument.companyId);
      events = events..addAll(eventsRequireDocuments);
      var documentTemplates = await _documentTemplateService
          .getAllDocumentTemplatesSimple(state.requireDocument.companyId);

      emit(state.copyWith(
          listStatus: reqDocStatus,
          events: events,
          documentTemplates: documentTemplates,
          isEditing: true,
          isLoading: false));
    } catch (ex, stacktrace) {
      var exception = _peopleManagementService.treatErrors(ex, stacktrace);
      emit(state.copyWith(isLoading: false, exception: exception));
    }
  }

  void _onCancelEditEvent(
      CancelEditEvent event, Emitter<RequireDocumentState> emit) {
    emit(state.copyWith(isEditing: false));
    if (state.requireDocument.id.isEmpty) {
      Modular.to.navigate("/employee/require-documents");
    } else {
      add(InitialEvent(state.requireDocument.id));
    }
  }

  Future _onAssociationTypeSelectedEvent(AssociationTypeSelectedEvent event,
      Emitter<RequireDocumentState> emit) async {
    try {
      emit(state.copyWith(isLoading: true));
      var associationType = event.associationType as AssociationType;
      var requireDocument = state.requireDocument.copyWith(
        association: Association.empty(),
        associationType: associationType,
      );
      var associations = await _peopleManagementService.getAllAssociation(
          state.requireDocument.companyId, associationType.id);
      emit(state.copyWith(
          requireDocument: requireDocument,
          associations: associations,
          isLoading: false));
    } catch (ex, stacktrace) {
      var exception = _peopleManagementService.treatErrors(ex, stacktrace);
      emit(state.copyWith(isLoading: false, exception: exception));
    }
  }

  void _onAddDocumentTemplateEvent(
      AddDocumentTemplateEvent event, Emitter<RequireDocumentState> emit) {
    var documentTemplates = List<DocumentTemplateSimple>.from(
        state.requireDocument.documentTemplates);
    documentTemplates.add(event.documentTemplate);
    emit(state.copyWith(
      requireDocument: state.requireDocument.copyWith(
        documentTemplates: documentTemplates,
      ),
    ));
  }

  Future _onRemoveDocumentTemplateEvent(RemoveDocumentTemplateEvent event,
      Emitter<RequireDocumentState> emit) async {
    var documentTemplates = List<DocumentTemplateSimple>.from(
        state.requireDocument.documentTemplates);
    documentTemplates.remove(event.documentTemplate);
    emit(state.copyWith(
      requireDocument: state.requireDocument.copyWith(
        documentTemplates: documentTemplates,
      ),
    ));
  }

  Future _onAddEventEvent(
      AddEventEvent event, Emitter<RequireDocumentState> emit) async {
    var list = List<ListenEvent>.from(state.requireDocument.listenEvents);
    list.add(ListenEvent(event.event, []));
    emit(state.copyWith(
      requireDocument: state.requireDocument.copyWith(
        listenEvents: list,
      ),
    ));
  }

  Future _onRemoveEventEvent(
      RemoveEventEvent event, Emitter<RequireDocumentState> emit) async {
    var list = List<ListenEvent>.from(state.requireDocument.listenEvents);
    list.removeWhere((element) => element.event == event.event);
    emit(state.copyWith(
      requireDocument: state.requireDocument.copyWith(
        listenEvents: list,
      ),
    ));
  }

  Future _onAddStatusEvent(
      AddStatusEvent event, Emitter<RequireDocumentState> emit) async {
    var list = List<ListenEvent>.from(state.requireDocument.listenEvents);
    list
        .firstWhere((element) => element.event == event.event)
        .statusList
        .add(event.status);
    emit(state.copyWith(
      requireDocument: state.requireDocument.copyWith(
        listenEvents: list,
      ),
    ));
  }

  Future _onRemoveStatusEvent(
      RemoveStatusEvent event, Emitter<RequireDocumentState> emit) async {
    var list = List<ListenEvent>.from(state.requireDocument.listenEvents);
    list
        .firstWhere((element) => element.event == event.event)
        .statusList
        .remove(event.status);
    emit(state.copyWith(
      requireDocument: state.requireDocument.copyWith(
        listenEvents: list,
      ),
    ));
  }

  void _onChangeFieldValueEvent(
      ChangeFieldValueEvent event, Emitter<RequireDocumentState> emit) {
    var newD = state.requireDocument.copyWith(generic: event.changeValue);
    emit(state.copyWith(requireDocument: newD));
  }

  Future _onSaveEvent(
      SaveEvent event, Emitter<RequireDocumentState> emit) async {
    emit(state.copyWith(isSavingData: true));
    try {
      var newDoc = state.requireDocument.copyWith();
      if (state.requireDocument.id == "") {
        newDoc = newDoc.copyWith(
            id: await _peopleManagementService.createRequireDocument(newDoc));
        emit(state.copyWith(requireDocument: newDoc));
      } else {
        await _peopleManagementService.editRequireDocument(newDoc);
      }

      emit(state.copyWith(
          isSavingData: false,
          isEditing: false,
          snackMessage: "Requerimento de documento salvo com sucesso!"));
    } catch (ex, stacktrace) {
      var exception = _peopleManagementService.treatErrors(ex, stacktrace);
      emit(state.copyWith(isSavingData: false, exception: exception));
    }
  }
}
