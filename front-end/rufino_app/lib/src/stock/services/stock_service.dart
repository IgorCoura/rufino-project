import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino_app/src/stock/db/dao/product_transaction_dao.dart';
import 'package:rufino_app/src/stock/db/stock_db.dart';
import 'package:rufino_app/src/stock/model/stock_order_item_model.dart';
import 'package:uuid/uuid.dart';

class StockService {
  final ProductTransactionDao productTransactionDao;

  const StockService(this.productTransactionDao);

  Future<void> insertTransaction(List<StockOrderItemModel> items,
      String idResponsible, String idTaker) async {
    var date = DateTime.now();
    var itemsTransaction = items.map((e) => ProductTransaction(
        id: const Uuid().v4(),
        idProduct: e.idProduct,
        quantityVariation: e.quantityVariation,
        date: date,
        idResponsible: idResponsible,
        idTaker: idTaker));
    await productTransactionDao.add(itemsTransaction);

    //TODO: Chamar Função responsavel por sincronizar back com front.
  }
}
