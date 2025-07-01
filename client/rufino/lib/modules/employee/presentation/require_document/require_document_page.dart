import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino/modules/employee/domain/model/document_template/document_template_simple.dart';
import 'package:rufino/modules/employee/domain/model/require_document/event.dart';
import 'package:rufino/modules/employee/domain/model/require_document/status.dart';
import 'package:rufino/modules/employee/presentation/components/enumeration_view_component.dart';
import 'package:rufino/modules/employee/presentation/components/text_edit_component.dart';
import 'package:rufino/modules/employee/presentation/require_document/bloc/require_document_bloc.dart';
import 'package:rufino/shared/components/box_with_label.dart';
import 'package:rufino/shared/components/error_components.dart';

class RequireDocumentPage extends StatelessWidget {
  final bloc = Modular.get<RequireDocumentBloc>();
  final String requireDocumentId;
  final _formKey = GlobalKey<FormState>();
  RequireDocumentPage({required this.requireDocumentId, super.key}) {
    bloc.add(InitialEvent(requireDocumentId));
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
          title: const Text('Requerimento de Documentos'),
        ),
        body: Align(
          alignment: Alignment.topCenter,
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 1000),
            child: SingleChildScrollView(
              child: Padding(
                padding: const EdgeInsets.all(16.0),
                child: BlocBuilder<RequireDocumentBloc, RequireDocumentState>(
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

                    return Form(
                      key: _formKey,
                      child: Column(
                        children: [
                          TextEditComponent(
                            onSaveChanges: (change) =>
                                bloc.add(ChangeFieldValueEvent(change)),
                            isEditing: state.isEditing,
                            isLoading: state.isLoading,
                            textProp: state.requireDocument.name,
                          ),
                          SizedBox(
                            height: 8.0,
                          ),
                          TextEditComponent(
                            onSaveChanges: (change) =>
                                bloc.add(ChangeFieldValueEvent(change)),
                            isEditing: state.isEditing,
                            isLoading: state.isLoading,
                            textProp: state.requireDocument.description,
                          ),
                          SizedBox(
                            height: 8.0,
                          ),
                          EnumerationViewComponent(
                              isEditing: state.isEditing,
                              isLoading: state.isLoading,
                              onChanged: (associationType) {
                                bloc.add(AssociationTypeSelectedEvent(
                                    associationType));
                              },
                              onSaveChanges: (change) =>
                                  bloc.add(ChangeFieldValueEvent(change)),
                              enumeration:
                                  state.requireDocument.associationType,
                              listEnumerationOptions: state.associationTypes),
                          SizedBox(
                            height: 8.0,
                          ),
                          EnumerationViewComponent(
                              isEditing: state.isEditing,
                              isLoading: state.isLoading,
                              onSaveChanges: (change) =>
                                  bloc.add(ChangeFieldValueEvent(change)),
                              enumeration: state.requireDocument.association,
                              listEnumerationOptions: state.associations),
                          SizedBox(
                            height: 8.0,
                          ),
                          _documentsTemplate(context, state),
                          SizedBox(
                            height: 8.0,
                          ),
                          _eventsWidget(context, state),
                        ],
                      ),
                    );
                  },
                ),
              ),
            ),
          ),
        ),
        floatingActionButton: _floatingButtons());
  }

  Widget _documentsTemplate(BuildContext context, RequireDocumentState state) {
    return BoxWithLabel(
      label: "Templates de Documento",
      child: Column(
        children: [
          Column(
            children: state.requireDocument.documentTemplates
                .map((e) => ListTile(
                      leading: const Icon(Icons.edit_document),
                      title: Text(
                        e.name,
                        maxLines: 1,
                      ),
                      subtitle: Text(
                        e.description,
                        maxLines: 2,
                      ),
                      trailing: state.isEditing
                          ? IconButton(
                              onPressed: () =>
                                  bloc.add(RemoveDocumentTemplateEvent(e)),
                              icon: Icon(Icons.delete),
                              color: Colors.red,
                            )
                          : SizedBox(),
                      onTap: () {
                        Modular.to.pushNamed(
                          '../document-template/${e.id}',
                        );
                      },
                    ))
                .toList(),
          ),
          SizedBox(
            height: 8,
          ),
          state.isEditing
              ? OutlinedButton(
                  onPressed: () => _showAddDocumentTemplateDialog(context),
                  child: Text("Adicionar Template"),
                )
              : SizedBox(),
        ],
      ),
    );
  }

  Widget _eventsWidget(BuildContext context, RequireDocumentState state) {
    return BoxWithLabel(
        label: "Eventos Observados",
        child: Column(
          children: [
            Column(
              children: state.requireDocument.listenEvents
                  .map(
                    (e) => Column(
                      children: [
                        Container(
                          decoration: BoxDecoration(
                            border: Border.all(
                              color: Theme.of(context).colorScheme.primary,
                              width: 1.0,
                            ),
                            borderRadius: BorderRadius.circular(8.0),
                          ),
                          child: ExpansionTile(
                            controlAffinity: ListTileControlAffinity.leading,
                            title: Text(e.event.name),
                            trailing: state.isEditing
                                ? IconButton(
                                    onPressed: () =>
                                        bloc.add(RemoveEventEvent(e.event)),
                                    icon: Icon(Icons.delete),
                                    color: Colors.red,
                                  )
                                : SizedBox(),
                            children: [
                              Padding(
                                padding: const EdgeInsets.all(8.0),
                                child: BoxWithLabel(
                                  label: "Status",
                                  child: Column(
                                    children: e.statusList
                                        .map(
                                          (status) => Row(
                                            mainAxisAlignment:
                                                MainAxisAlignment.spaceBetween,
                                            children: [
                                              Padding(
                                                padding:
                                                    const EdgeInsets.all(8.0),
                                                child: Row(
                                                  children: [
                                                    Icon(Icons.circle,
                                                        size: 8.0),
                                                    SizedBox(
                                                      width: 8.0,
                                                    ),
                                                    Text(status.name),
                                                  ],
                                                ),
                                              ),
                                              state.isEditing
                                                  ? IconButton(
                                                      onPressed: () => bloc.add(
                                                          RemoveStatusEvent(
                                                              status, e.event)),
                                                      icon: Icon(Icons.delete),
                                                      color: Colors.red,
                                                    )
                                                  : const SizedBox(),
                                            ],
                                          ),
                                        )
                                        .toList(),
                                  ),
                                ),
                              ),
                              state.isEditing
                                  ? OutlinedButton(
                                      onPressed: () => _showAddStatusDialog(
                                          context, e.event),
                                      child: Text("Adicionar Status"),
                                    )
                                  : const SizedBox(),
                              SizedBox(
                                height: 8,
                              )
                            ],
                          ),
                        ),
                        SizedBox(
                          height: 8,
                        )
                      ],
                    ),
                  )
                  .toList(),
            ),
            SizedBox(
              height: 8,
            ),
            state.isEditing
                ? OutlinedButton(
                    onPressed: () => _showAddEventDialog(context),
                    child: Text("Adicionar Evento"),
                  )
                : const SizedBox(),
          ],
        ));
  }

  Widget _floatingButtons() {
    return BlocBuilder<RequireDocumentBloc, RequireDocumentState>(
      bloc: bloc,
      builder: (context, state) {
        return state.isLoading
            ? CircularProgressIndicator()
            : state.isEditing
                ? Row(
                    mainAxisAlignment: MainAxisAlignment.end,
                    children: [
                      FloatingActionButton.extended(
                          heroTag: "save",
                          onPressed: () {
                            if (_formKey.currentState != null &&
                                _formKey.currentState!.validate()) {
                              _formKey.currentState!.save();

                              bloc.add(SaveEvent());
                            }
                          },
                          label: const Text("Salvar"),
                          icon: const Icon(Icons.save)),
                      SizedBox(
                        width: 8.0,
                      ),
                      FloatingActionButton.extended(
                          heroTag: "cancel",
                          onPressed: () {
                            _formKey.currentState!.reset();
                            bloc.add(CancelEditEvent());
                          },
                          label: const Text("Cancelar"),
                          icon: const Icon(Icons.cancel)),
                    ],
                  )
                : FloatingActionButton.extended(
                    heroTag: "edit",
                    onPressed: () => bloc.add(EditEvent()),
                    label: const Text("Editar"),
                    icon: const Icon(Icons.edit),
                  );
      },
    );
  }

  Future<void> _showAddDocumentTemplateDialog(BuildContext context) async {
    await showDialog(
      context: context,
      builder: (context) {
        var selectedItem = DocumentTemplateSimple.empty();
        var dropFormKey = GlobalKey<FormState>();
        return AlertDialog(
          title: const Text("Adicionar Template de Documento"),
          content: BlocBuilder<RequireDocumentBloc, RequireDocumentState>(
            bloc: bloc,
            builder: (context, state) {
              selectedItem = state.documentTemplates.isNotEmpty
                  ? state.documentTemplates.first
                  : DocumentTemplateSimple.empty();
              return DropdownButtonFormField(
                key: dropFormKey,
                value: selectedItem,
                items: bloc.state.documentTemplates
                    .map((item) => DropdownMenuItem(
                          value: item,
                          child: Text(item.name),
                        ))
                    .toList(),
                onChanged: (newValue) {
                  if (newValue != null) {
                    selectedItem = newValue;
                  }
                },
                decoration: const InputDecoration(
                  labelText: "Selecione um item",
                  border: OutlineInputBorder(),
                ),
              );
            },
          ),
          actions: [
            TextButton(
              onPressed: () {
                Navigator.of(context).pop();
              },
              child: const Text("Cancelar"),
            ),
            ElevatedButton(
              onPressed: () {
                Navigator.of(context).pop();

                bloc.add(AddDocumentTemplateEvent(selectedItem));
              },
              child: const Text("Adicionar"),
            ),
          ],
        );
      },
    );
  }

  Future<void> _showAddEventDialog(BuildContext context) async {
    await showDialog(
      context: context,
      builder: (context) {
        var selectedItem = Event.empty();
        var dropFormKey = GlobalKey<FormState>();
        return AlertDialog(
          title: const Text("Adicionar Evento Observado"),
          content: BlocBuilder<RequireDocumentBloc, RequireDocumentState>(
            bloc: bloc,
            builder: (context, state) {
              selectedItem =
                  state.events.isNotEmpty ? state.events.first : Event.empty();
              return DropdownButtonFormField(
                key: dropFormKey,
                value: selectedItem,
                items: bloc.state.events
                    .map((item) => DropdownMenuItem(
                          value: item,
                          child: Text(item.name),
                        ))
                    .toList(),
                onChanged: (newValue) {
                  if (newValue != null) {
                    selectedItem = newValue;
                  }
                },
                decoration: const InputDecoration(
                  labelText: "Selecione um item",
                  border: OutlineInputBorder(),
                ),
              );
            },
          ),
          actions: [
            TextButton(
              onPressed: () {
                Navigator.of(context).pop();
              },
              child: const Text("Cancelar"),
            ),
            ElevatedButton(
              onPressed: () {
                Navigator.of(context).pop();
                bloc.add(AddEventEvent(selectedItem));
              },
              child: const Text("Adicionar"),
            ),
          ],
        );
      },
    );
  }

  Future<void> _showAddStatusDialog(BuildContext context, Event event) async {
    await showDialog(
      context: context,
      builder: (context) {
        var selectedItem = Status.empty();
        var dropFormKey = GlobalKey<FormState>();
        return AlertDialog(
          title: const Text("Adicionar Status"),
          content: BlocBuilder<RequireDocumentBloc, RequireDocumentState>(
            bloc: bloc,
            builder: (context, state) {
              selectedItem = state.listStatus.isNotEmpty
                  ? state.listStatus.first
                  : Status.empty();
              return DropdownButtonFormField(
                key: dropFormKey,
                value: selectedItem,
                items: bloc.state.listStatus
                    .map((item) => DropdownMenuItem(
                          value: item,
                          child: Text(item.name),
                        ))
                    .toList(),
                onChanged: (newValue) {
                  if (newValue != null) {
                    selectedItem = newValue;
                  }
                },
                decoration: const InputDecoration(
                  labelText: "Selecione um item",
                  border: OutlineInputBorder(),
                ),
              );
            },
          ),
          actions: [
            TextButton(
              onPressed: () {
                Navigator.of(context).pop();
              },
              child: const Text("Cancelar"),
            ),
            ElevatedButton(
              onPressed: () {
                Navigator.of(context).pop();

                bloc.add(AddStatusEvent(selectedItem, event));
              },
              child: const Text("Adicionar"),
            ),
          ],
        );
      },
    );
  }
}
