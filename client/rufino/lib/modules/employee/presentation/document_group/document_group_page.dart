import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino/modules/employee/domain/model/document_group/description.dart';
import 'package:rufino/modules/employee/domain/model/document_group/name.dart';
import 'package:rufino/modules/employee/presentation/document_group/bloc/document_group_bloc.dart';

class DocumentGroupPage extends StatelessWidget {
  final bloc = Modular.get<DocumentGroupBloc>();
  DocumentGroupPage({super.key}) {
    bloc.add(LoadDocumentGroups());
    Modular.to.addListener(() {
      if (Modular.to.path == '/employee/document-group/') {
        bloc.add(LoadDocumentGroups());
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          onPressed: () {
            Navigator.of(context).pop();
          },
        ),
        title: const Text('Groupo de Documentos'),
      ),
      body: Center(
          child: ConstrainedBox(
        constraints: const BoxConstraints(maxWidth: 1000),
        child: Padding(
          padding: EdgeInsets.all(8),
          child: BlocBuilder<DocumentGroupBloc, DocumentGroupState>(
            bloc: bloc,
            builder: (context, state) {
              return state.documentGroups.isEmpty
                  ? Center(
                      child: Text('Nenhum grupo de documentos encontrado.'))
                  : ListView(
                      children: state.documentGroups.map((group) {
                        return ListTile(
                          leading: const Icon(Icons.file_copy_rounded),
                          title: Text(group.name.value),
                          subtitle: Text(group.description.value),
                          onTap: () {
                            _dialogCreateEmployee(context, true,
                                id: group.id,
                                name: group.name.value,
                                description: group.description.value);
                          },
                        );
                      }).toList(),
                    );
            },
          ),
        ),
      )),
      floatingActionButton: FloatingActionButton(
        onPressed: () {
          _dialogCreateEmployee(context, false);
        },
        child: const Icon(Icons.add),
      ),
    );
  }

  Future _dialogCreateEmployee(BuildContext context, bool isEdit,
      {String id = "", String name = "", String description = ""}) {
    return showDialog(
        context: context,
        builder: (context) {
          return AlertDialog(
            title:
                Text(isEdit ? 'Editar Funcionário' : 'Cadastrar Funcionário'),
            content: SizedBox(
              width: 600,
              child: BlocBuilder<DocumentGroupBloc, DocumentGroupState>(
                  bloc: bloc,
                  builder: (context, state) {
                    return Column(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        TextFormField(
                          onChanged: (value) => name = value,
                          decoration: InputDecoration(
                            labelText: 'Nome do grupo',
                            border: const OutlineInputBorder(),
                          ),
                          initialValue: name.isNotEmpty ? name : null,
                          validator: (value) {
                            return Name.validate(value);
                          },
                        ),
                        const SizedBox(height: 16),
                        TextFormField(
                          onChanged: (value) => description = value,
                          decoration: InputDecoration(
                            labelText: 'Descrição do grupo',
                            border: const OutlineInputBorder(),
                          ),
                          initialValue:
                              description.isNotEmpty ? description : null,
                          validator: (value) {
                            return Description.validate(value);
                          },
                        ),
                      ],
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
                  if (isEdit) {
                    bloc.add(UpdateDocumentGroup(
                      id,
                      name,
                      description,
                    ));
                  } else {
                    bloc.add(CreateDocumentGroup(name, description));
                  }
                  Navigator.of(context).pop();
                },
                child: Text(isEdit ? 'Salvar' : 'Criar'),
              ),
            ],
          );
        });
  }
}
