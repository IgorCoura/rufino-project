import 'package:flutter_modular/flutter_modular.dart';
import 'package:path/path.dart';
import 'package:rufino_app/src/stock_management/page/stock_management_home_page.dart';
import 'package:rufino_app/src/stock_management/stock_audit/module/stock_audit_module.dart';

class StockManagementModule extends Module {
  @override
  List<Bind<Object>> get binds => [];
  @override
  List<ModularRoute> get routes => [
        ChildRoute("/", child: (context, args) => StockManagementHomePage()),
        ModuleRoute("/audit", module: StockAuditModule()),
      ];
}
