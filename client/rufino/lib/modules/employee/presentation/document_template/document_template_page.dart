import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino/modules/employee/domain/model/document_template/place_signature.dart';
import 'package:rufino/modules/employee/presentation/components/enumeration_view_component.dart';
import 'package:rufino/modules/employee/presentation/components/text_edit_component.dart';
import 'package:rufino/modules/employee/presentation/document_template/bloc/document_template_bloc.dart';
import 'package:rufino/shared/components/box_with_label.dart';
import 'package:rufino/shared/components/error_components.dart';

class DocumentTemplatePage extends StatelessWidget {
  final bloc = Modular.get<DocumentTemplateBloc>();
  final String documentTemplateId;
  final _formKey = GlobalKey<FormState>();
  DocumentTemplatePage({required this.documentTemplateId, super.key}) {
    bloc.add(InitialEvent(documentTemplateId));
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
        title: const Text('Template de Documento'),
      ),
      body: Align(
        alignment: Alignment.topCenter,
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 1000),
          child: SingleChildScrollView(
            child: Padding(
              padding: const EdgeInsets.all(16.0),
              child: BlocBuilder<DocumentTemplateBloc, DocumentTemplateState>(
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
                          textProp: state.documentTemplate.name,
                        ),
                        SizedBox(
                          height: 8.0,
                        ),
                        TextEditComponent(
                          onSaveChanges: (change) =>
                              bloc.add(ChangeFieldValueEvent(change)),
                          isEditing: state.isEditing,
                          isLoading: state.isLoading,
                          textProp: state.documentTemplate.description,
                        ),
                        SizedBox(
                          height: 8.0,
                        ),
                        EnumerationViewComponent(
                            isEditing: state.isEditing,
                            isLoading: state.isLoading,
                            onSaveChanges: (change) =>
                                bloc.add(ChangeFieldValueEvent(change)),
                            enumeration: state.documentTemplate.recoverDataType,
                            listEnumerationOptions: state.recoverDataType),
                        SizedBox(
                          height: 8.0,
                        ),
                        TextEditComponent(
                          onSaveChanges: (change) =>
                              bloc.add(ChangeFieldValueEvent(change)),
                          isEditing: state.isEditing,
                          isLoading: state.isLoading,
                          textProp:
                              state.documentTemplate.documentValidityDuration,
                        ),
                        SizedBox(
                          height: 8.0,
                        ),
                        TextEditComponent(
                          onSaveChanges: (change) =>
                              bloc.add(ChangeFieldValueEvent(change)),
                          isEditing: state.isEditing,
                          isLoading: state.isLoading,
                          textProp: state.documentTemplate.workload,
                        ),
                        SizedBox(
                          height: 8.0,
                        ),
                        _fileNameTextFields(state),
                        SizedBox(
                          height: 8.0,
                        ),
                        _placeSignaturesFields(state),
                      ],
                    ),
                  );
                },
              ),
            ),
          ),
        ),
      ),
      floatingActionButton:
          BlocBuilder<DocumentTemplateBloc, DocumentTemplateState>(
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
      ),
    );
  }

  Widget _placeSignaturesFields(DocumentTemplateState state) {
    return BoxWithLabel(
      label: "Locais das Assinaturas",
      child: Padding(
        padding: const EdgeInsets.all(8.0),
        child: Column(
          children: [
            Column(
              children: state.placeSignatures.asMap().entries.map((entry) {
                final index = entry.key;
                final placeSignature = entry.value;
                return Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    _placeSignatureField(index, placeSignature, state),
                    SizedBox(
                      height: 16.0,
                    ),
                  ],
                );
              }).toList(),
            ),
            state.isEditing
                ? ElevatedButton(
                    onPressed: () => bloc.add(NewPlaceSignatureEvent()),
                    child: const Text("Adicionar Assinatura"),
                  )
                : const SizedBox()
          ],
        ),
      ),
    );
  }

  Widget _placeSignatureField(
      int index, PlaceSignature placeSignature, DocumentTemplateState state) {
    return BoxWithLabel(
      label: "Assinatura ${index + 1}",
      child: Padding(
        padding: const EdgeInsets.fromLTRB(8.0, 8.0, 8.0, 0),
        child: Column(
          children: [
            SizedBox(
              height: 8.0,
            ),
            EnumerationViewComponent(
                isEditing: state.isEditing,
                isLoading: state.isLoading,
                onSaveChanges: (change) =>
                    bloc.add(ChangePlaceSignatureValuesEvent(change, index)),
                enumeration: placeSignature.typeSignature,
                listEnumerationOptions: state.typeSignature),
            SizedBox(
              height: 8.0,
            ),
            TextEditComponent(
              onSaveChanges: (change) =>
                  bloc.add(ChangePlaceSignatureValuesEvent(change, index)),
              isEditing: state.isEditing,
              isLoading: state.isLoading,
              textProp: placeSignature.page,
            ),
            SizedBox(
              height: 8.0,
            ),
            TextEditComponent(
              onSaveChanges: (change) =>
                  bloc.add(ChangePlaceSignatureValuesEvent(change, index)),
              isEditing: state.isEditing,
              isLoading: state.isLoading,
              textProp: placeSignature.relativePositionBotton,
            ),
            SizedBox(
              height: 8.0,
            ),
            TextEditComponent(
              onSaveChanges: (change) =>
                  bloc.add(ChangePlaceSignatureValuesEvent(change, index)),
              isEditing: state.isEditing,
              isLoading: state.isLoading,
              textProp: placeSignature.relativePositionLeft,
            ),
            SizedBox(
              height: 8.0,
            ),
            TextEditComponent(
              onSaveChanges: (change) =>
                  bloc.add(ChangePlaceSignatureValuesEvent(change, index)),
              isEditing: state.isEditing,
              isLoading: state.isLoading,
              textProp: placeSignature.relativeSizeX,
            ),
            SizedBox(
              height: 8.0,
            ),
            TextEditComponent(
              onSaveChanges: (change) =>
                  bloc.add(ChangePlaceSignatureValuesEvent(change, index)),
              isEditing: state.isEditing,
              isLoading: state.isLoading,
              textProp: placeSignature.relativeSizeY,
            ),
            SizedBox(
              height: 8.0,
            ),
            state.isEditing
                ? Align(
                    alignment: Alignment.centerRight,
                    child: FilledButton(
                        style: ButtonStyle(
                            backgroundColor:
                                WidgetStatePropertyAll(Colors.red)),
                        onPressed: () =>
                            bloc.add(RemovePlaceSignatureEvent(index)),
                        child: const Text("Remover")))
                : const SizedBox(),
          ],
        ),
      ),
    );
  }

  Widget _fileNameTextFields(DocumentTemplateState state) {
    return BoxWithLabel(
      label: "Arquivos",
      child: Padding(
        padding: const EdgeInsets.fromLTRB(8.0, 8.0, 8.0, 0),
        child: Column(
          children: [
            SizedBox(
              height: 8.0,
            ),
            SizedBox(
              height: 8.0,
            ),
            TextEditComponent(
              onSaveChanges: (change) =>
                  bloc.add(ChangeFieldValueEvent(change)),
              isEditing: state.isEditing,
              isLoading: state.isLoading,
              textProp: state.documentTemplate.bodyFileName,
            ),
            SizedBox(
              height: 8.0,
            ),
            TextEditComponent(
              onSaveChanges: (change) =>
                  bloc.add(ChangeFieldValueEvent(change)),
              isEditing: state.isEditing,
              isLoading: state.isLoading,
              textProp: state.documentTemplate.headerFileName,
            ),
            SizedBox(
              height: 8.0,
            ),
            TextEditComponent(
              onSaveChanges: (change) =>
                  bloc.add(ChangeFieldValueEvent(change)),
              isEditing: state.isEditing,
              isLoading: state.isLoading,
              textProp: state.documentTemplate.footerFileName,
            ),
            SizedBox(
              height: 8.0,
            ),
            state.isLoading
                ? CircularProgressIndicator()
                : state.hasFile
                    ? Row(
                        mainAxisAlignment: MainAxisAlignment.center,
                        children: [
                          ElevatedButton(
                            onPressed: () => bloc.add(DownLoadFileEvent()),
                            child: Row(
                              children: [
                                Icon(size: 20, Icons.download),
                                SizedBox(
                                  width: 4.0,
                                ),
                                Text("Baixar Arquivo")
                              ],
                            ),
                          ),
                          SizedBox(
                            width: 8.0,
                          ),
                          state.isEditing
                              ? ElevatedButton(
                                  onPressed: () => bloc.add(SendFileEvent()),
                                  child: Row(
                                    children: [
                                      Icon(size: 20, Icons.upload),
                                      SizedBox(
                                        width: 4.0,
                                      ),
                                      Text("Alterar Arquivo")
                                    ],
                                  ),
                                )
                              : SizedBox(),
                        ],
                      )
                    : ElevatedButton(
                        onPressed: () => bloc.add(SendFileEvent()),
                        child: Row(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            Icon(size: 20, Icons.upload),
                            SizedBox(
                              width: 4.0,
                            ),
                            Text("Enviar Arquivo"),
                          ],
                        ),
                      ),
          ],
        ),
      ),
    );
  }
}
