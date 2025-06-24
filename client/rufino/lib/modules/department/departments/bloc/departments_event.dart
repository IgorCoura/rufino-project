part of 'departments_bloc.dart';

sealed class DepartmentsEvent extends Equatable {
  const DepartmentsEvent();

  @override
  List<Object> get props => [];
}

class LoadEvent extends DepartmentsEvent {
  const LoadEvent();

  @override
  List<Object> get props => [];
}
