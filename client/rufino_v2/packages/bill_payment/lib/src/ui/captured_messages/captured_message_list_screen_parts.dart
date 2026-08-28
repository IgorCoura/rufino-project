import 'package:flutter/material.dart';
import 'package:material_symbols_icons/symbols.dart';
import 'package:provider/provider.dart';
import 'package:rufino_core/rufino_core.dart';

import '../../bill_payment_permissions.dart';
import '../../domain/bill_payment_enums.dart';
import '../../domain/captured_message.dart';
import '../shared/formats.dart';
import 'captured_message_list_viewmodel.dart';

/// O cabeçalho: quando a caixa foi lida pela última vez.
///
/// É a primeira coisa da tela porque é a primeira pergunta de quem chega aqui —
/// "a varredura já rodou depois de eu mandar o e-mail?".
class CaptureSyncHeader extends StatelessWidget {
  /// Creates the header.
  const CaptureSyncHeader({super.key, required this.status});

  /// The status, or `null` while it has not loaded.
  final CaptureSyncStatus? status;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final lastSync = status?.lastSyncAt;

    return Padding(
      padding: const EdgeInsets.fromLTRB(
        AppSpacing.md,
        AppSpacing.md,
        AppSpacing.md,
        AppSpacing.xs,
      ),
      child: Align(
        alignment: Alignment.centerLeft,
        child: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(
              Symbols.sync,
              size: 18,
              color: theme.colorScheme.onSurfaceVariant,
            ),
            const SizedBox(width: AppSpacing.xs),
            Text(
              lastSync == null
                  ? 'Nenhuma sincronização ainda'
                  : 'Última sincronização: ${formatDateTime(lastSync)}',
              style: theme.textTheme.bodyMedium?.copyWith(
                color: theme.colorScheme.onSurfaceVariant,
              ),
            ),
          ],
        ),
      ),
    );
  }
}

/// O controle da janela de retenção, no topo da tela.
///
/// Fica aqui, e não numa tela de configuração, porque é sobre **este** histórico
/// — quem lê a lista é quem decide por quanto tempo ela existe.
class RetentionControl extends StatelessWidget {
  /// Creates the control.
  const RetentionControl({super.key, required this.viewModel});

  /// Drives the control.
  final CapturedMessageListViewModel viewModel;

  @override
  Widget build(BuildContext context) {
    final policy = viewModel.retention;
    if (policy == null) return const SizedBox.shrink();

    final theme = Theme.of(context);
    final canManage = context.watch<BillPaymentPermissionNotifier>().hasPermission(
          BillPaymentResources.captureRetention,
          BillPaymentScopes.manage,
        );

    // Sem permissão o prazo continua à vista: quem opera precisa dele para
    // interpretar a lista, mesmo sem poder mudá-lo.
    if (!canManage) {
      return Padding(
        padding: const EdgeInsets.symmetric(horizontal: AppSpacing.md),
        child: Align(
          alignment: Alignment.centerLeft,
          child: Text(
            policy.isEnabled
                ? 'Descartados são guardados por ${policy.windowDays} dias.'
                : 'O histórico de descartados não é purgado.',
            style: theme.textTheme.bodySmall?.copyWith(
              color: theme.colorScheme.onSurfaceVariant,
            ),
          ),
        ),
      );
    }

    final windows = policy.availableWindowDays.isEmpty
        ? const [7, 30, 90, 180]
        : policy.availableWindowDays;

    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: AppSpacing.md),
      child: Card.outlined(
        child: Padding(
          padding: const EdgeInsets.symmetric(
            horizontal: AppSpacing.md,
            vertical: AppSpacing.sm,
          ),
          child: Wrap(
            spacing: AppSpacing.md,
            runSpacing: AppSpacing.xs,
            crossAxisAlignment: WrapCrossAlignment.center,
            children: [
              Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Switch(
                    value: policy.isEnabled,
                    onChanged: viewModel.isMutating
                        ? null
                        : (enabled) => viewModel.configureRetention(
                              isEnabled: enabled,
                              windowDays: policy.windowDays,
                            ),
                  ),
                  const SizedBox(width: AppSpacing.xs),
                  Text(
                    'Guardar histórico de descartados',
                    style: theme.textTheme.bodyMedium,
                  ),
                ],
              ),
              SegmentedButton<int>(
                segments: [
                  for (final days in windows)
                    ButtonSegment(value: days, label: Text('$days d')),
                ],
                selected: {policy.windowDays},
                showSelectedIcon: false,
                onSelectionChanged: viewModel.isMutating || !policy.isEnabled
                    ? null
                    : (selection) => viewModel.configureRetention(
                          isEnabled: policy.isEnabled,
                          windowDays: selection.first,
                        ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

/// A busca e os filtros da lista.
class CapturedMessageFilters extends StatelessWidget {
  /// Creates the filter bar.
  const CapturedMessageFilters({
    super.key,
    required this.viewModel,
    required this.searchController,
  });

  /// Drives the filters.
  final CapturedMessageListViewModel viewModel;

  /// Holds the search term.
  final TextEditingController searchController;

  @override
  Widget build(BuildContext context) {
    final filter = viewModel.filter;

    return Padding(
      padding: const EdgeInsets.fromLTRB(
        AppSpacing.md,
        AppSpacing.sm,
        AppSpacing.md,
        AppSpacing.sm,
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          TextField(
            controller: searchController,
            decoration: InputDecoration(
              hintText: 'Buscar por remetente ou assunto',
              prefixIcon: const Icon(Symbols.search),
              border: const OutlineInputBorder(),
              isDense: true,
              suffixIcon: filter.search == null || filter.search!.isEmpty
                  ? null
                  : IconButton(
                      icon: const Icon(Symbols.close),
                      tooltip: 'Limpar busca',
                      onPressed: () {
                        searchController.clear();
                        viewModel.search(null);
                      },
                    ),
            ),
            textInputAction: TextInputAction.search,
            // No submit, e não a cada tecla: o mesmo padrão da busca de
            // beneficiários, que evita uma requisição por letra digitada.
            onSubmitted: viewModel.search,
          ),
          const SizedBox(height: AppSpacing.sm),
          Wrap(
            spacing: AppSpacing.sm,
            runSpacing: AppSpacing.xs,
            children: [
              FilterChip(
                label: const Text('Todos'),
                selected: filter.outcome == null,
                onSelected: (_) => viewModel.selectOutcome(null),
              ),
              for (final outcome in ArtifactOutcomes.filters)
                FilterChip(
                  label: Text(ArtifactOutcomes.label(outcome)),
                  selected: filter.outcome == outcome,
                  onSelected: (_) => viewModel.selectOutcome(outcome),
                ),
              ActionChip(
                avatar: const Icon(Symbols.date_range, size: 18),
                label: Text(
                  filter.from == null
                      ? 'Período'
                      : '${formatDate(filter.from)} – ${formatDate(filter.to)}',
                ),
                onPressed: () => _pickPeriod(context),
              ),
              if (!filter.isEmpty)
                ActionChip(
                  avatar: const Icon(Symbols.filter_alt_off, size: 18),
                  label: const Text('Limpar filtros'),
                  onPressed: () {
                    searchController.clear();
                    viewModel.clearFilters();
                  },
                ),
            ],
          ),
        ],
      ),
    );
  }

  Future<void> _pickPeriod(BuildContext context) async {
    final now = DateTime.now();

    final picked = await showDateRangePicker(
      context: context,
      firstDate: DateTime(now.year - 3),
      lastDate: now,
      initialDateRange: viewModel.filter.from == null
          ? null
          : DateTimeRange(
              start: viewModel.filter.from!,
              end: viewModel.filter.to ?? now,
            ),
    );

    if (picked != null) {
      await viewModel.selectPeriod(picked.start, picked.end);
    }
  }
}
