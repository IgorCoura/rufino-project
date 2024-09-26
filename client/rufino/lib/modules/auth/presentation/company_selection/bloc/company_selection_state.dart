part of 'company_selection_bloc.dart';

class CompanySelectionState extends Equatable {
  final List<Company> companies;
  final bool hasSelectedCompany;
  final bool isLoading;
  final String? selectedCompany;

  const CompanySelectionState(
      {required this.companies,
      this.selectedCompany,
      this.isLoading = true,
      this.hasSelectedCompany = false});

  CompanySelectionState copyWith(
      {List<Company>? companies,
      String? selectedCompany,
      bool? hasSelectedCompany,
      bool? isLoading}) {
    return CompanySelectionState(
        companies: companies ?? this.companies,
        hasSelectedCompany: hasSelectedCompany ?? this.hasSelectedCompany,
        isLoading: isLoading ?? this.isLoading,
        selectedCompany: selectedCompany ?? this.selectedCompany);
  }

  @override
  List<Object?> get props =>
      [companies, hasSelectedCompany, isLoading, selectedCompany];
}
