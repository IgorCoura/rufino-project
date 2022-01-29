part of 'ppe_manager_bloc.dart';

abstract class PpeManagerEvent extends Equatable {
  const PpeManagerEvent();

  @override
  List<Object> get props => [];
}

class PpeManagerLoadDataEvent extends PpeManagerEvent {
  final int offset;
  PpeManagerLoadDataEvent(this.offset);
}
