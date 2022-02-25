import 'package:drift/drift.dart';
import 'package:rufino_app/src/stock/db/stock_db.dart';

part 'worker_dao.g.dart';

@DriftAccessor(tables: [Workers])
class WorkerDao extends DatabaseAccessor<StockDb> with _$WorkerDaoMixin {
  WorkerDao(StockDb db) : super(db);

  Future<List<Worker>> getAll() {
    return (select(workers)).get();
  }

  Future<void> updateOrAdd(List<Worker> entry) {
    return batch((batch) {
      batch.insertAllOnConflictUpdate(workers, entry);
    });
  }
}
