import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:rufino/domain/services/company_service.dart';
import 'package:rufino/modules/employee/domain/model/document_template/document_template_simple.dart';
import 'package:rufino/modules/employee/domain/services/people_management_service.dart';
import 'package:rufino/shared/errors/aplication_errors.dart';

part 'document_template_list_event.dart';
part 'document_template_list_state.dart';

class DocumentTemplateListBloc
    extends Bloc<DocumentTemplateListEvent, DocumentTemplateListState> {
  final PeopleManagementService _peopleManagementService;
  final CompanyService _companyService;
  DocumentTemplateListBloc(this._peopleManagementService, this._companyService)
      : super(DocumentTemplateListState()) {
    on<InitialEvent>(_onInitialEvent);
  }

  Future _onInitialEvent(
      InitialEvent event, Emitter<DocumentTemplateListState> emit) async {
    try {
      emit(state.copyWith(isLoading: true));
      var company = await _companyService.getSelectedCompany();
      var documentTemplateList = await _peopleManagementService
          .getAllDocumentTemplatesSimple(company.id);

      emit(state.copyWith(
          documentTemplateList: documentTemplateList, isLoading: false));
    } catch (ex, stacktrace) {
      var exception = _peopleManagementService.treatErrors(ex, stacktrace);
      emit(state.copyWith(isLoading: false, exception: exception));
    }
  }
}
