import 'package:drift/drift.dart';
import 'package:rufino_app/src/stock/db/stock_db.dart';

part 'product_transaction_dao.g.dart';

@DriftAccessor(tables: [ProductsTransactions])
class ProductTransactionDao extends DatabaseAccessor<StockDb>
    with _$ProductTransactionDaoMixin {
  ProductTransactionDao(StockDb db) : super(db);

  Future<void> add(Iterable<ProductTransaction> listEntry) async {
    listEntry.forEach((e) => print(e.idProduct));
    return batch(
      (batch) => batch.insertAll(productsTransactions, listEntry),
    );
  }
}
