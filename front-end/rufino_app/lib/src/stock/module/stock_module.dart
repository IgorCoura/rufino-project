import 'package:dio/dio.dart';
import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino_app/src/stock/bloc/stock_home_bloc.dart';
import 'package:rufino_app/src/stock/db/dao/product_dao.dart';
import 'package:rufino_app/src/stock/db/dao/product_transaction_dao.dart';
import 'package:rufino_app/src/stock/db/dao/worker_dao.dart';
import 'package:rufino_app/src/stock/db/stock_db.dart';
import 'package:rufino_app/src/stock/page/stock_home_page.dart';
import 'package:rufino_app/src/stock/page/stock_order_page.dart';
import 'package:rufino_app/src/stock/page/stock_transaction_history_page.dart';
import 'package:rufino_app/src/stock/repository/product_repository.dart';
import 'package:rufino_app/src/stock/repository/product_transaction_repository.dart';
import 'package:rufino_app/src/stock/repository/worker_repository.dart';
import 'package:rufino_app/src/stock/services/product_service.dart';
import 'package:rufino_app/src/stock/services/product_transaction_service.dart';
import 'package:rufino_app/src/stock/services/worker_service.dart';

class StockModule extends Module {
  @override
  List<Bind<Object>> get binds => [
        Bind.singleton((i) => StockDb(ConnectOptions.dev)),
        Bind.singleton((i) => Dio()),
        //Dao
        Bind.singleton((i) => ProductDao(i())),
        Bind.singleton((i) => WorkerDao(i())),
        Bind.singleton((i) => ProductTransactionDao(i())),
        //Repository
        Bind.singleton((i) => ProductRepository(i())),
        Bind.singleton((i) => WorkerRepository(i())),
        Bind.singleton((i) => ProductTransactionRepository(i())),
        //Service
        Bind.singleton((i) => ProductTransactionService(i(), i())),
        Bind.singleton((i) => ProductService(i(), i())),
        Bind.singleton((i) => WorkerService(i(), i())),
        //Bloc
        Bind.factory((i) => StockHomeBloc(i())),
      ];

  @override
  List<ModularRoute> get routes => [
        ChildRoute('/', child: (context, args) => StockHomePage()),
        ChildRoute('/withdraw',
            child: (context, args) => StockOrderPage(
                  withdraw: true,
                  title: "Ordem de retirada",
                  bloc: args.data,
                )),
        ChildRoute('/devolucion',
            child: (context, args) => StockOrderPage(
                  title: "Ordem de devolução",
                )),
        ChildRoute('/history',
            child: (context, args) => StockTransactionHistoryPage()),
      ];
}
