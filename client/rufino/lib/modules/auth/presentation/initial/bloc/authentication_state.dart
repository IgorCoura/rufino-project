part of 'authentication_bloc.dart';

class AuthenticationState extends Equatable {
  const AuthenticationState._(
      {this.status = AuthStatus.unknown, this.exception});

  const AuthenticationState.unknown() : this._();

  const AuthenticationState.authenticated()
      : this._(status: AuthStatus.authenticated);

  const AuthenticationState.unSelectedCompany()
      : this._(status: AuthStatus.unSelectedCompany);

  const AuthenticationState.unauthenticated()
      : this._(status: AuthStatus.unauthenticated);

  const AuthenticationState.exception(AplicationException exception)
      : this._(status: AuthStatus.failure, exception: exception);

  final AuthStatus status;
  final AplicationException? exception;
  @override
  List<Object> get props => [status];
}
