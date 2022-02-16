import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino_app/src/stock/bloc/stock_home_bloc.dart';
import 'package:rufino_app/src/stock/db/dao/product_dao.dart';
import 'package:rufino_app/src/stock/db/dao/product_transaction_dao.dart';
import 'package:rufino_app/src/stock/db/dao/worker_dao.dart';
import 'package:rufino_app/src/stock/db/stock_db.dart';
import 'package:rufino_app/src/stock/page/stock_devolucion_page.dart';
import 'package:rufino_app/src/stock/page/stock_home_page.dart';
import 'package:rufino_app/src/stock/page/stock_withdraw_page.dart';
import 'package:rufino_app/src/stock/page/stock_transaction_history_page.dart';
import 'package:rufino_app/src/stock/services/stock_service.dart';

class StockModule extends Module {
  @override
  List<Bind<Object>> get binds => [
        Bind.singleton((i) => StockDb(ConnectOptions.dev)),
        Bind.singleton((i) => ProductDao(i())),
        Bind.singleton((i) => WorkerDao(i())),
        Bind.singleton((i) => ProductTransactionDao(i())),
        Bind.singleton((i) => StockService(i())),
        Bind.singleton((i) => StockHomeBloc(i())),
      ];

  @override
  List<ModularRoute> get routes => [
        ChildRoute('/', child: (context, args) => StockHomePage()),
        ChildRoute('/withdraw', child: (context, args) => StockWithdrawPage()),
        ChildRoute('/devolucion',
            child: (context, args) => StockDevolucionPage()),
        ChildRoute('/history',
            child: (context, args) => StockTransactionHistoryPage()),
      ];
}
