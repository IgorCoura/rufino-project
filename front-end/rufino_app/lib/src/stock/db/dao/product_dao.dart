import 'package:drift/drift.dart';
import 'package:rufino_app/src/stock/db/stock_db.dart';

part 'product_dao.g.dart';

@DriftAccessor(tables: [Products])
class ProductDao extends DatabaseAccessor<StockDb> with _$ProductDaoMixin {
  ProductDao(StockDb db) : super(db);

  Future<void> add(Product entry) {
    return into(products).insert(entry);
  }

  Stream<List<Product>> getAll() {
    return (select(products)).watch();
  }

  Stream<List<Product>> getFiltered(String searchString) {
    var search = "%$searchString%";
    return customSelect(
      'SELECT * FROM products WHERE name LIKE ?',
      variables: [
        Variable.withString(search),
      ],
      readsFrom: {products},
    ).watch().map((row) => row.map((e) => Product.fromData(e.data)).toList());
  }
}
