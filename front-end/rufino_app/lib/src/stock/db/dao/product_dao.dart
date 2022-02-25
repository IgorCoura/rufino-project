import 'package:drift/drift.dart';
import 'package:rufino_app/src/stock/db/stock_db.dart';

part 'product_dao.g.dart';

@DriftAccessor(tables: [Products])
class ProductDao extends DatabaseAccessor<StockDb> with _$ProductDaoMixin {
  ProductDao(StockDb db) : super(db);

  Future<void> add(Product entry) {
    return into(products).insert(entry);
  }

  Future<void> updateOrAdd(List<Product> entry) {
    return batch((batch) {
      batch.insertAllOnConflictUpdate(products, entry);
    });
  }

  Stream<List<Product>> getAll(String searchString) {
    var search = "%$searchString%";
    return (select(products)..where((tbl) => tbl.name.like(search))).watch();
  }
}
