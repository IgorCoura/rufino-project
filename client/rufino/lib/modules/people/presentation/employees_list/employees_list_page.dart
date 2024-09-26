import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_modular/flutter_modular.dart';
import 'package:infinite_scroll_pagination/infinite_scroll_pagination.dart';
import 'package:rufino/modules/people/presentation/domain/model/employee.dart';
import 'package:rufino/modules/people/presentation/domain/model/status.dart';
import 'package:rufino/modules/people/presentation/domain/model/search_params.dart';
import 'package:rufino/modules/people/presentation/employees_list/bloc/employees_list_bloc.dart';

class EmployeesListPage extends StatelessWidget {
  final bloc = Modular.get<EmployeesListBloc>();

  EmployeesListPage({super.key}) {
    bloc.add(InitialEmployeesListEvent());
  }

  // Paginação

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          onPressed: () {
            Modular.to.navigate("/home");
          },
        ),
        title: const Row(
          children: [
            CircleAvatar(
              backgroundImage: AssetImage("assets/img/company_default.jpg"),
            ),
            SizedBox(width: 10),
            Text(
              'Empresa XYZ',
              overflow: TextOverflow.ellipsis,
            ),
          ],
        ),
      ),
      body: Center(
        child: ConstrainedBox(
          constraints: const BoxConstraints(
            maxWidth: 1000.0,
          ),
          child: BlocBuilder<EmployeesListBloc, EmployeesListState>(
            bloc: bloc,
            builder: (context, state) {
              return Column(
                children: [
                  Padding(
                    padding: const EdgeInsets.all(12.0),
                    child: LayoutBuilder(builder: (context, layoutConstraints) {
                      var textfieldWidth = layoutConstraints.maxWidth - 200;
                      return Wrap(
                        runSpacing: 8,
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
                  state.isLoading
                      ? const CircularProgressIndicator()
                      : Expanded(
                          child: _employeesInfinityListView(state.listStatus),
                        ),
                ],
              );
            },
          ),
        ),
      ),
      floatingActionButton: FloatingActionButton(
        onPressed: () {
          // Ação do segundo botão
        },
        child: const Icon(Icons.add), // HeroTag único
      ),
    );
  }

  Widget _employeesInfinityListView(List<Status> listStatus) {
    return PagedListView<int, Employee>(
      pagingController: bloc.pagingController,
      builderDelegate: PagedChildBuilderDelegate<Employee>(
          itemBuilder: (context, item, index) => _employeeListItem(
              item,
              listStatus.singleWhere(
                (status) => status.id == item.status,
                orElse: () => Status.empty,
              ))),
    );
  }

  Widget _employeeListItem(Employee employee, Status status) {
    return ListTile(
      leading: const CircleAvatar(
        backgroundImage: AssetImage("assets/img/avatar_default.png"),
      ),
      title: Text(
        employee.name,
      ),
      subtitle: Text('Função: ${employee.roleName}'),
      trailing: Text(
        status.name,
        style: const TextStyle(fontSize: 14),
      ),
      onTap: () {},
    );
  }
}
