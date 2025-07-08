part of 'dependents_list_component_bloc.dart';

class DependentsListComponentState extends Equatable {
  final String companyId;
  final String employeeId;
  final List<Dependent> dependents;
  final bool isLoading;
  final bool isLazyLoading;
  final bool isSavingData;
  final List<Gender> genderOptions;
  final List<DependencyType> dependencyTypeOptions;
  final bool isExpanded;
  final String? snackMessage;
  final AplicationException? exception;

  const DependentsListComponentState(
      {this.companyId = "",
      this.employeeId = "",
      this.dependents = const [],
      this.genderOptions = const [Gender.empty()],
      this.dependencyTypeOptions = const [DependencyType.empty()],
      this.isLoading = false,
      this.isLazyLoading = false,
      this.isSavingData = false,
      this.isExpanded = false,
      this.snackMessage = "",
      this.exception});

  DependentsListComponentState copyWith({
    String? companyId,
    String? employeeId,
    List<Dependent>? dependents,
    bool? isLoading,
    bool? isLazyLoading,
    bool? isSavingData,
    List<Gender>? genderOptions,
    List<DependencyType>? dependencyTypeOptions,
    bool? isExpanded,
    String? snackMessage,
    AplicationException? exception,
  }) =>
      DependentsListComponentState(
        companyId: companyId ?? this.companyId,
        employeeId: employeeId ?? this.employeeId,
        dependents: dependents ?? this.dependents,
        isLoading: isLoading ?? this.isLoading,
        isLazyLoading: isLazyLoading ?? this.isLazyLoading,
        isSavingData: isSavingData ?? this.isSavingData,
        genderOptions: genderOptions ?? this.genderOptions,
        dependencyTypeOptions:
            dependencyTypeOptions ?? this.dependencyTypeOptions,
        isExpanded: isExpanded ?? this.isExpanded,
        snackMessage: snackMessage ?? this.snackMessage,
        exception: exception ?? this.exception,
      );

  @override
  List<Object?> get props => [
        companyId,
        employeeId,
        dependents.hashCode,
        isLoading,
        isLazyLoading,
        isSavingData,
        genderOptions.hashCode,
        dependencyTypeOptions.hashCode,
        isExpanded,
        snackMessage,
        exception,
      ];
}
