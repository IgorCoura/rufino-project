part of 'company_edit_bloc.dart';

class CompanyEditState extends Equatable {
  final bool isSavingData;
  final bool isLoading;
  final AplicationException? exception;
  final String? snackMessage;
  final CompanyModel company;

  const CompanyEditState({
    this.isSavingData = false,
    this.isLoading = false,
    this.exception,
    this.snackMessage,
    this.company = const CompanyModel.empty(),
  });

  CompanyEditState copyWith({
    bool? isSavingData,
    bool? isLoading,
    AplicationException? exception,
    String? snackMessage,
    CompanyModel? company,
  }) {
    return CompanyEditState(
      isSavingData: isSavingData ?? this.isSavingData,
      isLoading: isLoading ?? this.isLoading,
      exception: exception ?? this.exception,
      snackMessage: snackMessage ?? this.snackMessage,
      company: company ?? this.company,
    );
  }

  @override
  List<Object?> get props => [
        isSavingData,
        isLoading,
        exception,
        snackMessage,
        company,
      ];
}
