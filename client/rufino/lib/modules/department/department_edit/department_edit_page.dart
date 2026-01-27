import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino/modules/department/department_edit/bloc/department_edit_bloc.dart';
import 'package:rufino/modules/department/domain/models/description.dart';
import 'package:rufino/modules/department/domain/models/name.dart';
import 'package:rufino/shared/components/error_components.dart';
import 'package:shimmer/shimmer.dart';

class DepartmentEditPage extends StatelessWidget {
  final DepartmentEditBloc bloc = Modular.get<DepartmentEditBloc>();
  final GlobalKey<FormState> _formKey = GlobalKey<FormState>();
  DepartmentEditPage(String? id, {super.key}) {
    bloc.add(InitializeEvent(
      Modular.args.params['id'],
    ));
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
              child: BlocBuilder<DepartmentEditBloc, DepartmentEditState>(
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

                  return Column(
                    crossAxisAlignment: CrossAxisAlignment.center,
                    children: [
                      Text(
                        'Setor',
                        style: TextStyle(
                          fontWeight: FontWeight.bold,
                          fontSize: 24,
                        ),
                      ),
                      const SizedBox(height: 16),
                      state.isLoading
                          ? _buildLoadingView(context)
                          : Form(
                              key: _formKey,
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.center,
                                children: [
                                  TextFormField(
                                    controller: TextEditingController(
                                        text: state.department.name.value),
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
                                        text:
                                            state.department.description.value),
                                    decoration: const InputDecoration(
                                      labelText: 'Descrição',
                                      border: OutlineInputBorder(),
                                    ),
                                    minLines: 2,
                                    maxLines: null,
                                    keyboardType: TextInputType.multiline,
                                    onSaved: (value) => bloc.add(
                                      ChangePropEvent(
                                          value: Description(value!)),
                                    ),
                                    validator: (value) {
                                      return Description.validate(value);
                                    },
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
                                          if (_formKey.currentState
                                                  ?.validate() ??
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
                            ),
                    ],
                  );
                },
              ),
            ),
          ),
        ),
      ),
    );
  }

  Widget _buildLoadingView(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.center,
      children: [
        Shimmer.fromColors(
          baseColor: Theme.of(context).colorScheme.surface,
          highlightColor: Theme.of(context).colorScheme.onSurface,
          child: TextFormField(
            enabled: false,
            decoration: const InputDecoration(
              border: OutlineInputBorder(),
            ),
          ),
        ),
        const SizedBox(height: 16),
        Shimmer.fromColors(
          baseColor: Theme.of(context).colorScheme.surface,
          highlightColor: Theme.of(context).colorScheme.onSurface,
          child: TextFormField(
            enabled: false,
            minLines: 2,
            maxLines: 2,
            decoration: const InputDecoration(
              border: OutlineInputBorder(),
            ),
          ),
        ),
      ],
    );
  }
}
