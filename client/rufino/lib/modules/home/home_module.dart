import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino/app_module.dart';
import 'package:rufino/modules/home/presentation/home/bloc/home_bloc.dart';
import 'package:rufino/modules/home/presentation/home/home_page.dart';

class HomeModule extends Module {
  @override
  void binds(i) {
    i.add<HomeBloc>(HomeBloc.new);
  }

  @override
  List<Module> get imports => [AppModule()];

  @override
  void routes(r) {
    r.child('/', child: (context) => HomePage());
  }
}
