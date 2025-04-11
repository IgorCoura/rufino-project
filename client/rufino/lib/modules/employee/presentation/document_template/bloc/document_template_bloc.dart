import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:rufino/domain/services/company_service.dart';
import 'package:rufino/modules/employee/domain/model/document_template/document_template.dart';
import 'package:rufino/modules/employee/domain/services/people_management_service.dart';
import 'package:rufino/shared/errors/aplication_errors.dart';

part 'document_template_event.dart';
part 'document_template_state.dart';

class DocumentTemplateBloc
    extends Bloc<DocumentTemplateEvent, DocumentTemplateState> {
  final PeopleManagementService _peopleManagementService;
  final CompanyService _companyService;

  DocumentTemplateBloc(this._peopleManagementService, this._companyService)
      : super(DocumentTemplateState()) {
    on<SnackMessageWasShow>(_onSnackMessageWasShow);
    on<InitialEvent>(_onInitialEvent);
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
      final documentTemplates =
          await _peopleManagementService.getAllDocumentTemplates(company.id);

      emit(state.copyWith(
        isLoading: false,
        documentTemplates: documentTemplates,
      ));
    } catch (ex, stacktrace) {
      var exception = _peopleManagementService.treatErrors(ex, stacktrace);
      emit(state.copyWith(isLoading: false, exception: exception));
    }
  }
}
