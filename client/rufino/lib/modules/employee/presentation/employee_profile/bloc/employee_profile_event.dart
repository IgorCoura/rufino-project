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

class EditDocumentSigningOptionsEvent extends EmployeeProfileEvent {
  final bool edit;

  const EditDocumentSigningOptionsEvent(this.edit);

  @override
  List<Object> get props => [edit];
}

class SaveDocumentSigningOptionsEvent extends EmployeeProfileEvent {}

class ChangeDocumentSigningOptionsEvent extends EmployeeProfileEvent {
  final DocumentSigningOptions option;

  const ChangeDocumentSigningOptionsEvent(this.option);

  @override
  List<Object> get props => [option];
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

class LoadingVoteIdEvent extends EmployeeProfileEvent {}

class SaveVoteIdEvent extends EmployeeProfileEvent {
  final List<Object> changes;

  const SaveVoteIdEvent(this.changes);

  @override
  List<Object> get props => [changes];
}

class LoadingMilitaryDocumentEvent extends EmployeeProfileEvent {}

class SaveMilitaryDocumentEvent extends EmployeeProfileEvent {
  final List<Object> changes;

  const SaveMilitaryDocumentEvent(this.changes);

  @override
  List<Object> get props => [changes];
}

class LoadingMedicalAdmissionExamEvent extends EmployeeProfileEvent {}

class SaveMedicalAdmissionExamEvent extends EmployeeProfileEvent {
  final List<Object> changes;

  const SaveMedicalAdmissionExamEvent(this.changes);

  @override
  List<Object> get props => [changes];
}

class LoadingRoleEvent extends EmployeeProfileEvent {}

class LazyLoadingRoleInfoEvent extends EmployeeProfileEvent {}

class ChangeRoleInfoEvent extends EmployeeProfileEvent {
  final Object change;

  const ChangeRoleInfoEvent(this.change);
  @override
  List<Object> get props => [change];
}

class SaveRoleInfoEvent extends EmployeeProfileEvent {}

class LoadingWorkplaceEvent extends EmployeeProfileEvent {}

class LazyLoadingWorkplaceEvent extends EmployeeProfileEvent {}

class ChangeWorkplaceEvent extends EmployeeProfileEvent {
  final Object change;

  const ChangeWorkplaceEvent(this.change);
  @override
  List<Object> get props => [change];
}

class SaveWorkplaceEvent extends EmployeeProfileEvent {}

class LoadingContractsEvent extends EmployeeProfileEvent {}

class FinishedContractEvent extends EmployeeProfileEvent {
  final String finalDate;

  const FinishedContractEvent(this.finalDate);
  @override
  List<Object> get props => [finalDate];
}

class NewContractEvent extends EmployeeProfileEvent {
  final String initDate;
  final String contractTypeId;
  final String registration;

  const NewContractEvent(this.initDate, this.contractTypeId, this.registration);
  @override
  List<Object> get props => [initDate, contractTypeId, registration];
}
