import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/theme/app_spacing.dart';
import '../../../../domain/entities/employee.dart';
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
    widget.viewModel.refresh();
    _scrollController.addListener(_onScroll);
  }

  @override
  void didUpdateWidget(EmployeeListScreen oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.viewModel != widget.viewModel) {
      widget.viewModel.refresh();
    }
  }

  @override
  void dispose() {
    _searchController.dispose();
    _scrollController.dispose();
    super.dispose();
  }

  void _onScroll() {
    if (_scrollController.position.pixels >=
        _scrollController.position.maxScrollExtent - 200) {
      widget.viewModel.loadNextPage();
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          tooltip: 'Voltar para início',
          onPressed: () => context.go('/home'),
        ),
        title: const Text('Funcionários'),
      ),
      body: SafeArea(
        child: Center(
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 960),
            child: ListenableBuilder(
              listenable: widget.viewModel,
              builder: (context, _) => Column(
                children: [
                  _SearchBar(
                    controller: _searchController,
                    viewModel: widget.viewModel,
                  ),
                  _FilterRow(viewModel: widget.viewModel),
                  const _ColumnHeaders(),
                  const Divider(height: 1),
                  Expanded(child: _buildList(context)),
                ],
              ),
            ),
          ),
        ),
      ),
      floatingActionButton: FloatingActionButton(
        onPressed: () => context
            .push('/employee/create')
            .then((_) => widget.viewModel.refresh()),
        tooltip: 'Adicionar funcionário',
        child: const Icon(Icons.person_add_outlined),
      ),
    );
  }

  Widget _buildList(BuildContext context) {
    if (widget.viewModel.isLoading) {
      return const Center(child: CircularProgressIndicator());
    }

    if (widget.viewModel.hasError && widget.viewModel.employees.isEmpty) {
      return _ErrorState(
        message:
            widget.viewModel.errorMessage ?? 'Erro ao carregar funcionários.',
        onRetry: widget.viewModel.refresh,
      );
    }

    if (widget.viewModel.employees.isEmpty) {
      return const Center(
        child: Text('Nenhum funcionário encontrado.'),
      );
    }

    return RefreshIndicator(
      onRefresh: widget.viewModel.refresh,
      child: ListView.separated(
        controller: _scrollController,
        padding: const EdgeInsets.fromLTRB(
          AppSpacing.md,
          AppSpacing.sm,
          AppSpacing.md,
          AppSpacing.md + 80,
        ),
        itemCount: widget.viewModel.employees.length +
            (widget.viewModel.isLoadingMore ? 1 : 0),
        separatorBuilder: (_, __) => const SizedBox(height: AppSpacing.xs),
        itemBuilder: (context, index) {
          if (index >= widget.viewModel.employees.length) {
            return const Padding(
              padding: EdgeInsets.symmetric(vertical: AppSpacing.md),
              child: Center(child: CircularProgressIndicator()),
            );
          }
          final employee = widget.viewModel.employees[index];
          return _EmployeeTile(
            employee: employee,
            imageBytes: widget.viewModel.imageFor(employee.id),
            onTap: () => context.push('/employee/${employee.id}'),
          );
        },
      ),
    );
  }
}

/// Combined search field + search-parameter dropdown with pill-shaped borders.
///
/// The text field occupies available width with left-rounded corners; the
/// dropdown is right-rounded with a fixed max-width of 120dp.
class _SearchBar extends StatelessWidget {
  const _SearchBar({required this.controller, required this.viewModel});

  final TextEditingController controller;
  final EmployeeListViewModel viewModel;

  @override
  Widget build(BuildContext context) {
    const leftRadius = BorderRadius.only(
      topLeft: Radius.circular(30),
      bottomLeft: Radius.circular(30),
    );
    const rightRadius = BorderRadius.only(
      topRight: Radius.circular(30),
      bottomRight: Radius.circular(30),
    );

    return Padding(
      padding: const EdgeInsets.fromLTRB(
        AppSpacing.md,
        AppSpacing.md,
        AppSpacing.md,
        AppSpacing.sm,
      ),
      child: Row(
        children: [
          Expanded(
            child: TextField(
              controller: controller,
              decoration: InputDecoration(
                hintText: 'Buscar por ${viewModel.searchParam.label}…',
                prefixIcon: const Icon(Icons.search),
                border: const OutlineInputBorder(borderRadius: leftRadius),
                enabledBorder:
                    const OutlineInputBorder(borderRadius: leftRadius),
                focusedBorder: OutlineInputBorder(
                  borderRadius: leftRadius,
                  borderSide: BorderSide(
                    color: Theme.of(context).colorScheme.primary,
                    width: 2,
                  ),
                ),
                suffixIcon: controller.text.isNotEmpty
                    ? IconButton(
                        icon: const Icon(Icons.clear),
                        onPressed: () {
                          controller.clear();
                          viewModel.onSearchQueryChanged('');
                          viewModel.onSearchSubmitted();
                        },
                      )
                    : null,
              ),
              onChanged: viewModel.onSearchQueryChanged,
              onSubmitted: (_) => viewModel.onSearchSubmitted(),
              onEditingComplete: () => viewModel.onSearchSubmitted(),
            ),
          ),
          SizedBox(
            width: 120,
            child: DropdownButtonFormField<SearchParam>(
              isExpanded: true,
              value: viewModel.searchParam,
              items: SearchParam.values
                  .map((p) => DropdownMenuItem(value: p, child: Text(p.label)))
                  .toList(),
              onChanged: (p) {
                if (p != null) viewModel.onSearchParamChanged(p);
              },
              decoration: const InputDecoration(
                border: OutlineInputBorder(borderRadius: rightRadius),
                enabledBorder: OutlineInputBorder(borderRadius: rightRadius),
                focusedBorder: OutlineInputBorder(borderRadius: rightRadius),
                isDense: true,
                contentPadding: EdgeInsets.symmetric(
                  horizontal: AppSpacing.sm,
                  vertical: AppSpacing.sm,
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

/// Row with pill-shaped status/document-status dropdowns and a sort button.
class _FilterRow extends StatelessWidget {
  const _FilterRow({required this.viewModel});

  final EmployeeListViewModel viewModel;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(
        AppSpacing.md,
        0,
        AppSpacing.md,
        AppSpacing.sm,
      ),
      child: Row(
        children: [
          Expanded(
            child: _PillDropdown<EmployeeStatus>(
              value: viewModel.selectedStatus,
              items: EmployeeStatus.values,
              labelOf: (s) => s.label,
              onChanged: (s) {
                if (s != null) viewModel.onStatusChanged(s);
              },
            ),
          ),
          const SizedBox(width: AppSpacing.sm),
          Expanded(
            child: _PillDropdown<DocumentStatus>(
              value: viewModel.selectedDocumentStatus,
              items: DocumentStatus.values,
              labelOf: (s) => s.label,
              onChanged: (s) {
                if (s != null) viewModel.onDocumentStatusChanged(s);
              },
            ),
          ),
          const SizedBox(width: AppSpacing.sm),
          IconButton.filled(
            onPressed: viewModel.toggleSort,
            tooltip: viewModel.ascending ? 'Ordenar Z→A' : 'Ordenar A→Z',
            icon: Transform.scale(
              scaleY: viewModel.ascending ? 1.0 : -1.0,
              child: const Icon(Icons.sort),
            ),
          ),
        ],
      ),
    );
  }
}

/// A pill-shaped [DropdownButtonFormField] used for list filters.
class _PillDropdown<T> extends StatelessWidget {
  const _PillDropdown({
    required this.value,
    required this.items,
    required this.labelOf,
    required this.onChanged,
  });

  final T value;
  final List<T> items;
  final String Function(T) labelOf;
  final ValueChanged<T?> onChanged;

  @override
  Widget build(BuildContext context) {
    const pillRadius = BorderRadius.all(Radius.circular(30));
    return DropdownButtonFormField<T>(
      isExpanded: true,
      value: value,
      decoration: const InputDecoration(
        border: OutlineInputBorder(borderRadius: pillRadius),
        enabledBorder: OutlineInputBorder(borderRadius: pillRadius),
        focusedBorder: OutlineInputBorder(borderRadius: pillRadius),
        isDense: true,
        contentPadding: EdgeInsets.symmetric(
          horizontal: AppSpacing.sm,
          vertical: AppSpacing.sm,
        ),
      ),
      items: items
          .map((item) => DropdownMenuItem<T>(
                value: item,
                child: Text(
                  labelOf(item),
                  overflow: TextOverflow.ellipsis,
                ),
              ))
          .toList(),
      onChanged: onChanged,
    );
  }
}

/// Column header row aligned with the trailing columns of [_EmployeeTile].
class _ColumnHeaders extends StatelessWidget {
  const _ColumnHeaders();

  @override
  Widget build(BuildContext context) {
    final labelStyle = Theme.of(context).textTheme.labelSmall?.copyWith(
          color: Theme.of(context).colorScheme.onSurfaceVariant,
        );
    return Padding(
      padding: const EdgeInsets.fromLTRB(AppSpacing.md, 0, AppSpacing.md, 4),
      child: Row(
        children: [
          // 40dp leading (CircleAvatar) + 16dp gap = 56dp offset
          const SizedBox(width: 56),
          Expanded(
            child: Text(
              'Nomes',
              style: labelStyle,
            ),
          ),
          SizedBox(
            width: 90,
            child: Text(
              'Documentos',
              textAlign: TextAlign.center,
              style: labelStyle,
            ),
          ),
          const SizedBox(width: AppSpacing.xs),
          SizedBox(
            width: 80,
            child: Text(
              'Status',
              textAlign: TextAlign.center,
              style: labelStyle,
            ),
          ),
        ],
      ),
    );
  }
}

/// A single employee row showing avatar, name, role, document status, and status.
class _EmployeeTile extends StatelessWidget {
  const _EmployeeTile({
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
    return ListTile(
      leading: CircleAvatar(
        backgroundImage: hasImage ? MemoryImage(imageBytes!) : null,
        backgroundColor: hasImage
            ? null
            : Theme.of(context).colorScheme.primaryContainer,
        child: hasImage
            ? null
            : Text(
                employee.name.isNotEmpty
                    ? employee.name[0].toUpperCase()
                    : '?',
                style: TextStyle(
                  color: Theme.of(context).colorScheme.onPrimaryContainer,
                  fontWeight: FontWeight.bold,
                ),
              ),
      ),
      title: Text(
        employee.name,
        style: Theme.of(context).textTheme.titleSmall,
        overflow: TextOverflow.ellipsis,
      ),
      subtitle: Text(
        employee.roleName.isNotEmpty
            ? employee.roleName
            : 'Sem função atribuída',
        style: Theme.of(context).textTheme.bodySmall,
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
}

class _ErrorState extends StatelessWidget {
  const _ErrorState({required this.message, required this.onRetry});

  final String message;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Text(message, style: Theme.of(context).textTheme.bodyLarge),
          const SizedBox(height: AppSpacing.md),
          FilledButton.tonal(
            onPressed: onRetry,
            child: const Text('Tentar novamente'),
          ),
        ],
      ),
    );
  }
}
