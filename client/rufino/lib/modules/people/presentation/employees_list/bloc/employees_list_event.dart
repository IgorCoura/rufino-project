part of 'employees_list_bloc.dart';

class EmployeesListEvent extends Equatable {
  @override
  List<Object?> get props => [];
}

class InitialEmployeesListEvent extends EmployeesListEvent {}

class ChangeSearchParam extends EmployeesListEvent {
  final int? paramSelected;

  ChangeSearchParam(this.paramSelected);

  @override
  List<Object?> get props => [paramSelected];
}

class ChangeSearchInput extends EmployeesListEvent {
  final String input;

  ChangeSearchInput(this.input);
  @override
  List<Object?> get props => [input];
}

class SearchEditComplet extends EmployeesListEvent {}

class ChangeStatusSelect extends EmployeesListEvent {
  final int? selection;

  ChangeStatusSelect(this.selection);

  @override
  List<Object?> get props => [selection];
}

class ChangeSortList extends EmployeesListEvent {}
