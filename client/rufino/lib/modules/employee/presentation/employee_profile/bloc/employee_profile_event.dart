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

class SaveContactChanges extends EmployeeProfileEvent {
  final List<Object> changes;

  const SaveContactChanges(this.changes);

  @override
  List<Object> get props => [changes];
}

class LoadingAddressEvent extends EmployeeProfileEvent {}

class SaveAddressEvent extends EmployeeProfileEvent {
  final List<Object> changes;

  const SaveAddressEvent(this.changes);

  @override
  List<Object> get props => [changes];
}

class LoadingPersonalInfoEvent extends EmployeeProfileEvent {}

class SavePersonalInfoEvent extends EmployeeProfileEvent {
  final List<Object> changes;

  const SavePersonalInfoEvent(this.changes);

  @override
  List<Object> get props => [changes];
}

class LazyLoadingPersonalInfoEvent extends EmployeeProfileEvent {}

class LoadingIdCardEvent extends EmployeeProfileEvent {}

class SaveIdCardEvent extends EmployeeProfileEvent {
  final List<Object> changes;

  const SaveIdCardEvent(this.changes);

  @override
  List<Object> get props => [changes];
}
