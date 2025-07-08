import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:rufino/modules/employee/domain/model/dependent/dependency_type.dart';
import 'package:rufino/modules/employee/domain/model/dependent/dependent.dart';
import 'package:rufino/modules/employee/domain/model/gender.dart';
import 'package:rufino/modules/employee/domain/model/name.dart';
import 'package:rufino/modules/employee/services/people_management_service.dart';
import 'package:rufino/shared/errors/aplication_errors.dart';

part 'dependents_list_component_event.dart';
part 'dependents_list_component_state.dart';

class DependentsListComponentBloc
    extends Bloc<DependentsListComponentEvent, DependentsListComponentState> {
  final PeopleManagementService _peopleManagementService;
  DependentsListComponentBloc(this._peopleManagementService)
      : super(const DependentsListComponentState()) {
    on<LazyLoadingEvent>(_onLazyLoadingEvent);
    on<ExpandInfoEvent>(_onExpandInfoEvent);
    on<InitialEvent>(_onInitialEvent);
    on<RemoveDependentEvent>(_onRemoveDependentEvent);
    on<AddDependentEvent>(_onAddDependentEvent);
    on<SaveChangesDependentEvent>(_onSaveChangesDependentEvent);
    on<SnackMessageWasShowDependentEvent>(_onSnackMessageWasShow);
  }

  Future _onInitialEvent(
      InitialEvent event, Emitter<DependentsListComponentState> emit) async {
    emit(state.copyWith(
      companyId: event.companyId,
      employeeId: event.employeeId,
    ));
  }

  Future _onExpandInfoEvent(
      ExpandInfoEvent event, Emitter<DependentsListComponentState> emit) async {
    try {
      if (state.isExpanded) {
        emit(state.copyWith(isExpanded: false));
      } else {
        emit(state.copyWith(isExpanded: true));
        if (state.dependents.isEmpty) {
          emit(state.copyWith(isLoading: true));
          var dependets = await _peopleManagementService.getEmployeeDependents(
              state.employeeId, state.companyId);

          emit(state.copyWith(dependents: dependets, isLoading: false));
        }
        if (state.genderOptions.length <= 1) {
          var genderOptions = state.genderOptions.toList();
          var resultGenderOptions =
              await _peopleManagementService.getGender(state.companyId);
          genderOptions.addAll(resultGenderOptions);
          emit(state.copyWith(genderOptions: genderOptions));
        }
        if (state.dependencyTypeOptions.length <= 1) {
          var dependencyTypeOptions = state.dependencyTypeOptions.toList();
          var resultDependencyTypeOptions =
              await _peopleManagementService.getDependencyType(state.companyId);
          dependencyTypeOptions.addAll(resultDependencyTypeOptions);
          emit(state.copyWith(dependencyTypeOptions: dependencyTypeOptions));
        }
      }
    } catch (ex, stacktrace) {
      var exception = _peopleManagementService.treatErrors(ex, stacktrace);
      emit(state.copyWith(isLoading: false, exception: exception));
    }
  }

  Future _onLazyLoadingEvent(LazyLoadingEvent event,
      Emitter<DependentsListComponentState> emit) async {
    var genderOptions =
        await _peopleManagementService.getGender(state.companyId);
    var dependecyOptions =
        await _peopleManagementService.getDependencyType(state.companyId);
    emit(state.copyWith(
        genderOptions: genderOptions, dependencyTypeOptions: dependecyOptions));
  }

  Future _onAddDependentEvent(AddDependentEvent event,
      Emitter<DependentsListComponentState> emit) async {
    var depedents = state.dependents.toList();
    depedents.add(const Dependent.newDepedent());
    emit(state.copyWith(dependents: depedents));
  }

  Future _onRemoveDependentEvent(RemoveDependentEvent event,
      Emitter<DependentsListComponentState> emit) async {
    try {
      emit(state.copyWith(isSavingData: true));
      var dependents = state.dependents.toList();

      if (dependents[event.index].idName != const Name.empty()) {
        await _peopleManagementService.removeEmployeeDependent(
          state.employeeId,
          state.companyId,
          dependents[event.index].idName.value,
        );
      }

      dependents.removeAt(event.index);

      emit(state.copyWith(isSavingData: false, dependents: dependents));
    } catch (ex, stacktrace) {
      var exception = _peopleManagementService.treatErrors(ex, stacktrace);
      emit(state.copyWith(isLoading: false, exception: exception));
    }
  }

  Future _onSaveChangesDependentEvent(SaveChangesDependentEvent event,
      Emitter<DependentsListComponentState> emit) async {
    try {
      var dependents = state.dependents.toList();

      for (var change in event.changes) {
        dependents[event.index] =
            dependents[event.index].copyWith(generic: change);
      }

      emit(state.copyWith(isSavingData: true, dependents: dependents));

      if (dependents[event.index].idName == const Name.empty()) {
        await _peopleManagementService.createEmployeeDependent(
            state.employeeId, state.companyId, dependents[event.index]);
        emit(state.copyWith(
            isSavingData: false,
            snackMessage: "Dependente criado com sucesso."));
      } else {
        await _peopleManagementService.editEmployeeDependent(
            state.employeeId, state.companyId, dependents[event.index]);
        emit(state.copyWith(
            isSavingData: false,
            snackMessage: "Dependente alterado com sucesso."));
      }
    } catch (ex, stacktrace) {
      var exception = _peopleManagementService.treatErrors(ex, stacktrace);
      emit(state.copyWith(isLoading: false, exception: exception));
    }
  }

  void _onSnackMessageWasShow(SnackMessageWasShowDependentEvent event,
      Emitter<DependentsListComponentState> emit) {
    emit(state.copyWith(snackMessage: ""));
  }
}
