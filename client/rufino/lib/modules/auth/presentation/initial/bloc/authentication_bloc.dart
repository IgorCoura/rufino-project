import 'dart:async';

import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:rufino/domain/enum/auth_status.dart';
import 'package:rufino/domain/services/auth_service.dart';

part 'authentication_event.dart';
part 'authentication_state.dart';

class AuthenticationBloc
    extends Bloc<AuthenticationEvent, AuthenticationState> {
  AuthenticationBloc({required AuthService authService})
      : _authService = authService,
        super(const AuthenticationState.unknown()) {
    on<AuthenticationSubscriptionRequested>(_onSubscriptionRequested);
  }

  final AuthService _authService;
  Future<void> _onSubscriptionRequested(
    AuthenticationSubscriptionRequested event,
    Emitter<AuthenticationState> emit,
  ) async {
    try {
      await _authService.getCredentials();
      emit(const AuthenticationState.authenticated());
    } catch (ex) {
      emit(const AuthenticationState.unauthenticated());
    }
  }
}
