import 'package:flutter/material.dart';
import 'package:rufino_core/rufino_core.dart';

import '../../domain/bill_payment_enums.dart';
import '../bill_payment_back_button.dart';
import 'expectation_form_viewmodel.dart';

/// The expectation register form.
class ExpectationFormScreen extends StatefulWidget {
  /// Creates the screen.
  const ExpectationFormScreen({
    super.key,
    required this.viewModel,
    required this.backFallback,
    required this.onRegistered,
  });

  /// Drives the screen.
  final ExpectationFormViewModel viewModel;

  /// Para onde o voltar leva quando não há pilha.
  final String backFallback;

  /// Called with the id of the expectation just registered.
  final void Function(String id) onRegistered;

  @override
  State<ExpectationFormScreen> createState() => _ExpectationFormScreenState();
}

class _ExpectationFormScreenState extends State<ExpectationFormScreen> {
  final _formKey = GlobalKey<FormState>();
  final _labelController = TextEditingController();
  final _accountController = TextEditingController();
  final _dueDayController = TextEditingController(text: '10');
  final _leadDaysController = TextEditingController(text: '7');
  final _alertLeadController = TextEditingController();

  @override
  void initState() {
    super.initState();
    widget.viewModel.loadPayees();
  }

  @override
  void dispose() {
    _labelController.dispose();
    _accountController.dispose();
    _dueDayController.dispose();
    _leadDaysController.dispose();
    _alertLeadController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;

    final id = await widget.viewModel.register(
      label: _labelController.text.trim(),
      expectedDueDay: int.parse(_dueDayController.text),
      observedLeadDays: int.parse(_leadDaysController.text),
      accountReference: _accountController.text.trim().isEmpty
          ? null
          : _accountController.text.trim(),
      alertLeadDays: int.tryParse(_alertLeadController.text),
    );
    if (id != null && mounted) widget.onRegistered(id);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Nova expectativa'),
        leading: BillPaymentBackButton(fallback: widget.backFallback),
      ),
      body: SafeArea(
        child: ListenableBuilder(
          listenable: widget.viewModel,
          builder: (context, _) {
            final viewModel = widget.viewModel;
            if (viewModel.isLoadingPayees) {
              return const Center(child: CircularProgressIndicator());
            }
            return Center(
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
                        DropdownButtonFormField<String>(
                          initialValue: viewModel.selectedPayeeId,
                          decoration: const InputDecoration(
                            labelText: 'Beneficiário',
                            border: OutlineInputBorder(),
                          ),
                          items: [
                            for (final payee in viewModel.payeeOptions)
                              DropdownMenuItem(
                                value: payee.id,
                                child: Text(
                                  payee.legalName,
                                  overflow: TextOverflow.ellipsis,
                                ),
                              ),
                          ],
                          onChanged: viewModel.selectPayee,
                          validator: (value) =>
                              value == null ? 'Escolha o beneficiário.' : null,
                        ),
                        const SizedBox(height: AppSpacing.md),
                        TextFormField(
                          controller: _accountController,
                          decoration: const InputDecoration(
                            labelText: 'Conta / referência (opcional)',
                            hintText:
                                'Ex.: instalação da EDP, matrícula do DAE',
                            helperText:
                                'É o que separa duas contas do mesmo '
                                'beneficiário — informe, nunca é deduzida.',
                            helperMaxLines: 2,
                            border: OutlineInputBorder(),
                          ),
                        ),
                        const SizedBox(height: AppSpacing.md),
                        TextFormField(
                          controller: _labelController,
                          decoration: const InputDecoration(
                            labelText: 'Nome da expectativa',
                            hintText: 'Ex.: EDP — Casa Florentino',
                            border: OutlineInputBorder(),
                          ),
                          validator: (value) =>
                              (value == null || value.trim().isEmpty)
                                  ? 'Dê um nome.'
                                  : null,
                        ),
                        const SizedBox(height: AppSpacing.md),
                        DropdownButtonFormField<String>(
                          initialValue: viewModel.recurrence,
                          decoration: const InputDecoration(
                            labelText: 'Recorrência',
                            border: OutlineInputBorder(),
                          ),
                          items: [
                            for (final recurrence in Recurrences.all)
                              DropdownMenuItem(
                                value: recurrence,
                                child:
                                    Text(Recurrences.label(recurrence)),
                              ),
                          ],
                          onChanged: (value) =>
                              viewModel.selectRecurrence(value!),
                        ),
                        const SizedBox(height: AppSpacing.md),
                        Row(
                          children: [
                            Expanded(
                              child: _IntField(
                                controller: _dueDayController,
                                label: 'Dia do vencimento',
                                min: 1,
                                max: 31,
                              ),
                            ),
                            const SizedBox(width: AppSpacing.md),
                            Expanded(
                              child: _IntField(
                                controller: _leadDaysController,
                                label: 'Chega quantos dias antes',
                                min: 0,
                                max: 60,
                              ),
                            ),
                          ],
                        ),
                        const SizedBox(height: AppSpacing.md),
                        _IntField(
                          controller: _alertLeadController,
                          label: 'Avisar quantos dias antes (opcional)',
                          min: 1,
                          max: 60,
                          requiredField: false,
                        ),
                        if (viewModel.errorMessage != null) ...[
                          const SizedBox(height: AppSpacing.md),
                          Text(
                            viewModel.errorMessage!,
                            style: TextStyle(
                              color: Theme.of(context).colorScheme.error,
                            ),
                          ),
                        ],
                        const SizedBox(height: AppSpacing.lg),
                        FilledButton(
                          onPressed: viewModel.isSaving ? null : _submit,
                          child: viewModel.isSaving
                              ? const SizedBox(
                                  height: 20,
                                  width: 20,
                                  child: CircularProgressIndicator(
                                      strokeWidth: 2),
                                )
                              : const Text('Cadastrar'),
                        ),
                      ],
                    ),
                  ),
                ),
              ),
            );
          },
        ),
      ),
    );
  }
}

class _IntField extends StatelessWidget {
  const _IntField({
    required this.controller,
    required this.label,
    required this.min,
    required this.max,
    this.requiredField = true,
  });

  final TextEditingController controller;
  final String label;
  final int min;
  final int max;
  final bool requiredField;

  @override
  Widget build(BuildContext context) {
    return TextFormField(
      controller: controller,
      decoration: InputDecoration(
        labelText: label,
        border: const OutlineInputBorder(),
      ),
      keyboardType: TextInputType.number,
      validator: (value) {
        final text = value?.trim() ?? '';
        if (text.isEmpty) return requiredField ? 'Informe o valor.' : null;
        final parsed = int.tryParse(text);
        if (parsed == null || parsed < min || parsed > max) {
          return 'Entre $min e $max.';
        }
        return null;
      },
    );
  }
}
