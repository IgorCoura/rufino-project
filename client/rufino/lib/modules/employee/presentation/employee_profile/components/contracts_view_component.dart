import 'package:flutter/material.dart';
import 'package:mask_text_input_formatter/mask_text_input_formatter.dart';
import 'package:rufino/modules/employee/domain/model/employee_contract.dart';
import 'package:rufino/modules/employee/domain/model/employee_contract_type.dart';

class ContractsViewComponent extends StatefulWidget {
  final Function loadingContainerData;
  final Function(String finalDate) onFineshedContract;
  final List<EmployeeContract> contracts;
  final bool isSavingData;
  final List<EmployeeContractType> listContractTypeOptions;
  const ContractsViewComponent(
      {required this.loadingContainerData,
      required this.onFineshedContract,
      required this.contracts,
      required this.isSavingData,
      required this.listContractTypeOptions,
      super.key});

  @override
  State<ContractsViewComponent> createState() => _ContractsViewComponentState();
}

class _ContractsViewComponentState extends State<ContractsViewComponent> {
  bool _isExpanded = false;
  final _formKey = GlobalKey<FormState>();
  String date = "";

  void expand() {
    setState(() {
      _isExpanded = !_isExpanded;
      if (_isExpanded) {
        widget.loadingContainerData();
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
          _isExpanded
              ? Column(children: [
                  Column(
                    children: widget.contracts
                        .map((el) => _bodyItem(
                            context,
                            EmployeeContract.displayName,
                            el.initDate,
                            el.finalDate ?? "__________ ",
                            el.type.name))
                        .toList(),
                  ),
                  widget.isSavingData
                      ? const CircularProgressIndicator()
                      : widget.contracts.any((el) => el.finalDate == null) ==
                              true
                          ? _buttonFinishedContract(context)
                          : const SizedBox(),
                ])
              : const SizedBox(),
        ],
      ),
    );
  }

  Widget _buttonFinishedContract(BuildContext context) {
    return Container(
      alignment: Alignment.centerRight,
      child: Padding(
        padding: const EdgeInsets.fromLTRB(0, 0, 8, 8),
        child: FilledButton(
          style: ButtonStyle(
              backgroundColor:
                  WidgetStatePropertyAll(Theme.of(context).colorScheme.error)),
          onPressed: () => _confirmAction(context, "Finalizar Contrato",
              (value) => EmployeeContract.validate(value), () {
            if (_formKey.currentState != null &&
                _formKey.currentState!.validate()) {
              widget.onFineshedContract(date);
              Navigator.pop(context);
            }
          }),
          child: const Text("Finalizar Contrato"),
        ),
      ),
    );
  }

  Widget _bodyItem(BuildContext context, String displayName, String initDate,
      String finalDate, String type) {
    return Padding(
      padding: const EdgeInsets.all(8.0),
      child: Column(children: [
        _textContainer(
          context,
          displayName,
          initDate,
          finalDate,
          type,
        ),
        const SizedBox(
          height: 16,
        ),
      ]),
    );
  }

  Widget _header() {
    return Container(
      padding: const EdgeInsets.all(8),
      decoration: BoxDecoration(
        border: _isExpanded ? const Border(bottom: BorderSide()) : null,
        borderRadius: BorderRadius.circular(5),
      ),
      child: InkWell(
        onTap: () => expand(),
        child: Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            Text(
              "Contratos",
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

  Widget _textContainer(BuildContext context, String displayName,
      String initDate, String finalDate, String type) {
    return Stack(
      children: [
        Padding(
          padding: const EdgeInsets.only(top: 8),
          child: Container(
              width: double.infinity,
              padding: const EdgeInsets.all(12),
              decoration: BoxDecoration(
                border:
                    Border.all(color: Theme.of(context).colorScheme.primary),
                borderRadius: BorderRadius.circular(5),
              ),
              child: Row(
                children: [
                  Text("Data de Início: ",
                      style: TextStyle(fontWeight: FontWeight.bold)),
                  Text(initDate),
                  Text(" - "),
                  Text("Data de Finalização: ",
                      style: TextStyle(fontWeight: FontWeight.bold)),
                  Text(finalDate),
                  Text(" - "),
                  Text("Tipo de contrato: ",
                      style: TextStyle(fontWeight: FontWeight.bold)),
                  Text(type)
                ],
              )),
        ),
        _labelText(context, displayName),
      ],
    );
  }

  Widget _labelText(BuildContext context, String displayName) {
    return Positioned(
      left: 10,
      top: -2,
      child: Container(
        color: Theme.of(context).colorScheme.surface,
        padding: const EdgeInsets.symmetric(horizontal: 5),
        child: Text(
          "Contrato",
          style: TextStyle(
              color: Theme.of(context).colorScheme.primary, fontSize: 12),
        ),
      ),
    );
  }

  void _confirmAction(BuildContext context, String title,
      Function(String?) validation, Function() onPressed) {
    WidgetsBinding.instance.addPostFrameCallback((_) {
      showDialog(
          barrierDismissible: false,
          context: context,
          builder: (_) {
            return AlertDialog(
              title: Text(title),
              content: SizedBox(
                  width: 400,
                  child: Form(
                    key: _formKey,
                    child: _dataFrom(validation),
                  )),
              actions: [
                TextButton(
                  onPressed: () => Navigator.pop(context),
                  child: const Text("Cancelar"),
                ),
                FilledButton(
                  onPressed: () {
                    onPressed();
                  },
                  child: const Text("Confirmar"),
                ),
              ],
            );
          });
    });
  }

  Widget _dataFrom(Function(String?) validation) {
    return TextFormField(
      inputFormatters: [
        MaskTextInputFormatter(
            mask: '##/##/####',
            filter: {"#": RegExp(r'[0-9]')},
            type: MaskAutoCompletionType.lazy)
      ],
      keyboardType: TextInputType.datetime,
      controller: TextEditingController(),
      enabled: true,
      decoration: InputDecoration(
        labelText: "Data de Finalização",
        border: const OutlineInputBorder(),
      ),
      style: TextStyle(color: Theme.of(context).colorScheme.onSurface),
      validator: (value) => validation(value),
      onChanged: (value) => {date = value},
    );
  }
}
