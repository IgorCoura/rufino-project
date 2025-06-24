import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:rufino/domain/services/company_global_service.dart';
import 'package:rufino/modules/workplace/domain/model/workplace.dart';
import 'package:rufino/modules/workplace/domain/services/workplace_service.dart';
import 'package:rufino/shared/errors/aplication_errors.dart';

part 'workplace_edit_event.dart';
part 'workplace_edit_state.dart';

class WorkplaceEditBloc extends Bloc<WorkplaceEditEvent, WorkplaceEditState> {
  final WorkplaceService _workplaceService;
  final CompanyGlobalService _companyService;
  WorkplaceEditBloc(this._workplaceService, this._companyService)
      : super(WorkplaceEditState()) {
    on<ChangePropEvent>(_onChangePropEvent);
    on<SaveChangesEvent>(_onSaveChangesEvent);
    on<SnackMessageWasShownEvent>(_onSnackMessageWasShownEvent);
    on<InitializeWorkplaceEvent>(_onInitializeWorkplaceEvent);
  }

  Future _onInitializeWorkplaceEvent(
      InitializeWorkplaceEvent event, Emitter<WorkplaceEditState> emit) async {
    try {
      emit(state.copyWith(isLoading: true));

      if (event.workplaceId != null && event.workplaceId!.isNotEmpty) {
        final company = await _companyService.getSelectedCompany();
        final workplace =
            await _workplaceService.getById(company.id, event.workplaceId!);
        emit(state.copyWith(workplace: workplace));
      } else {
        emit(state.copyWith(workplace: Workplace.empty()));
      }
      emit(state.copyWith(isLoading: false));
    } catch (ex, stacktrace) {
      var exception = _companyService.treatErrors(ex, stacktrace);
      emit(state.copyWith(isLoading: false, exception: exception));
    }
  }

  void _onChangePropEvent(
      ChangePropEvent event, Emitter<WorkplaceEditState> emit) {
    final workplace = state.workplace.copyWith(generic: event.value);
    emit(state.copyWith(
      workplace: workplace,
    ));
  }

  Future _onSaveChangesEvent(
      SaveChangesEvent event, Emitter<WorkplaceEditState> emit) async {
    try {
      emit(state.copyWith(isSavingData: true));
      final company = await _companyService.getSelectedCompany();

      if (state.workplace.id.isEmpty) {
        await _workplaceService.create(
          company.id,
          state.workplace,
        );
      } else {
        await _workplaceService.edit(
          company.id,
          state.workplace,
        );
      }

      emit(state.copyWith(
          isSavingData: false,
          snackMessage: "Local de trabalho salva com sucesso!"));
    } catch (ex, stacktrace) {
      var exception = _companyService.treatErrors(ex, stacktrace);
      emit(state.copyWith(isSavingData: false, exception: exception));
    }
  }

  void _onSnackMessageWasShownEvent(
      SnackMessageWasShownEvent event, Emitter<WorkplaceEditState> emit) {
    emit(state.copyWith(snackMessage: ""));
  }
}
