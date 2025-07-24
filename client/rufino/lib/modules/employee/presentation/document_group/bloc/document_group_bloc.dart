import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:rufino/domain/services/company_global_service.dart';
import 'package:rufino/modules/employee/domain/model/document_group/description.dart';
import 'package:rufino/modules/employee/domain/model/document_group/document_group.dart';
import 'package:rufino/modules/employee/domain/model/document_group/name.dart';
import 'package:rufino/modules/employee/services/document_group_service.dart';
import 'package:rufino/shared/errors/aplication_errors.dart';

part 'document_group_event.dart';
part 'document_group_state.dart';

class DocumentGroupBloc extends Bloc<DocumentGroupEvent, DocumentGroupState> {
  final DocumentGroupService _documentGroupService;
  final CompanyGlobalService _companyService;

  DocumentGroupBloc(this._documentGroupService, this._companyService)
      : super(DocumentGroupState()) {
    on<LoadDocumentGroups>(_onloadDocumentGroups);
    on<CreateDocumentGroup>(_onCreateDocumentGroup);
    on<UpdateDocumentGroup>(_onUpdateDocumentGroup);
  }

  Future _onloadDocumentGroups(
      LoadDocumentGroups event, Emitter<DocumentGroupState> emit) async {
    try {
      emit(state.copyWith(isLoading: true));
      final company = await _companyService.getSelectedCompany();
      final documentGroups = await _documentGroupService.getAll(company.id);
      emit(state.copyWith(isLoading: false, documentGroups: documentGroups));
    } catch (ex, stacktrace) {
      var exception = _documentGroupService.treatErrors(ex, stacktrace);
      emit(state.copyWith(isLoading: false, exception: exception));
    }
  }

  Future _onCreateDocumentGroup(
      CreateDocumentGroup event, Emitter<DocumentGroupState> emit) async {
    try {
      emit(state.copyWith(isLoading: true));
      final company = await _companyService.getSelectedCompany();

      await _documentGroupService.create(
        company.id,
        event.name,
        event.description,
      );

      add(LoadDocumentGroups());

      emit(state.copyWith(
        isLoading: false,
        snackMessage: "Groupo de documentos criado com sucesso!",
      ));
    } catch (ex, stacktrace) {
      var exception = _documentGroupService.treatErrors(ex, stacktrace);
      emit(state.copyWith(isSavingData: false, exception: exception));
    }
  }

  Future _onUpdateDocumentGroup(
      UpdateDocumentGroup event, Emitter<DocumentGroupState> emit) async {
    try {
      emit(state.copyWith(isLoading: true));
      final company = await _companyService.getSelectedCompany();

      await _documentGroupService.update(
        company.id,
        DocumentGroup(
          event.id,
          Name(event.name),
          Description(event.description),
        ),
      );
      add(LoadDocumentGroups());

      emit(state.copyWith(
        isLoading: false,
        snackMessage: "Grupo de documentos editado com sucesso!",
      ));
    } catch (ex, stacktrace) {
      var exception = _documentGroupService.treatErrors(ex, stacktrace);
      emit(state.copyWith(isSavingData: false, exception: exception));
    }
  }
}
