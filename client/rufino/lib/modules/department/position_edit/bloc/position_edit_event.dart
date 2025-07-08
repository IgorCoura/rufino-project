part of 'position_edit_bloc.dart';

sealed class PositionEditEvent extends Equatable {
  const PositionEditEvent();

  @override
  List<Object?> get props => [];
}

class SnackMessageWasShownEvent extends PositionEditEvent {
  const SnackMessageWasShownEvent();

  @override
  List<Object> get props => [];
}

class ChangePropEvent extends PositionEditEvent {
  final Object? value;

  const ChangePropEvent({
    this.value,
  });

  @override
  List<Object?> get props => [value];
}

class SaveChangesEvent extends PositionEditEvent {}

class InitializeEvent extends PositionEditEvent {
  final String? id;
  final String? departmentId;
  const InitializeEvent(
    this.id,
    this.departmentId,
  );

  @override
  List<Object?> get props => [id, departmentId];
}
