import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino/modules/employee/presentation/require_document_list/bloc/require_document_list_bloc.dart';
import 'package:rufino/shared/components/error_components.dart';

class RequireDocumentListPage extends StatelessWidget {
  final RequireDocumentListBloc bloc = Modular.get<RequireDocumentListBloc>();
  RequireDocumentListPage({super.key}) {
    bloc.add(const InitialEvent());
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          onPressed: () {
            Modular.to.pop();
          },
        ),
        title: const Text('Requerimentos de Documentos'),
      ),
      body: Center(
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 1000),
          child: Padding(
            padding: EdgeInsets.all(8.0),
            child:
                BlocBuilder<RequireDocumentListBloc, RequireDocumentListState>(
              bloc: bloc,
              builder: (context, state) {
                if (state.exception != null) {
                  ErrorComponent.showException(context, state.exception!,
                      () => Modular.to.popUntil((route) => route.isFirst));
                }
                return state.isLoading
                    ? CircularProgressIndicator()
                    : state.requireDocumentList.isEmpty
                        ? Text("Lista Vazia")
                        : ListView(
                            children: state.requireDocumentList
                                .map((element) => ListTile(
                                      leading: const Icon(Icons.edit_document),
                                      title: Text(element.name),
                                      subtitle: Text(element.description),
                                      onTap: () {
                                        Modular.to.pushNamed(
                                          'require-documents/${element.id}',
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
          Modular.to.pushNamed('require-documents/new');
        },
        child: const Icon(Icons.add),
      ),
    );
  }
}
