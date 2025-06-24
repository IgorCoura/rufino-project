part of 'company_edit_bloc.dart';

sealed class CompanyEditEvent extends Equatable {
  const CompanyEditEvent();

  @override
  List<Object?> get props => [];
}

class ChangePropEvent extends CompanyEditEvent {
  final Object? value;

  const ChangePropEvent({
    this.value,
  });

  @override
  List<Object?> get props => [value];
}

class SaveChangesEvent extends CompanyEditEvent {}

class SnackMessageWasShownEvent extends CompanyEditEvent {}

class InitializeCompanyEvent extends CompanyEditEvent {
  final String? companyId;

  const InitializeCompanyEvent({
    this.companyId,
  });

  @override
  List<Object?> get props => [companyId];
}
