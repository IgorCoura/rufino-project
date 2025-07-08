part of 'dependents_list_component_bloc.dart';

sealed class DependentsListComponentEvent extends Equatable {
  const DependentsListComponentEvent();

  @override
  List<Object> get props => [];
}

class InitialEvent extends DependentsListComponentEvent {
  final String companyId;
  final String employeeId;

  const InitialEvent(this.companyId, this.employeeId);

  @override
  List<Object> get props => [companyId, employeeId];
}

class ExpandInfoEvent extends DependentsListComponentEvent {}

class LazyLoadingEvent extends DependentsListComponentEvent {}

class AddDependentEvent extends DependentsListComponentEvent {}

class RemoveDependentEvent extends DependentsListComponentEvent {
  final int index;

  const RemoveDependentEvent(this.index);

  @override
  List<Object> get props => [index];
}

class SaveChangesDependentEvent extends DependentsListComponentEvent {
  final int index;
  final List<Object> changes;

  const SaveChangesDependentEvent(this.index, this.changes);

  @override
  List<Object> get props => [index, changes];
}

class SnackMessageWasShowDependentEvent extends DependentsListComponentEvent {}
