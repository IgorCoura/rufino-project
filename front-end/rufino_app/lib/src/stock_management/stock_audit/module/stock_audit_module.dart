import 'package:dio/dio.dart';
import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino_app/src/stock_management/stock_audit/page/stock_audit_home_page.dart';
import 'package:rufino_app/src/stock_management/stock_audit/repository/product_repository.dart';

class StockAuditModule extends Module {
  @override
  List<Bind<Object>> get binds => [
        Bind.singleton((i) => Dio()),
        Bind.singleton((i) => ProductRepository(i())),
      ];

  @override
  List<ModularRoute> get routes => [
        ChildRoute(
          "/",
          child: ((context, args) => StockAuditHomePage()),
        )
      ];
}
