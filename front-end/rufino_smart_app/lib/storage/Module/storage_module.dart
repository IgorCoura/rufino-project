import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino_smart_app/storage/bloc/order/storage_order_bloc.dart';
import 'package:rufino_smart_app/storage/bloc/search/storage_order__search_bloc.dart';
import 'package:rufino_smart_app/storage/database/dao/product_dao.dart';
import 'package:rufino_smart_app/storage/database/stock_db.dart';
import 'package:rufino_smart_app/storage/page/stock_home_page.dart';
import 'package:rufino_smart_app/storage/page/storage_home_old_page.dart';
import 'package:rufino_smart_app/storage/page/storage_order_page.dart';
import 'package:rufino_smart_app/storage/service/product_service.dart';

class StorageModule extends Module {
  @override
  List<Bind> get binds => [
        Bind.singleton((i) => StorageOrderSearchBloc()),
        Bind.singleton((i) => StorageOrderBloc()),
      ];

  @override
  List<ModularRoute> get routes => [
        ChildRoute('/', child: (context, args) => StockHomePage()),
        ChildRoute('/withdrawal',
            child: (context, args) => StorageOrderPage(
                  withdrawal: true,
                )),
      ];
}
