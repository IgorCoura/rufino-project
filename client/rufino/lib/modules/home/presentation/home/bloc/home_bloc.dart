import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:rufino/domain/services/auth_service.dart';

part 'home_event.dart';
part 'home_state.dart';

class HomeBloc extends Bloc<HomeEvent, HomeState> {
  HomeBloc({required AuthService authService})
      : _authService = authService,
        super(const HomeState()) {
    on<LogoutRequested>(_onLogoutRequested);
  }

  final AuthService _authService;

  Future<void> _onLogoutRequested(
      HomeEvent event, Emitter<HomeState> emit) async {
    await _authService.logOut();
  }
}
