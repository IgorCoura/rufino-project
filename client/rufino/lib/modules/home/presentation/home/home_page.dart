import 'package:flutter/material.dart';
import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino/domain/services/auth_service.dart';
import 'package:rufino/modules/home/presentation/home/bloc/home_bloc.dart';

class HomePage extends StatelessWidget {
  HomePage({super.key});

  final bloc = Modular.get<HomeBloc>();

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text("Rufino App"),
        actions: [
          PopupMenuButton(
            icon: const Row(
              children: [
                CircleAvatar(
                  backgroundImage: AssetImage("assets/img/avatar_default.png"),
                ),
                Icon(Icons.arrow_drop_down_sharp)
              ],
            ),
            itemBuilder: (BuildContext context) {
              return const <PopupMenuEntry>[
                PopupMenuItem(
                  value: "profile",
                  child: Text('Perfil'),
                ),
                PopupMenuItem(
                  value: "logout",
                  child: Text('Log out'),
                ),
              ];
            },
            onSelected: (value) async {
              if (value == "logout") {
                bloc.add(LogoutRequested());
                Modular.to.navigate("/login");
                return;
              }
            },
          ),
        ],
      ),
      body: Padding(
        padding: const EdgeInsets.all(16.0),
        child: ElevatedButton(
          onPressed: () => Modular.to.navigate("/people/list"),
          child: const Text("Funcionarios"),
        ),
      ),
    );
  }
}
