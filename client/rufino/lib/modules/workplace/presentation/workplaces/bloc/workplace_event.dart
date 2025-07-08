part of 'workplace_bloc.dart';

sealed class WorkplaceEvent extends Equatable {
  const WorkplaceEvent();

  @override
  List<Object> get props => [];
}

class WorkplaceLoadEvent extends WorkplaceEvent {
  const WorkplaceLoadEvent();

  @override
  List<Object> get props => [];
}
