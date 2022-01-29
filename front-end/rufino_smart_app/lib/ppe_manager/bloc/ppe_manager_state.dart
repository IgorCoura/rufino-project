part of 'ppe_manager_bloc.dart';

abstract class PpeManagerState extends Equatable {
  const PpeManagerState();

  @override
  List<Object> get props => [];
}

class PpeManagerInitial extends PpeManagerState {}

class PpeManagerHasData extends PpeManagerState {
  List<WorkerModel> workers = [];
  PpeManagerHasData(this.workers);
}

class PpeManagerErroState extends PpeManagerState {
  String message = "";
  PpeManagerErroState(this.message);
}
