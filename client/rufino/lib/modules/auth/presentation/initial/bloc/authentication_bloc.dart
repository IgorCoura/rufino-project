import 'dart:async';

import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:rufino/domain/enum/auth_status.dart';
import 'package:rufino/domain/services/auth_service.dart';
import 'package:rufino/domain/services/company_service.dart';
import 'package:rufino/shared/errors/aplication_errors.dart';

part 'authentication_event.dart';
part 'authentication_state.dart';

class AuthenticationBloc
    extends Bloc<AuthenticationEvent, AuthenticationState> {
  AuthenticationBloc(
      {required AuthService authService,
      required CompanyService companyService})
      : _authService = authService,
        _companyService = companyService,
        super(const AuthenticationState.unknown()) {
    on<AuthenticationSubscriptionRequested>(_onSubscriptionRequested);
  }

  final AuthService _authService;
  final CompanyService _companyService;

  Future<void> _onSubscriptionRequested(
    AuthenticationSubscriptionRequested event,
    Emitter<AuthenticationState> emit,
  ) async {
    try {
      emit(const AuthenticationState.unknown());
      await _authService.getCredentials();
      var hasCompanySeleted = await _companyService.hasCompanySeleted();

      if (hasCompanySeleted) {
        emit(const AuthenticationState.authenticated());
        return;
      }
      emit(const AuthenticationState.unSelectedCompany());
    } catch (ex) {
      emit(const AuthenticationState.unauthenticated());
    }
  }
}
