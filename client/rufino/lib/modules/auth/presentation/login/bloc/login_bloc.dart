import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:rufino/domain/services/auth_service.dart';
import 'package:rufino/modules/auth/domain/enums/login_status.dart';
import 'package:rufino/modules/auth/domain/models/password.dart';
import 'package:rufino/modules/auth/domain/models/username.dart';

part 'login_event.dart';
part 'login_state.dart';

class LoginBloc extends Bloc<LoginEvent, LoginState> {
  LoginBloc({
    required AuthService authService,
  })  : _authService = authService,
        super(const LoginState()) {
    on<LoginUsernameChanged>(_onUsernameChanged);
    on<LoginPasswordChanged>(_onPasswordChanged);
    on<LoginSubmitted>(_onSubmitted);
  }

  final AuthService _authService;

  void _onUsernameChanged(
    LoginUsernameChanged event,
    Emitter<LoginState> emit,
  ) {
    final username = Username(event.username);
    emit(
      state.copyWith(username: username),
    );
  }

  void _onPasswordChanged(
    LoginPasswordChanged event,
    Emitter<LoginState> emit,
  ) {
    final password = Password(event.password);
    emit(
      state.copyWith(password: password),
    );
  }

  Future<void> _onSubmitted(
    LoginSubmitted event,
    Emitter<LoginState> emit,
  ) async {
    emit(state.copyWith(status: LoginStatus.inProgress));
    try {
      await _authService.logIn(
        username: state.username.value,
        password: state.password.value,
      );
      emit(state.copyWith(status: LoginStatus.success));
    } catch (_) {
      emit(state.copyWith(status: LoginStatus.failure));
    }
  }
}
