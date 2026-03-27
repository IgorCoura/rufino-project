import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:rufino_v2/ui/core/widgets/permission_guard.dart';

import '../../../../core/theme/app_spacing.dart';
import '../../../../domain/entities/company.dart';
import '../viewmodel/company_selection_viewmodel.dart';

class CompanySelectionScreen extends StatefulWidget {
  const CompanySelectionScreen({super.key, required this.viewModel});

  final CompanySelectionViewModel viewModel;

  @override
  State<CompanySelectionScreen> createState() => _CompanySelectionScreenState();
}

class _CompanySelectionScreenState extends State<CompanySelectionScreen> {
  @override
  void initState() {
    super.initState();
    widget.viewModel.addListener(_onStatusChanged);
    widget.viewModel.loadCompanies();
  }

  @override
  void didUpdateWidget(covariant CompanySelectionScreen oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.viewModel != widget.viewModel) {
      oldWidget.viewModel.removeListener(_onStatusChanged);
      widget.viewModel.addListener(_onStatusChanged);
      widget.viewModel.loadCompanies();
    }
  }

  @override
  void dispose() {
    widget.viewModel.removeListener(_onStatusChanged);
    super.dispose();
  }

  void _onStatusChanged() {
    if (!mounted) return;
    switch (widget.viewModel.status) {
      case CompanySelectionStatus.selected:
        context.go('/home');
      case CompanySelectionStatus.noCompanies:
        context.go('/company/create');
      case CompanySelectionStatus.error:
        ScaffoldMessenger.of(context)
          ..hideCurrentSnackBar()
          ..showSnackBar(
            SnackBar(
              content:
                  Text(widget.viewModel.errorMessage ?? 'Erro desconhecido.'),
              behavior: SnackBarBehavior.floating,
            ),
          );
      default:
        break;
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: SafeArea(
        child: Center(
          child: Padding(
            padding: const EdgeInsets.fromLTRB(
              AppSpacing.md,
              AppSpacing.md,
              AppSpacing.md,
              AppSpacing.md + 72,
            ),
            child: ListenableBuilder(
              listenable: widget.viewModel,
              builder: (context, _) {
                if (widget.viewModel.isLoading) {
                  return const CircularProgressIndicator();
                }
                return _CompanySelectionContent(viewModel: widget.viewModel);
              },
            ),
          ),
        ),
      ),
      floatingActionButton: PermissionGuard(
        resource: 'company',
        scope: 'create',
        child: FloatingActionButton.extended(
          onPressed: () => context.go('/company/create'),
          icon: const Icon(Icons.add),
          label: const Text('Criar Empresa'),
        ),
      ),
    );
  }
}

class _CompanySelectionContent extends StatelessWidget {
  const _CompanySelectionContent({required this.viewModel});

  final CompanySelectionViewModel viewModel;

  @override
  Widget build(BuildContext context) {
    if (viewModel.companies.isEmpty) {
      return Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(
            Icons.business_outlined,
            size: 64,
            color: Theme.of(context).colorScheme.outline,
          ),
          const SizedBox(height: AppSpacing.md),
          Text(
            'Nenhuma empresa encontrada.',
            style: Theme.of(context).textTheme.titleMedium,
          ),
          const SizedBox(height: AppSpacing.sm),
          Text(
            'Crie uma empresa para continuar.',
            style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                  color: Theme.of(context).colorScheme.onSurfaceVariant,
                ),
          ),
        ],
      );
    }

    return ConstrainedBox(
      constraints: const BoxConstraints(maxWidth: 600),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Text(
            'Selecione a Empresa',
            style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                  fontWeight: FontWeight.bold,
                ),
            textAlign: TextAlign.center,
          ),
          const SizedBox(height: AppSpacing.lg),
          _CompanyDropdown(viewModel: viewModel),
          const SizedBox(height: AppSpacing.lg),
          FilledButton(
            onPressed: viewModel.confirmSelection,
            child: const Padding(
              padding: EdgeInsets.symmetric(vertical: AppSpacing.sm),
              child: Text('Selecionar'),
            ),
          ),
        ],
      ),
    );
  }
}

class _CompanyDropdown extends StatelessWidget {
  const _CompanyDropdown({required this.viewModel});

  final CompanySelectionViewModel viewModel;

  @override
  Widget build(BuildContext context) {
    return DropdownButtonFormField<Company>(
      // ignore: deprecated_member_use
      value: viewModel.selectedCompany,
      decoration: const InputDecoration(
        labelText: 'Empresa',
        border: OutlineInputBorder(),
        prefixIcon: Icon(Icons.business_outlined),
      ),
      items: viewModel.companies
          .map(
            (company) => DropdownMenuItem<Company>(
              value: company,
              child: Text(company.fantasyName),
            ),
          )
          .toList(),
      onChanged: (company) {
        if (company != null) viewModel.onCompanySelected(company);
      },
    );
  }
}
