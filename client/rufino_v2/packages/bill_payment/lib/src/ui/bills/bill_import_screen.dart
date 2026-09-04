import 'package:flutter/material.dart';
import 'package:material_symbols_icons/symbols.dart';
import 'package:rufino_core/rufino_core.dart';

import '../bill_payment_back_button.dart';
import '../shared/document_picker.dart';
import 'bill_import_viewmodel.dart';

/// The manual import form: the digitable line, the Pix payload, the bill's
/// own file, or any combination of the three.
class BillImportScreen extends StatefulWidget {
  /// Creates the screen.
  const BillImportScreen({
    super.key,
    required this.viewModel,
    required this.backFallback,
    required this.onImported,
    required this.onPickDocument,
  });

  /// Drives the screen.
  final BillImportViewModel viewModel;

  /// Para onde o voltar leva quando não há pilha.
  final String backFallback;

  /// Called with the id of the bill just imported.
  final void Function(String id) onImported;

  /// Opens the system file picker. Supplied by the shell — picking a file is
  /// a platform capability, and this module carries no platform plugin.
  final DocumentPicker onPickDocument;

  @override
  State<BillImportScreen> createState() => _BillImportScreenState();
}

class _BillImportScreenState extends State<BillImportScreen> {
  final _formKey = GlobalKey<FormState>();
  final _lineController = TextEditingController();
  final _pixController = TextEditingController();

  @override
  void dispose() {
    _lineController.dispose();
    _pixController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;

    final id = await widget.viewModel.import(
      digitableLine: _lineController.text,
      pixPayload: _pixController.text,
    );
    if (id != null && mounted) widget.onImported(id);
  }

  Future<void> _pickDocument() async {
    final picked = await widget.onPickDocument();
    if (picked == null || !mounted) return;

    widget.viewModel.setDocument(picked);

    // O arquivo satisfaz a exigência de "informe ao menos um"; sem revalidar,
    // a mensagem de erro do campo de texto ficaria na tela contradizendo o
    // anexo que a pessoa acabou de escolher.
    _formKey.currentState?.validate();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Importar boleto'),
        leading: BillPaymentBackButton(fallback: widget.backFallback),
      ),
      body: SafeArea(
        child: ListenableBuilder(
          listenable: widget.viewModel,
          builder: (context, _) => Center(
            child: ConstrainedBox(
              constraints:
                  const BoxConstraints(maxWidth: AppBreakpoints.tablet),
              child: SingleChildScrollView(
                padding: const EdgeInsets.all(AppSpacing.md),
                child: Form(
                  key: _formKey,
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      Text(
                        'Cole a linha digitável, o código Pix, anexe o '
                        'arquivo do boleto, ou combine os três. O sistema '
                        'confere os dígitos, consulta a fonte oficial e roda '
                        'as verificações antes de qualquer aprovação.',
                        style: Theme.of(context).textTheme.bodyMedium,
                      ),
                      const SizedBox(height: AppSpacing.lg),
                      TextFormField(
                        controller: _lineController,
                        decoration: const InputDecoration(
                          labelText: 'Linha digitável',
                          hintText: '47 ou 48 dígitos',
                          border: OutlineInputBorder(),
                        ),
                        keyboardType: TextInputType.number,
                        maxLines: 2,
                        minLines: 1,
                        validator: (_) => _atLeastOne(),
                      ),
                      const SizedBox(height: AppSpacing.md),
                      TextFormField(
                        controller: _pixController,
                        decoration: const InputDecoration(
                          labelText: 'Código Pix (copia e cola)',
                          border: OutlineInputBorder(),
                        ),
                        maxLines: 4,
                        minLines: 2,
                      ),
                      const SizedBox(height: AppSpacing.md),
                      _DocumentField(
                        document: widget.viewModel.document,
                        onPick: widget.viewModel.isSaving ? null : _pickDocument,
                        onRemove: widget.viewModel.isSaving
                            ? null
                            : () {
                                widget.viewModel.setDocument(null);
                                _formKey.currentState?.validate();
                              },
                      ),
                      if (widget.viewModel.errorMessage != null) ...[
                        const SizedBox(height: AppSpacing.md),
                        Text(
                          widget.viewModel.errorMessage!,
                          style: TextStyle(
                            color: Theme.of(context).colorScheme.error,
                          ),
                        ),
                      ],
                      const SizedBox(height: AppSpacing.lg),
                      FilledButton(
                        onPressed:
                            widget.viewModel.isSaving ? null : _submit,
                        child: widget.viewModel.isSaving
                            ? const SizedBox(
                                height: 20,
                                width: 20,
                                child: CircularProgressIndicator(
                                    strokeWidth: 2),
                              )
                            : const Text('Importar'),
                      ),
                    ],
                  ),
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }

  String? _atLeastOne() {
    final hasLine = _lineController.text.trim().isNotEmpty;
    final hasPix = _pixController.text.trim().isNotEmpty;
    final hasDocument = widget.viewModel.document != null;
    return hasLine || hasPix || hasDocument
        ? null
        : 'Informe a linha digitável, o código Pix ou anexe o arquivo.';
  }
}

/// The attachment slot: an empty invitation, or the chosen file with a way out.
class _DocumentField extends StatelessWidget {
  const _DocumentField({
    required this.document,
    required this.onPick,
    required this.onRemove,
  });

  final PickedDocument? document;
  final VoidCallback? onPick;
  final VoidCallback? onRemove;

  @override
  Widget build(BuildContext context) {
    final picked = document;
    if (picked == null) {
      return OutlinedButton.icon(
        onPressed: onPick,
        icon: const Icon(Symbols.attach_file_rounded),
        label: const Text('Anexar arquivo do boleto'),
      );
    }

    final theme = Theme.of(context);

    return Card(
      margin: EdgeInsets.zero,
      child: ListTile(
        leading: const Icon(Symbols.description_rounded),
        title: Text(picked.fileName, overflow: TextOverflow.ellipsis),
        subtitle: Text(
          _sizeLabel(picked.bytes.length),
          style: theme.textTheme.bodySmall,
        ),
        trailing: IconButton(
          onPressed: onRemove,
          icon: const Icon(Symbols.close_rounded),
          tooltip: 'Remover arquivo',
        ),
      ),
    );
  }

  static String _sizeLabel(int bytes) {
    if (bytes < 1024) return '$bytes B';
    if (bytes < 1024 * 1024) return '${(bytes / 1024).toStringAsFixed(0)} KB';
    return '${(bytes / (1024 * 1024)).toStringAsFixed(1)} MB';
  }
}
