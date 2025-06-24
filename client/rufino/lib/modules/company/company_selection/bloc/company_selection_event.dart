part of "company_selection_bloc.dart";

class CompanySelectionEvent extends Equatable {
  const CompanySelectionEvent();

  @override
  List<Object?> get props => [];
}

class InitialCompanyEvent extends CompanySelectionEvent {}

class SelectCompanyEvent extends CompanySelectionEvent {}

class ChangeSelectionOptionEvent extends CompanySelectionEvent {
  final String? companyId;

  const ChangeSelectionOptionEvent(this.companyId);

  @override
  List<Object?> get props => [companyId];
}
