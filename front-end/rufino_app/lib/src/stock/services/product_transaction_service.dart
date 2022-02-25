import 'package:connectivity_plus/connectivity_plus.dart';
import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino_app/src/stock/db/dao/product_transaction_dao.dart';
import 'package:rufino_app/src/stock/db/stock_db.dart';
import 'package:rufino_app/src/stock/model/stock_order_item_model.dart';
import 'package:rufino_app/src/stock/repository/product_transaction_repository.dart';
import 'package:uuid/uuid.dart';

class ProductTransactionService {
  final ProductTransactionDao _productTransactionDao;
  final ProductTransactionRepository _productTransactionRepository;

  const ProductTransactionService(
      this._productTransactionDao, this._productTransactionRepository);

  Future<void> insertTransaction(List<StockOrderItemModel> items,
      String idResponsible, String idTaker, bool withdrawl) async {
    var date = DateTime.now();
    var itemsTransaction = items.map((e) => ProductTransaction(
        id: const Uuid().v4(),
        idProduct: e.idProduct,
        quantityVariation:
            withdrawl ? -e.quantityVariation : e.quantityVariation,
        date: date,
        idResponsible: idResponsible,
        idTaker: idTaker));
    await _productTransactionDao.add(itemsTransaction);
    sendTransactionsToServer();
  }

  void sendTransactionsToServer() async {
    var connectivityResult = await (Connectivity().checkConnectivity());
    if (connectivityResult == ConnectivityResult.ethernet ||
        connectivityResult == ConnectivityResult.wifi) {
      var items = await _productTransactionDao.getTransactionsWithNotServerId();
      var returnTransactions =
          await _productTransactionRepository.postTransactions(items);
      _productTransactionDao.updateOrAdd(returnTransactions);
    }
  }
}
