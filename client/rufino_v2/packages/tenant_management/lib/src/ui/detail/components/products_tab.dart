import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:provider/provider.dart';
import 'package:rufino_core/rufino_core.dart';

import '../../../domain/tenant.dart';
import '../../../domain/tenant_enums.dart';
import '../../../tenant_permissions.dart';
import '../tenant_detail_viewmodel.dart';

const _allProducts = [
  TenantProducts.peopleManagement,
  TenantProducts.billPayment,
];

/// Which products this customer has, and since when.
///
/// Turning a product off keeps the record: the history of when it was on is
/// what explains past billing and past access.
class ProductsTab extends StatelessWidget {
  /// Creates the tab.
  const ProductsTab({super.key, required this.viewModel});

  /// Drives the tab.
  final TenantDetailViewModel viewModel;

  @override
  Widget build(BuildContext context) {
    final tenant = viewModel.tenant;
    if (tenant == null) return const SizedBox.shrink();

    final canEdit = context
        .watch<TenantPermissionNotifier>()
        .hasPermission(TenantResources.tenantProduct, TenantScopes.edit);
    final isSaving =
        viewModel.productsStatus == TenantSectionStatus.saving;

    return ListView(
      padding: const EdgeInsets.all(AppSpacing.md),
      children: [
        for (final product in _allProducts)
          Padding(
            padding: const EdgeInsets.only(bottom: AppSpacing.sm),
            child: _ProductRow(
              product: product,
              record: tenant.products
                  .where((p) => p.product == product)
                  .firstOrNull,
              canEdit: canEdit && !viewModel.isFrozen && !isSaving,
              onChanged: (enabled) =>
                  viewModel.setProduct(product, enabled: enabled),
            ),
          ),
      ],
    );
  }
}

class _ProductRow extends StatelessWidget {
  const _ProductRow({
    required this.product,
    required this.record,
    required this.canEdit,
    required this.onChanged,
  });

  final String product;
  final TenantProductInfo? record;
  final bool canEdit;
  final void Function(bool enabled) onChanged;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final isActive = record?.isActive ?? false;
    final dateFormat = DateFormat('dd/MM/yyyy');

    final history = switch (record) {
      null => 'Nunca habilitado',
      final r when r.isActive =>
        'Habilitado em ${dateFormat.format(r.activatedAt)}',
      final r => 'Desabilitado em '
          '${dateFormat.format(r.deactivatedAt ?? r.activatedAt)}',
    };

    return Card.outlined(
      child: Padding(
        padding: const EdgeInsets.all(AppSpacing.md),
        child: Row(
          children: [
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    TenantProductLabels.label(product),
                    style: theme.textTheme.titleMedium,
                  ),
                  const SizedBox(height: 2),
                  Text(
                    history,
                    style: theme.textTheme.bodySmall?.copyWith(
                      color: theme.colorScheme.onSurfaceVariant,
                    ),
                  ),
                ],
              ),
            ),
            // Sem permissão o estado vira texto, não controle desabilitado:
            // um switch cinza sugere que existe algo a fazer ali.
            if (canEdit)
              Switch(value: isActive, onChanged: onChanged)
            else
              Text(
                isActive ? 'Habilitado' : 'Desabilitado',
                style: theme.textTheme.labelLarge?.copyWith(
                  color: theme.colorScheme.onSurfaceVariant,
                ),
              ),
          ],
        ),
      ),
    );
  }
}
