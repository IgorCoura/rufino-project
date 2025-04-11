import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino/modules/employee/domain/model/archive_category/archive_category.dart';
import 'package:rufino/modules/employee/domain/model/archive_category/description.dart';
import 'package:rufino/modules/employee/domain/model/archive_category/event.dart';
import 'package:rufino/modules/employee/domain/model/archive_category/listen_events.dart';
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

  final ArchiveCategory exampleCategory = ArchiveCategory(
    "1",
    "Categoria Exemplo",
    Description("Descrição Exemplo AA"),
    ListenEvents(
      [
        Event("1", "Event1"),
        Event("3", "Evento 2"),
      ],
    ),
  );

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
                      () => Modular.to.navigate("/home"));
                }
                return ListView(
                  children: state.archiveCategories.isNotEmpty
                      ? state.archiveCategories.map((category) {
                          return _archiveCategoryContainer(state, category);
                        }).toList()
                      : [
                          Center(
                              child:
                                  const Text("Nenhuma categoria encontrada")),
                        ],
                );
              },
            ),
          ),
        ),
      ),
    );
  }

  Widget _archiveCategoryContainer(
      ArchiveCategoryState state, ArchiveCategory category) {
    return Column(
      children: [
        PropsContainerComponent(
            containerName: category.name,
            isSavingData: false,
            saveContainerData: (objs) {},
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
}
