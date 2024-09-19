part of 'authentication_bloc.dart';

sealed class AuthenticationEvent {
  const AuthenticationEvent();
}

final class AuthenticationSubscriptionRequested extends AuthenticationEvent {}
