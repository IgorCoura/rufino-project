import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino/modules/employee/presentation/document_template_list/bloc/document_template_list_bloc.dart';
import 'package:rufino/shared/components/error_components.dart';

class DocumentTemplateListPage extends StatelessWidget {
  final bloc = Modular.get<DocumentTemplateListBloc>();
  DocumentTemplateListPage({super.key}) {
    bloc.add(const InitialEvent());

    Modular.to.addListener(() {
      if (Modular.to.path == '/employee/document-template/') {
        bloc.add(InitialEvent());
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
            Modular.to.navigate('/employee');
          },
        ),
        title: const Text('Templates de Documentos'),
      ),
      body: Center(
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 1000),
          child: Padding(
            padding: EdgeInsets.all(8.0),
            child: BlocBuilder<DocumentTemplateListBloc,
                DocumentTemplateListState>(
              bloc: bloc,
              builder: (context, state) {
                if (state.exception != null) {
                  ErrorComponent.showException(context, state.exception!,
                      () => Modular.to.popUntil((route) => route.isFirst));
                }
                return state.isLoading
                    ? CircularProgressIndicator()
                    : state.documentTemplateList.isEmpty
                        ? Text("Lista Vazia")
                        : ListView(
                            children: state.documentTemplateList
                                .map((element) => ListTile(
                                      leading: const Icon(Icons.edit_document),
                                      title: Text(
                                        element.name,
                                        style: const TextStyle(
                                          fontWeight: FontWeight.bold,
                                        ),
                                      ),
                                      subtitle: Text(
                                        element.description,
                                        style: const TextStyle(
                                          fontSize: 12,
                                        ),
                                      ),
                                      onTap: () {
                                        Modular.to.pushNamed(
                                          'document-template/${element.id}',
                                        );
                                      },
                                    ))
                                .toList(),
                          );
              },
            ),
          ),
        ),
      ),
      floatingActionButton: FloatingActionButton(
        onPressed: () {
          Modular.to.pushNamed('document-template/new');
        },
        child: const Icon(Icons.add),
      ),
    );
  }
}
