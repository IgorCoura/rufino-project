part of 'employee_profile_bloc.dart';

sealed class EmployeeProfileEvent extends Equatable {
  const EmployeeProfileEvent();

  @override
  List<Object> get props => [];
}

class InitialEmployeeProfileEvent extends EmployeeProfileEvent {
  final String employeeId;

  const InitialEmployeeProfileEvent(this.employeeId);

  @override
  List<Object> get props => [employeeId];
}

class EditNameEvent extends EmployeeProfileEvent {
  final bool edit;

  const EditNameEvent(this.edit);

  @override
  List<Object> get props => [edit];
}

class SaveNewNameEvent extends EmployeeProfileEvent {}

class ChangeNameEvent extends EmployeeProfileEvent {
  final String name;

  const ChangeNameEvent(this.name);

  @override
  List<Object> get props => [name];
}

class LoadingContactEvent extends EmployeeProfileEvent {}

class SnackMessageWasShow extends EmployeeProfileEvent {}

class SaveContactChanges extends EmployeeProfileEvent {}

class LoadingAddressEvent extends EmployeeProfileEvent {}

class SaveAddressEvent extends EmployeeProfileEvent {}

class LoadingPersonalInfoEvent extends EmployeeProfileEvent {}

class SavePersonalInfoEvent extends EmployeeProfileEvent {
  final List<Object> changes;

  const SavePersonalInfoEvent(this.changes);

  @override
  List<Object> get props => [changes];
}

class RemoveDisabilityFromPersonalInfoEvent extends EmployeeProfileEvent {
  final int id;

  const RemoveDisabilityFromPersonalInfoEvent(this.id);

  @override
  List<Object> get props => [id];
}

class LazyLoadingPersonalInfoEvent extends EmployeeProfileEvent {}
