part of 'department_edit_bloc.dart';

sealed class DepartmentEditEvent extends Equatable {
  const DepartmentEditEvent();

  @override
  List<Object?> get props => [];
}

class SnackMessageWasShownEvent extends DepartmentEditEvent {
  const SnackMessageWasShownEvent();

  @override
  List<Object> get props => [];
}

class ChangePropEvent extends DepartmentEditEvent {
  final Object? value;

  const ChangePropEvent({
    this.value,
  });

  @override
  List<Object?> get props => [value];
}

class SaveChangesEvent extends DepartmentEditEvent {}

class InitializeEvent extends DepartmentEditEvent {
  final String? id;

  const InitializeEvent(
    this.id,
  );

  @override
  List<Object?> get props => [id];
}
