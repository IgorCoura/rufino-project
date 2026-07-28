import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../../../core/theme/app_breakpoints.dart';
import '../../../../core/theme/app_spacing.dart';
import '../../../../domain/entities/document_dashboard.dart';
import '../../../../domain/repositories/company_repository.dart';
import '../../../../domain/repositories/document_dashboard_repository.dart';
import '../../../../domain/repositories/document_group_repository.dart';
import '../viewmodel/document_dashboard_viewmodel.dart';

/// Route-level entry point that owns the [DocumentDashboardViewModel]
/// lifecycle.
///
/// go_router re-runs route builders whenever the navigation stack changes,
/// so creating the view model inline in the builder would replace it with a
/// fresh instance on every push/pop — wiping bucket, filters, pagination and
/// scroll position when the user returns from an employee profile. This
/// widget resolves the selected company and creates the view model once; the
/// state object survives route rebuilds and is disposed with the route.
class DocumentDashboardPage extends StatefulWidget {
  const DocumentDashboardPage({super.key});

  @override
  State<DocumentDashboardPage> createState() => _DocumentDashboardPageState();
}

class _DocumentDashboardPageState extends State<DocumentDashboardPage> {
  DocumentDashboardViewModel? _viewModel;

  @override
  void initState() {
    super.initState();
    _createViewModel();
  }

  Future<void> _createViewModel() async {
    final companyResult =
        await context.read<CompanyRepository>().getSelectedCompany();
    if (!mounted) return;
    final company = companyResult.valueOrNull;
    if (company == null) return;
    setState(() {
      _viewModel = DocumentDashboardViewModel(
        dashboardRepository: context.read<DocumentDashboardRepository>(),
        documentGroupRepository: context.read<DocumentGroupRepository>(),
        companyId: company.id,
      );
    });
    await _viewModel!.loadDashboard();
  }

  @override
  void dispose() {
    _viewModel?.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final viewModel = _viewModel;
    if (viewModel == null) {
      return const Scaffold(
        body: Center(child: CircularProgressIndicator()),
      );
    }
    return DocumentDashboardScreen(viewModel: viewModel);
  }
}

/// Company-wide document triage dashboard.
///
/// Shows clickable KPI cards with the per-bucket unit counts (vencidos, a
/// vencer, pendentes, aguardando assinatura, requer validação) above an
/// urgency-ordered list of the selected bucket's units. Tapping a row opens
/// the owning employee's profile on the documents tab.
class DocumentDashboardScreen extends StatefulWidget {
  const DocumentDashboardScreen({super.key, required this.viewModel});

  /// The view model that owns the dashboard state.
  final DocumentDashboardViewModel viewModel;

  @override
  State<DocumentDashboardScreen> createState() =>
      _DocumentDashboardScreenState();
}

class _DocumentDashboardScreenState extends State<DocumentDashboardScreen> {
  /// Owned at screen level so the scroll offset survives rebuilds and
  /// returning from an employee profile.
  final ScrollController _scrollController = ScrollController();
  late final TextEditingController _searchController;

  static const _pageSizeOptions = [20, 50, 100];

  static const _employeeStatusOptions = [
    (id: null, label: 'Todos'),
    (id: 2, label: 'Ativos'),
    (id: 3, label: 'Férias'),
    (id: 4, label: 'Afastados'),
    (id: 5, label: 'Inativos'),
  ];

  @override
  void initState() {
    super.initState();
    _searchController =
        TextEditingController(text: widget.viewModel.employeeNameFilter);
  }

  @override
  void dispose() {
    _scrollController.dispose();
    _searchController.dispose();
    super.dispose();
  }

  void _openEmployeeProfile(DashboardUnitItem unit) {
    context.push('/employee/${unit.employeeId}?tab=documents');
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          tooltip: 'Voltar para o menu',
          onPressed: () => context.go('/home'),
        ),
        title: const Text('Dashboard de Documentos'),
      ),
      body: SafeArea(
        child: ListenableBuilder(
          listenable: widget.viewModel,
          builder: (context, _) {
            final viewModel = widget.viewModel;

            if (viewModel.status == DocumentDashboardStatus.loading ||
                viewModel.status == DocumentDashboardStatus.idle) {
              return const Center(child: CircularProgressIndicator());
            }

            if (viewModel.status == DocumentDashboardStatus.error) {
              return _DashboardErrorState(
                message: viewModel.errorMessage ??
                    'Não foi possível carregar o dashboard.',
                onRetry: viewModel.loadDashboard,
              );
            }

            return LayoutBuilder(
              builder: (context, constraints) {
                final isWide = constraints.maxWidth >= AppBreakpoints.tablet;
                return SingleChildScrollView(
                  controller: _scrollController,
                  padding:
                      EdgeInsets.all(isWide ? AppSpacing.lg : AppSpacing.md),
                  child: Center(
                    child: ConstrainedBox(
                      constraints: const BoxConstraints(maxWidth: 1100),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          _SummaryCards(viewModel: viewModel),
                          const SizedBox(height: AppSpacing.lg),
                          _DashboardFilters(
                            viewModel: viewModel,
                            searchController: _searchController,
                            employeeStatusOptions: _employeeStatusOptions,
                          ),
                          const SizedBox(height: AppSpacing.md),
                          if (viewModel.isLoadingUnits)
                            const LinearProgressIndicator()
                          else
                            _UnitList(
                              viewModel: viewModel,
                              onOpenProfile: _openEmployeeProfile,
                            ),
                          const SizedBox(height: AppSpacing.md),
                          _PaginationBar(
                            viewModel: viewModel,
                            pageSizeOptions: _pageSizeOptions,
                          ),
                        ],
                      ),
                    ),
                  ),
                );
              },
            );
          },
        ),
      ),
    );
  }
}

/// Visual identity of a dashboard bucket (icon + accent color).
({IconData icon, Color color}) _bucketStyle(
    BuildContext context, DashboardBucket bucket) {
  final colorScheme = Theme.of(context).colorScheme;
  return switch (bucket) {
    DashboardBucket.expired =>
      (icon: Icons.error_outline, color: colorScheme.error),
    DashboardBucket.expiring =>
      (icon: Icons.schedule, color: Colors.deepOrange),
    DashboardBucket.pending =>
      (icon: Icons.hourglass_empty, color: Colors.orange),
    DashboardBucket.awaitingSignature =>
      (icon: Icons.draw_outlined, color: Colors.blue),
    DashboardBucket.requiresValidation =>
      (icon: Icons.fact_check_outlined, color: Colors.amber),
  };
}

/// Row of clickable KPI cards — one per bucket, the selected one filters the
/// unit list below.
class _SummaryCards extends StatelessWidget {
  const _SummaryCards({required this.viewModel});

  final DocumentDashboardViewModel viewModel;

  @override
  Widget build(BuildContext context) {
    return Wrap(
      spacing: AppSpacing.md,
      runSpacing: AppSpacing.md,
      children: [
        for (final bucket in DashboardBucket.values)
          _SummaryCard(
            key: ValueKey('bucket-card-${bucket.name}'),
            bucket: bucket,
            count: viewModel.summary.countFor(bucket),
            isSelected: viewModel.selectedBucket == bucket,
            onTap: () => viewModel.selectBucket(bucket),
          ),
      ],
    );
  }
}

class _SummaryCard extends StatelessWidget {
  const _SummaryCard({
    super.key,
    required this.bucket,
    required this.count,
    required this.isSelected,
    required this.onTap,
  });

  final DashboardBucket bucket;
  final int count;
  final bool isSelected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final style = _bucketStyle(context, bucket);
    return Card(
      clipBehavior: Clip.antiAlias,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
        side: isSelected
            ? BorderSide(color: style.color, width: 2)
            : BorderSide.none,
      ),
      child: InkWell(
        onTap: onTap,
        child: SizedBox(
          width: 196,
          child: Padding(
            padding: const EdgeInsets.all(AppSpacing.md),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Icon(style.icon, color: style.color, size: 20),
                    const SizedBox(width: AppSpacing.xs),
                    Expanded(
                      child: Text(
                        bucket.label,
                        style: theme.textTheme.labelLarge,
                        overflow: TextOverflow.ellipsis,
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: AppSpacing.sm),
                Text(
                  '$count',
                  style: theme.textTheme.headlineMedium?.copyWith(
                    color: style.color,
                    fontWeight: FontWeight.w600,
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

/// Filter bar: horizon selector, group/template dropdowns, employee status,
/// name search and the grouping toggle.
class _DashboardFilters extends StatelessWidget {
  const _DashboardFilters({
    required this.viewModel,
    required this.searchController,
    required this.employeeStatusOptions,
  });

  final DocumentDashboardViewModel viewModel;
  final TextEditingController searchController;
  final List<({int? id, String label})> employeeStatusOptions;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Wrap(
          spacing: AppSpacing.md,
          runSpacing: AppSpacing.sm,
          crossAxisAlignment: WrapCrossAlignment.center,
          children: [
            SegmentedButton<int>(
              key: const ValueKey('horizon-selector'),
              segments: [
                for (final days
                    in DocumentDashboardViewModel.horizonOptions)
                  ButtonSegment(value: days, label: Text('$days dias')),
              ],
              selected: {viewModel.expiringInDays},
              onSelectionChanged: (selection) =>
                  viewModel.setHorizon(selection.first),
            ),
            SegmentedButton<DashboardGrouping>(
              key: const ValueKey('grouping-toggle'),
              segments: const [
                ButtonSegment(
                  value: DashboardGrouping.byDocument,
                  icon: Icon(Icons.description_outlined),
                  label: Text('Por documento'),
                ),
                ButtonSegment(
                  value: DashboardGrouping.byEmployee,
                  icon: Icon(Icons.people_outline),
                  label: Text('Por funcionário'),
                ),
              ],
              selected: {viewModel.grouping},
              onSelectionChanged: (selection) =>
                  viewModel.setGrouping(selection.first),
            ),
          ],
        ),
        const SizedBox(height: AppSpacing.sm),
        Wrap(
          spacing: AppSpacing.md,
          runSpacing: AppSpacing.sm,
          crossAxisAlignment: WrapCrossAlignment.center,
          children: [
            SizedBox(
              width: 220,
              child: DropdownButtonFormField<String?>(
                key: const ValueKey('group-filter'),
                initialValue: viewModel.selectedGroupId,
                decoration: const InputDecoration(
                  labelText: 'Grupo',
                  border: OutlineInputBorder(),
                  isDense: true,
                ),
                items: [
                  const DropdownMenuItem<String?>(
                    value: null,
                    child: Text('Todos'),
                  ),
                  for (final group in viewModel.groups)
                    DropdownMenuItem<String?>(
                      value: group.id,
                      child:
                          Text(group.name, overflow: TextOverflow.ellipsis),
                    ),
                ],
                onChanged: (value) => viewModel.selectGroup(value),
              ),
            ),
            SizedBox(
              width: 220,
              child: DropdownButtonFormField<String?>(
                key: ValueKey(
                    'template-filter-${viewModel.selectedGroupId ?? 'all'}'),
                initialValue: viewModel.selectedTemplateId,
                decoration: const InputDecoration(
                  labelText: 'Template',
                  border: OutlineInputBorder(),
                  isDense: true,
                ),
                items: [
                  const DropdownMenuItem<String?>(
                    value: null,
                    child: Text('Todos'),
                  ),
                  for (final template in viewModel.templates)
                    DropdownMenuItem<String?>(
                      value: template.id,
                      child: Text(template.name,
                          overflow: TextOverflow.ellipsis),
                    ),
                ],
                onChanged: viewModel.selectedGroupId == null
                    ? null
                    : (value) => viewModel.selectTemplate(value),
              ),
            ),
            SizedBox(
              width: 180,
              child: DropdownButtonFormField<int?>(
                key: const ValueKey('employee-status-filter'),
                initialValue: viewModel.employeeStatusFilter,
                decoration: const InputDecoration(
                  labelText: 'Funcionários',
                  border: OutlineInputBorder(),
                  isDense: true,
                ),
                items: [
                  for (final option in employeeStatusOptions)
                    DropdownMenuItem<int?>(
                      value: option.id,
                      child: Text(option.label),
                    ),
                ],
                onChanged: (value) =>
                    viewModel.setEmployeeStatusFilter(value),
              ),
            ),
            SizedBox(
              width: 260,
              child: TextField(
                key: const ValueKey('employee-search'),
                controller: searchController,
                decoration: InputDecoration(
                  labelText: 'Buscar por nome',
                  border: const OutlineInputBorder(),
                  isDense: true,
                  prefixIcon: const Icon(Icons.search),
                  suffixIcon: IconButton(
                    icon: const Icon(Icons.clear),
                    tooltip: 'Limpar busca',
                    onPressed: () {
                      searchController.clear();
                      viewModel.setEmployeeNameFilter(null);
                    },
                  ),
                ),
                onSubmitted: viewModel.setEmployeeNameFilter,
              ),
            ),
          ],
        ),
      ],
    );
  }
}

/// The unit list of the selected bucket, flat or grouped by employee.
class _UnitList extends StatelessWidget {
  const _UnitList({required this.viewModel, required this.onOpenProfile});

  final DocumentDashboardViewModel viewModel;
  final void Function(DashboardUnitItem unit) onOpenProfile;

  @override
  Widget build(BuildContext context) {
    if (viewModel.units.isEmpty) {
      return const _DashboardEmptyState();
    }

    if (viewModel.grouping == DashboardGrouping.byEmployee) {
      final grouped = viewModel.unitsByEmployee;
      return Column(
        children: [
          for (final entry in grouped.entries)
            Card(
              key: ValueKey('employee-group-${entry.key}'),
              clipBehavior: Clip.antiAlias,
              child: ExpansionTile(
                initiallyExpanded: grouped.length <= 3,
                leading: const Icon(Icons.person_outline),
                title: Text(entry.value.first.employeeName),
                subtitle:
                    Text(entry.value.first.employeeStatusLabel),
                trailing: Badge.count(count: entry.value.length),
                children: [
                  for (final unit in entry.value)
                    _UnitRow(
                      unit: unit,
                      referenceDate: viewModel.referenceDate,
                      showEmployee: false,
                      onTap: () => onOpenProfile(unit),
                    ),
                ],
              ),
            ),
        ],
      );
    }

    return Column(
      children: [
        for (final unit in viewModel.units)
          Card(
            key: ValueKey('unit-card-${unit.documentUnitId}'),
            clipBehavior: Clip.antiAlias,
            child: _UnitRow(
              unit: unit,
              referenceDate: viewModel.referenceDate,
              showEmployee: true,
              onTap: () => onOpenProfile(unit),
            ),
          ),
      ],
    );
  }
}

/// A single document unit row with status, dates, competência and urgency.
class _UnitRow extends StatelessWidget {
  const _UnitRow({
    required this.unit,
    required this.referenceDate,
    required this.showEmployee,
    required this.onTap,
  });

  final DashboardUnitItem unit;
  final DateTime referenceDate;
  final bool showEmployee;
  final VoidCallback onTap;

  Color _statusColor(BuildContext context) {
    return switch (unit.statusId) {
      '1' => Colors.orange,
      '2' => Colors.green,
      '3' => Colors.grey,
      '4' => Colors.red,
      '5' => Colors.amber,
      '6' => Colors.blueGrey,
      '7' => Colors.blue,
      '8' => Colors.deepOrange,
      _ => Colors.grey,
    };
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final urgency = unit.urgencyLabel(referenceDate);
    final days = unit.daysUntilValidity(referenceDate);
    final urgencyColor = days != null && days < 0
        ? theme.colorScheme.error
        : Colors.deepOrange;

    final details = <String>[
      unit.documentGroupName,
      if (unit.period != null) 'Competência: ${unit.period!.formattedPeriod}',
      if (unit.date.isNotEmpty) 'Data: ${unit.date}',
      if (unit.hasValidity) 'Validade: ${unit.validity}',
    ];

    return ListTile(
      onTap: onTap,
      leading: Icon(Icons.circle, size: 12, color: _statusColor(context)),
      title: Text(
        showEmployee
            ? '${unit.employeeName} — ${unit.documentTemplateName}'
            : unit.documentTemplateName,
        maxLines: 1,
        overflow: TextOverflow.ellipsis,
      ),
      subtitle: Text(
        '${unit.statusLabel} · ${details.join(' · ')}',
        maxLines: 2,
        overflow: TextOverflow.ellipsis,
      ),
      trailing: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          if (urgency != null)
            Container(
              padding: const EdgeInsets.symmetric(
                horizontal: AppSpacing.sm,
                vertical: AppSpacing.xs,
              ),
              decoration: BoxDecoration(
                color: urgencyColor.withValues(alpha: 0.12),
                borderRadius: BorderRadius.circular(8),
              ),
              child: Text(
                urgency,
                style: theme.textTheme.labelMedium
                    ?.copyWith(color: urgencyColor),
              ),
            ),
          const SizedBox(width: AppSpacing.xs),
          const Icon(Icons.chevron_right),
        ],
      ),
    );
  }
}

/// Pagination controls: page size, previous/next and position label.
class _PaginationBar extends StatelessWidget {
  const _PaginationBar({
    required this.viewModel,
    required this.pageSizeOptions,
  });

  final DocumentDashboardViewModel viewModel;
  final List<int> pageSizeOptions;

  @override
  Widget build(BuildContext context) {
    if (viewModel.totalCount == 0) return const SizedBox.shrink();
    return Wrap(
      spacing: AppSpacing.md,
      runSpacing: AppSpacing.sm,
      crossAxisAlignment: WrapCrossAlignment.center,
      children: [
        Text('${viewModel.totalCount} documentos'),
        DropdownButton<int>(
          key: const ValueKey('page-size-selector'),
          value: viewModel.pageSize,
          items: [
            for (final size in pageSizeOptions)
              DropdownMenuItem(value: size, child: Text('$size por página')),
          ],
          onChanged: (value) {
            if (value != null) viewModel.setPageSize(value);
          },
        ),
        IconButton(
          key: const ValueKey('previous-page'),
          icon: const Icon(Icons.chevron_left),
          tooltip: 'Página anterior',
          onPressed: viewModel.pageNumber > 1
              ? () => viewModel.setPage(viewModel.pageNumber - 1)
              : null,
        ),
        Text('Página ${viewModel.pageNumber} de ${viewModel.pageCount}'),
        IconButton(
          key: const ValueKey('next-page'),
          icon: const Icon(Icons.chevron_right),
          tooltip: 'Próxima página',
          onPressed: viewModel.pageNumber < viewModel.pageCount
              ? () => viewModel.setPage(viewModel.pageNumber + 1)
              : null,
        ),
      ],
    );
  }
}

class _DashboardEmptyState extends StatelessWidget {
  const _DashboardEmptyState();

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: AppSpacing.xxl),
      child: Center(
        child: Column(
          children: [
            Icon(
              Icons.task_alt,
              size: 48,
              color: Theme.of(context).colorScheme.primary,
            ),
            const SizedBox(height: AppSpacing.sm),
            const Text('Nenhum documento neste filtro.'),
          ],
        ),
      ),
    );
  }
}

class _DashboardErrorState extends StatelessWidget {
  const _DashboardErrorState({required this.message, required this.onRetry});

  final String message;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(AppSpacing.lg),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(
              Icons.error_outline,
              size: 48,
              color: Theme.of(context).colorScheme.error,
            ),
            const SizedBox(height: AppSpacing.sm),
            Text(message, textAlign: TextAlign.center),
            const SizedBox(height: AppSpacing.md),
            FilledButton.tonal(
              onPressed: onRetry,
              child: const Text('Tentar novamente'),
            ),
          ],
        ),
      ),
    );
  }
}
