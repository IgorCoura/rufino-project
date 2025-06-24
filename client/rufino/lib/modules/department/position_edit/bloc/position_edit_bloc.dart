import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:rufino/domain/services/company_global_service.dart';
import 'package:rufino/modules/department/domain/models/position.dart';
import 'package:rufino/modules/department/domain/services/position_service.dart';
import 'package:rufino/shared/errors/aplication_errors.dart';

part 'position_edit_event.dart';
part 'position_edit_state.dart';

class PositionEditBloc extends Bloc<PositionEditEvent, PositionEditState> {
  final PositionService _positionService;
  final CompanyGlobalService _companyService;

  PositionEditBloc(this._positionService, this._companyService)
      : super(PositionEditState()) {
    on<ChangePropEvent>(_onChangePropEvent);
    on<SaveChangesEvent>(_onSaveChangesEvent);
    on<SnackMessageWasShownEvent>(_onSnackMessageWasShownEvent);
    on<InitializeEvent>(_onInitializeEvent);
  }

  Future _onInitializeEvent(
      InitializeEvent event, Emitter<PositionEditState> emit) async {
    try {
      emit(state.copyWith(isLoading: true, departmentId: event.departmentId));

      if (event.id != null && event.id!.isNotEmpty) {
        final company = await _companyService.getSelectedCompany();
        final model = await _positionService.getById(company.id, event.id!);
        emit(state.copyWith(position: model));
      } else {
        emit(state.copyWith(position: Position.empty()));
      }
      emit(state.copyWith(isLoading: false));
    } catch (ex, stacktrace) {
      var exception = _positionService.treatErrors(ex, stacktrace);
      emit(state.copyWith(isLoading: false, exception: exception));
    }
  }

  void _onChangePropEvent(
      ChangePropEvent event, Emitter<PositionEditState> emit) {
    final model = state.position.copyWith(generic: event.value);
    emit(state.copyWith(
      position: model,
    ));
  }

  Future _onSaveChangesEvent(
      SaveChangesEvent event, Emitter<PositionEditState> emit) async {
    try {
      emit(state.copyWith(isSavingData: true));
      final company = await _companyService.getSelectedCompany();

      if (state.position.id.isEmpty) {
        await _positionService.create(
          company.id,
          state.departmentId,
          state.position,
        );
      } else {
        await _positionService.edit(
          company.id,
          state.position,
        );
      }

      emit(state.copyWith(
          isSavingData: false, snackMessage: "Cargo salva com sucesso!"));
    } catch (ex, stacktrace) {
      var exception = _companyService.treatErrors(ex, stacktrace);
      emit(state.copyWith(isSavingData: false, exception: exception));
    }
  }

  void _onSnackMessageWasShownEvent(
      SnackMessageWasShownEvent event, Emitter<PositionEditState> emit) {
    emit(state.copyWith(snackMessage: ""));
  }
}
