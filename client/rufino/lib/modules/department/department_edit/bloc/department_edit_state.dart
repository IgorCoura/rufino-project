part of 'department_edit_bloc.dart';

class DepartmentEditState extends Equatable {
  final bool isSavingData;
  final bool isLoading;
  final AplicationException? exception;
  final String? snackMessage;
  final Department department;

  const DepartmentEditState({
    this.isSavingData = false,
    this.isLoading = false,
    this.exception,
    this.snackMessage,
    this.department = const Department.empty(),
  });

  DepartmentEditState copyWith({
    bool? isSavingData,
    bool? isLoading,
    AplicationException? exception,
    String? snackMessage,
    Department? department,
  }) {
    return DepartmentEditState(
      isSavingData: isSavingData ?? this.isSavingData,
      isLoading: isLoading ?? this.isLoading,
      exception: exception ?? this.exception,
      snackMessage: snackMessage ?? this.snackMessage,
      department: department ?? this.department,
    );
  }

  @override
  List<Object?> get props => [
        isSavingData,
        isLoading,
        exception,
        snackMessage,
        department,
      ];
}
