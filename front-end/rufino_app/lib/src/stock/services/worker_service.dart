import 'package:connectivity_plus/connectivity_plus.dart';
import 'package:rufino_app/src/stock/db/dao/worker_dao.dart';
import 'package:rufino_app/src/stock/db/stock_db.dart';
import 'package:rufino_app/src/stock/repository/worker_repository.dart';
import 'package:rufino_app/src/stock/utils/stock_constants.dart';
import 'package:shared_preferences/shared_preferences.dart';

class WorkerService {
  final WorkerRepository _workerRepository;
  final WorkerDao _workerDao;

  WorkerService(this._workerRepository, this._workerDao);

  Future<List<Worker>> getAll() async {
    await syncWorkerWithServer();
    return _workerDao.getAll();
  }

  Future<void> syncWorkerWithServer([DateTime? modificationDate]) async {
    var sharedPreferences = await SharedPreferences.getInstance();
    modificationDate ??= DateTime.parse(
        sharedPreferences.getString("workerModificationDate") ?? kDateDefault);
    var connectivityResult = await (Connectivity().checkConnectivity());
    if (connectivityResult == ConnectivityResult.ethernet ||
        connectivityResult == ConnectivityResult.wifi) {
      try {
        var worker = await _workerRepository.getAllWorkers(modificationDate);
        _workerDao.updateOrAdd(worker);
        sharedPreferences.setString(
            "workerModificationDate", DateTime.now().toUtc().toString());
      } catch (e) {
        //TODO: Criar um log para erros de server;
      }
    }
  }
}
