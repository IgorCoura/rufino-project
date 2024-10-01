import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:rufino/domain/model/company.dart';
import 'package:rufino/domain/services/auth_service.dart';
import 'package:rufino/domain/services/company_service.dart';
import 'package:rufino/shared/errors/aplication_errors.dart';

part 'home_event.dart';
part 'home_state.dart';

class HomeBloc extends Bloc<HomeEvent, HomeState> {
  HomeBloc(this._authService, this._companyService) : super(const HomeState()) {
    on<InitialHomeEvent>(_onInitialHomeEvent);
    on<LogoutRequested>(_onLogoutRequested);
  }

  final AuthService _authService;
  final CompanyService _companyService;

  Future _onInitialHomeEvent(
      InitialHomeEvent event, Emitter<HomeState> emit) async {
    emit(const HomeState());
    try {
      var company = await _companyService.getSelectedCompany();
      emit(state.copyWith(company: company));
    } catch (ex, stacktrace) {
      var exception = _companyService.treatErrors(ex, stacktrace);
      emit(state.copyWith(exception: exception));
    }
  }

  Future<void> _onLogoutRequested(
      LogoutRequested event, Emitter<HomeState> emit) async {
    await _authService.logOut();
  }
}
