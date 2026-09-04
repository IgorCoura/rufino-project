import 'package:flutter/material.dart';
import 'package:rufino_core/rufino_core.dart';

import '../../domain/bill_payment_enums.dart';
import '../bill_payment_back_button.dart';
import '../shared/number_field.dart';
import '../shared/tax_id_input_formatter.dart';
import 'payee_form_viewmodel.dart';

/// The payee register form: name, document and the expected amount policy.
class PayeeFormScreen extends StatefulWidget {
  /// Creates the screen.
  const PayeeFormScreen({
    super.key,
    required this.viewModel,
    required this.backFallback,
    required this.onRegistered,
  });

  /// Drives the screen.
  final PayeeFormViewModel viewModel;

  /// Para onde o voltar leva quando não há pilha.
  final String backFallback;

  /// Called with the id of the payee just registered.
  final void Function(String id) onRegistered;

  @override
  State<PayeeFormScreen> createState() => _PayeeFormScreenState();
}

class _PayeeFormScreenState extends State<PayeeFormScreen> {
  final _formKey = GlobalKey<FormState>();
  final _nameController = TextEditingController();
  final _taxIdController = TextEditingController();
  final _expectedController = TextEditingController();
  final _toleranceController = TextEditingController();
  final _minController = TextEditingController();
  final _maxController = TextEditingController();

  // Cresce de CPF para CNPJ sozinho — quem decide a máscara é o formatador,
  // porque só ele vê a 12ª tecla antes de uma máscara de 11 posições engoli-la.
  final _taxIdFormatter = TaxIdInputFormatter();

  @override
  void dispose() {
    _nameController.dispose();
    _taxIdController.dispose();
    _expectedController.dispose();
    _toleranceController.dispose();
    _minController.dispose();
    _maxController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;

    final id = await widget.viewModel.register(
      legalName: _nameController.text,
      taxId: _taxIdController.text,
      expectedAmount: NumberField.read(_expectedController),
      tolerancePercent: NumberField.read(_toleranceController),
      minAmount: NumberField.read(_minController),
      maxAmount: NumberField.read(_maxController),
    );
    if (id != null && mounted) widget.onRegistered(id);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Novo beneficiário'),
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
                      TextFormField(
                        controller: _nameController,
                        decoration: const InputDecoration(
                          labelText: 'Razão social',
                          border: OutlineInputBorder(),
                        ),
                        textCapitalization: TextCapitalization.words,
                        validator: (value) =>
                            (value == null || value.trim().isEmpty)
                                ? 'Informe a razão social.'
                                : null,
                      ),
                      const SizedBox(height: AppSpacing.md),
                      TextFormField(
                        controller: _taxIdController,
                        decoration: const InputDecoration(
                          labelText: 'CPF ou CNPJ',
                          border: OutlineInputBorder(),
                        ),
                        keyboardType: TextInputType.number,
                        inputFormatters: [_taxIdFormatter],
                        validator: (value) {
                          final digits =
                              value?.replaceAll(RegExp(r'\D'), '') ?? '';
                          return (digits.length == 11 || digits.length == 14)
                              ? null
                              : 'Informe um CPF (11) ou CNPJ (14 dígitos).';
                        },
                      ),
                      const SizedBox(height: AppSpacing.lg),
                      Text(
                        'Política de valor',
                        style: Theme.of(context).textTheme.titleMedium,
                      ),
                      const SizedBox(height: AppSpacing.sm),
                      SegmentedButton<String>(
                        segments: const [
                          ButtonSegment(
                            value: AmountPolicyKinds.fixed,
                            label: Text('Fixo'),
                          ),
                          ButtonSegment(
                            value: AmountPolicyKinds.range,
                            label: Text('Faixa'),
                          ),
                          ButtonSegment(
                            value: AmountPolicyKinds.unbounded,
                            label: Text('Sem limite'),
                          ),
                        ],
                        selected: {widget.viewModel.policyKind},
                        onSelectionChanged: (selection) =>
                            widget.viewModel.selectPolicyKind(selection.first),
                      ),
                      const SizedBox(height: AppSpacing.md),
                      if (widget.viewModel.policyKind ==
                          AmountPolicyKinds.fixed) ...[
                        NumberField(
                          controller: _expectedController,
                          label: 'Valor esperado (R\$)',
                          requiredField: true,
                        ),
                        const SizedBox(height: AppSpacing.md),
                        // Obrigatória: AmountPolicy.From recusa valor fixo sem
                        // tolerância (BLP.PYE07). Em branco, o cadastro
                        // voltava do servidor sem dizer qual campo faltava.
                        NumberField(
                          controller: _toleranceController,
                          label: 'Tolerância (%)',
                          requiredField: true,
                          helperText: 'Use 0 para exigir o valor exato.',
                        ),
                      ],
                      if (widget.viewModel.policyKind ==
                          AmountPolicyKinds.range) ...[
                        NumberField(
                          controller: _minController,
                          label: 'Valor mínimo (R\$)',
                          requiredField: true,
                        ),
                        const SizedBox(height: AppSpacing.md),
                        NumberField(
                          controller: _maxController,
                          label: 'Valor máximo (R\$)',
                          requiredField: true,
                        ),
                      ],
                      if (widget.viewModel.policyKind ==
                          AmountPolicyKinds.unbounded)
                        Text(
                          'Sem limite, a verificação de valor do boleto fica '
                          'inconclusiva — qualquer valor passa sem alerta.',
                          style: Theme.of(context)
                              .textTheme
                              .bodySmall
                              ?.copyWith(
                                color: Theme.of(context)
                                    .colorScheme
                                    .onSurfaceVariant,
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
                      const SizedBox(height: AppSpacing.lg),
                      FilledButton(
                        onPressed:
                            widget.viewModel.isSaving ? null : _submit,
                        child: widget.viewModel.isSaving
                            ? const SizedBox(
                                height: 20,
                                width: 20,
                                child: CircularProgressIndicator(
                                  strokeWidth: 2,
                                ),
                              )
                            : const Text('Cadastrar'),
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
}
