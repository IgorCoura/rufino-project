import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:flutter_modular/flutter_modular.dart';

part 'login_event.dart';
part 'login_state.dart';

class LoginBloc extends Bloc<LoginEvent, LoginState> {
  bool _rememberMe = false;

  LoginBloc() : super(LoginIdleState()) {
    on<LoginSendEvent>((event, emit) async {
      emit(LoginLoadingState());
      //await Future.delayed(Duration(seconds: 2));
      if (event.cpf == "123" && event.password == "123" || true) {
        Modular.to.navigate("/home");
        emit(LoginIdleState());
      } else {
        emit(LoginErrorState("cpf ou senha estão errados"));
      }
    });

    on<LoginVerifiedSessionEvent>((event, emit) async {
      emit(LoginLoadingState());

      emit(LoginIdleState());
      if (false) {
        Modular.to.navigate("/home");
      }
    });

    on<LoginSelectRememberMeEvent>((event, emit) async {
      _rememberMe = !_rememberMe;
    });
  }
}
