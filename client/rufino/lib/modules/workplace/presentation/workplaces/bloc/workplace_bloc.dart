import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:rufino/domain/services/company_global_service.dart';
import 'package:rufino/modules/workplace/domain/model/workplace.dart';
import 'package:rufino/modules/workplace/domain/services/workplace_service.dart';
import 'package:rufino/shared/errors/aplication_errors.dart';

part 'workplace_event.dart';
part 'workplace_state.dart';

class WorkplaceBloc extends Bloc<WorkplaceEvent, WorkplaceState> {
  final WorkplaceService _workplaceService;
  final CompanyGlobalService _companyService;

  WorkplaceBloc(this._workplaceService, this._companyService)
      : super(WorkplaceState()) {
    on<WorkplaceLoadEvent>(_onWorkplaceLoadEvent);
  }

  Future _onWorkplaceLoadEvent(
      WorkplaceLoadEvent event, Emitter<WorkplaceState> emit) async {
    try {
      emit(state.copyWith(isLoading: true));

      final company = await _companyService.getSelectedCompany();
      final workplaces = await _workplaceService.getAll(
        company.id,
      );

      emit(state.copyWith(
        isLoading: false,
        workplace: workplaces,
      ));
    } catch (ex, stacktrace) {
      var exception = _workplaceService.treatErrors(ex, stacktrace);
      emit(state.copyWith(isLoading: false, exception: exception));
    }
  }
}
