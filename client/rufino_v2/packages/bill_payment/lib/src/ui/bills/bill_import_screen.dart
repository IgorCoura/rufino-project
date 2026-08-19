import 'package:flutter/material.dart';
import 'package:rufino_core/rufino_core.dart';

import '../bill_payment_back_button.dart';
import 'bill_import_viewmodel.dart';

/// The manual import form: the digitable line, the Pix payload, or both.
class BillImportScreen extends StatefulWidget {
  /// Creates the screen.
  const BillImportScreen({
    super.key,
    required this.viewModel,
    required this.backFallback,
    required this.onImported,
  });

  /// Drives the screen.
  final BillImportViewModel viewModel;

  /// Para onde o voltar leva quando não há pilha.
  final String backFallback;

  /// Called with the id of the bill just imported.
  final void Function(String id) onImported;

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
                        'Cole a linha digitável, o código Pix, ou os dois. '
                        'O sistema confere os dígitos, consulta a fonte '
                        'oficial e roda as verificações antes de qualquer '
                        'aprovação.',
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
    return hasLine || hasPix
        ? null
        : 'Informe a linha digitável ou o código Pix.';
  }
}
