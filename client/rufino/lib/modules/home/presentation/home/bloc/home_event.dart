part of 'home_bloc.dart';

sealed class HomeEvent {
  const HomeEvent();
}

final class InitialHomeEvent extends HomeEvent {}

final class LogoutRequested extends HomeEvent {}
