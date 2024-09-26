import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:rufino/domain/model/company.dart';
import 'package:rufino/domain/services/auth_service.dart';
import 'package:rufino/domain/services/company_service.dart';

part 'company_selection_event.dart';
part 'company_selection_state.dart';

class CompanySelectionBloc
    extends Bloc<CompanySelectionEvent, CompanySelectionState> {
  CompanySelectionBloc(this._companyService, this._authService)
      : super(CompanySelectionState(companies: List.empty())) {
    on<InitialCompanyEvent>(_onInitialCompanyEvent);
    on<SelectCompanyEvent>(onSelectCompanyEvent);
    on<ChangeSelectionOptionEvent>(onChangeSelectionOptionEvent);
  }

  final CompanyService _companyService;
  final AuthService _authService;

  void _onInitialCompanyEvent(
      InitialCompanyEvent event, Emitter<CompanySelectionState> emit) async {
    var companiesIds = await _authService.getCompaniesIds();
    var companies = await _companyService.getCompanies(companiesIds);
    if (companies.length == 1) {
      _companyService.selectCompany(companies.first);
      emit(state.copyWith(hasSelectedCompany: true));
    }
    emit(state.copyWith(
        companies: companies,
        selectedCompany: companies.first.id,
        isLoading: false));
  }

  void onSelectCompanyEvent(
      SelectCompanyEvent event, Emitter<CompanySelectionState> emit) {
    emit(state.copyWith(isLoading: true));
    var company =
        state.companies.firstWhere((el) => el.id == state.selectedCompany);
    _companyService.selectCompany(company);
    emit(state.copyWith(hasSelectedCompany: true, isLoading: false));
  }

  void onChangeSelectionOptionEvent(
      ChangeSelectionOptionEvent event, Emitter<CompanySelectionState> emit) {
    emit(state.copyWith(selectedCompany: event.companyId));
  }
}
