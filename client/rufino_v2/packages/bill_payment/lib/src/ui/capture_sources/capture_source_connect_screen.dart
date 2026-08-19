import 'package:flutter/material.dart';
import 'package:rufino_core/rufino_core.dart';

import '../bill_payment_back_button.dart';
import 'capture_source_connect_viewmodel.dart';

/// The connect-mailbox flow: the Entra ID walkthrough, then the credential
/// form.
///
/// There is no OAuth dance here on purpose — the customer registers an app
/// in their own Entra ID and hands the three credential fields over. The
/// walkthrough is the price of that design, so it lives on the screen, not
/// in a manual.
class CaptureSourceConnectScreen extends StatefulWidget {
  /// Creates the screen.
  const CaptureSourceConnectScreen({
    super.key,
    required this.viewModel,
    required this.backFallback,
    required this.onConnected,
  });

  /// Drives the screen.
  final CaptureSourceConnectViewModel viewModel;

  /// Para onde o voltar leva quando não há pilha.
  final String backFallback;

  /// Called with the id of the source just connected.
  final void Function(String id) onConnected;

  @override
  State<CaptureSourceConnectScreen> createState() =>
      _CaptureSourceConnectScreenState();
}

class _CaptureSourceConnectScreenState
    extends State<CaptureSourceConnectScreen> {
  final _formKey = GlobalKey<FormState>();
  final _nameController = TextEditingController();
  final _addressController = TextEditingController();
  final _directoryController = TextEditingController();
  final _clientIdController = TextEditingController();
  final _secretController = TextEditingController();
  final _folderController = TextEditingController();
  int _step = 0;

  @override
  void dispose() {
    _nameController.dispose();
    _addressController.dispose();
    _directoryController.dispose();
    _clientIdController.dispose();
    _secretController.dispose();
    _folderController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;

    final id = await widget.viewModel.connect(
      displayName: _nameController.text,
      address: _addressController.text,
      directoryId: _directoryController.text,
      clientId: _clientIdController.text,
      clientSecret: _secretController.text,
      folderPath: _folderController.text,
    );
    if (id == null || !mounted) return;

    if (widget.viewModel.sharedMailboxWarning) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Esta caixa já é monitorada por outra conta.'),
        ),
      );
    }
    widget.onConnected(id);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Conectar caixa'),
        leading: BillPaymentBackButton(fallback: widget.backFallback),
      ),
      body: SafeArea(
        child: ListenableBuilder(
          listenable: widget.viewModel,
          builder: (context, _) => Center(
            child: ConstrainedBox(
              constraints:
                  const BoxConstraints(maxWidth: AppBreakpoints.tablet),
              child: Stepper(
                currentStep: _step,
                onStepContinue: () {
                  if (_step == 0) {
                    setState(() => _step = 1);
                  } else {
                    _submit();
                  }
                },
                onStepCancel:
                    _step == 0 ? null : () => setState(() => _step = 0),
                controlsBuilder: (context, details) => Padding(
                  padding: const EdgeInsets.only(top: AppSpacing.md),
                  child: Row(
                    children: [
                      FilledButton.tonal(
                        onPressed: widget.viewModel.isSaving
                            ? null
                            : details.onStepContinue,
                        child: widget.viewModel.isSaving
                            ? const SizedBox(
                                height: 20,
                                width: 20,
                                child: CircularProgressIndicator(
                                    strokeWidth: 2),
                              )
                            : Text(_step == 0 ? 'Continuar' : 'Conectar'),
                      ),
                      if (details.onStepCancel != null) ...[
                        const SizedBox(width: AppSpacing.sm),
                        TextButton(
                          onPressed: details.onStepCancel,
                          child: const Text('Voltar'),
                        ),
                      ],
                    ],
                  ),
                ),
                steps: [
                  Step(
                    title: const Text('Prepare o Entra ID'),
                    isActive: _step >= 0,
                    content: const _EntraWalkthrough(),
                  ),
                  Step(
                    title: const Text('Credencial e caixa'),
                    isActive: _step >= 1,
                    content: Form(
                      key: _formKey,
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.stretch,
                        children: [
                          _Field(
                            controller: _nameController,
                            label: 'Nome de exibição',
                            hint: 'Ex.: Contas a pagar',
                          ),
                          _Field(
                            controller: _addressController,
                            label: 'Endereço da caixa',
                            hint: 'contas@empresa.com.br',
                            keyboardType: TextInputType.emailAddress,
                          ),
                          _Field(
                            controller: _directoryController,
                            label: 'Directory (tenant) ID',
                          ),
                          _Field(
                            controller: _clientIdController,
                            label: 'Application (client) ID',
                          ),
                          _Field(
                            controller: _secretController,
                            label: 'Client secret',
                            obscure: true,
                          ),
                          TextFormField(
                            controller: _folderController,
                            decoration: const InputDecoration(
                              labelText:
                                  'Pasta (opcional — vazio = caixa de '
                                  'entrada)',
                              border: OutlineInputBorder(),
                            ),
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
                        ],
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}

class _EntraWalkthrough extends StatelessWidget {
  const _EntraWalkthrough();

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    const steps = [
      '1. No portal do Entra ID da SUA organização, registre um aplicativo '
          'novo (Registros de aplicativo → Novo registro).',
      '2. Em Permissões de API, conceda Mail.Read do tipo APLICATIVO e dê o '
          'consentimento do administrador.',
      '3. Restrinja o alcance com uma Application Access Policy ao grupo que '
          'contém só as caixas monitoradas — sem ela, Mail.Read alcança '
          'todas as caixas da organização.',
      '4. Em Certificados e segredos, crie um client secret e copie o valor '
          'na hora (ele não aparece de novo).',
      '5. Traga para cá o Directory ID, o Application ID e o secret.',
    ];

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        for (final step in steps)
          Padding(
            padding: const EdgeInsets.only(bottom: AppSpacing.sm),
            child: Text(step, style: theme.textTheme.bodyMedium),
          ),
        Text(
          'A credencial é guardada cifrada no cofre do servidor e nunca '
          'volta pela API. A conexão só se completa se a caixa responder à '
          'prova de acesso.',
          style: theme.textTheme.bodySmall?.copyWith(
            color: theme.colorScheme.onSurfaceVariant,
          ),
        ),
      ],
    );
  }
}

class _Field extends StatelessWidget {
  const _Field({
    required this.controller,
    required this.label,
    this.hint,
    this.obscure = false,
    this.keyboardType,
  });

  final TextEditingController controller;
  final String label;
  final String? hint;
  final bool obscure;
  final TextInputType? keyboardType;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: AppSpacing.md),
      child: TextFormField(
        controller: controller,
        obscureText: obscure,
        keyboardType: keyboardType,
        decoration: InputDecoration(
          labelText: label,
          hintText: hint,
          border: const OutlineInputBorder(),
        ),
        validator: (value) => (value == null || value.trim().isEmpty)
            ? 'Campo obrigatório.'
            : null,
      ),
    );
  }
}
