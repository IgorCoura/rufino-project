import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:rufino/domain/services/company_service.dart';
import 'package:rufino/modules/employee/domain/model/require_document/require_document_simple.dart';
import 'package:rufino/modules/employee/services/people_management_service.dart';
import 'package:rufino/shared/errors/aplication_errors.dart';

part 'require_document_list_event.dart';
part 'require_document_list_state.dart';

class RequireDocumentListBloc
    extends Bloc<RequireDocumentListEvent, RequireDocumentListState> {
  final PeopleManagementService _peopleManagementService;
  final CompanyService _companyService;
  RequireDocumentListBloc(this._companyService, this._peopleManagementService)
      : super(RequireDocumentListState()) {
    on<InitialEvent>(_onInitialEvent);
  }

  Future _onInitialEvent(
      InitialEvent event, Emitter<RequireDocumentListState> emit) async {
    try {
      emit(state.copyWith(isLoading: true));
      var company = await _companyService.getSelectedCompany();
      var requireDocumentList = await _peopleManagementService
          .getAllRequireDocumentsSimple(company.id);

      emit(state.copyWith(
          requireDocumentList: requireDocumentList, isLoading: false));
    } catch (ex, stacktrace) {
      var exception = _peopleManagementService.treatErrors(ex, stacktrace);
      emit(state.copyWith(isLoading: false, exception: exception));
    }
  }
}
