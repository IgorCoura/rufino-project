import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino/modules/workplace/presentation/workplaces/bloc/workplace_bloc.dart';
import 'package:rufino/shared/components/error_components.dart';

class WorkplacesPage extends StatelessWidget {
  final bloc = Modular.get<WorkplaceBloc>();
  WorkplacesPage({super.key}) {
    bloc.add(WorkplaceLoadEvent());
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          onPressed: () {
            Modular.to.navigate("/");
          },
        ),
        title: const Text('Locais de trabalho'),
      ),
      body: Center(
        child: BlocBuilder<WorkplaceBloc, WorkplaceState>(
          bloc: bloc,
          builder: (context, state) {
            if (state.exception != null) {
              ErrorComponent.showException(
                  context, state.exception!, () => Modular.to.navigate("/"));
            }
            return ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 1000),
              child: Padding(
                padding: const EdgeInsets.all(8.0),
                child: ListView(
                  children: state.workplace.map((workplace) {
                    return ListTile(
                      leading: const Icon(Icons.work),
                      title: Text(workplace.name),
                      subtitle: Text(workplace.minimalAddress),
                      onTap: () {
                        // Ação ao clicar no local de trabalho
                        Modular.to.pushNamed('/workplace/edit/${workplace.id}');
                      },
                    );
                  }).toList(),
                ),
              ),
            );
          },
        ),
      ),
      floatingActionButton: FloatingActionButton(
        onPressed: () {
          Modular.to.pushNamed('/workplace/edit/');
        },
        child: const Icon(Icons.add),
      ),
    );
  }
}
