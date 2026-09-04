import 'package:flutter/material.dart';
import 'package:material_symbols_icons/symbols.dart';
import 'package:provider/provider.dart';
import 'package:rufino_core/rufino_core.dart';

import '../../bill_payment_permissions.dart';
import '../../domain/bill_payment_enums.dart';
import '../../domain/payee.dart';
import '../../domain/payee_repository.dart';
import '../bill_payment_back_button.dart';
import '../shared/amount_policy_view.dart';
import '../shared/message_panel.dart';
import '../shared/number_field.dart';
import '../shared/status_badge.dart';
import 'payee_detail_viewmodel.dart';

/// The payee detail: read in blocks, edit in place (D8).
class PayeeDetailScreen extends StatefulWidget {
  /// Creates the screen.
  const PayeeDetailScreen({
    super.key,
    required this.viewModel,
    required this.backFallback,
    required this.onDeleted,
  });

  /// Drives the screen.
  final PayeeDetailViewModel viewModel;

  /// Para onde o voltar leva quando não há pilha.
  final String backFallback;

  /// Called after the payee is removed.
  final VoidCallback onDeleted;

  @override
  State<PayeeDetailScreen> createState() => _PayeeDetailScreenState();
}

class _PayeeDetailScreenState extends State<PayeeDetailScreen> {
  @override
  void initState() {
    super.initState();
    widget.viewModel.load();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Beneficiário'),
        leading: BillPaymentBackButton(fallback: widget.backFallback),
      ),
      body: SafeArea(
        child: ListenableBuilder(
          listenable: widget.viewModel,
          builder: (context, _) {
            final viewModel = widget.viewModel;
            switch (viewModel.status) {
              case PayeeDetailStatus.loading:
                return const Center(child: CircularProgressIndicator());
              case PayeeDetailStatus.error:
                return MessagePanel(
                  icon: Symbols.error,
                  title: viewModel.errorMessage ??
                      'Não foi possível carregar o beneficiário.',
                  action: FilledButton.tonal(
                    onPressed: viewModel.load,
                    child: const Text('Tentar novamente'),
                  ),
                );
              case PayeeDetailStatus.loaded:
                return _Body(
                  viewModel: viewModel,
                  onDeleted: widget.onDeleted,
                );
            }
          },
        ),
      ),
    );
  }
}

class _Body extends StatelessWidget {
  const _Body({required this.viewModel, required this.onDeleted});

  final PayeeDetailViewModel viewModel;
  final VoidCallback onDeleted;

  @override
  Widget build(BuildContext context) {
    final payee = viewModel.payee!;
    return Center(
      child: ConstrainedBox(
        constraints: const BoxConstraints(maxWidth: AppBreakpoints.tablet),
        child: ListView(
          padding: const EdgeInsets.all(AppSpacing.md),
          children: [
            if (viewModel.errorMessage != null)
              Padding(
                padding: const EdgeInsets.only(bottom: AppSpacing.md),
                child: Text(
                  viewModel.errorMessage!,
                  style:
                      TextStyle(color: Theme.of(context).colorScheme.error),
                ),
              ),
            _IdentificationSection(viewModel: viewModel, payee: payee),
            const SizedBox(height: AppSpacing.md),
            _AmountPolicySection(viewModel: viewModel, payee: payee),
            const SizedBox(height: AppSpacing.md),
            _ChipsSection(
              title: 'Apelidos',
              subtitle: 'Nomes alternativos sob os quais este beneficiário '
                  'aparece nos documentos.',
              values: payee.aliases,
              hint: 'Novo apelido',
              onAdd: viewModel.addAlias,
              onRemove: viewModel.removeAlias,
              isMutating: viewModel.isMutating,
            ),
            const SizedBox(height: AppSpacing.md),
            _ChipsSection(
              title: 'Bancos aceitos',
              subtitle: 'Códigos COMPE (3 dígitos) pelos quais este '
                  'beneficiário costuma cobrar.',
              values: payee.acceptedBanks,
              hint: 'Código (ex.: 033)',
              onAdd: viewModel.addBank,
              onRemove: viewModel.removeBank,
              isMutating: viewModel.isMutating,
              digitsOnly: true,
            ),
            const SizedBox(height: AppSpacing.md),
            _StandingSection(viewModel: viewModel, payee: payee),
            const SizedBox(height: AppSpacing.md),
            _ActivationSection(
              viewModel: viewModel,
              payee: payee,
              onDeleted: onDeleted,
            ),
          ],
        ),
      ),
    );
  }
}

class _IdentificationSection extends StatefulWidget {
  const _IdentificationSection({required this.viewModel, required this.payee});

  final PayeeDetailViewModel viewModel;
  final Payee payee;

  @override
  State<_IdentificationSection> createState() =>
      _IdentificationSectionState();
}

class _IdentificationSectionState extends State<_IdentificationSection> {
  bool _editing = false;
  late final _nameController =
      TextEditingController(text: widget.payee.legalName);

  @override
  void dispose() {
    _nameController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final payee = widget.payee;
    return SectionCard(
      title: 'Identificação',
      trailing: _editing
          ? null
          : BillPaymentPermissionGuard(
              resource: BillPaymentResources.payee,
              scope: BillPaymentScopes.manage,
              child: IconButton(
                icon: const Icon(Symbols.edit),
                tooltip: 'Editar',
                onPressed: () => setState(() => _editing = true),
              ),
            ),
      child: _editing
          ? Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                TextField(
                  controller: _nameController,
                  decoration: const InputDecoration(
                    labelText: 'Razão social',
                    border: OutlineInputBorder(),
                  ),
                ),
                const SizedBox(height: AppSpacing.md),
                Row(
                  mainAxisAlignment: MainAxisAlignment.end,
                  children: [
                    TextButton(
                      onPressed: () => setState(() {
                        _nameController.text = payee.legalName;
                        _editing = false;
                      }),
                      child: const Text('Cancelar'),
                    ),
                    const SizedBox(width: AppSpacing.sm),
                    FilledButton.tonal(
                      onPressed: widget.viewModel.isMutating
                          ? null
                          : () async {
                              final saved = await widget.viewModel
                                  .saveLegalName(_nameController.text);
                              if (saved && mounted) {
                                setState(() => _editing = false);
                              }
                            },
                      child: const Text('Salvar'),
                    ),
                  ],
                ),
              ],
            )
          : Column(
              children: [
                InfoRow(
                  icon: Symbols.storefront,
                  label: 'Razão social',
                  value: payee.legalName,
                ),
                InfoRow(
                  icon: Symbols.badge,
                  label: payee.taxIdKind,
                  value: payee.taxId,
                ),

              ],
            ),
    );
  }
}

/// A política de valor: leitura completa e edição no lugar.
///
/// Card próprio, e não uma linha dentro de "Identificação", porque são
/// perguntas diferentes — quem é o beneficiário × quanto ele cobra — e esta
/// tem quatro campos e um seletor de tipo.
class _AmountPolicySection extends StatefulWidget {
  const _AmountPolicySection({required this.viewModel, required this.payee});

  final PayeeDetailViewModel viewModel;
  final Payee payee;

  @override
  State<_AmountPolicySection> createState() => _AmountPolicySectionState();
}

class _AmountPolicySectionState extends State<_AmountPolicySection> {
  final _formKey = GlobalKey<FormState>();
  final _expectedController = TextEditingController();
  final _toleranceController = TextEditingController();
  final _minController = TextEditingController();
  final _maxController = TextEditingController();

  bool _editing = false;
  late String _kind = widget.payee.amountPolicy.kind;

  @override
  void dispose() {
    _expectedController.dispose();
    _toleranceController.dispose();
    _minController.dispose();
    _maxController.dispose();
    super.dispose();
  }

  /// Recarrega os campos a partir da política vigente — abre a edição com o
  /// que está valendo, e é também o que o Cancelar desfaz.
  void _resetFields() {
    final policy = widget.payee.amountPolicy;
    _kind = policy.kind;
    _expectedController.text = _text(policy.expectedAmount);
    _toleranceController.text = _text(policy.tolerancePercent);
    _minController.text = _text(policy.minAmount);
    _maxController.text = _text(policy.maxAmount);
  }

  static String _text(double? value) =>
      value == null ? '' : value.toString().replaceAll('.', ',');

  /// Só os campos do tipo escolhido viajam. O domínio ignora os demais, mas
  /// mandar min/max junto de um valor fixo descreveria uma política que não
  /// existe.
  AmountPolicyInput _input() => switch (_kind) {
        AmountPolicyKinds.fixed => AmountPolicyInput(
            kind: _kind,
            expectedAmount: NumberField.read(_expectedController),
            tolerancePercent: NumberField.read(_toleranceController),
          ),
        AmountPolicyKinds.range => AmountPolicyInput(
            kind: _kind,
            minAmount: NumberField.read(_minController),
            maxAmount: NumberField.read(_maxController),
          ),
        _ => AmountPolicyInput(kind: _kind),
      };

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;

    final saved = await widget.viewModel.savePolicy(_input());
    if (saved && mounted) setState(() => _editing = false);
  }

  @override
  Widget build(BuildContext context) {
    return SectionCard(
      title: 'Política de valor',
      trailing: _editing
          ? null
          : BillPaymentPermissionGuard(
              resource: BillPaymentResources.payee,
              scope: BillPaymentScopes.manage,
              child: IconButton(
                icon: const Icon(Symbols.edit),
                tooltip: 'Editar política de valor',
                onPressed: () => setState(() {
                  _resetFields();
                  _editing = true;
                }),
              ),
            ),
      child: _editing
          ? _buildEditor(context)
          : AmountPolicyDetails(policy: widget.payee.amountPolicy),
    );
  }

  Widget _buildEditor(BuildContext context) {
    return Form(
      key: _formKey,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
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
            selected: {_kind},
            onSelectionChanged: (selection) =>
                setState(() => _kind = selection.first),
          ),
          const SizedBox(height: AppSpacing.md),
          if (_kind == AmountPolicyKinds.fixed) ...[
            NumberField(
              controller: _expectedController,
              label: 'Valor esperado (R\$)',
              requiredField: true,
            ),
            const SizedBox(height: AppSpacing.md),
            NumberField(
              controller: _toleranceController,
              label: 'Tolerância (%)',
              requiredField: true,
              helperText: 'Use 0 para exigir o valor exato.',
            ),
          ],
          if (_kind == AmountPolicyKinds.range) ...[
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
          if (_kind == AmountPolicyKinds.unbounded)
            Text(
              'Sem limite, a verificação de valor do boleto fica inconclusiva '
              '— qualquer valor passa sem alerta.',
              style: Theme.of(context).textTheme.bodySmall?.copyWith(
                    color: Theme.of(context).colorScheme.tertiary,
                  ),
            ),
          const SizedBox(height: AppSpacing.md),
          Row(
            mainAxisAlignment: MainAxisAlignment.end,
            children: [
              TextButton(
                onPressed: () => setState(() {
                  _resetFields();
                  _editing = false;
                }),
                child: const Text('Cancelar'),
              ),
              const SizedBox(width: AppSpacing.sm),
              FilledButton.tonal(
                onPressed: widget.viewModel.isMutating ? null : _save,
                child: const Text('Salvar'),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _ChipsSection extends StatefulWidget {
  const _ChipsSection({
    required this.title,
    required this.subtitle,
    required this.values,
    required this.hint,
    required this.onAdd,
    required this.onRemove,
    required this.isMutating,
    this.digitsOnly = false,
  });

  final String title;
  final String subtitle;
  final List<String> values;
  final String hint;
  final Future<bool> Function(String value) onAdd;
  final Future<bool> Function(String value) onRemove;
  final bool isMutating;
  final bool digitsOnly;

  @override
  State<_ChipsSection> createState() => _ChipsSectionState();
}

class _ChipsSectionState extends State<_ChipsSection> {
  final _controller = TextEditingController();

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  Future<void> _add() async {
    final value = _controller.text.trim();
    if (value.isEmpty) return;
    final added = await widget.onAdd(value);
    if (added && mounted) _controller.clear();
  }

  @override
  Widget build(BuildContext context) {
    final canManage = context.watch<BillPaymentPermissionNotifier>()
        .hasPermission(BillPaymentResources.payee, BillPaymentScopes.manage);

    return SectionCard(
      title: widget.title,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            widget.subtitle,
            style: Theme.of(context).textTheme.bodySmall?.copyWith(
                  color: Theme.of(context).colorScheme.onSurfaceVariant,
                ),
          ),
          const SizedBox(height: AppSpacing.sm),
          if (widget.values.isEmpty)
            Text(
              'Nenhum cadastrado.',
              style: Theme.of(context).textTheme.bodyMedium,
            )
          else
            Wrap(
              spacing: AppSpacing.xs,
              runSpacing: AppSpacing.xs,
              children: [
                for (final value in widget.values)
                  InputChip(
                    label: Text(value),
                    // Sem permissão o chip fica, a remoção some.
                    onDeleted: canManage && !widget.isMutating
                        ? () => widget.onRemove(value)
                        : null,
                  ),
              ],
            ),
          const SizedBox(height: AppSpacing.sm),
          BillPaymentPermissionGuard(
            resource: BillPaymentResources.payee,
            scope: BillPaymentScopes.manage,
            child: Row(
              children: [
                Expanded(
                  child: TextField(
                    controller: _controller,
                    decoration: InputDecoration(
                      hintText: widget.hint,
                      border: const OutlineInputBorder(),
                      isDense: true,
                    ),
                    keyboardType: widget.digitsOnly
                        ? TextInputType.number
                        : TextInputType.text,
                    onSubmitted: (_) => _add(),
                  ),
                ),
                const SizedBox(width: AppSpacing.sm),
                IconButton.filledTonal(
                  icon: const Icon(Symbols.add),
                  tooltip: 'Adicionar',
                  onPressed: widget.isMutating ? null : _add,
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

/// The trust mark, read-and-act like the activation block: badge + actions,
/// no edit mode. Blacklist reads in red so whoever opens the cadastro sees
/// the block before anything else.
class _StandingSection extends StatelessWidget {
  const _StandingSection({required this.viewModel, required this.payee});

  final PayeeDetailViewModel viewModel;
  final Payee payee;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;
    return SectionCard(
      title: 'Marca de confiança',
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              if (payee.isBlacklisted) ...[
                Icon(Symbols.block, color: colorScheme.error),
                const SizedBox(width: AppSpacing.sm),
              ],
              StatusBadge(
                label: payee.isBlacklisted
                    ? 'Blacklist'
                    : payee.isWhitelisted
                        ? 'Whitelist'
                        : 'Sem marca',
                tone: payee.isBlacklisted
                    ? BadgeTone.problem
                    : payee.isWhitelisted
                        ? BadgeTone.positive
                        : BadgeTone.neutral,
              ),
            ],
          ),
          if (payee.isBlacklisted) ...[
            const SizedBox(height: AppSpacing.sm),
            Text(
              'Todo boleto deste beneficiário é sinalizado como Perigo nas '
              'verificações, e aprová-lo exige assumir o risco.',
              style: Theme.of(context)
                  .textTheme
                  .bodySmall
                  ?.copyWith(color: colorScheme.error),
            ),
          ],
          const SizedBox(height: AppSpacing.sm),
          BillPaymentPermissionGuard(
            resource: BillPaymentResources.payee,
            scope: BillPaymentScopes.manage,
            child: Wrap(
              spacing: AppSpacing.sm,
              children: [
                if (!payee.isBlacklisted)
                  FilledButton.tonal(
                    onPressed: viewModel.isMutating
                        ? null
                        : () => _confirmBlacklist(context),
                    child: const Text('Marcar como blacklist'),
                  ),
                if (!payee.isWhitelisted)
                  OutlinedButton(
                    onPressed: viewModel.isMutating
                        ? null
                        : () =>
                            viewModel.setStanding(PayeeStandings.whitelisted),
                    child: const Text('Marcar como whitelist'),
                  ),
                if (payee.isBlacklisted || payee.isWhitelisted)
                  TextButton(
                    onPressed: viewModel.isMutating
                        ? null
                        : () => viewModel.setStanding(PayeeStandings.normal),
                    child: const Text('Remover marca'),
                  ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Future<void> _confirmBlacklist(BuildContext context) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: const Text('Marcar na blacklist?'),
        content: Text(
          'Todo boleto de ${payee.legalName} passará a ser sinalizado como '
          'Perigo nas verificações, e aprovar exigirá assumir o risco '
          'explicitamente. A marca vale a partir da próxima verificação de '
          'cada boleto.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(dialogContext).pop(false),
            child: const Text('Cancelar'),
          ),
          FilledButton.tonal(
            onPressed: () => Navigator.of(dialogContext).pop(true),
            child: const Text('Marcar'),
          ),
        ],
      ),
    );
    if (confirmed == true) {
      await viewModel.setStanding(PayeeStandings.blacklisted);
    }
  }
}

class _ActivationSection extends StatelessWidget {
  const _ActivationSection({
    required this.viewModel,
    required this.payee,
    required this.onDeleted,
  });

  final PayeeDetailViewModel viewModel;
  final Payee payee;
  final VoidCallback onDeleted;

  @override
  Widget build(BuildContext context) {
    return SectionCard(
      title: 'Situação',
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              StatusBadge(
                label: payee.isActive ? 'Ativo' : 'Desativado',
                tone: payee.isActive ? BadgeTone.positive : BadgeTone.problem,
              ),
            ],
          ),
          const SizedBox(height: AppSpacing.sm),
          BillPaymentPermissionGuard(
            resource: BillPaymentResources.payee,
            scope: BillPaymentScopes.manage,
            child: Wrap(
              spacing: AppSpacing.sm,
              children: [
                FilledButton.tonal(
                  onPressed: viewModel.isMutating
                      ? null
                      : () =>
                          viewModel.setActivation(isActive: !payee.isActive),
                  child: Text(payee.isActive ? 'Desativar' : 'Reativar'),
                ),
                OutlinedButton(
                  onPressed: viewModel.isMutating
                      ? null
                      : () => _confirmDelete(context),
                  child: const Text('Excluir'),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Future<void> _confirmDelete(BuildContext context) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: const Text('Excluir beneficiário?'),
        content: Text(
          'Os boletos de ${payee.legalName} deixarão de casar com o '
          'cadastro, e os não reconhecidos passam a ser descartados.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(dialogContext).pop(false),
            child: const Text('Cancelar'),
          ),
          FilledButton.tonal(
            onPressed: () => Navigator.of(dialogContext).pop(true),
            child: const Text('Excluir'),
          ),
        ],
      ),
    );
    if (confirmed != true) return;
    final deleted = await viewModel.deletePayee();
    if (deleted) onDeleted();
  }
}
