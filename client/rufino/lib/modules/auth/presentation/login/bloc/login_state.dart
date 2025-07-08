part of 'login_bloc.dart';

final class LoginState extends Equatable {
  const LoginState({
    this.status = LoginStatus.initial,
    this.username = Username.empty,
    this.password = Password.empty,
  });

  final LoginStatus status;
  final Username username;
  final Password password;

  LoginState copyWith({
    LoginStatus? status,
    Username? username,
    Password? password,
  }) {
    return LoginState(
      status: status ?? this.status,
      username: username ?? this.username,
      password: password ?? this.password,
    );
  }

  @override
  List<Object> get props => [status, username, password];
}
