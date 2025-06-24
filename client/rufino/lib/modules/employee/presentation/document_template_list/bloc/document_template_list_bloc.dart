import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:rufino/domain/services/company_global_service.dart';
import 'package:rufino/modules/employee/domain/model/document_template/document_template_simple.dart';
import 'package:rufino/modules/employee/services/document_template_service.dart';
import 'package:rufino/shared/errors/aplication_errors.dart';

part 'document_template_list_event.dart';
part 'document_template_list_state.dart';

class DocumentTemplateListBloc
    extends Bloc<DocumentTemplateListEvent, DocumentTemplateListState> {
  final DocumentTemplateService _documentTemplateService;
  final CompanyGlobalService _companyService;
  DocumentTemplateListBloc(this._documentTemplateService, this._companyService)
      : super(DocumentTemplateListState()) {
    on<InitialEvent>(_onInitialEvent);
  }

  Future _onInitialEvent(
      InitialEvent event, Emitter<DocumentTemplateListState> emit) async {
    try {
      emit(state.copyWith(isLoading: true));
      var company = await _companyService.getSelectedCompany();
      var documentTemplateList = await _documentTemplateService
          .getAllDocumentTemplatesSimple(company.id);

      emit(state.copyWith(
          documentTemplateList: documentTemplateList, isLoading: false));
    } catch (ex, stacktrace) {
      var exception = _documentTemplateService.treatErrors(ex, stacktrace);
      emit(state.copyWith(isLoading: false, exception: exception));
    }
  }
}
