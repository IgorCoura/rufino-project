import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:rufino/domain/services/company_global_service.dart';
import 'package:rufino/modules/department/domain/models/department.dart';
import 'package:rufino/modules/department/domain/services/department_service.dart';
import 'package:rufino/shared/errors/aplication_errors.dart';

part 'department_edit_event.dart';
part 'department_edit_state.dart';

class DepartmentEditBloc
    extends Bloc<DepartmentEditEvent, DepartmentEditState> {
  final DepartmentService _departmentService;
  final CompanyGlobalService _companyService;
  DepartmentEditBloc(this._departmentService, this._companyService)
      : super(DepartmentEditState()) {
    on<ChangePropEvent>(_onChangePropEvent);
    on<SaveChangesEvent>(_onSaveChangesEvent);
    on<SnackMessageWasShownEvent>(_onSnackMessageWasShownEvent);
    on<InitializeEvent>(_onInitializeEvent);
  }

  Future _onInitializeEvent(
      InitializeEvent event, Emitter<DepartmentEditState> emit) async {
    try {
      emit(state.copyWith(isLoading: true));

      if (event.id != null && event.id!.isNotEmpty) {
        final company = await _companyService.getSelectedCompany();
        final model = await _departmentService.getById(company.id, event.id!);
        emit(state.copyWith(department: model));
      } else {
        emit(state.copyWith(department: Department.empty()));
      }
      emit(state.copyWith(isLoading: false));
    } catch (ex, stacktrace) {
      var exception = _departmentService.treatErrors(ex, stacktrace);
      emit(state.copyWith(isLoading: false, exception: exception));
    }
  }

  void _onChangePropEvent(
      ChangePropEvent event, Emitter<DepartmentEditState> emit) {
    final model = state.department.copyWith(generic: event.value);
    emit(state.copyWith(
      department: model,
    ));
  }

  Future _onSaveChangesEvent(
      SaveChangesEvent event, Emitter<DepartmentEditState> emit) async {
    try {
      emit(state.copyWith(isSavingData: true));
      final company = await _companyService.getSelectedCompany();

      if (state.department.id.isEmpty) {
        await _departmentService.create(
          company.id,
          state.department,
        );
      } else {
        await _departmentService.edit(
          company.id,
          state.department,
        );
      }

      emit(state.copyWith(
          isSavingData: false,
          snackMessage: "Departamento salva com sucesso!"));
    } catch (ex, stacktrace) {
      var exception = _companyService.treatErrors(ex, stacktrace);
      emit(state.copyWith(isSavingData: false, exception: exception));
    }
  }

  void _onSnackMessageWasShownEvent(
      SnackMessageWasShownEvent event, Emitter<DepartmentEditState> emit) {
    emit(state.copyWith(snackMessage: ""));
  }
}
