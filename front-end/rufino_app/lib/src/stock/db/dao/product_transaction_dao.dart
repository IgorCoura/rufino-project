import 'package:drift/drift.dart';
import 'package:rufino_app/src/stock/db/stock_db.dart';

part 'product_transaction_dao.g.dart';

@DriftAccessor(tables: [ProductsTransactions, Products, Workers])
class ProductTransactionDao extends DatabaseAccessor<StockDb>
    with _$ProductTransactionDaoMixin {
  ProductTransactionDao(StockDb db) : super(db);

  Future<void> add(Iterable<ProductTransaction> listEntry) async {
    return transaction(() async {
      for (var element in listEntry) {
        var product = await (select(products)
              ..where((tbl) => tbl.id.equals(element.idProduct)))
            .getSingle();
        var updateProduct = product.copyWith(
            quantity: product.quantity + element.quantityVariation);
        await update(products).replace(updateProduct);
        await into(productsTransactions).insert(element);
      }
    });
  }

  Stream<List<ProductTransaction>> getAll() {
    return (select(productsTransactions)).watch();
  }
}
