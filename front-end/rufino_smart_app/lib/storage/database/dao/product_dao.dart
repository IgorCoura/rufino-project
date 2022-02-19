import 'package:drift/drift.dart';
import 'package:rufino_smart_app/storage/database/stock_db.dart';

part 'product_dao.g.dart';

@DriftAccessor(tables: [Products])
class ProductDao extends DatabaseAccessor<StockDb> with _$ProductDaoMixin {
  ProductDao(StockDb db) : super(db);

  Future<void> add(Product entry) {
    return into(products).insert(entry);
  }

  Stream<List<Product>> listAll() {
    return (select(products)).watch();
  }
}
