import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino/modules/employee/domain/model/document_template/document_template.dart';
import 'package:rufino/modules/employee/presentation/components/props_container_component.dart';
import 'package:rufino/modules/employee/presentation/document_template/bloc/document_template_bloc.dart';
import 'package:rufino/shared/components/error_components.dart';

class DocumentTemplatePage extends StatelessWidget {
  final bloc = Modular.get<DocumentTemplateBloc>();
  DocumentTemplatePage({super.key}) {
    bloc.add(InitialEvent());
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          onPressed: () {
            Modular.to.navigate("/employee/list");
          },
        ),
        title: const Text('Templates de Documentos'),
      ),
      body: Center(
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 1000),
          child: Padding(
            padding: const EdgeInsets.all(8.0),
            child: BlocBuilder<DocumentTemplateBloc, DocumentTemplateState>(
              bloc: bloc,
              builder: (context, state) {
                if (state.exception != null) {
                  ErrorComponent.showException(context, state.exception!,
                      () => Modular.to.navigate("/home"));
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
                  children: state.documentTemplates.isNotEmpty
                      ? state.documentTemplates.map((template) {
                          return _documentTemplateContainer(state, template);
                        }).toList()
                      : [
                          Center(
                              child: state.isLoading
                                  ? CircularProgressIndicator()
                                  : const Text("Nenhuma template encontrada")),
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

  Widget _documentTemplateContainer(
      DocumentTemplateState state, DocumentTemplate template) {
    return Column(
      children: [
        PropsContainerComponent(
          containerName: template.name.value,
          isSavingData: state.isSavingData,
          saveContainerData: (objs) => {},
          loadingContainerData: () {},
          isLoading: state.isLoading,
          children: [],
        ),
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
          var _formkey = GlobalKey<FormState>();
          return AlertDialog(
            title: const Text('Cadastrar de Categoria'),
            content: SizedBox(
              width: 600,
              child: BlocBuilder<DocumentTemplateBloc, DocumentTemplateState>(
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
                            // validator: archiveCategory.validateName,
                            // onSaved: (newValue) {
                            //   archiveCategory = archiveCategory.copyWith(
                            //       // name: newValue!,
                            //       );
                            // },
                          ),
                          SizedBox(
                            height: 8,
                          ),
                          TextFormField(
                            decoration: InputDecoration(
                              labelText: 'Descrição da Categoria',
                              border: const OutlineInputBorder(),
                            ),
                            // validator: archiveCategory.description.validate,
                            // onSaved: (newValue) {
                            //   archiveCategory = archiveCategory.copyWith(
                            //       // description: Description(newValue!),
                            //       );
                            // },
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
                    // bloc.add(CreateNewArchiveCategoryEvent(archiveCategory));
                  }
                },
                child: const Text('Criar'),
              ),
            ],
          );
        });
  }
}
