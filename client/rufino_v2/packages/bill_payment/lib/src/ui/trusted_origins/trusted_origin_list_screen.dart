import 'package:flutter/material.dart';
import 'package:material_symbols_icons/symbols.dart';
import 'package:rufino_core/rufino_core.dart';

import '../../bill_payment_permissions.dart';
import '../../domain/bill_payment_enums.dart';
import '../../domain/trusted_origin.dart';
import '../bill_payment_back_button.dart';
import '../shared/message_panel.dart';
import '../shared/status_badge.dart';
import 'trusted_origin_list_viewmodel.dart';

/// The trusted origin listing, with the sender resolver and the register
/// sheet.
///
/// Decision changes and removal live on each row — an origin is one value
/// and one decision, too little cadastro for a detail route.
class TrustedOriginListScreen extends StatefulWidget {
  /// Creates the screen.
  const TrustedOriginListScreen({
    super.key,
    required this.viewModel,
    required this.backFallback,
  });

  /// Drives the screen.
  final TrustedOriginListViewModel viewModel;

  /// Para onde o voltar leva quando não há pilha.
  final String backFallback;

  @override
  State<TrustedOriginListScreen> createState() =>
      _TrustedOriginListScreenState();
}

class _TrustedOriginListScreenState extends State<TrustedOriginListScreen> {
  final _resolveController = TextEditingController();
  final _scrollController = ScrollController();

  @override
  void initState() {
    super.initState();
    _scrollController.addListener(_onScroll);
    widget.viewModel.load();
  }

  @override
  void dispose() {
    _scrollController
      ..removeListener(_onScroll)
      ..dispose();
    _resolveController.dispose();
    super.dispose();
  }

  void _onScroll() {
    if (!_scrollController.hasClients) return;
    final position = _scrollController.position;
    if (position.pixels >= position.maxScrollExtent - 240) {
      widget.viewModel.loadMore();
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Origens confiáveis'),
        leading: BillPaymentBackButton(fallback: widget.backFallback),
      ),
      floatingActionButton: BillPaymentPermissionGuard(
        resource: BillPaymentResources.origin,
        scope: BillPaymentScopes.manage,
        child: FloatingActionButton.extended(
          onPressed: () => _openRegisterSheet(context),
          icon: const Icon(Symbols.add),
          label: const Text('Cadastrar'),
        ),
      ),
      body: SafeArea(
        child: ListenableBuilder(
          listenable: widget.viewModel,
          builder: (context, _) => Center(
            child: ConstrainedBox(
              constraints:
                  const BoxConstraints(maxWidth: AppBreakpoints.desktop),
              child: Column(
                children: [
                  _ResolveTester(
                    viewModel: widget.viewModel,
                    controller: _resolveController,
                  ),
                  Expanded(child: _Results(
                    viewModel: widget.viewModel,
                    scrollController: _scrollController,
                  )),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }

  Future<void> _openRegisterSheet(BuildContext context) async {
    await showModalBottomSheet<void>(
      context: context,
      isScrollControlled: true,
      builder: (sheetContext) => Padding(
        padding: EdgeInsets.only(
          bottom: MediaQuery.of(sheetContext).viewInsets.bottom,
        ),
        child: _RegisterSheet(viewModel: widget.viewModel),
      ),
    );
  }
}

class _ResolveTester extends StatelessWidget {
  const _ResolveTester({required this.viewModel, required this.controller});

  final TrustedOriginListViewModel viewModel;
  final TextEditingController controller;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Padding(
      padding: const EdgeInsets.fromLTRB(
        AppSpacing.md,
        AppSpacing.md,
        AppSpacing.md,
        AppSpacing.sm,
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          TextField(
            controller: controller,
            decoration: const InputDecoration(
              hintText: 'Quem responde por este remetente?',
              prefixIcon: Icon(Symbols.travel_explore),
              border: OutlineInputBorder(),
            ),
            textInputAction: TextInputAction.search,
            onSubmitted: viewModel.resolveSender,
          ),
          const SizedBox(height: AppSpacing.xs),
          switch (viewModel.resolveOutcome) {
            ResolveOutcome.idle => const SizedBox.shrink(),
            ResolveOutcome.resolving => const Padding(
                padding: EdgeInsets.all(AppSpacing.xs),
                child: SizedBox(
                  height: 16,
                  width: 16,
                  child: CircularProgressIndicator(strokeWidth: 2),
                ),
              ),
            ResolveOutcome.unknown => Text(
                'Origem desconhecida — nenhum cadastro casa com este '
                'remetente.',
                style: theme.textTheme.bodySmall,
              ),
            ResolveOutcome.matched => Text(
                'Casa com ${OriginKinds.label(viewModel.resolved!.kind)} '
                '"${viewModel.resolved!.value}" — '
                '${TrustDecisions.label(viewModel.resolved!.decision)}.',
                style: theme.textTheme.bodySmall,
              ),
          },
        ],
      ),
    );
  }
}

class _Results extends StatelessWidget {
  const _Results({required this.viewModel, required this.scrollController});

  final TrustedOriginListViewModel viewModel;
  final ScrollController scrollController;

  @override
  Widget build(BuildContext context) {
    switch (viewModel.status) {
      case TrustedOriginListStatus.loading:
        return const Center(child: CircularProgressIndicator());
      case TrustedOriginListStatus.error:
        return MessagePanel(
          icon: Symbols.error,
          title: viewModel.errorMessage ??
              'Não foi possível carregar as origens.',
          action: FilledButton.tonal(
            onPressed: viewModel.load,
            child: const Text('Tentar novamente'),
          ),
        );
      case TrustedOriginListStatus.empty:
        return const MessagePanel(
          icon: Symbols.mark_email_read,
          title: 'Nenhuma origem cadastrada.\nRemetente conhecido é o que '
              'transforma descarte em quarentena revisável.',
        );
      case TrustedOriginListStatus.loaded:
      case TrustedOriginListStatus.loadingMore:
        return ListView.builder(
          controller: scrollController,
          padding: const EdgeInsets.fromLTRB(
            AppSpacing.md,
            0,
            AppSpacing.md,
            AppSpacing.md + 72,
          ),
          itemCount: viewModel.items.length + (viewModel.hasMore ? 1 : 0),
          itemBuilder: (context, index) {
            if (index >= viewModel.items.length) {
              return const Padding(
                padding: EdgeInsets.all(AppSpacing.md),
                child: Center(child: CircularProgressIndicator()),
              );
            }
            return _OriginRow(
              origin: viewModel.items[index],
              viewModel: viewModel,
            );
          },
        );
    }
  }
}

class _OriginRow extends StatelessWidget {
  const _OriginRow({required this.origin, required this.viewModel});

  final TrustedOrigin origin;
  final TrustedOriginListViewModel viewModel;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Padding(
      padding: const EdgeInsets.only(bottom: AppSpacing.sm),
      child: Card.outlined(
        child: Padding(
          padding: const EdgeInsets.symmetric(
            horizontal: AppSpacing.md,
            vertical: AppSpacing.sm,
          ),
          child: Row(
            children: [
              Icon(
                origin.isBlocked ? Symbols.block : Symbols.verified_user,
                color: origin.isBlocked
                    ? theme.colorScheme.error
                    : theme.colorScheme.primary,
              ),
              const SizedBox(width: AppSpacing.md),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(origin.value, style: theme.textTheme.titleMedium),
                    Text(
                      OriginKinds.label(origin.kind) +
                          (origin.note == null ? '' : ' · ${origin.note}'),
                      style: theme.textTheme.bodySmall?.copyWith(
                        color: theme.colorScheme.onSurfaceVariant,
                      ),
                    ),
                  ],
                ),
              ),
              StatusBadge(
                label: TrustDecisions.label(origin.decision),
                tone: origin.isBlocked
                    ? BadgeTone.problem
                    : BadgeTone.positive,
              ),
              BillPaymentPermissionGuard(
                resource: BillPaymentResources.origin,
                scope: BillPaymentScopes.manage,
                child: PopupMenuButton<String>(
                  tooltip: 'Ações',
                  enabled: !viewModel.isMutating,
                  onSelected: (action) => switch (action) {
                    'toggle' => viewModel.changeDecision(
                        origin.id,
                        origin.isBlocked
                            ? TrustDecisions.trusted
                            : TrustDecisions.blocked,
                      ),
                    _ => viewModel.deleteOrigin(origin.id),
                  },
                  itemBuilder: (context) => [
                    PopupMenuItem(
                      value: 'toggle',
                      child: Text(
                        origin.isBlocked
                            ? 'Marcar como confiável'
                            : 'Bloquear',
                      ),
                    ),
                    const PopupMenuItem(
                      value: 'delete',
                      child: Text('Excluir'),
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _RegisterSheet extends StatefulWidget {
  const _RegisterSheet({required this.viewModel});

  final TrustedOriginListViewModel viewModel;

  @override
  State<_RegisterSheet> createState() => _RegisterSheetState();
}

class _RegisterSheetState extends State<_RegisterSheet> {
  final _formKey = GlobalKey<FormState>();
  final _valueController = TextEditingController();
  final _noteController = TextEditingController();
  String _kind = OriginKinds.emailDomain;
  String _decision = TrustDecisions.trusted;

  @override
  void dispose() {
    _valueController.dispose();
    _noteController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.all(AppSpacing.md),
      child: Form(
        key: _formKey,
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(
              'Nova origem',
              style: Theme.of(context).textTheme.titleLarge,
            ),
            const SizedBox(height: AppSpacing.md),
            DropdownButtonFormField<String>(
              initialValue: _kind,
              decoration: const InputDecoration(
                labelText: 'Tipo',
                border: OutlineInputBorder(),
              ),
              items: const [
                DropdownMenuItem(
                  value: OriginKinds.emailAddress,
                  child: Text('Endereço de e-mail'),
                ),
                DropdownMenuItem(
                  value: OriginKinds.emailDomain,
                  child: Text('Domínio de e-mail'),
                ),
                DropdownMenuItem(
                  value: OriginKinds.webDomain,
                  child: Text('Domínio web'),
                ),
              ],
              onChanged: (value) => setState(() => _kind = value!),
            ),
            const SizedBox(height: AppSpacing.md),
            TextFormField(
              controller: _valueController,
              decoration: const InputDecoration(
                labelText: 'Valor (ex.: fornecedor.com.br)',
                border: OutlineInputBorder(),
              ),
              keyboardType: TextInputType.emailAddress,
              validator: (value) => (value == null || value.trim().isEmpty)
                  ? 'Informe o endereço ou domínio.'
                  : null,
            ),
            const SizedBox(height: AppSpacing.md),
            SegmentedButton<String>(
              segments: const [
                ButtonSegment(
                  value: TrustDecisions.trusted,
                  label: Text('Confiável'),
                ),
                ButtonSegment(
                  value: TrustDecisions.blocked,
                  label: Text('Bloqueada'),
                ),
              ],
              selected: {_decision},
              onSelectionChanged: (selection) =>
                  setState(() => _decision = selection.first),
            ),
            const SizedBox(height: AppSpacing.md),
            TextFormField(
              controller: _noteController,
              decoration: const InputDecoration(
                labelText: 'Observação (opcional)',
                border: OutlineInputBorder(),
              ),
            ),
            const SizedBox(height: AppSpacing.lg),
            FilledButton(
              onPressed: () async {
                if (!_formKey.currentState!.validate()) return;
                final registered = await widget.viewModel.register(
                  kind: _kind,
                  value: _valueController.text,
                  decision: _decision,
                  note: _noteController.text.trim().isEmpty
                      ? null
                      : _noteController.text,
                );
                if (registered && context.mounted) {
                  Navigator.of(context).pop();
                }
              },
              child: const Text('Cadastrar'),
            ),
            const SizedBox(height: AppSpacing.md),
          ],
        ),
      ),
    );
  }
}
