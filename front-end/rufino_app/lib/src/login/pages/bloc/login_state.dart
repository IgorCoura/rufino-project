part of 'login_bloc.dart';

class LoginState extends Equatable {
  @override
  List<Object?> get props => [];
}

class LoginIdleState extends LoginState {}

class LoginLoadingState extends LoginState {}

class LoginSecondState extends LoginState {}

class LoginErrorState extends LoginState {
  String message = "";
  LoginErrorState(this.message);
}
