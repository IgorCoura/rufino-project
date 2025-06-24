import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino/modules/department/domain/models/CBO.dart';
import 'package:rufino/modules/department/domain/models/description.dart';
import 'package:rufino/modules/department/domain/models/name.dart';
import 'package:rufino/modules/department/domain/models/remuneration.dart';
import 'package:rufino/modules/department/role_edit/bloc/role_edit_bloc.dart';
import 'package:rufino/shared/components/box_with_label.dart';
import 'package:rufino/shared/components/error_components.dart';

class RoleEditPage extends StatelessWidget {
  final _formKey = GlobalKey<FormState>();
  final RoleEditBloc bloc = Modular.get<RoleEditBloc>();
  RoleEditPage(String? id, String positionId, {super.key}) {
    bloc.add(InitializeEvent(id, positionId));
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Padding(
        padding: const EdgeInsets.all(16.0),
        child: SingleChildScrollView(
          child: Center(
            child: Container(
              padding: const EdgeInsets.all(16.0),
              width: 1000,
              child: BlocBuilder<RoleEditBloc, RoleEditState>(
                bloc: bloc,
                builder: (context, state) {
                  if (state.exception != null) {
                    ErrorComponent.showException(context, state.exception!,
                        () => Modular.to.navigate('/department/'));
                  }
                  if (state.snackMessage != null &&
                      state.snackMessage!.isNotEmpty) {
                    WidgetsBinding.instance.addPostFrameCallback((_) {
                      ScaffoldMessenger.of(context).showSnackBar(SnackBar(
                        content: Text(state.snackMessage!),
                      ));
                      bloc.add(SnackMessageWasShownEvent());
                    });
                  }

                  return Form(
                    key: _formKey,
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.center,
                      children: [
                        Text(
                          'Função',
                          style: TextStyle(
                            fontWeight: FontWeight.bold,
                            fontSize: 24,
                          ),
                        ),
                        const SizedBox(height: 16),
                        TextFormField(
                          controller: TextEditingController(
                              text: state.role.name.value),
                          decoration: const InputDecoration(
                            labelText: 'Nome',
                            border: OutlineInputBorder(),
                          ),
                          onSaved: (value) => bloc.add(
                            ChangePropEvent(value: Name(value!)),
                          ),
                          validator: (value) {
                            return Name.validate(value);
                          },
                        ),
                        const SizedBox(height: 16),
                        TextFormField(
                          controller: TextEditingController(
                              text: state.role.description.value),
                          decoration: const InputDecoration(
                            labelText: 'Descrição',
                            border: OutlineInputBorder(),
                          ),
                          onSaved: (value) => bloc.add(
                            ChangePropEvent(value: Description(value!)),
                          ),
                          validator: (value) {
                            return Description.validate(value);
                          },
                        ),
                        const SizedBox(height: 16),
                        TextFormField(
                          controller:
                              TextEditingController(text: state.role.cbo.value),
                          decoration: const InputDecoration(
                            labelText: 'CBO',
                            border: OutlineInputBorder(),
                          ),
                          onSaved: (value) => bloc.add(
                            ChangePropEvent(value: CBO(value!)),
                          ),
                          validator: (value) {
                            return CBO.validate(value);
                          },
                        ),
                        const SizedBox(height: 16),
                        BoxWithLabel(
                            label: "Remuneração",
                            child: Column(
                              children: [
                                DropdownButtonFormField(
                                  value: state.paymentUnits.any((e) =>
                                          e.id ==
                                          state
                                              .role.remuneration.paymentUnit.id)
                                      ? state.role.remuneration.paymentUnit.id
                                      : null,
                                  items: state.paymentUnits
                                      .map((e) => DropdownMenuItem(
                                            value: e.id,
                                            child: Text(e.name),
                                          ))
                                      .toList(),
                                  onChanged: (value) {},
                                  onSaved: (newValue) {
                                    bloc.add(ChangePropEvent(
                                        value: PaymentUnit(newValue!, "")));
                                  },
                                  decoration: InputDecoration(
                                    labelText: "Unidade de Pagamento",
                                    border: const OutlineInputBorder(),
                                  ),
                                ),
                                const SizedBox(height: 16),
                                TextFormField(
                                  controller: TextEditingController(
                                      text: state.role.remuneration.baseSalary
                                          .value.value
                                          .toString()),
                                  decoration: const InputDecoration(
                                    labelText: 'Valor',
                                    border: OutlineInputBorder(),
                                  ),
                                  onSaved: (value) => bloc.add(
                                    ChangePropEvent(
                                        value: MonetaryValue(value!)),
                                  ),
                                  validator: MonetaryValue.validate,
                                ),
                                const SizedBox(height: 16),
                                DropdownButtonFormField(
                                  value: state.salaryTypes.any((e) =>
                                          e.id ==
                                          state.role.remuneration.baseSalary
                                              .type.id)
                                      ? state
                                          .role.remuneration.baseSalary.type.id
                                      : null,
                                  items: state.salaryTypes
                                      .map((e) => DropdownMenuItem(
                                            value: e.id,
                                            child: Text(e.name),
                                          ))
                                      .toList(),
                                  onChanged: (value) {},
                                  onSaved: (newValue) {
                                    bloc.add(ChangePropEvent(
                                        value: SalaryType(newValue!, "")));
                                  },
                                  decoration: InputDecoration(
                                    labelText: "Tipo de Monetário",
                                    border: const OutlineInputBorder(),
                                  ),
                                ),
                                const SizedBox(height: 16),
                                TextFormField(
                                  controller: TextEditingController(
                                      text: state
                                          .role.remuneration.description.value),
                                  decoration: const InputDecoration(
                                    labelText: 'Descrição',
                                    border: OutlineInputBorder(),
                                  ),
                                  onSaved: (value) => bloc.add(
                                    ChangePropEvent(
                                        value: SecundaryDescription(value!)),
                                  ),
                                  validator: (value) {
                                    return Description.validate(value);
                                  },
                                ),
                              ],
                            )),
                        const SizedBox(height: 16),
                        Row(
                          mainAxisAlignment: MainAxisAlignment.center,
                          children: [
                            FilledButton(
                              style: FilledButton.styleFrom(
                                padding: const EdgeInsets.symmetric(
                                  horizontal: 32,
                                  vertical: 16,
                                ),
                              ),
                              onPressed: () {
                                if (_formKey.currentState?.validate() ??
                                    false) {
                                  _formKey.currentState?.save();
                                  bloc.add(SaveChangesEvent());
                                }
                              },
                              child: const Text('Salvar'),
                            ),
                            const SizedBox(width: 16),
                            TextButton(
                              style: TextButton.styleFrom(
                                padding: const EdgeInsets.symmetric(
                                  horizontal: 32,
                                  vertical: 16,
                                ),
                              ),
                              onPressed: () {
                                Modular.to.navigate('/department/');
                              },
                              child: const Text('Cancelar'),
                            ),
                          ],
                        ),
                      ],
                    ),
                  );
                },
              ),
            ),
          ),
        ),
      ),
    );
  }
}
