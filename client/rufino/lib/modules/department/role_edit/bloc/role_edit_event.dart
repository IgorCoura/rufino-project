part of 'role_edit_bloc.dart';

sealed class RoleEditEvent extends Equatable {
  const RoleEditEvent();

  @override
  List<Object?> get props => [];
}

class SnackMessageWasShownEvent extends RoleEditEvent {
  const SnackMessageWasShownEvent();

  @override
  List<Object> get props => [];
}

class ChangePropEvent extends RoleEditEvent {
  final Object? value;

  const ChangePropEvent({
    this.value,
  });

  @override
  List<Object?> get props => [value];
}

class SaveChangesEvent extends RoleEditEvent {}

class InitializeEvent extends RoleEditEvent {
  final String? id;
  final String? departmentId;
  const InitializeEvent(
    this.id,
    this.departmentId,
  );

  @override
  List<Object?> get props => [id, departmentId];
}
