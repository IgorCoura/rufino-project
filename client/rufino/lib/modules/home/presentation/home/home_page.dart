import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino/modules/home/presentation/home/bloc/home_bloc.dart';
import 'package:rufino/shared/components/error_message_components.dart';

class HomePage extends StatelessWidget {
  HomePage({super.key}) {
    bloc.add(InitialHomeEvent());
  }

  final bloc = Modular.get<HomeBloc>();

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: BlocBuilder<HomeBloc, HomeState>(
          bloc: bloc,
          builder: (context, state) {
            if (state.exception != null) {
              ErrorMessageComponent.showAlertDialog(
                  context, state.exception!, () => Modular.to.navigate("/"));
            }
            var title =
                state.company == null ? "Home" : state.company!.fantasyName;
            return Row(
              children: [
                const CircleAvatar(
                  backgroundImage: AssetImage("assets/img/company_default.jpg"),
                ),
                const SizedBox(width: 10),
                Expanded(
                  child: Text(
                    title,
                    overflow: TextOverflow.ellipsis,
                  ),
                ),
              ],
            );
          },
        ),
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
                  value: "company-section",
                  child: Text('Alterar Empresa'),
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
              if (value == "company-section") {
                Modular.to.navigate("/company-selection");
                return;
              }
            },
          ),
        ],
      ),
      body: Padding(
        padding: const EdgeInsets.all(16.0),
        child: SizedBox(
          width: 100,
          height: 100,
          child: FloatingActionButton(
            backgroundColor: Theme.of(context).colorScheme.primary,
            onPressed: () => Modular.to.navigate("/people/list"),
            child: Text(
              "Funcionarios",
              style: TextStyle(
                color: Theme.of(context).colorScheme.onPrimary,
              ),
            ),
          ),
        ),
      ),
    );
  }
}
