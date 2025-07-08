import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:rufino/domain/services/company_global_service.dart';
import 'package:rufino/modules/department/domain/models/remuneration.dart';
import 'package:rufino/modules/department/domain/models/role.dart';
import 'package:rufino/modules/department/domain/services/role_service.dart';
import 'package:rufino/shared/errors/aplication_errors.dart';

part 'role_edit_event.dart';
part 'role_edit_state.dart';

class RoleEditBloc extends Bloc<RoleEditEvent, RoleEditState> {
  final RoleService _roleService;
  final CompanyGlobalService _companyService;

  RoleEditBloc(this._roleService, this._companyService)
      : super(RoleEditState()) {
    on<ChangePropEvent>(_onChangePropEvent);
    on<SaveChangesEvent>(_onSaveChangesEvent);
    on<SnackMessageWasShownEvent>(_onSnackMessageWasShownEvent);
    on<InitializeEvent>(_onInitializeEvent);
  }

  Future _onInitializeEvent(
      InitializeEvent event, Emitter<RoleEditState> emit) async {
    try {
      emit(state.copyWith(isLoading: true, positionId: event.departmentId));

      if (event.id != null && event.id!.isNotEmpty) {
        final company = await _companyService.getSelectedCompany();
        final model = await _roleService.getById(company.id, event.id!);
        emit(state.copyWith(role: model));
      } else {
        emit(state.copyWith(role: Role.empty()));
      }
      emit(state.copyWith(isLoading: false));
    } catch (ex, stacktrace) {
      var exception = _roleService.treatErrors(ex, stacktrace);
      emit(state.copyWith(isLoading: false, exception: exception));
    }
  }

  void _onChangePropEvent(ChangePropEvent event, Emitter<RoleEditState> emit) {
    final model = state.role.copyWith(generic: event.value);
    emit(state.copyWith(
      role: model,
    ));
  }

  Future _onSaveChangesEvent(
      SaveChangesEvent event, Emitter<RoleEditState> emit) async {
    try {
      emit(state.copyWith(isSavingData: true));
      final company = await _companyService.getSelectedCompany();

      if (state.role.id.isEmpty) {
        await _roleService.create(
          company.id,
          state.positionId,
          state.role,
        );
      } else {
        await _roleService.edit(
          company.id,
          state.role,
        );
      }

      emit(state.copyWith(
          isSavingData: false, snackMessage: "Função salva com sucesso!"));
    } catch (ex, stacktrace) {
      var exception = _companyService.treatErrors(ex, stacktrace);
      emit(state.copyWith(isSavingData: false, exception: exception));
    }
  }

  void _onSnackMessageWasShownEvent(
      SnackMessageWasShownEvent event, Emitter<RoleEditState> emit) {
    emit(state.copyWith(snackMessage: ""));
  }
}
