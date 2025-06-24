import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:rufino/domain/model/company.dart';
import 'package:rufino/domain/services/auth_service.dart';
import 'package:rufino/domain/services/company_global_service.dart';
import 'package:rufino/shared/errors/aplication_errors.dart';

part 'company_selection_event.dart';
part 'company_selection_state.dart';

class CompanySelectionBloc
    extends Bloc<CompanySelectionEvent, CompanySelectionState> {
  CompanySelectionBloc(this._companyService, this._authService)
      : super(CompanySelectionState(companies: List.empty())) {
    on<InitialCompanyEvent>(_onInitialCompanyEvent);
    on<SelectCompanyEvent>(_onSelectCompanyEvent);
    on<ChangeSelectionOptionEvent>(_onChangeSelectionOptionEvent);
  }

  final CompanyGlobalService _companyService;
  final AuthService _authService;

  void _onInitialCompanyEvent(
      InitialCompanyEvent event, Emitter<CompanySelectionState> emit) async {
    emit(CompanySelectionState(companies: List.empty(), isLoading: true));
    try {
      var companiesIds = await _authService.getCompaniesIds();
      var companies = await _companyService.getCompanies(companiesIds);
      if (companies.length == 1) {
        await _companyService.selectCompany(companies.first);
        emit(state.copyWith(hasSelectedCompany: true));
      }
      emit(state.copyWith(
          companies: companies,
          selectedCompany: companies.first.id,
          isLoading: false));
    } catch (ex, stacktrace) {
      var exception = _companyService.treatErrors(ex, stacktrace);
      emit(state.copyWith(isLoading: false, exception: exception));
    }
  }

  Future _onSelectCompanyEvent(
      SelectCompanyEvent event, Emitter<CompanySelectionState> emit) async {
    try {
      emit(state.copyWith(isLoading: true));
      var company =
          state.companies.firstWhere((el) => el.id == state.selectedCompany);
      await _companyService.selectCompany(company);
      emit(state.copyWith(hasSelectedCompany: true, isLoading: false));
    } catch (ex, stacktrace) {
      var exception = _companyService.treatErrors(ex, stacktrace);
      emit(state.copyWith(isLoading: false, exception: exception));
    }
  }

  void _onChangeSelectionOptionEvent(
      ChangeSelectionOptionEvent event, Emitter<CompanySelectionState> emit) {
    emit(state.copyWith(selectedCompany: event.companyId));
  }
}
