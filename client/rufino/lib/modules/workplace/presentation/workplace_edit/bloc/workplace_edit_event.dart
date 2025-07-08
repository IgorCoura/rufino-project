part of 'workplace_edit_bloc.dart';

sealed class WorkplaceEditEvent extends Equatable {
  const WorkplaceEditEvent();

  @override
  List<Object?> get props => [];
}

class SnackMessageWasShownEvent extends WorkplaceEditEvent {
  const SnackMessageWasShownEvent();

  @override
  List<Object> get props => [];
}

class ChangePropEvent extends WorkplaceEditEvent {
  final Object? value;

  const ChangePropEvent({
    this.value,
  });

  @override
  List<Object?> get props => [value];
}

class SaveChangesEvent extends WorkplaceEditEvent {}

class InitializeWorkplaceEvent extends WorkplaceEditEvent {
  final String? workplaceId;

  const InitializeWorkplaceEvent(
    this.workplaceId,
  );

  @override
  List<Object?> get props => [workplaceId];
}
