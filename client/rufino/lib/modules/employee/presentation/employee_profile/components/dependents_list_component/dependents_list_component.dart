import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino/modules/employee/presentation/components/base_edit_component.dart';
import 'package:rufino/modules/employee/presentation/employee_profile/components/dependents_list_component/bloc/dependents_list_component_bloc.dart';
import 'package:rufino/modules/employee/presentation/components/enumeration_view_component.dart';
import 'package:rufino/modules/employee/presentation/components/props_container_component.dart';
import 'package:rufino/modules/employee/presentation/components/text_edit_component.dart';
import 'package:rufino/shared/components/error_components.dart';

class DependentsListComponent extends StatelessWidget {
  final DependentsListComponentBloc bloc =
      Modular.get<DependentsListComponentBloc>();

  DependentsListComponent(
      {required String companyId, required String employeeId, super.key}) {
    bloc.add(InitialEvent(companyId, employeeId));
  }

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        border: Border.all(),
        borderRadius: BorderRadius.circular(5),
      ),
      child: BlocBuilder<DependentsListComponentBloc,
          DependentsListComponentState>(
        bloc: bloc,
        builder: (context, state) {
          if (state.exception != null) {
            ErrorComponent.showException(
                context, state.exception!, () => Modular.to.navigate("/home"));
          }

          if (state.snackMessage != null && state.snackMessage!.isNotEmpty) {
            WidgetsBinding.instance.addPostFrameCallback((_) {
              ScaffoldMessenger.of(context).showSnackBar(SnackBar(
                content: Text(state.snackMessage!),
              ));
              bloc.add(SnackMessageWasShowDependentEvent());
            });
          }
          return Column(
            children: [
              _header(state),
              state.isExpanded
                  ? Column(
                      children: [
                        _body(state),
                        _buttons(context),
                      ],
                    )
                  : Container(),
            ],
          );
        },
      ),
    );
  }

  Widget _body(DependentsListComponentState state) {
    return Padding(
      padding: const EdgeInsets.all(8.0),
      child: Column(
        children: state.dependents.isEmpty
            ? [const Text("Não há dependentes")]
            : state.dependents
                .asMap()
                .map(
                  (index, e) => MapEntry(
                    index,
                    PropsContainerComponent(
                      containerName: e.name.value,
                      isSavingData: state.isSavingData,
                      saveContainerData: (changes) =>
                          bloc.add(SaveChangesDependentEvent(index, changes)),
                      loadingContainerData: () {},
                      isLoading: state.isLoading,
                      removeButton: () => bloc.add(RemoveDependentEvent(index)),
                      children: BaseEditComponent.combineList([
                        [
                          TextEditComponent(textProp: e.name),
                          EnumerationViewComponent(
                              enumeration: e.gender,
                              listEnumerationOptions: state.genderOptions),
                          EnumerationViewComponent(
                              enumeration: e.type,
                              listEnumerationOptions:
                                  state.dependencyTypeOptions),
                        ],
                        e.idCard.textProps
                            .map((prop) => TextEditComponent(textProp: prop))
                            .toList(),
                      ]),
                    ),
                  ),
                )
                .values
                .toList()
                .expand((child) => [
                      const SizedBox(
                        height: 16,
                      ),
                      child
                    ])
                .toList(),
      ),
    );
  }

  Widget _header(DependentsListComponentState state) {
    return Container(
      padding: const EdgeInsets.all(8),
      decoration: BoxDecoration(
        border: state.isExpanded ? const Border(bottom: BorderSide()) : null,
        borderRadius: BorderRadius.circular(5),
      ),
      child: InkWell(
        onTap: () => bloc.add(ExpandInfoEvent()),
        child: const Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            Text(
              "Dependentes",
              style: TextStyle(fontWeight: FontWeight.bold, fontSize: 16),
            ),
            Icon(
              Icons.arrow_drop_down_sharp,
            )
          ],
        ),
      ),
    );
  }

  Widget _buttons(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.all(8.0),
      child: Align(
        alignment: Alignment.centerRight,
        child: TextButton(
          onPressed: () => bloc.add(AddDependentEvent()),
          child: const Text("Adicionar Dependente"),
        ),
      ),
    );
  }
}
