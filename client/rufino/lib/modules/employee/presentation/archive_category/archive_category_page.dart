import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino/modules/employee/domain/model/archive_category/archive_category.dart';
import 'package:rufino/modules/employee/domain/model/archive_category/description.dart';
import 'package:rufino/modules/employee/presentation/archive_category/bloc/archive_category_bloc.dart';
import 'package:rufino/modules/employee/presentation/components/enumeration_list_view_component.dart';
import 'package:rufino/modules/employee/presentation/components/props_container_component.dart';
import 'package:rufino/modules/employee/presentation/components/text_edit_component.dart';
import 'package:rufino/shared/components/error_components.dart';

class ArchiveCategoryPage extends StatelessWidget {
  final bloc = Modular.get<ArchiveCategoryBloc>();
  ArchiveCategoryPage({super.key}) {
    bloc.add(InitialEvent());
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          onPressed: () {
            Modular.to.navigate("/employee/");
          },
        ),
        title: const Text('Categorias de Arquivos'),
      ),
      body: Center(
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 1000),
          child: Padding(
            padding: const EdgeInsets.all(8.0),
            child: BlocBuilder<ArchiveCategoryBloc, ArchiveCategoryState>(
              bloc: bloc,
              builder: (context, state) {
                if (state.exception != null) {
                  ErrorComponent.showException(context, state.exception!,
                      () => Modular.to.popUntil((route) => route.isFirst));
                }
                if (state.snackMessage != null &&
                    state.snackMessage!.isNotEmpty) {
                  WidgetsBinding.instance.addPostFrameCallback((_) {
                    ScaffoldMessenger.of(context).showSnackBar(SnackBar(
                      content: Text(state.snackMessage!),
                    ));
                    bloc.add(SnackMessageWasShow());
                  });
                }

                return ListView(
                  children: state.archiveCategories.isNotEmpty
                      ? state.archiveCategories.map((category) {
                          return _archiveCategoryContainer(state, category);
                        }).toList()
                      : [
                          Center(
                              child: state.isLoading
                                  ? CircularProgressIndicator()
                                  : const Text("Nenhuma categoria encontrada")),
                        ],
                );
              },
            ),
          ),
        ),
      ),
      floatingActionButton: FloatingActionButton(
        onPressed: () => _dialogCreateEmployee(context),
        child: const Icon(Icons.add),
      ),
    );
  }

  Widget _archiveCategoryContainer(
      ArchiveCategoryState state, ArchiveCategory category) {
    return Column(
      children: [
        PropsContainerComponent(
            containerName: category.name,
            isSavingData: state.isSavingData,
            saveContainerData: (objs) => bloc.add(SaveEvent(category, objs)),
            loadingContainerData: () {},
            isLoading: state.isLoading,
            children: [
              TextEditComponent(textProp: category.description),
              EnumerationListViewComponent(
                enumerationsList: category.listenEvents,
                enumerationOptionsList: state.events,
              )
            ]),
        const SizedBox(
          height: 8,
        ),
      ],
    );
  }

  Future _dialogCreateEmployee(BuildContext context) {
    return showDialog(
        context: context,
        builder: (context) {
          var archiveCategory = new ArchiveCategory.empty();
          var _formkey = GlobalKey<FormState>();
          return AlertDialog(
            title: const Text('Cadastrar de Categoria'),
            content: SizedBox(
              width: 600,
              child: BlocBuilder<ArchiveCategoryBloc, ArchiveCategoryState>(
                  bloc: bloc,
                  builder: (context, state) {
                    return Form(
                      key: _formkey,
                      child: Column(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          TextFormField(
                            decoration: InputDecoration(
                              labelText: 'Nome da Categoria de Arquivos',
                              border: const OutlineInputBorder(),
                            ),
                            validator: archiveCategory.validateName,
                            onSaved: (newValue) {
                              archiveCategory = archiveCategory.copyWith(
                                name: newValue!,
                              );
                            },
                          ),
                          SizedBox(
                            height: 8,
                          ),
                          TextFormField(
                            decoration: InputDecoration(
                              labelText: 'Descrição da Categoria',
                              border: const OutlineInputBorder(),
                            ),
                            validator: archiveCategory.description.validate,
                            onSaved: (newValue) {
                              archiveCategory = archiveCategory.copyWith(
                                description: Description(newValue!),
                              );
                            },
                            maxLines: 5,
                            minLines: 1,
                          ),
                        ],
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
                  if (_formkey.currentState != null &&
                      _formkey.currentState!.validate()) {
                    _formkey.currentState!.save();
                    bloc.add(CreateNewArchiveCategoryEvent(archiveCategory));
                  }
                },
                child: const Text('Criar'),
              ),
            ],
          );
        });
  }
}
