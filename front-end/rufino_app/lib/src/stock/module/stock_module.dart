import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino_app/src/stock/bloc/stock_home_bloc.dart';
import 'package:rufino_app/src/stock/page/stock_home_page.dart';
import 'package:rufino_app/src/stock/page/stock_order_page.dart';

class StockModule extends Module {
  @override
  List<Bind<Object>> get binds => [
        Bind.singleton((i) => StockHomeBloc()),
      ];

  @override
  List<ModularRoute> get routes => [
        ChildRoute('/', child: (context, args) => StockHomePage()),
        ChildRoute('/order', child: (context, args) => StockOrderPage()),
      ];
}
