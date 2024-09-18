import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:rufino/modules/auth/domain/enums/login_status.dart';
import 'package:rufino/modules/auth/domain/models/password.dart';
import 'package:rufino/modules/auth/domain/models/username.dart';
import 'package:rufino/modules/auth/domain/services/auth_service.dart';

part 'login_event.dart';
part 'login_state.dart';

class LoginBloc extends Bloc<LoginEvent, LoginState> {
  LoginBloc({
    required AuthenticationService authenticationRepository,
  })  : _authenticationRepository = authenticationRepository,
        super(const LoginState()) {
    on<LoginUsernameChanged>(_onUsernameChanged);
    on<LoginPasswordChanged>(_onPasswordChanged);
    on<LoginSubmitted>(_onSubmitted);
  }

  final AuthenticationService _authenticationRepository;

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
      var result = await _authenticationRepository.logIn(
        username: state.username.value,
        password: state.password.value,
      );
      if (result) {
        emit(state.copyWith(status: LoginStatus.success));
      } else {
        emit(state.copyWith(status: LoginStatus.failure));
      }
    } catch (_) {
      emit(state.copyWith(status: LoginStatus.failure));
    }
  }
}
