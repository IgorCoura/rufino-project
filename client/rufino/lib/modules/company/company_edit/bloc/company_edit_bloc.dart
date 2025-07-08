import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:rufino/modules/company/domain/models/company_model.dart';
import 'package:rufino/modules/company/domain/services/company_service.dart';
import 'package:rufino/shared/errors/aplication_errors.dart';

part 'company_edit_event.dart';
part 'company_edit_state.dart';

class CompanyEditBloc extends Bloc<CompanyEditEvent, CompanyEditState> {
  final CompanyService _companyService;

  CompanyEditBloc(this._companyService) : super(CompanyEditState()) {
    on<ChangePropEvent>(_onChangePropEvent);
    on<SaveChangesEvent>(_onSaveChangesEvent);
    on<SnackMessageWasShownEvent>(_onSnackMessageWasShownEvent);
    on<InitializeCompanyEvent>(_onInitializeCompanyEvent);
  }

  Future _onInitializeCompanyEvent(
      InitializeCompanyEvent event, Emitter<CompanyEditState> emit) async {
    try {
      emit(state.copyWith(isLoading: true));

      if (event.companyId != null && event.companyId!.isNotEmpty) {
        final company = await _companyService.getCompany(event.companyId!);
        emit(state.copyWith(company: company));
      }
      emit(state.copyWith(isLoading: false));
    } catch (ex, stacktrace) {
      var exception = _companyService.treatErrors(ex, stacktrace);
      emit(state.copyWith(isLoading: false, exception: exception));
    }
  }

  void _onChangePropEvent(
      ChangePropEvent event, Emitter<CompanyEditState> emit) {
    final Company = state.company.copyWith(generic: event.value);
    emit(state.copyWith(
      company: Company,
    ));
  }

  Future _onSaveChangesEvent(
      SaveChangesEvent event, Emitter<CompanyEditState> emit) async {
    try {
      emit(state.copyWith(isSavingData: true));

      if (state.company.id.isEmpty) {
        await _companyService.createCompany(
          state.company,
        );
      } else {
        await _companyService.editCompany(
          state.company,
        );
      }

      emit(state.copyWith(
          isSavingData: false, snackMessage: "Empresa salva com sucesso!"));
    } catch (ex, stacktrace) {
      var exception = _companyService.treatErrors(ex, stacktrace);
      emit(state.copyWith(isSavingData: false, exception: exception));
    }
  }

  void _onSnackMessageWasShownEvent(
      SnackMessageWasShownEvent event, Emitter<CompanyEditState> emit) {
    emit(state.copyWith(snackMessage: ""));
  }
}
