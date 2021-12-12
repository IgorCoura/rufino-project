import 'package:equatable/equatable.dart';

class LoginEvent extends Equatable {
  @override
  List<Object?> get props => [];
}

class LoginVerifiedSessionEvent extends LoginEvent {}

class LoginSendEvent extends LoginEvent {
  final String cpf;
  final String password;

  LoginSendEvent(this.cpf, this.password);
}

class LoginCheckBoxEvent extends LoginEvent {}

class LoginSelectRememberMeEvent extends LoginEvent {}
