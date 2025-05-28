import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_modular/flutter_modular.dart';
import 'package:mask_text_input_formatter/mask_text_input_formatter.dart';
import 'package:rufino/modules/employee/domain/model/document/document.dart';
import 'package:rufino/modules/employee/domain/model/document/document_unit.dart';
import 'package:rufino/modules/employee/domain/model/require_document/require_document_simple_with_documents.dart';
import 'package:rufino/modules/employee/presentation/employee_profile/components/documents_component/bloc/documents_component_bloc.dart';
import 'package:rufino/shared/components/error_components.dart';

class DocumentsComponent extends StatelessWidget {
  final bloc = Modular.get<DocumentsComponentBloc>();

  DocumentsComponent(
      {required String companyId, required String employeeId, super.key}) {
    bloc.add(InitialEvent(
      companyId: companyId,
      employeeId: employeeId,
    ));
  }

  @override
  Widget build(BuildContext context) {
    return BlocBuilder<DocumentsComponentBloc, DocumentsComponentState>(
      bloc: bloc,
      builder: (context, state) {
        if (state.exception != null) {
          ErrorComponent.showException(
              context, state.exception!, () => Modular.to.navigate("/home"));
        }

        if (state.snackMessage != null && state.snackMessage!.isNotEmpty) {
          WidgetsBinding.instance.addPostFrameCallback((_) {
            ScaffoldMessenger.of(context).showSnackBar(SnackBar(
              content: Text(state.snackMessage!),
            ));
            bloc.add(SnackMessageWasShowEvent());
          });
        }
        return Container(
          decoration: BoxDecoration(
            border: Border.all(),
            borderRadius: BorderRadius.circular(5),
          ),
          child: Column(
            children: [
              _header(state),
              state.isExpanded
                  ? state.reqDocuments.isEmpty
                      ? Padding(
                          padding: const EdgeInsets.all(8.0),
                          child: Text("Nenhum documento encontrado"),
                        )
                      : Column(
                          children: state.reqDocuments
                              .map((reqDocument) => _reqDocumentWidget(
                                  context, state, reqDocument))
                              .toList(),
                        )
                  : const SizedBox(),
            ],
          ),
        );
      },
    );
  }

  Widget _header(DocumentsComponentState state) {
    return Container(
      padding: const EdgeInsets.all(8),
      decoration: BoxDecoration(
        border: state.isExpanded ? const Border(bottom: BorderSide()) : null,
        borderRadius: BorderRadius.circular(5),
      ),
      child: InkWell(
        onTap: () => bloc.add(ExpandEvent()),
        child: Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            Text(
              "Documentos",
              style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 16),
            ),
            const Icon(
              Icons.arrow_drop_down_sharp,
            )
          ],
        ),
      ),
    );
  }

  Widget _reqDocumentWidget(BuildContext context, DocumentsComponentState state,
      RequireDocumentSimpleWithDocuments reqDocument) {
    return Padding(
      padding: const EdgeInsets.all(8.0),
      child: ExpansionTile(
        onExpansionChanged: (value) => {},
        controlAffinity: ListTileControlAffinity.leading,
        title: Text(reqDocument.name),
        subtitle: Text("Status: ${reqDocument.status.name}"),
        children: reqDocument.documents.isEmpty
            ? [
                state.isLazyLoading
                    ? const Center(
                        child: CircularProgressIndicator(),
                      )
                    : const Padding(
                        padding: EdgeInsets.all(8.0),
                        child: Text("Nenhum item encontrado"),
                      ),
              ]
            : reqDocument.documents
                .map((document) => _documentWidget(
                      context,
                      state,
                      document,
                    ))
                .toList(),
      ),
    );
  }

  Widget _documentWidget(
      BuildContext context, DocumentsComponentState state, Document document) {
    return Padding(
      padding: const EdgeInsets.all(8.0),
      child: Container(
        decoration: BoxDecoration(
          border: Border.all(
            color: Theme.of(context).colorScheme.primary,
            width: 1.0,
          ),
          borderRadius: BorderRadius.circular(8.0),
        ),
        child: ExpansionTile(
          onExpansionChanged: (value) =>
              bloc.add(ExpandDocumentEvent(document.id, value)),
          controlAffinity: ListTileControlAffinity.leading,
          title: Text(document.name),
          subtitle: Text("Status: ${document.status.name}"),
          trailing: TextButton(
            onPressed: () => bloc.add(CreateDocumentUnitEvent(document.id)),
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              crossAxisAlignment: CrossAxisAlignment.center,
              children: state.isSavingData
                  ? [
                      CircularProgressIndicator(),
                    ]
                  : [
                      Icon(Icons.add, size: 20),
                      Text(
                        "Adicionar",
                        style: TextStyle(fontSize: 12),
                      ),
                    ],
            ),
          ),
          children: document.documentsUnits.isEmpty
              ? [
                  state.isLazyLoading
                      ? const Center(
                          child: CircularProgressIndicator(),
                        )
                      : const Padding(
                          padding: EdgeInsets.all(8.0),
                          child: Text("Nenhum item encontrado"),
                        ),
                ]
              : document.documentsUnits
                  .map((documentUnit) => _documentUnitWidget(
                      context, document, documentUnit, state))
                  .toList(),
        ),
      ),
    );
  }

  Widget _documentUnitWidget(BuildContext context, Document document,
      DocumentUnit documentUnit, DocumentsComponentState state) {
    return Padding(
      padding: const EdgeInsets.all(8.0),
      child: ListTile(
        leading: Text(documentUnit.status.name),
        title: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                Text(
                  "ID: ",
                  style: TextStyle(overflow: TextOverflow.ellipsis),
                ),
                Text(documentUnit.id.substring(0, 5)),
              ],
            ),
            SizedBox(width: 8),
            Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                Text(
                  "Criado: ",
                  style: TextStyle(overflow: TextOverflow.ellipsis),
                ),
                Text(
                  documentUnit.getCreateAt,
                  style: TextStyle(overflow: TextOverflow.ellipsis),
                ),
              ],
            ),
            SizedBox(width: 8),
          ],
        ),
        subtitle: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                Text(
                  "Data: ",
                  style: TextStyle(overflow: TextOverflow.ellipsis),
                ),
                Text(
                  documentUnit.getDate,
                  style: TextStyle(overflow: TextOverflow.ellipsis),
                ),
              ],
            ),
            SizedBox(width: 8),
            Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                Text(
                  "Vencimento: ",
                  style: TextStyle(overflow: TextOverflow.ellipsis),
                ),
                Text(
                  documentUnit.getValidity,
                  style: TextStyle(overflow: TextOverflow.ellipsis),
                ),
              ],
            )
          ],
        ),
        trailing: state.isSavingData
            ? const CircularProgressIndicator()
            : documentUnit.isPanding
                ? Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      TextButton(
                        onPressed: () => _editDocumentUnitDialog(
                            context, document, documentUnit),
                        child: Column(
                          mainAxisAlignment: MainAxisAlignment.center,
                          crossAxisAlignment: CrossAxisAlignment.center,
                          children: const [
                            Icon(Icons.edit),
                            Text(
                              "Atualizar",
                              style: TextStyle(fontSize: 10),
                            ),
                          ],
                        ),
                      ),
                      TextButton(
                        onPressed: () => _showGenerateButtonsDialog(
                            context, document, documentUnit),
                        child: Column(
                          mainAxisAlignment: MainAxisAlignment.center,
                          crossAxisAlignment: CrossAxisAlignment.center,
                          children: const [
                            Icon(Icons.sim_card_download_outlined),
                            Text(
                              "Gerar",
                              style: TextStyle(fontSize: 10),
                            ),
                          ],
                        ),
                      ),
                      TextButton(
                        onPressed: () => _showSendButtonsDialog(
                            context, document, documentUnit),
                        child: Column(
                          mainAxisAlignment: MainAxisAlignment.center,
                          crossAxisAlignment: CrossAxisAlignment.center,
                          children: const [
                            Icon(Icons.upload_file),
                            Text(
                              "Enviar",
                              style: TextStyle(fontSize: 10),
                            ),
                          ],
                        ),
                      ),
                    ],
                  )
                : documentUnit.hasFile
                    ? TextButton(
                        onPressed: () => bloc.add(DownloadDocumentUnitEvent(
                          document.id,
                          documentUnit.id,
                        )),
                        child: Column(
                          mainAxisAlignment: MainAxisAlignment.center,
                          crossAxisAlignment: CrossAxisAlignment.center,
                          children: const [
                            Icon(Icons.search),
                            Text(
                              "Visualizar",
                              style: TextStyle(fontSize: 10),
                            ),
                          ],
                        ),
                      )
                    : const SizedBox(),
      ),
    );
  }

  Future<void> _showSendButtonsDialog(BuildContext context, Document document,
      DocumentUnit documentUnit) async {
    await showDialog(
      context: context,
      builder: (context) {
        return AlertDialog(
          title: Center(child: const Text("Opções de envio")),
          content: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            mainAxisSize: MainAxisSize.min,
            children: [
              OutlinedButton(
                onPressed: () {
                  Navigator.of(context).pop();
                  bloc.add(LoadDocumentUnitEvent(documentUnit.id, document.id));
                },
                child: SizedBox(
                  width: 300,
                  child: Center(
                      child: const Text(
                    "Enviar arquivo",
                  )),
                ),
              ),
              SizedBox(height: 8),
              OutlinedButton(
                onPressed: () {
                  _getDataToSignDialog(context,
                      (dateLimitToSign, eminderEveryNDays) {
                    bloc.add(LoadDocumentUnitToSignEvent(
                      dateLimitToSign,
                      eminderEveryNDays,
                      documentUnit.id,
                      document.id,
                    ));
                  });
                },
                child: SizedBox(
                  width: 300,
                  child: Center(
                    child: const Text("Enviar arquivo para ser assinado"),
                  ),
                ),
              ),
            ],
          ),
          actions: [
            TextButton(
              onPressed: () {
                Navigator.of(context).pop();
              },
              child: const Text("Cancelar"),
            ),
          ],
        );
      },
    );
  }

  Future<void> _showGenerateButtonsDialog(BuildContext context,
      Document document, DocumentUnit documentUnit) async {
    await showDialog(
      context: context,
      builder: (context) {
        return AlertDialog(
          title: Center(child: const Text("Opções de geranção de arquivo")),
          content: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            mainAxisSize: MainAxisSize.min,
            children: [
              OutlinedButton(
                onPressed: () {
                  Navigator.of(context).pop();
                  bloc.add(
                      GenerateDocumentUnitEvent(document.id, documentUnit.id));
                },
                child: SizedBox(
                  width: 300,
                  child: Center(
                      child: const Text(
                    "Gerar arquivo",
                  )),
                ),
              ),
              SizedBox(height: 8),
              OutlinedButton(
                onPressed: () {
                  _getDataToSignDialog(
                      context,
                      (dateLimitToSign, eminderEveryNDays) => bloc.add(
                          GenerateAndSend2SignEvent(
                              dateLimitToSign,
                              eminderEveryNDays,
                              document.id,
                              documentUnit.id)));
                },
                child: SizedBox(
                  width: 300,
                  child: Center(
                    child: const Text("Gerar arquivo e enviar para assinar"),
                  ),
                ),
              ),
            ],
          ),
          actions: [
            TextButton(
              onPressed: () {
                Navigator.of(context).pop();
              },
              child: const Text("Cancelar"),
            ),
          ],
        );
      },
    );
  }

  void _editDocumentUnitDialog(
      BuildContext context, Document document, DocumentUnit documentUnit) {
    WidgetsBinding.instance.addPostFrameCallback((_) {
      showDialog(
          barrierDismissible: false,
          context: context,
          builder: (_) {
            var _dialogKey = GlobalKey<FormState>();
            String date = "";
            return AlertDialog(
              title: Text("Atualizar data do documento"),
              content: SizedBox(
                width: 400,
                child: Form(
                  key: _dialogKey,
                  child: TextFormField(
                    inputFormatters: [
                      MaskTextInputFormatter(
                          mask: '##/##/####',
                          filter: {"#": RegExp(r'[0-9]')},
                          type: MaskAutoCompletionType.lazy)
                    ],
                    keyboardType: TextInputType.datetime,
                    controller: TextEditingController(),
                    enabled: true,
                    decoration: InputDecoration(
                      labelText: "Data do documento",
                      border: const OutlineInputBorder(),
                    ),
                    style: TextStyle(
                        color: Theme.of(context).colorScheme.onSurface),
                    validator: (value) => DocumentUnit.validateDate(value),
                    onChanged: (value) => {date = value},
                  ),
                ),
              ),
              actions: [
                TextButton(
                  onPressed: () {
                    Navigator.pop(context);
                  },
                  child: const Text("Cancelar"),
                ),
                FilledButton(
                  onPressed: () {
                    if (_dialogKey.currentState != null &&
                        _dialogKey.currentState!.validate()) {
                      Navigator.pop(context);
                      bloc.add(EditDocumentUnitEvent(
                          date, document.id, documentUnit.id));
                    }
                  },
                  child: const Text("Confirmar"),
                ),
              ],
            );
          });
    });
  }

  void _getDataToSignDialog(
    BuildContext context,
    Function(String, String) sendData,
  ) {
    WidgetsBinding.instance.addPostFrameCallback((_) {
      showDialog(
          barrierDismissible: false,
          context: context,
          builder: (_) {
            var _dialogKey = GlobalKey<FormState>();
            String dateLimitToSign = "";
            String eminderEveryNDays = "";
            return AlertDialog(
              title: Text("Dados para assinatura."),
              content: SizedBox(
                width: 400,
                child: Form(
                  key: _dialogKey,
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      TextFormField(
                        inputFormatters: [
                          MaskTextInputFormatter(
                              mask: '##/##/####',
                              filter: {"#": RegExp(r'[0-9]')},
                              type: MaskAutoCompletionType.lazy)
                        ],
                        keyboardType: TextInputType.datetime,
                        controller: TextEditingController(),
                        enabled: true,
                        decoration: InputDecoration(
                          labelText: "Data limite para assinatura",
                          border: const OutlineInputBorder(),
                        ),
                        style: TextStyle(
                            color: Theme.of(context).colorScheme.onSurface),
                        validator: (value) =>
                            DocumentUnit.validateDateLimitToSign(value),
                        onChanged: (value) => {dateLimitToSign = value},
                      ),
                      SizedBox(height: 8),
                      TextFormField(
                        inputFormatters: [
                          MaskTextInputFormatter(
                              mask: '## dias',
                              filter: {"#": RegExp(r'[0-9]')},
                              type: MaskAutoCompletionType.lazy)
                        ],
                        keyboardType: TextInputType.datetime,
                        controller: TextEditingController(),
                        enabled: true,
                        decoration: InputDecoration(
                          labelText: "Relembrar assinatura a cada (dias)",
                          border: const OutlineInputBorder(),
                        ),
                        style: TextStyle(
                            color: Theme.of(context).colorScheme.onSurface),
                        validator: (value) =>
                            DocumentUnit.validateEminderEveryNDays(value),
                        onChanged: (value) => {eminderEveryNDays = value},
                      ),
                    ],
                  ),
                ),
              ),
              actions: [
                TextButton(
                  onPressed: () {
                    Navigator.pop(context);
                  },
                  child: const Text("Cancelar"),
                ),
                FilledButton(
                  onPressed: () {
                    if (_dialogKey.currentState != null &&
                        _dialogKey.currentState!.validate()) {
                      Navigator.pop(context);
                      Navigator.pop(context);
                      sendData(dateLimitToSign, eminderEveryNDays);
                    }
                  },
                  child: const Text("Confirmar"),
                ),
              ],
            );
          });
    });
  }
}
