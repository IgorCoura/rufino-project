import 'package:flutter/material.dart';
import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino/modules/employee/domain/model/base/model_base.dart';
import "package:rufino/modules/employee/presentation/employee_profile/bloc/employee_profile_bloc.dart";
import 'package:rufino/modules/employee/presentation/employee_profile/components/props_container_component.dart';
import 'package:rufino/modules/employee/presentation/employee_profile/components/text_edit_component.dart';

class PropsListComponent extends StatefulWidget {
  final String containerName;
  final List<ModelBase> propsContainerList;
  final Function(int, List<Object>) saveData;
  final Function addData;
  final Function(int)? removeData;
  final Function loadingData;
  final bool isSavingData;
  final bool isLoading;

  const PropsListComponent(
      {required this.containerName,
      required this.propsContainerList,
      required this.isSavingData,
      required this.isLoading,
      required this.saveData,
      required this.addData,
      required this.removeData,
      required this.loadingData,
      required super.key});

  @override
  State<PropsListComponent> createState() => _PropsListComponentState();
}

class _PropsListComponentState extends State<PropsListComponent> {
  final EmployeeProfileBloc bloc = Modular.get<EmployeeProfileBloc>();
  bool isExpanded = false;

  void expande() {
    setState(() {
      isExpanded = !isExpanded;
      if (isExpanded) {
        widget.loadingData();
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        border: Border.all(),
        borderRadius: BorderRadius.circular(5),
      ),
      child: Column(
        children: [
          _header(),
          isExpanded
              ? Column(
                  children: [
                    _body(),
                    _buttons(context),
                  ],
                )
              : Container(),
        ],
      ),
    );
  }

  Widget _body() {
    return Padding(
      padding: const EdgeInsets.all(8.0),
      child: Column(
        children: widget.propsContainerList.isEmpty
            ? [Text("Não há ${widget.containerName}")]
            : widget.propsContainerList
                .asMap()
                .map(
                  (index, e) => MapEntry(
                    index,
                    PropsContainerComponent(
                      containerName: e.textProps.first.value,
                      isSavingData: widget.isSavingData,
                      saveContainerData: (changes) =>
                          widget.saveData(index, changes),
                      loadingContainerData: () {},
                      isLoading: widget.isLoading,
                      removeButton: widget.removeData == null
                          ? null
                          : () => widget.removeData!(index),
                      children: e.textProps
                          .map((prop) => TextEditComponent(textProp: prop))
                          .toList(),
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

  Widget _header() {
    return Container(
      padding: const EdgeInsets.all(8),
      decoration: BoxDecoration(
        border: isExpanded ? const Border(bottom: BorderSide()) : null,
        borderRadius: BorderRadius.circular(5),
      ),
      child: InkWell(
        onTap: () => expande(),
        child: Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            Text(
              widget.containerName,
              style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 16),
            ),
            const Icon(
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
          onPressed: () => widget.addData(),
          child: Text("Adicionar ${widget.containerName}"),
        ),
      ),
    );
  }
}
