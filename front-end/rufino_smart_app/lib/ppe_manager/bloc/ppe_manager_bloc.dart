import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:rufino_smart_app/ppe_manager/model/worker_model.dart';
import 'package:rufino_smart_app/ppe_manager/repositories/ppe_manager_repository.dart';

part 'ppe_manager_event.dart';
part 'ppe_manager_state.dart';

class PpeManagerBloc extends Bloc<PpeManagerEvent, PpeManagerState> {
  List<WorkerModel> workerList = [];
  int offset = 0;
  PpeManagerBloc() : super(PpeManagerInitial()) {
    on<PpeManagerLoadDataEvent>((event, emit) async {
      var repository = PpeManagerRepository();
      var data = await repository.getWorkers(offset + event.offset, 10);
      if (data.isNotEmpty) {
        workerList.addAll(data);
        emit(PpeManagerInitial());
        emit(PpeManagerHasData(workerList));
      } else {
        emit(PpeManagerErroState("Não foi possivel carregar os dados"));
      }
    });
  }
}
