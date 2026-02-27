import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_modular/flutter_modular.dart';
import 'package:mask_text_input_formatter/mask_text_input_formatter.dart';
import 'package:rufino/modules/employee/domain/model/document/document.dart';
import 'package:rufino/modules/employee/domain/model/document/document_unit.dart';
import 'package:rufino/modules/employee/domain/model/document_group/document_group_with_documents.dart';
import 'package:rufino/modules/employee/presentation/components/period_components.dart';
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
                  ? state.isLoading
                      ? const Padding(
                          padding: EdgeInsets.all(8.0),
                          child: Center(child: CircularProgressIndicator()),
                        )
                      : state.reqDocuments.isEmpty
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
      DocumentGroupWithDocuments reqDocument) {
    return Padding(
      padding: const EdgeInsets.all(8.0),
      child: ExpansionTile(
        onExpansionChanged: (value) => {},
        controlAffinity: ListTileControlAffinity.leading,
        title: Row(
          children: [
            _statusBadge(reqDocument.status.name,
                _getDocumentGroupStatusColor(reqDocument.status.id)),
            const SizedBox(width: 8),
            Expanded(
              child: Text(reqDocument.name),
            ),
          ],
        ),
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
          title: Row(
            children: [
              _statusBadge(document.status.name,
                  _getDocumentStatusColor(document.status.id)),
              const SizedBox(width: 8),
              Expanded(
                child: Text(document.name),
              ),
              if (document.usePreviousPeriod) ...[
                const SizedBox(width: 8),
                UsePreviousPeriodBadge(
                  usePreviousPeriod: document.usePreviousPeriod,
                ),
              ],
            ],
          ),
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
              : (List<DocumentUnit>.from(document.documentsUnits)
                    ..sort((a, b) {
                      if (a.createAt.isEmpty) return 1;
                      if (b.createAt.isEmpty) return -1;
                      return DateTime.parse(b.createAt)
                          .compareTo(DateTime.parse(a.createAt));
                    }))
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
        leading: _statusBadge(documentUnit.status.name,
            _getDocumentUnitStatusColor(documentUnit.status.id)),
        title: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(Icons.access_time, size: 14),
            const SizedBox(width: 4),
            Text(
              "Criado: ",
              style: TextStyle(
                fontSize: 12,
                fontWeight: FontWeight.bold,
              ),
            ),
            Text(
              documentUnit.getCreateAt,
              style: TextStyle(
                fontSize: 12,
                fontWeight: FontWeight.w500,
              ),
            ),
          ],
        ),
        subtitle: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(Icons.event, size: 14),
            const SizedBox(width: 4),
            Text(
              "Data: ",
              style: TextStyle(
                fontSize: 12,
                fontWeight: FontWeight.bold,
              ),
            ),
            Text(
              documentUnit.getDate,
              style: TextStyle(
                fontSize: 12,
                fontWeight: FontWeight.w500,
              ),
            ),
            if (documentUnit.validity.isNotEmpty) ...[
              const SizedBox(width: 16),
              Icon(Icons.event_busy, size: 14),
              const SizedBox(width: 4),
              Text(
                "Vencimento: ",
                style: TextStyle(
                  fontSize: 12,
                  fontWeight: FontWeight.bold,
                ),
              ),
              Text(
                documentUnit.getValidity,
                style: TextStyle(
                  fontSize: 12,
                  fontWeight: FontWeight.w500,
                ),
              ),
            ],
            if (documentUnit.hasPeriod) ...[
              const SizedBox(width: 16),
              PeriodBadgeComponent(period: documentUnit.period),
            ],
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
                              "Editar",
                              style: TextStyle(fontSize: 10),
                            ),
                          ],
                        ),
                      ),
                      if (documentUnit.date.isNotEmpty &&
                          documentUnit.date != "0001-01-01") ...[
                        if (document.canGenerateDocument)
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
              if (document.isSignable) ...[
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
              if (document.isSignable) ...[
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
              title: Text("Editar data do documento"),
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
            final dateLimitController = TextEditingController();
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
                        controller: dateLimitController,
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
                      const SizedBox(height: 8),
                      Wrap(
                        spacing: 8,
                        runSpacing: 8,
                        children: [
                          OutlinedButton(
                            onPressed: () {
                              final futureDate =
                                  DateTime.now().add(Duration(days: 3));
                              final formattedDate =
                                  "${futureDate.day.toString().padLeft(2, '0')}/${futureDate.month.toString().padLeft(2, '0')}/${futureDate.year}";
                              dateLimitController.text = formattedDate;
                              dateLimitToSign = formattedDate;
                            },
                            child: const Text("3 dias"),
                          ),
                          OutlinedButton(
                            onPressed: () {
                              final futureDate =
                                  DateTime.now().add(Duration(days: 5));
                              final formattedDate =
                                  "${futureDate.day.toString().padLeft(2, '0')}/${futureDate.month.toString().padLeft(2, '0')}/${futureDate.year}";
                              dateLimitController.text = formattedDate;
                              dateLimitToSign = formattedDate;
                            },
                            child: const Text("5 dias"),
                          ),
                          OutlinedButton(
                            onPressed: () {
                              final futureDate =
                                  DateTime.now().add(Duration(days: 10));
                              final formattedDate =
                                  "${futureDate.day.toString().padLeft(2, '0')}/${futureDate.month.toString().padLeft(2, '0')}/${futureDate.year}";
                              dateLimitController.text = formattedDate;
                              dateLimitToSign = formattedDate;
                            },
                            child: const Text("10 dias"),
                          ),
                        ],
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
                      sendData(dateLimitToSign, "0");
                    }
                  },
                  child: const Text("Confirmar"),
                ),
              ],
            );
          });
    });
  }

  Color _getDocumentGroupStatusColor(String statusId) {
    switch (statusId) {
      case "1":
        return Colors.green;
      case "2":
        return Colors.orange;
      case "3":
        return Colors.red;
      default:
        return Colors.grey;
    }
  }

  Color _getDocumentStatusColor(String statusId) {
    switch (statusId) {
      case "1":
        return Colors.orange;
      case "2":
        return Colors.amber;
      case "3":
        return Colors.green;
      case "4":
        return Colors.grey;
      case "5":
        return Colors.blue;
      default:
        return Colors.grey;
    }
  }

  Color _getDocumentUnitStatusColor(String statusId) {
    switch (statusId) {
      case "1":
        return Colors.orange;
      case "2":
        return Colors.green;
      case "3":
        return Colors.grey;
      case "4":
        return Colors.red;
      case "5":
        return Colors.amber;
      case "6":
        return Colors.blueGrey;
      case "7":
        return Colors.blue;
      default:
        return Colors.grey;
    }
  }

  Widget _statusBadge(String statusName, Color color) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Container(
          padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
          decoration: BoxDecoration(
            color: color.withOpacity(0.15),
            borderRadius: BorderRadius.circular(12),
            border: Border.all(color: color, width: 1),
          ),
          constraints: const BoxConstraints(minWidth: 0),
          child: FittedBox(
            fit: BoxFit.scaleDown,
            child: Text(
              statusName,
              style: TextStyle(
                color: color,
                fontWeight: FontWeight.bold,
                fontSize: 12,
              ),
            ),
          ),
        ),
      ],
    );
  }
}
