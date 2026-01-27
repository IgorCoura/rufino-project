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

class ChangeNameNewEmployee extends EmployeesListEvent {
  final String name;

  ChangeNameNewEmployee(this.name);

  @override
  List<Object?> get props => [name];
}

class CreateNewEmployee extends EmployeesListEvent {}

class ErrorEvent extends EmployeesListEvent {
  final AplicationException exception;

  ErrorEvent(this.exception);
  @override
  List<Object?> get props => [exception];
}

class FeatchNextPage extends EmployeesListEvent {}

class RefreshPage extends EmployeesListEvent {}

class LoadInfoToCreateEmployee extends EmployeesListEvent {}

class ChangeDepartment extends EmployeesListEvent {
  final Department department;

  ChangeDepartment(this.department);

  @override
  List<Object?> get props => [department];
}

class ChangePosition extends EmployeesListEvent {
  final Position position;

  ChangePosition(this.position);

  @override
  List<Object?> get props => [position];
}

class ChangeRole extends EmployeesListEvent {
  final Role role;

  ChangeRole(this.role);

  @override
  List<Object?> get props => [role];
}

class ChangeWorkplace extends EmployeesListEvent {
  final Workplace workplace;

  ChangeWorkplace(this.workplace);

  @override
  List<Object?> get props => [workplace];
}

class LoadSingleEmployeeImage extends EmployeesListEvent {
  final String employeeId;

  LoadSingleEmployeeImage(this.employeeId);

  @override
  List<Object?> get props => [employeeId];
}
