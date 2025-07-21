import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino/modules/department/departments/bloc/departments_bloc.dart';
import 'package:rufino/shared/components/error_components.dart';

class DepartmentsPage extends StatelessWidget {
  final _bloc = Modular.get<DepartmentsBloc>();
  DepartmentsPage({super.key}) {
    _bloc.add(LoadEvent());

    // Adiciona um listener para atualizar quando a navegação para '/department/' ocorrer
    Modular.to.addListener(() {
      if (Modular.to.path == '/department/') {
        _bloc.add(LoadEvent());
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          onPressed: () => Modular.to.navigate("/"),
        ),
        title: const Text("Setores"),
      ),
      body: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Center(
            child: BlocBuilder<DepartmentsBloc, DepartmentsState>(
          bloc: _bloc,
          builder: (context, state) {
            if (state.exception != null) {
              ErrorComponent.showException(
                  context, state.exception!, () => Modular.to.navigate("/"));
            }
            if (state.isLoading) {
              return const CircularProgressIndicator();
            }
            if (state.department.isEmpty) {
              return const Text("Nenhum setor cadastrado.");
            }
            return ListView(
                children: state.department
                    .map((element) => _departamentWidget(
                        element.id,
                        element.name.value,
                        element.description.value,
                        element.positions.map((position) {
                          return _positionWidget(
                            position.id,
                            element.id,
                            position.name.value,
                            position.description.value,
                            position.roles.map((role) {
                              return _roleWidget(
                                role.id,
                                position.id,
                                role.name.value,
                                role.description.value,
                              );
                            }).toList(),
                          );
                        }).toList()))
                    .toList());
          },
        )),
      ),
      floatingActionButton: FloatingActionButton(
        onPressed: () {
          Modular.to.pushNamed('/department/edit/');
        },
        child: const Icon(Icons.add),
      ),
    );
  }

  Widget _departamentWidget(
      String id, String title, String description, List<Widget> children) {
    return ExpansionTile(
      controlAffinity: ListTileControlAffinity.leading,
      trailing: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          IconButton(
            icon: Icon(Icons.edit),
            onPressed: () {
              Modular.to.pushNamed('/department/edit/$id');
            },
          ),
          IconButton(
            icon: Icon(Icons.add),
            onPressed: () {
              Modular.to.pushNamed('/department/position/edit/$id/');
            },
          )
        ],
      ),
      title: Text(
        title,
        style: TextStyle(
          fontWeight: FontWeight.bold,
        ),
      ),
      subtitle: Text(
        description,
        style: TextStyle(fontSize: 12),
      ),
      children: children,
    );
  }

  Widget _positionWidget(String id, String departmentId, String title,
      String description, List<Widget> children) {
    return Padding(
      padding: const EdgeInsets.all(8.0),
      child: Container(
        decoration: BoxDecoration(
          border: Border.all(
            width: 1.0,
          ),
          borderRadius: BorderRadius.circular(8.0),
        ),
        child: ExpansionTile(
          controlAffinity: ListTileControlAffinity.leading,
          trailing: Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              IconButton(
                icon: Icon(Icons.edit),
                onPressed: () {
                  Modular.to
                      .pushNamed('/department/position/edit/$departmentId/$id');
                },
              ),
              IconButton(
                icon: Icon(Icons.add),
                onPressed: () {
                  Modular.to.pushNamed('/department/role/edit/$id/');
                },
              )
            ],
          ),
          title: Text(
            title,
            style: TextStyle(
              fontWeight: FontWeight.bold,
            ),
          ),
          subtitle: Text(
            description,
            style: TextStyle(fontSize: 12),
          ),
          children: children,
        ),
      ),
    );
  }

  Widget _roleWidget(
      String id, String positionId, String title, String description) {
    return ListTile(
      title: Text(
        title,
        style: TextStyle(
          fontWeight: FontWeight.bold,
        ),
      ),
      subtitle: Text(
        description,
        style: TextStyle(fontSize: 12),
      ),
      onTap: () {
        Modular.to.pushNamed('/department/role/edit/$positionId/$id');
      },
    );
  }
}
