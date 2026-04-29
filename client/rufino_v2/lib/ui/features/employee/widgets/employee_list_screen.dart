import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/theme/app_breakpoints.dart';
import '../../../../core/theme/app_spacing.dart';
import '../../../../domain/entities/employee.dart';
import '../../../core/widgets/filter_sheet.dart';
import '../../../core/widgets/permission_guard.dart';
import '../viewmodel/employee_list_viewmodel.dart';

/// Displays a paginated, searchable list of employees for the selected company.
///
/// Supports search by name or role, status/document-status filtering, sort
/// direction toggle, and async profile photo loading.
/// Uses [ListenableBuilder] to react to [EmployeeListViewModel] state changes.
/// Pagination is triggered automatically as the user scrolls near the bottom.
class EmployeeListScreen extends StatefulWidget {
  const EmployeeListScreen({super.key, required this.viewModel});

  final EmployeeListViewModel viewModel;

  @override
  State<EmployeeListScreen> createState() => _EmployeeListScreenState();
}

class _EmployeeListScreenState extends State<EmployeeListScreen> {
  final _searchController = TextEditingController();
  final _scrollController = ScrollController();

  @override
  void initState() {
    super.initState();
    _searchController.text = widget.viewModel.searchQuery;
    widget.viewModel.refresh();
    _scrollController.addListener(_onScroll);
  }

  @override
  void didUpdateWidget(EmployeeListScreen oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.viewModel != widget.viewModel) {
      _searchController.text = widget.viewModel.searchQuery;
      widget.viewModel.refresh();
    }
  }

  @override
  void dispose() {
    _searchController.dispose();
    _scrollController.dispose();
    super.dispose();
  }

  /// Requests the next page when the scroll position is close to the bottom.
  void _onScroll() {
    if (!_scrollController.hasClients) return;

    if (_scrollController.position.pixels >=
        _scrollController.position.maxScrollExtent - 240) {
      widget.viewModel.loadNextPage();
    }
  }

  @override
  Widget build(BuildContext context) {
    final width = MediaQuery.sizeOf(context).width;
    final horizontalPadding = width >= AppBreakpoints.tablet
        ? AppSpacing.xl
        : width >= AppBreakpoints.mobile
            ? AppSpacing.lg
            : AppSpacing.md;

    return Scaffold(
      appBar: AppBar(
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          tooltip: 'Voltar para início',
          onPressed: () => context.go('/home'),
        ),
        title: const Text('Funcionários'),
        actions: [
          PermissionGuard(
            resource: 'employee',
            scope: 'download',
            child: _ExportMenuButton(viewModel: widget.viewModel),
          ),
        ],
      ),
      body: SafeArea(
        child: Center(
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 960),
            child: Padding(
              padding: EdgeInsets.symmetric(horizontal: horizontalPadding),
              child: ListenableBuilder(
                listenable: widget.viewModel,
                builder: (context, _) => Column(
                  children: [
                    const SizedBox(height: AppSpacing.md),
                    _SearchSection(
                      controller: _searchController,
                      viewModel: widget.viewModel,
                    ),
                    const SizedBox(height: AppSpacing.md),
                    Expanded(
                      child: _EmployeeContent(
                        viewModel: widget.viewModel,
                        scrollController: _scrollController,
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ),
        ),
      ),
      floatingActionButton: PermissionGuard(
        resource: 'employee',
        scope: 'create',
        child: FloatingActionButton(
          onPressed: () => context
              .push('/employee/create')
              .then((_) => widget.viewModel.refresh()),
          tooltip: 'Adicionar funcionário',
          child: const Icon(Icons.person_add_alt_1),
        ),
      ),
    );
  }
}

/// Groups search and filter controls using Material 3 surfaces.
class _SearchSection extends StatelessWidget {
  const _SearchSection({
    required this.controller,
    required this.viewModel,
  });

  final TextEditingController controller;
  final EmployeeListViewModel viewModel;

  /// Number of filters that differ from the screen defaults.
  int get _activeFilterCount {
    var count = 0;
    if (viewModel.searchParam != SearchParam.name) count++;
    if (viewModel.selectedStatus != EmployeeStatus.none) count++;
    if (viewModel.selectedDocumentStatus != DocumentStatus.all) count++;
    if (!viewModel.ascending) count++;
    return count;
  }

  Future<void> _openFiltersSheet(BuildContext context) async {
    SearchParam draftSearchParam = viewModel.searchParam;
    EmployeeStatus draftStatus = viewModel.selectedStatus;
    DocumentStatus draftDocumentStatus = viewModel.selectedDocumentStatus;
    bool draftAscending = viewModel.ascending;

    await showFilterSheet<void>(
      context: context,
      title: 'Filtros',
      builder: (sheetContext, setSheetState) {
        return Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            DropdownButtonFormField<SearchParam>(
              isExpanded: true,
              initialValue: draftSearchParam,
              decoration: const InputDecoration(
                labelText: 'Buscar por',
                border: OutlineInputBorder(),
              ),
              items: SearchParam.values
                  .map(
                    (p) => DropdownMenuItem<SearchParam>(
                      value: p,
                      child: Text(p.label, overflow: TextOverflow.ellipsis),
                    ),
                  )
                  .toList(),
              onChanged: (value) {
                if (value != null) {
                  setSheetState(() => draftSearchParam = value);
                }
              },
            ),
            const SizedBox(height: AppSpacing.md),
            DropdownButtonFormField<EmployeeStatus>(
              isExpanded: true,
              initialValue: draftStatus,
              decoration: const InputDecoration(
                labelText: 'Status',
                border: OutlineInputBorder(),
              ),
              items: EmployeeStatus.values
                  .map(
                    (s) => DropdownMenuItem<EmployeeStatus>(
                      value: s,
                      child: Text(s.label, overflow: TextOverflow.ellipsis),
                    ),
                  )
                  .toList(),
              onChanged: (value) {
                if (value != null) {
                  setSheetState(() => draftStatus = value);
                }
              },
            ),
            const SizedBox(height: AppSpacing.md),
            DropdownButtonFormField<DocumentStatus>(
              isExpanded: true,
              initialValue: draftDocumentStatus,
              decoration: const InputDecoration(
                labelText: 'Documentos',
                border: OutlineInputBorder(),
              ),
              items: DocumentStatus.values
                  .map(
                    (s) => DropdownMenuItem<DocumentStatus>(
                      value: s,
                      child: Text(s.label, overflow: TextOverflow.ellipsis),
                    ),
                  )
                  .toList(),
              onChanged: (value) {
                if (value != null) {
                  setSheetState(() => draftDocumentStatus = value);
                }
              },
            ),
            const SizedBox(height: AppSpacing.md),
            SwitchListTile(
              contentPadding: EdgeInsets.zero,
              title: const Text('Ordem A-Z'),
              subtitle: Text(
                draftAscending ? 'A-Z (crescente)' : 'Z-A (decrescente)',
              ),
              value: draftAscending,
              onChanged: (value) =>
                  setSheetState(() => draftAscending = value),
            ),
          ],
        );
      },
      onApply: () {
        if (draftSearchParam != viewModel.searchParam) {
          viewModel.onSearchParamChanged(draftSearchParam);
        }
        if (draftStatus != viewModel.selectedStatus) {
          viewModel.onStatusChanged(draftStatus);
        }
        if (draftDocumentStatus != viewModel.selectedDocumentStatus) {
          viewModel.onDocumentStatusChanged(draftDocumentStatus);
        }
        if (draftAscending != viewModel.ascending) {
          viewModel.toggleSort();
        }
      },
      onClear: () {
        draftSearchParam = SearchParam.name;
        draftStatus = EmployeeStatus.none;
        draftDocumentStatus = DocumentStatus.all;
        draftAscending = true;
      },
    );
  }

  @override
  Widget build(BuildContext context) {
    return Card.outlined(
      child: Padding(
        padding: const EdgeInsets.all(AppSpacing.md),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'Busca e filtros',
              style: Theme.of(context).textTheme.titleSmall,
            ),
            const SizedBox(height: AppSpacing.sm),
            LayoutBuilder(
              builder: (context, constraints) {
                final isWide = constraints.maxWidth >= AppBreakpoints.mobile;

                if (isWide) {
                  return Column(
                    children: [
                      Row(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Expanded(
                            flex: 3,
                            child: _SearchField(
                              controller: controller,
                              viewModel: viewModel,
                            ),
                          ),
                          const SizedBox(width: AppSpacing.md),
                          Expanded(
                            child: _SearchParamField(viewModel: viewModel),
                          ),
                        ],
                      ),
                      const SizedBox(height: AppSpacing.md),
                      _FilterSection(viewModel: viewModel),
                    ],
                  );
                }

                final count = _activeFilterCount;
                final filterLabel =
                    count > 0 ? 'Filtros ($count)' : 'Filtros';
                return Column(
                  children: [
                    _SearchField(
                      controller: controller,
                      viewModel: viewModel,
                    ),
                    const SizedBox(height: AppSpacing.md),
                    Align(
                      alignment: Alignment.centerRight,
                      child: OutlinedButton.icon(
                        icon: const Icon(Icons.tune, size: 18),
                        label: Text(filterLabel),
                        onPressed: () => _openFiltersSheet(context),
                      ),
                    ),
                  ],
                );
              },
            ),
          ],
        ),
      ),
    );
  }
}

/// Renders the search field with submit and clear affordances.
class _SearchField extends StatelessWidget {
  const _SearchField({
    required this.controller,
    required this.viewModel,
  });

  final TextEditingController controller;
  final EmployeeListViewModel viewModel;

  @override
  Widget build(BuildContext context) {
    return ListenableBuilder(
      listenable: controller,
      builder: (context, _) {
        return TextField(
          controller: controller,
          textInputAction: TextInputAction.search,
          decoration: InputDecoration(
            labelText: 'Buscar',
            hintText: 'Nome ou função',
            border: const OutlineInputBorder(),
            prefixIcon: const Icon(Icons.search),
            suffixIcon: controller.text.isEmpty
                ? null
                : IconButton(
                    tooltip: 'Limpar busca',
                    onPressed: () {
                      controller.clear();
                      viewModel.onSearchQueryChanged('');
                      viewModel.onSearchSubmitted();
                    },
                    icon: const Icon(Icons.close),
                  ),
          ),
          onChanged: viewModel.onSearchQueryChanged,
          onSubmitted: (_) => viewModel.onSearchSubmitted(),
        );
      },
    );
  }
}

/// Chooses whether the search is applied to employee name or role.
class _SearchParamField extends StatelessWidget {
  const _SearchParamField({required this.viewModel});

  final EmployeeListViewModel viewModel;

  @override
  Widget build(BuildContext context) {
    return DropdownButtonFormField<SearchParam>(
      initialValue: viewModel.searchParam,
      decoration: const InputDecoration(
        labelText: 'Buscar por',
        border: OutlineInputBorder(),
      ),
      items: SearchParam.values
          .map(
            (param) => DropdownMenuItem<SearchParam>(
              value: param,
              child: Text(param.label),
            ),
          )
          .toList(),
      onChanged: (param) {
        if (param != null) {
          viewModel.onSearchParamChanged(param);
        }
      },
    );
  }
}

/// Shows list filters in a responsive Material 3 layout.
class _FilterSection extends StatelessWidget {
  const _FilterSection({required this.viewModel});

  final EmployeeListViewModel viewModel;

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        final isWide = constraints.maxWidth >= AppBreakpoints.mobile;

        if (isWide) {
          return Row(
            children: [
              Expanded(
                child: _StatusDropdown<EmployeeStatus>(
                  label: 'Status',
                  value: viewModel.selectedStatus,
                  items: EmployeeStatus.values,
                  labelOf: (status) => status.label,
                  onChanged: (status) {
                    if (status != null) {
                      viewModel.onStatusChanged(status);
                    }
                  },
                ),
              ),
              const SizedBox(width: AppSpacing.md),
              Expanded(
                child: _StatusDropdown<DocumentStatus>(
                  label: 'Documentos',
                  value: viewModel.selectedDocumentStatus,
                  items: DocumentStatus.values,
                  labelOf: (status) => status.label,
                  onChanged: (status) {
                    if (status != null) {
                      viewModel.onDocumentStatusChanged(status);
                    }
                  },
                ),
              ),
              const SizedBox(width: AppSpacing.md),
              _SortButton(
                ascending: viewModel.ascending,
                onPressed: viewModel.toggleSort,
              ),
            ],
          );
        }

        return Column(
          children: [
            _StatusDropdown<EmployeeStatus>(
              label: 'Status',
              value: viewModel.selectedStatus,
              items: EmployeeStatus.values,
              labelOf: (status) => status.label,
              onChanged: (status) {
                if (status != null) {
                  viewModel.onStatusChanged(status);
                }
              },
            ),
            const SizedBox(height: AppSpacing.md),
            _StatusDropdown<DocumentStatus>(
              label: 'Documentos',
              value: viewModel.selectedDocumentStatus,
              items: DocumentStatus.values,
              labelOf: (status) => status.label,
              onChanged: (status) {
                if (status != null) {
                  viewModel.onDocumentStatusChanged(status);
                }
              },
            ),
            const SizedBox(height: AppSpacing.sm),
            Align(
              alignment: Alignment.centerRight,
              child: _SortButton(
                ascending: viewModel.ascending,
                onPressed: viewModel.toggleSort,
              ),
            ),
          ],
        );
      },
    );
  }
}

/// Reuses an outlined dropdown style for status-based filters.
class _StatusDropdown<T> extends StatelessWidget {
  const _StatusDropdown({
    required this.label,
    required this.value,
    required this.items,
    required this.labelOf,
    required this.onChanged,
  });

  final String label;
  final T value;
  final List<T> items;
  final String Function(T item) labelOf;
  final ValueChanged<T?> onChanged;

  @override
  Widget build(BuildContext context) {
    return DropdownButtonFormField<T>(
      initialValue: value,
      decoration: InputDecoration(
        labelText: label,
        border: const OutlineInputBorder(),
      ),
      items: items
          .map(
            (item) => DropdownMenuItem<T>(
              value: item,
              child: Text(
                labelOf(item),
                overflow: TextOverflow.ellipsis,
              ),
            ),
          )
          .toList(),
      onChanged: onChanged,
    );
  }
}

/// Toggles the list sort direction with a Material 3 tonal button.
class _SortButton extends StatelessWidget {
  const _SortButton({
    required this.ascending,
    required this.onPressed,
  });

  final bool ascending;
  final VoidCallback onPressed;

  @override
  Widget build(BuildContext context) {
    return FilledButton.tonalIcon(
      onPressed: onPressed,
      icon: Icon(
        ascending ? Icons.arrow_upward : Icons.arrow_downward,
      ),
      label: Text(ascending ? 'A-Z' : 'Z-A'),
    );
  }
}

/// Renders the loading, error, empty, list, and pagination states.
class _EmployeeContent extends StatelessWidget {
  const _EmployeeContent({
    required this.viewModel,
    required this.scrollController,
  });

  final EmployeeListViewModel viewModel;
  final ScrollController scrollController;

  @override
  Widget build(BuildContext context) {
    if (viewModel.isLoading) {
      return const Center(child: CircularProgressIndicator());
    }

    if (viewModel.hasError && viewModel.employees.isEmpty) {
      return _EmptyStateCard(
        icon: Icons.error_outline,
        title: 'Não foi possível carregar a lista.',
        message: viewModel.errorMessage ?? 'Erro ao carregar funcionários.',
        actionLabel: 'Tentar novamente',
        onAction: viewModel.refresh,
      );
    }

    if (viewModel.employees.isEmpty) {
      return const _EmptyStateCard(
        icon: Icons.badge_outlined,
        title: 'Nenhum funcionário encontrado.',
        message: 'Ajuste os filtros ou cadastre um novo funcionário.',
      );
    }

    final itemCount =
        viewModel.employees.length + (viewModel.isLoadingMore ? 1 : 0);

    return LayoutBuilder(
      builder: (context, constraints) {
        final showLegend = constraints.maxWidth >= AppBreakpoints.mobile;

        return Column(
          children: [
            if (showLegend) ...[
              const _EmployeeListLegend(),
              const Divider(height: 1),
            ],
            Expanded(
              child: RefreshIndicator(
                onRefresh: viewModel.refresh,
                child: ListView.separated(
                  controller: scrollController,
                  padding: const EdgeInsets.fromLTRB(
                    0,
                    AppSpacing.sm,
                    0,
                    AppSpacing.md + 80,
                  ),
                  itemCount: itemCount,
                  separatorBuilder: (_, __) => const Divider(height: 1),
                  itemBuilder: (context, index) {
                    if (index >= viewModel.employees.length) {
                      return const _LoadingMoreListItem();
                    }

                    final employee = viewModel.employees[index];
                    return _EmployeeListItem(
                      employee: employee,
                      imageBytes: viewModel.imageFor(employee.id),
                      onTap: () => context.push('/employee/${employee.id}'),
                    );
                  },
                ),
              ),
            ),
          ],
        );
      },
    );
  }
}

/// Labels the fixed-width columns used by the wide employee list rows.
class _EmployeeListLegend extends StatelessWidget {
  const _EmployeeListLegend();

  @override
  Widget build(BuildContext context) {
    final labelStyle = Theme.of(context).textTheme.labelMedium?.copyWith(
          color: Theme.of(context).colorScheme.onSurfaceVariant,
        );

    return DefaultTextStyle.merge(
      style: labelStyle,
      child: const Padding(
        padding: EdgeInsets.fromLTRB(AppSpacing.md, 0, AppSpacing.lg, 0),
        child: Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            Padding(
              padding: EdgeInsets.fromLTRB(54, 0, 0, 0),
              child: Text(
                'Nomes',
                textAlign: TextAlign.center,
              ),
            ),
            Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                SizedBox(
                  width: 90,
                  child: Text(
                    'Documentos',
                    textAlign: TextAlign.center,
                  ),
                ),
                SizedBox(width: AppSpacing.xs),
                SizedBox(
                  width: 80,
                  child: Text(
                    'Status',
                    textAlign: TextAlign.center,
                  ),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

/// Displays a single employee row inspired by the legacy employees list item.
class _EmployeeListItem extends StatelessWidget {
  const _EmployeeListItem({
    required this.employee,
    required this.imageBytes,
    required this.onTap,
  });

  final Employee employee;
  final Uint8List? imageBytes;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final hasImage = imageBytes != null;

    return LayoutBuilder(
      builder: (context, constraints) {
        final isWide = constraints.maxWidth >= AppBreakpoints.mobile;

        if (isWide) {
          return ListTile(
            contentPadding: const EdgeInsets.fromLTRB(
              AppSpacing.md,
              AppSpacing.sm,
              AppSpacing.lg,
              AppSpacing.sm,
            ),
            minVerticalPadding: AppSpacing.sm,
            leading: CircleAvatar(
              backgroundImage: hasImage ? MemoryImage(imageBytes!) : null,
              child: hasImage
                  ? null
                  : Text(
                      employee.name.isNotEmpty
                          ? employee.name[0].toUpperCase()
                          : '?',
                    ),
            ),
            title: Text(
              employee.name,
              overflow: TextOverflow.ellipsis,
            ),
            subtitle: Text(
              employee.roleName.isNotEmpty
                  ? 'Função: ${employee.roleName}'
                  : 'Sem função atribuída',
              overflow: TextOverflow.ellipsis,
            ),
            trailing: Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                SizedBox(
                  width: 90,
                  child: Text(
                    employee.documentStatus.label,
                    textAlign: TextAlign.center,
                    style: Theme.of(context).textTheme.bodySmall,
                  ),
                ),
                const SizedBox(width: AppSpacing.xs),
                SizedBox(
                  width: 80,
                  child: Text(
                    employee.status.label,
                    textAlign: TextAlign.center,
                    style: Theme.of(context).textTheme.bodySmall,
                  ),
                ),
              ],
            ),
            onTap: onTap,
          );
        }

        return ListTile(
          contentPadding: const EdgeInsets.symmetric(
            horizontal: AppSpacing.md,
            vertical: AppSpacing.sm,
          ),
          minVerticalPadding: AppSpacing.sm,
          leading: CircleAvatar(
            backgroundImage: hasImage ? MemoryImage(imageBytes!) : null,
            child: hasImage
                ? null
                : Text(
                    employee.name.isNotEmpty
                        ? employee.name[0].toUpperCase()
                        : '?',
                  ),
          ),
          title: Text(
            employee.name,
            overflow: TextOverflow.ellipsis,
          ),
          subtitle: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            mainAxisSize: MainAxisSize.min,
            children: [
              Text(
                employee.roleName.isNotEmpty
                    ? 'Função: ${employee.roleName}'
                    : 'Sem função atribuída',
                overflow: TextOverflow.ellipsis,
              ),
              const SizedBox(height: AppSpacing.xs),
              Text(
                'Documentos: ${employee.documentStatus.label}',
                style: Theme.of(context).textTheme.bodySmall,
              ),
              Text(
                'Status: ${employee.status.label}',
                style: Theme.of(context).textTheme.bodySmall,
              ),
            ],
          ),
          onTap: onTap,
        );
      },
    );
  }
}

/// Shows a compact loading row while more employees are being fetched.
class _LoadingMoreListItem extends StatelessWidget {
  const _LoadingMoreListItem();

  @override
  Widget build(BuildContext context) {
    return const Padding(
      padding: EdgeInsets.symmetric(vertical: AppSpacing.lg),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          SizedBox(
            width: 20,
            height: 20,
            child: CircularProgressIndicator(strokeWidth: 2),
          ),
          SizedBox(width: AppSpacing.md),
          Text('Carregando mais funcionários...'),
        ],
      ),
    );
  }
}

/// Identifies which spreadsheet to build when an export menu item is tapped.
enum EmployeeExportType {
  /// The labels printing spreadsheet (5 columns).
  etiquetas,

  /// The full SOC import spreadsheet (38 columns).
  soc,
}

/// Three-dot AppBar menu that triggers spreadsheet exports.
///
/// While an export is running the items are disabled to prevent overlapping
/// downloads. The progress dialog is displayed and dismissed by this widget;
/// success and error feedback flow through a snack bar after the export
/// finishes.
class _ExportMenuButton extends StatelessWidget {
  const _ExportMenuButton({required this.viewModel});

  final EmployeeListViewModel viewModel;

  Future<void> _handleSelection(
      BuildContext context, EmployeeExportType type) async {
    if (viewModel.isExporting) return;

    final messenger = ScaffoldMessenger.of(context);
    final navigator = Navigator.of(context);

    showDialog<void>(
      context: context,
      barrierDismissible: false,
      builder: (_) => _ExportProgressDialog(viewModel: viewModel),
    );

    switch (type) {
      case EmployeeExportType.etiquetas:
        await viewModel.exportEtiquetas();
      case EmployeeExportType.soc:
        await viewModel.exportSoc();
    }

    if (navigator.canPop()) navigator.pop();

    final error = viewModel.exportErrorMessage;
    final success = viewModel.exportSuccessLabel;
    if (error != null) {
      messenger.showSnackBar(SnackBar(content: Text(error)));
    } else if (success != null) {
      messenger.showSnackBar(SnackBar(content: Text(success)));
    }
    viewModel.clearExportFeedback();
  }

  @override
  Widget build(BuildContext context) {
    return ListenableBuilder(
      listenable: viewModel,
      builder: (context, _) {
        final disabled = viewModel.isExporting;
        return PopupMenuButton<EmployeeExportType>(
          icon: const Icon(Icons.more_vert),
          tooltip: 'Mais opções',
          enabled: !disabled,
          onSelected: (type) => _handleSelection(context, type),
          itemBuilder: (context) => const [
            PopupMenuItem<EmployeeExportType>(
              value: EmployeeExportType.etiquetas,
              child: ListTile(
                contentPadding: EdgeInsets.zero,
                leading: Icon(Icons.label_outline),
                title: Text('Planilha Etiquetas'),
                subtitle: Text('.xlsx'),
              ),
            ),
            PopupMenuItem<EmployeeExportType>(
              value: EmployeeExportType.soc,
              child: ListTile(
                contentPadding: EdgeInsets.zero,
                leading: Icon(Icons.fact_check_outlined),
                title: Text('Planilha SOC'),
                subtitle: Text('.xlsx'),
              ),
            ),
          ],
        );
      },
    );
  }
}

/// Modal dialog that mirrors [EmployeeListViewModel.exportProgress] while
/// a spreadsheet export is in flight.
class _ExportProgressDialog extends StatelessWidget {
  const _ExportProgressDialog({required this.viewModel});

  final EmployeeListViewModel viewModel;

  @override
  Widget build(BuildContext context) {
    return PopScope(
      canPop: false,
      child: AlertDialog(
        title: const Text('Exportando funcionários'),
        content: ListenableBuilder(
          listenable: viewModel,
          builder: (context, _) {
            final progress = viewModel.exportProgress;
            final percent = (progress * 100).round();
            return Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                LinearProgressIndicator(
                  value: progress > 0 ? progress : null,
                ),
                const SizedBox(height: AppSpacing.md),
                Text(
                  progress > 0
                      ? 'Processando funcionários ($percent%)...'
                      : 'Carregando funcionários...',
                  textAlign: TextAlign.center,
                ),
              ],
            );
          },
        ),
      ),
    );
  }
}

/// Presents empty and error states inside a centered Material card.
class _EmptyStateCard extends StatelessWidget {
  const _EmptyStateCard({
    required this.icon,
    required this.title,
    required this.message,
    this.actionLabel,
    this.onAction,
  });

  final IconData icon;
  final String title;
  final String message;
  final String? actionLabel;
  final VoidCallback? onAction;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;

    return Center(
      child: SingleChildScrollView(
        padding: const EdgeInsets.symmetric(vertical: AppSpacing.md),
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 420),
          child: Card.outlined(
            child: Padding(
              padding: const EdgeInsets.all(AppSpacing.xl),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Icon(
                    icon,
                    size: 48,
                    color: colorScheme.primary,
                  ),
                  const SizedBox(height: AppSpacing.md),
                  Text(
                    title,
                    style: Theme.of(context).textTheme.titleMedium,
                    textAlign: TextAlign.center,
                  ),
                  const SizedBox(height: AppSpacing.sm),
                  Text(
                    message,
                    style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                          color: colorScheme.onSurfaceVariant,
                        ),
                    textAlign: TextAlign.center,
                  ),
                  if (actionLabel != null && onAction != null) ...[
                    const SizedBox(height: AppSpacing.lg),
                    FilledButton.tonal(
                      onPressed: onAction,
                      child: Text(actionLabel!),
                    ),
                  ],
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}
