part of 'departments_bloc.dart';

class DepartmentsState extends Equatable {
  final bool isLoading;
  final AplicationException? exception;
  final List<Department> department;

  const DepartmentsState({
    this.isLoading = true,
    this.exception,
    this.department = const [],
  });

  DepartmentsState copyWith({
    bool? isLoading,
    AplicationException? exception,
    List<Department>? department,
  }) {
    return DepartmentsState(
      isLoading: isLoading ?? this.isLoading,
      exception: exception ?? this.exception,
      department: department ?? this.department,
    );
  }

  @override
  List<Object?> get props => [department, isLoading, exception];
}
