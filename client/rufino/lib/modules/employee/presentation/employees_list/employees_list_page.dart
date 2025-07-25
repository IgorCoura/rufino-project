import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_modular/flutter_modular.dart';
import 'package:infinite_scroll_pagination/infinite_scroll_pagination.dart';
import 'package:rufino/modules/employee/domain/model/employee_with_role.dart';
import 'package:rufino/modules/employee/domain/model/search_params.dart';
import 'package:rufino/modules/employee/presentation/employees_list/bloc/employees_list_bloc.dart';
import 'package:rufino/shared/components/error_components.dart';

class EmployeesListPage extends StatelessWidget {
  final bloc = Modular.get<EmployeesListBloc>();

  EmployeesListPage({super.key}) {
    bloc.add(InitialEmployeesListEvent());
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
        title: BlocBuilder<EmployeesListBloc, EmployeesListState>(
          bloc: bloc,
          builder: (context, state) {
            return Row(
              children: [
                const CircleAvatar(
                  backgroundImage: AssetImage("assets/img/company_default.jpg"),
                ),
                const SizedBox(width: 10),
                Expanded(
                  child: Text(
                    state.company != null
                        ? state.company!.fantasyName
                        : "Lista de Funcionarios",
                    overflow: TextOverflow.ellipsis,
                  ),
                ),
              ],
            );
          },
        ),
        actions: [
          Padding(
            padding: const EdgeInsets.all(8.0),
            child: PopupMenuButton(
              icon: Icon(Icons.settings),
              itemBuilder: (BuildContext context) {
                return const <PopupMenuEntry>[
                  PopupMenuItem(
                    value: "documents",
                    child: Text('Templates de Documentos'),
                  ),
                  PopupMenuItem(
                    value: "require-documents",
                    child: Text('Requerimentos de Documentos'),
                  ),
                  PopupMenuItem(
                    value: "document-groups",
                    child: Text('Grupos de Documentos'),
                  ),
                ];
              },
              onSelected: (value) async {
                if (value == "archive") {
                  Modular.to.pushNamed("archive-category");
                  return;
                }
                if (value == "documents") {
                  Modular.to.pushNamed("document-template");
                  return;
                }
                if (value == "require-documents") {
                  Modular.to.pushNamed("require-documents");
                  return;
                }
                if (value == "document-groups") {
                  Modular.to.pushNamed("document-group");
                  return;
                }
              },
            ),
          ),
        ],
      ),
      body: Center(
        child: ConstrainedBox(
          constraints: const BoxConstraints(
            maxWidth: 1000.0,
          ),
          child: BlocBuilder<EmployeesListBloc, EmployeesListState>(
            bloc: bloc,
            builder: (context, state) {
              if (state.exception != null) {
                ErrorComponent.showException(
                    context, state.exception!, () => Modular.to.navigate("/"));
              }
              return Column(
                children: [
                  Padding(
                    padding: const EdgeInsets.all(12.0),
                    child: LayoutBuilder(builder: (context, layoutConstraints) {
                      var textfieldWidth = layoutConstraints.maxWidth - 200;
                      return Wrap(
                        runSpacing: 8,
                        spacing: 8,
                        crossAxisAlignment: WrapCrossAlignment.center,
                        alignment: WrapAlignment.spaceBetween,
                        children: [
                          SizedBox(
                            width: textfieldWidth <= 500 ? 700 : textfieldWidth,
                            child: Row(
                              children: [
                                Expanded(
                                  child: TextField(
                                    decoration: InputDecoration(
                                      hintText:
                                          "Buscar por ${state.searchParam.value}...",
                                      prefixIcon: const Padding(
                                        padding: EdgeInsets.only(
                                            left: 16.0, right: 16.0),
                                        child: Icon(Icons.search),
                                      ),
                                      border: const OutlineInputBorder(
                                        borderRadius: BorderRadius.only(
                                            topLeft: Radius.circular(30),
                                            bottomLeft: Radius.circular(30)),
                                      ),
                                    ),
                                    onChanged: (input) =>
                                        bloc.add(ChangeSearchInput(input)),
                                    onEditingComplete: () =>
                                        bloc.add(SearchEditComplet()),
                                  ),
                                ),
                                DropdownButtonFormField(
                                  value: state.searchParam.id,
                                  items: SearchParam.values
                                      .map((e) => DropdownMenuItem(
                                          value: e.id, child: Text(e.value)))
                                      .toList(),
                                  onChanged: (selection) =>
                                      bloc.add(ChangeSearchParam(selection)),
                                  hint: const Text("status"),
                                  decoration: const InputDecoration(
                                      border: OutlineInputBorder(
                                          borderRadius: BorderRadius.only(
                                              topRight: Radius.circular(30),
                                              bottomRight:
                                                  Radius.circular(30))),
                                      enabledBorder: OutlineInputBorder(
                                          borderRadius: BorderRadius.only(
                                              topRight: Radius.circular(30),
                                              bottomRight:
                                                  Radius.circular(30))),
                                      focusedBorder: OutlineInputBorder(
                                          borderRadius: BorderRadius.only(
                                              topRight: Radius.circular(30),
                                              bottomRight:
                                                  Radius.circular(30))),
                                      constraints:
                                          BoxConstraints(maxWidth: 120)),
                                ),
                              ],
                            ),
                          ),
                          ConstrainedBox(
                            constraints: const BoxConstraints(maxWidth: 140),
                            child: DropdownButtonFormField(
                              padding: const EdgeInsets.all(4),
                              value: state.selectedStatus,
                              items: state.listStatus
                                  .map((e) => DropdownMenuItem(
                                      value: e.id, child: Text(e.name)))
                                  .toList(),
                              onChanged: (selection) =>
                                  bloc.add(ChangeStatusSelect(selection)),
                              hint: const Text("status"),
                              decoration: const InputDecoration(
                                border: OutlineInputBorder(
                                    borderRadius:
                                        BorderRadius.all(Radius.circular(30))),
                                enabledBorder: OutlineInputBorder(
                                    borderRadius:
                                        BorderRadius.all(Radius.circular(30))),
                                focusedBorder: OutlineInputBorder(
                                    borderRadius:
                                        BorderRadius.all(Radius.circular(30))),
                              ),
                            ),
                          ),
                          IconButton.filled(
                            onPressed: () => bloc.add(ChangeSortList()),
                            icon: Transform(
                              alignment: Alignment.center,
                              transform: Matrix4.identity()
                                ..scale(1.0, state.isAscSort ? -1.0 : 1.0),
                              child: const Icon(Icons.sort),
                            ),
                          ),
                        ],
                      );
                    }),
                  ),
                  const Padding(
                    padding: EdgeInsets.fromLTRB(16, 0, 24, 0),
                    child: Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Padding(
                          padding: EdgeInsets.fromLTRB(54, 0, 0, 0),
                          child: Text(
                            "Nomes",
                            textAlign: TextAlign.center,
                            style: TextStyle(
                              fontSize: 14,
                            ),
                          ),
                        ),
                        Row(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            SizedBox(
                              width: 90,
                              child: Text(
                                "Documentos",
                                textAlign: TextAlign.center,
                                style: TextStyle(
                                  fontSize: 14,
                                ),
                              ),
                            ),
                            SizedBox(width: 4),
                            SizedBox(
                              width: 80,
                              child: Text(
                                "Status",
                                textAlign: TextAlign.center,
                                style: TextStyle(
                                  fontSize: 14,
                                ),
                              ),
                            ),
                          ],
                        ),
                      ],
                    ),
                  ),
                  Divider(),
                  state.isLoading
                      ? const CircularProgressIndicator()
                      : Expanded(
                          child: _employeesInfinityListView(state),
                        ),
                ],
              );
            },
          ),
        ),
      ),
      floatingActionButton: FloatingActionButton(
        onPressed: () {
          _dialogCreateEmployee(context);
        },
        child: const Icon(Icons.add),
      ),
    );
  }

  Future _dialogCreateEmployee(BuildContext context) {
    return showDialog(
        context: context,
        builder: (context) {
          return AlertDialog(
            title: const Text('Cadastrar Funcionário'),
            content: SizedBox(
              width: 600,
              child: BlocBuilder<EmployeesListBloc, EmployeesListState>(
                  bloc: bloc,
                  builder: (context, state) {
                    return TextField(
                      onChanged: (name) =>
                          bloc.add(ChangeNameNewEmployee(name)),
                      decoration: InputDecoration(
                        labelText: 'Nome do Funcionário',
                        border: const OutlineInputBorder(),
                        errorText: state.textfieldErrorMessage.isEmpty
                            ? null
                            : state.textfieldErrorMessage,
                      ),
                    );
                  }),
            ),
            actions: [
              TextButton(
                onPressed: () {
                  Navigator.of(context).pop();
                },
                child: const Text('Cancelar'),
              ),
              ElevatedButton(
                onPressed: () {
                  Navigator.of(context).pop();
                  bloc.add(CreateNewEmployee());
                },
                child: const Text('Criar'),
              ),
            ],
          );
        });
  }

  Widget _employeesInfinityListView(EmployeesListState state) {
    return PagedListView<int, EmployeeWithRole>(
      state: state.pagingState ?? PagingState<int, EmployeeWithRole>(),
      fetchNextPage: () => bloc.add(FeatchNextPage()),
      builderDelegate: PagedChildBuilderDelegate<EmployeeWithRole>(
          itemBuilder: (context, item, index) => _employeeListItem(item)),
    );
  }

  Widget _employeeListItem(EmployeeWithRole employee) {
    return ListTile(
      leading: const CircleAvatar(
        backgroundImage: AssetImage("assets/img/avatar_default.png"),
      ),
      title: Text(
        employee.name,
      ),
      subtitle: Text('Função: ${employee.roleName}'),
      trailing: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Container(
            alignment: Alignment.center,
            width: 90,
            child: Text(
              textAlign: TextAlign.center,
              employee.documentStatus.name,
              style: const TextStyle(fontSize: 14),
            ),
          ),
          SizedBox(width: 4),
          Container(
            alignment: Alignment.center,
            width: 80,
            child: Text(
              textAlign: TextAlign.center,
              employee.status.name,
              style: const TextStyle(fontSize: 14),
            ),
          ),
        ],
      ),
      onTap: () => Modular.to.pushNamed("profile/${employee.id}"),
    );
  }
}
