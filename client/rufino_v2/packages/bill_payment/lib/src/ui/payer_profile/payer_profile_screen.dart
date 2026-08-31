import 'package:flutter/material.dart';
import 'package:mask_text_input_formatter/mask_text_input_formatter.dart';
import 'package:material_symbols_icons/symbols.dart';
import 'package:provider/provider.dart';
import 'package:rufino_core/rufino_core.dart';

import '../../bill_payment_permissions.dart';
import '../../domain/payer_profile.dart';
import '../bill_payment_back_button.dart';
import '../shared/message_panel.dart';
import '../shared/status_badge.dart';
import 'payer_profile_viewmodel.dart';

/// The payer profile: onboarding when absent, inline edits once it exists.
///
/// This cadastro is a functional prerequisite of capture — the screen says
/// so instead of assuming everybody read the manual.
class PayerProfileScreen extends StatefulWidget {
  /// Creates the screen.
  const PayerProfileScreen({
    super.key,
    required this.viewModel,
    required this.backFallback,
  });

  /// Drives the screen.
  final PayerProfileViewModel viewModel;

  /// Para onde o voltar leva quando não há pilha.
  final String backFallback;

  @override
  State<PayerProfileScreen> createState() => _PayerProfileScreenState();
}

class _PayerProfileScreenState extends State<PayerProfileScreen> {
  @override
  void initState() {
    super.initState();
    widget.viewModel.load();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Perfil do pagador'),
        leading: BillPaymentBackButton(fallback: widget.backFallback),
      ),
      body: SafeArea(
        child: ListenableBuilder(
          listenable: widget.viewModel,
          builder: (context, _) {
            final viewModel = widget.viewModel;
            switch (viewModel.status) {
              case PayerProfileStatus.loading:
                return const Center(child: CircularProgressIndicator());
              case PayerProfileStatus.error:
                return MessagePanel(
                  icon: Symbols.error,
                  title: viewModel.errorMessage ??
                      'Não foi possível carregar o perfil.',
                  action: FilledButton.tonal(
                    onPressed: viewModel.load,
                    child: const Text('Tentar novamente'),
                  ),
                );
              case PayerProfileStatus.onboarding:
                return _OnboardingForm(viewModel: viewModel);
              case PayerProfileStatus.loaded:
                return _ProfileBody(viewModel: viewModel);
            }
          },
        ),
      ),
    );
  }
}

class _OnboardingForm extends StatefulWidget {
  const _OnboardingForm({required this.viewModel});

  final PayerProfileViewModel viewModel;

  @override
  State<_OnboardingForm> createState() => _OnboardingFormState();
}

class _OnboardingFormState extends State<_OnboardingForm> {
  final _formKey = GlobalKey<FormState>();
  final _nameController = TextEditingController();
  final _taxIdController = TextEditingController();
  String _kind = PayerKinds.company;

  late final _taxIdMask = MaskTextInputFormatter(
    mask: '##.###.###/####-##',
    filter: {'#': RegExp(r'[0-9]')},
  );

  @override
  void dispose() {
    _nameController.dispose();
    _taxIdController.dispose();
    super.dispose();
  }

  void _applyMaskFor(String kind) {
    final mask =
        kind == PayerKinds.company ? '##.###.###/####-##' : '###.###.###-##';
    if (_taxIdMask.getMask() != mask) {
      _taxIdController.value = _taxIdMask.updateMask(mask: mask);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Center(
      child: ConstrainedBox(
        constraints: const BoxConstraints(maxWidth: AppBreakpoints.tablet),
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(AppSpacing.md),
          child: Form(
            key: _formKey,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Card.outlined(
                  child: Padding(
                    padding: const EdgeInsets.all(AppSpacing.md),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          'Antes de tudo, quem paga?',
                          style: Theme.of(context).textTheme.titleMedium,
                        ),
                        const SizedBox(height: AppSpacing.sm),
                        Text(
                          'Sem o perfil do pagador não há senha derivada '
                          'para abrir PDF protegido nem verificação de '
                          'pagador — e o que a captura não reconhecer é '
                          'descartado. Cadastre antes de conectar uma '
                          'caixa de e-mail.',
                          style: Theme.of(context).textTheme.bodyMedium,
                        ),
                      ],
                    ),
                  ),
                ),
                const SizedBox(height: AppSpacing.lg),
                SegmentedButton<String>(
                  segments: const [
                    ButtonSegment(
                      value: PayerKinds.company,
                      label: Text('Pessoa jurídica'),
                    ),
                    ButtonSegment(
                      value: PayerKinds.individual,
                      label: Text('Pessoa física'),
                    ),
                  ],
                  selected: {_kind},
                  onSelectionChanged: (selection) => setState(() {
                    _kind = selection.first;
                    _applyMaskFor(_kind);
                  }),
                ),
                const SizedBox(height: AppSpacing.md),
                TextFormField(
                  controller: _nameController,
                  decoration: const InputDecoration(
                    labelText: 'Razão social / nome completo',
                    border: OutlineInputBorder(),
                  ),
                  textCapitalization: TextCapitalization.words,
                  validator: (value) =>
                      (value == null || value.trim().isEmpty)
                          ? 'Informe o nome.'
                          : null,
                ),
                const SizedBox(height: AppSpacing.md),
                TextFormField(
                  controller: _taxIdController,
                  decoration: InputDecoration(
                    labelText:
                        _kind == PayerKinds.company ? 'CNPJ' : 'CPF',
                    border: const OutlineInputBorder(),
                  ),
                  keyboardType: TextInputType.number,
                  inputFormatters: [_taxIdMask],
                  validator: (value) {
                    final digits =
                        value?.replaceAll(RegExp(r'\D'), '') ?? '';
                    final expected =
                        _kind == PayerKinds.company ? 14 : 11;
                    return digits.length == expected
                        ? null
                        : 'Informe o documento completo.';
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
                BillPaymentPermissionGuard(
                  resource: BillPaymentResources.payerProfile,
                  scope: BillPaymentScopes.manage,
                  child: FilledButton(
                    onPressed: widget.viewModel.isMutating
                        ? null
                        : () {
                            if (!_formKey.currentState!.validate()) return;
                            widget.viewModel.register(
                              kind: _kind,
                              legalName: _nameController.text,
                              primaryTaxId: _taxIdController.text,
                            );
                          },
                    child: const Text('Cadastrar perfil'),
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _ProfileBody extends StatefulWidget {
  const _ProfileBody({required this.viewModel});

  final PayerProfileViewModel viewModel;

  @override
  State<_ProfileBody> createState() => _ProfileBodyState();
}

class _ProfileBodyState extends State<_ProfileBody> {
  final _taxIdController = TextEditingController();
  final _accountController = TextEditingController();

  @override
  void dispose() {
    _taxIdController.dispose();
    _accountController.dispose();
    super.dispose();
  }

  Future<void> _linkAccount(BuildContext context) async {
    final apiKey = _accountController.text.trim();
    if (apiKey.isEmpty) return;

    final linked = await widget.viewModel.linkAsaasAccount(apiKey);
    // A chave nunca é reexibida: vinculou, o campo esvazia.
    if (linked) _accountController.clear();
  }

  Future<void> _confirmUnlink(BuildContext context) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: const Text('Remover a chave Asaas?'),
        content: const Text(
          'A chave é apagada do cofre e a consulta oficial dos boletos '
          'deste cliente fica indisponível até uma nova chave ser vinculada.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(dialogContext).pop(false),
            child: const Text('Cancelar'),
          ),
          FilledButton.tonal(
            onPressed: () => Navigator.of(dialogContext).pop(true),
            child: const Text('Remover'),
          ),
        ],
      ),
    );
    if (confirmed == true) await widget.viewModel.unlinkAsaasAccount();
  }

  @override
  Widget build(BuildContext context) {
    final viewModel = widget.viewModel;
    final profile = viewModel.profile!;
    final canManage = context.watch<BillPaymentPermissionNotifier>()
        .hasPermission(
      BillPaymentResources.payerProfile,
      BillPaymentScopes.manage,
    );

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
            SectionCard(
              title: 'Identificação',
              child: Column(
                children: [
                  InfoRow(
                    icon: profile.kind == PayerKinds.company
                        ? Symbols.apartment
                        : Symbols.person,
                    label: PayerKinds.label(profile.kind),
                    value: profile.legalName,
                  ),
                  InfoRow(
                    icon: Symbols.badge,
                    label: profile.primaryTaxIdKind,
                    value: profile.primaryTaxId,
                  ),
                ],
              ),
            ),
            const SizedBox(height: AppSpacing.md),
            SectionCard(
              title: 'Documentos adicionais',
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    'Filiais, o CPF do titular MEI, cônjuge — cada um '
                    'aumenta o alcance da senha derivada e do roteamento.',
                    style: Theme.of(context).textTheme.bodySmall?.copyWith(
                          color:
                              Theme.of(context).colorScheme.onSurfaceVariant,
                        ),
                  ),
                  const SizedBox(height: AppSpacing.sm),
                  if (profile.additionalTaxIds.isEmpty)
                    const Text('Nenhum documento adicional.')
                  else
                    Wrap(
                      spacing: AppSpacing.xs,
                      runSpacing: AppSpacing.xs,
                      children: [
                        for (final doc in profile.additionalTaxIds)
                          InputChip(
                            label: Text('${doc.kind} ${doc.value}'),
                            onDeleted: canManage && !viewModel.isMutating
                                ? () => viewModel.removeTaxId(doc.value)
                                : null,
                          ),
                      ],
                    ),
                  if (canManage) ...[
                    const SizedBox(height: AppSpacing.sm),
                    Row(
                      children: [
                        Expanded(
                          child: TextField(
                            controller: _taxIdController,
                            decoration: const InputDecoration(
                              hintText: 'CPF ou CNPJ',
                              border: OutlineInputBorder(),
                              isDense: true,
                            ),
                            keyboardType: TextInputType.number,
                          ),
                        ),
                        const SizedBox(width: AppSpacing.sm),
                        IconButton.filledTonal(
                          icon: const Icon(Symbols.add),
                          tooltip: 'Adicionar documento',
                          onPressed: viewModel.isMutating
                              ? null
                              : () async {
                                  final added = await viewModel
                                      .addTaxId(_taxIdController.text);
                                  if (added && mounted) {
                                    _taxIdController.clear();
                                  }
                                },
                        ),
                      ],
                    ),
                  ],
                ],
              ),
            ),
            if (profile.supportsCnpjRootMatching) ...[
              const SizedBox(height: AppSpacing.md),
              SectionCard(
                title: 'Casar por raiz de CNPJ',
                child: SwitchListTile(
                  contentPadding: EdgeInsets.zero,
                  title: const Text('Filiais contam como o mesmo pagador'),
                  subtitle: const Text(
                    'Boletos impressos com qualquer filial da mesma raiz '
                    'são atribuídos a este cliente.',
                  ),
                  value: profile.matchByCnpjRoot,
                  onChanged: canManage && !viewModel.isMutating
                      ? (enabled) =>
                          viewModel.setCnpjRootMatching(enabled: enabled)
                      : null,
                ),
              ),
            ],
            const SizedBox(height: AppSpacing.md),
            SectionCard(
              title: 'Conta Asaas',
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      StatusBadge(
                        label: profile.canSchedulePayments
                            ? 'Conectada'
                            : 'Não configurada',
                        tone: profile.canSchedulePayments
                            ? BadgeTone.positive
                            : BadgeTone.attention,
                      ),
                    ],
                  ),
                  if (!profile.canSchedulePayments) ...[
                    const SizedBox(height: AppSpacing.sm),
                    Text(
                      'Sem a chave da sua subconta Asaas, a consulta oficial '
                      'dos boletos fica indisponível e nada pode ser '
                      'verificado nem agendado.',
                      style: Theme.of(context).textTheme.bodySmall?.copyWith(
                            color: Theme.of(context).colorScheme.error,
                          ),
                    ),
                  ],
                  if (canManage) ...[
                    const SizedBox(height: AppSpacing.sm),
                    Row(
                      children: [
                        Expanded(
                          // A chave é gravação única: vai para o cofre do
                          // servidor e nunca é reexibida — por isso o campo
                          // esconde o texto e é limpo após vincular.
                          child: TextField(
                            controller: _accountController,
                            obscureText: true,
                            decoration: const InputDecoration(
                              hintText: 'Chave de API da subconta Asaas',
                              border: OutlineInputBorder(),
                              isDense: true,
                            ),
                          ),
                        ),
                        const SizedBox(width: AppSpacing.sm),
                        FilledButton.tonal(
                          onPressed: viewModel.isMutating
                              ? null
                              : () => _linkAccount(context),
                          child: const Text('Vincular'),
                        ),
                      ],
                    ),
                    if (profile.canSchedulePayments) ...[
                      const SizedBox(height: AppSpacing.sm),
                      TextButton(
                        onPressed: viewModel.isMutating
                            ? null
                            : () => _confirmUnlink(context),
                        child: const Text('Remover chave'),
                      ),
                    ],
                  ],
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}
