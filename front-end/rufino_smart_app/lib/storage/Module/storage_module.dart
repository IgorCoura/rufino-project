import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino_smart_app/storage/page/storage_home_page.dart';
import 'package:rufino_smart_app/storage/page/storage_withdrawal_page.dart';

class StorageModule extends Module {
  @override
  List<Bind> get binds => [];

  @override
  List<ModularRoute> get routes => [
        ChildRoute('/', child: (context, args) => StorageHomePage()),
        ChildRoute('/withdrawal',
            child: (context, args) => StorageWithdrawalPage()),
      ];
}
