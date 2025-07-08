import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino/modules/workplace/domain/model/address_prop.dart';
import 'package:rufino/modules/workplace/domain/model/workplace.dart';
import 'package:rufino/modules/workplace/presentation/workplace_edit/bloc/workplace_edit_bloc.dart';
import 'package:rufino/shared/components/box_with_label.dart';
import 'package:rufino/shared/components/error_components.dart';

class WorkplaceEditPage extends StatelessWidget {
  final bloc = Modular.get<WorkplaceEditBloc>();
  final _formKey = GlobalKey<FormState>();
  WorkplaceEditPage(String? id, {super.key}) {
    bloc.add(InitializeWorkplaceEvent(id));
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
              child: BlocBuilder<WorkplaceEditBloc, WorkplaceEditState>(
                bloc: bloc,
                builder: (context, state) {
                  if (state.exception != null) {
                    ErrorComponent.showException(context, state.exception!,
                        () => Modular.to.navigate('/'));
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
                          'Local de Trabalho',
                          style: TextStyle(
                            fontWeight: FontWeight.bold,
                            fontSize: 24,
                          ),
                        ),
                        const SizedBox(height: 16),
                        TextFormField(
                          controller:
                              TextEditingController(text: state.workplace.name),
                          decoration: const InputDecoration(
                            labelText: 'Nome',
                            border: OutlineInputBorder(),
                          ),
                          onSaved: (value) => bloc.add(
                            ChangePropEvent(value: value!),
                          ),
                          validator: (value) {
                            return Workplace.validateName(value);
                          },
                        ),
                        const SizedBox(height: 16),
                        BoxWithLabel(
                          label: 'Endereço',
                          child: Column(
                            children: [
                              const SizedBox(height: 8),
                              TextFormField(
                                controller: TextEditingController(
                                    text:
                                        state.workplace.address.zipCode.value),
                                decoration: const InputDecoration(
                                  labelText: 'CEP',
                                  border: OutlineInputBorder(),
                                ),
                                onSaved: (value) => bloc.add(
                                  ChangePropEvent(value: ZipCodeProp(value!)),
                                ),
                                validator: (value) {
                                  return ZipCodeProp.validate(value);
                                },
                              ),
                              const SizedBox(height: 16),
                              TextFormField(
                                controller: TextEditingController(
                                    text: state.workplace.address.street.value),
                                decoration: const InputDecoration(
                                  labelText: 'Rua',
                                  border: OutlineInputBorder(),
                                ),
                                onSaved: (value) => bloc.add(
                                  ChangePropEvent(value: StreetProp(value!)),
                                ),
                                validator: (value) {
                                  return StreetProp.validate(value);
                                },
                              ),
                              const SizedBox(height: 16),
                              TextFormField(
                                controller: TextEditingController(
                                    text: state.workplace.address.number.value),
                                decoration: const InputDecoration(
                                  labelText: 'Número',
                                  border: OutlineInputBorder(),
                                ),
                                onSaved: (value) => bloc.add(
                                  ChangePropEvent(value: NumberProp(value!)),
                                ),
                                validator: (value) {
                                  return NumberProp.validate(value);
                                },
                              ),
                              const SizedBox(height: 16),
                              TextFormField(
                                controller: TextEditingController(
                                    text: state
                                        .workplace.address.complement.value),
                                decoration: const InputDecoration(
                                  labelText: 'Complemento',
                                  border: OutlineInputBorder(),
                                ),
                                onSaved: (value) => bloc.add(
                                  ChangePropEvent(
                                      value: ComplementProp(value!)),
                                ),
                                validator: (value) {
                                  return ComplementProp.validate(value);
                                },
                              ),
                              const SizedBox(height: 16),
                              TextFormField(
                                controller: TextEditingController(
                                    text: state
                                        .workplace.address.neighborhood.value),
                                decoration: const InputDecoration(
                                  labelText: 'Bairro',
                                  border: OutlineInputBorder(),
                                ),
                                onSaved: (value) => bloc.add(
                                  ChangePropEvent(
                                      value: NeighborhoodProp(value!)),
                                ),
                                validator: (value) {
                                  return NeighborhoodProp.validate(value);
                                },
                              ),
                              const SizedBox(height: 16),
                              TextFormField(
                                controller: TextEditingController(
                                    text: state.workplace.address.city.value),
                                decoration: const InputDecoration(
                                  labelText: 'Cidade',
                                  border: OutlineInputBorder(),
                                ),
                                onSaved: (value) => bloc.add(
                                  ChangePropEvent(value: CityProp(value!)),
                                ),
                                validator: (value) {
                                  return CityProp.validate(value);
                                },
                              ),
                              const SizedBox(height: 16),
                              TextFormField(
                                controller: TextEditingController(
                                    text: state.workplace.address.state.value),
                                decoration: const InputDecoration(
                                  labelText: 'Estado',
                                  border: OutlineInputBorder(),
                                ),
                                onSaved: (value) => bloc.add(
                                  ChangePropEvent(value: StateProp(value!)),
                                ),
                                validator: (value) {
                                  return StateProp.validate(value);
                                },
                              ),
                              const SizedBox(height: 16),
                              TextFormField(
                                controller: TextEditingController(
                                    text:
                                        state.workplace.address.country.value),
                                decoration: const InputDecoration(
                                  labelText: 'País',
                                  border: OutlineInputBorder(),
                                ),
                                onSaved: (value) => bloc.add(
                                  ChangePropEvent(value: CountryProp(value!)),
                                ),
                                validator: (value) {
                                  return CountryProp.validate(value);
                                },
                              ),
                            ],
                          ),
                        ),
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
                                Modular.to.navigate('/workplace/');
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
