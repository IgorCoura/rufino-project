import 'package:flutter/material.dart';

import '../../../../core/theme/app_spacing.dart';
import '../../../../domain/entities/batch_download.dart';
import '../viewmodel/batch_download_viewmodel.dart';

/// Step 3 (combine mode): Review combination groups and download merged PDFs.
///
/// Displays the committed groups organized per employee, with a progress
/// indicator during download and a "Combinar e Baixar" action button.
class CombineReviewStep extends StatelessWidget {
  const CombineReviewStep({
    super.key,
    required this.viewModel,
    required this.onDownload,
  });

  /// The parent view model.
  final BatchDownloadViewModel viewModel;

  /// Callback invoked when the user taps "Combinar e Baixar".
  final VoidCallback onDownload;

  @override
  Widget build(BuildContext context) {
    final vm = viewModel;
    final colorScheme = Theme.of(context).colorScheme;
    final textTheme = Theme.of(context).textTheme;
    final byEmployee = vm.combinedUnitsByEmployee;
    final isDownloading = vm.status == BatchDownloadStatus.downloading;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        const SizedBox(height: AppSpacing.md),
        // Summary
        Card.filled(
          child: Padding(
            padding: const EdgeInsets.all(AppSpacing.md),
            child: Row(
              children: [
                Icon(
                  Icons.merge_type_rounded,
                  size: 32,
                  color: colorScheme.primary,
                ),
                const SizedBox(width: AppSpacing.md),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        '${vm.combinationGroupCount} grupo(s), '
                        '${vm.combinedTotalUnitCount} documento(s)',
                        style: textTheme.titleMedium,
                      ),
                      Text(
                        'de ${vm.combinedEmployeeCount} funcionario(s)',
                        style: textTheme.bodyMedium?.copyWith(
                          color: colorScheme.onSurfaceVariant,
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),
        ),
        const SizedBox(height: AppSpacing.md),
        // Progress indicator
        if (isDownloading) ...[
          LinearProgressIndicator(value: vm.downloadProgress),
          const SizedBox(height: AppSpacing.sm),
          Text(
            'Baixando e combinando... ${(vm.downloadProgress * 100).toInt()}%',
            style: textTheme.bodySmall?.copyWith(
              color: colorScheme.onSurfaceVariant,
            ),
            textAlign: TextAlign.center,
          ),
          const SizedBox(height: AppSpacing.sm),
        ],
        // Grouped list by employee
        Expanded(
          child: byEmployee.isEmpty
              ? Center(
                  child: Text(
                    'Nenhum documento na combinacao',
                    style: textTheme.bodyLarge?.copyWith(
                      color: colorScheme.onSurfaceVariant,
                    ),
                  ),
                )
              : ListView.builder(
                  itemCount: byEmployee.length,
                  itemBuilder: (context, index) {
                    final entry = byEmployee.entries.elementAt(index);
                    return _EmployeeCombineCard(
                      employeeName: entry.key,
                      groups: entry.value,
                      onRemoveGroup: isDownloading
                          ? null
                          : (groupNumber) =>
                              vm.removeCombinationGroup(groupNumber),
                    );
                  },
                ),
        ),
        // Navigation + Download
        const Divider(height: 1),
        Padding(
          padding: const EdgeInsets.symmetric(vertical: AppSpacing.sm),
          child: Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              OutlinedButton(
                onPressed: isDownloading ? null : vm.goBack,
                child: const Text('Voltar'),
              ),
              isDownloading
                  ? const SizedBox(
                      width: 180,
                      child: Center(child: CircularProgressIndicator()),
                    )
                  : FilledButton.icon(
                      onPressed: vm.combinationGroupCount > 0
                          ? onDownload
                          : null,
                      icon: const Icon(Icons.merge_type_rounded, size: 18),
                      label: const Text('Combinar e Baixar'),
                    ),
            ],
          ),
        ),
      ],
    );
  }
}

/// Displays one employee's combination groups with numbered sections.
class _EmployeeCombineCard extends StatelessWidget {
  const _EmployeeCombineCard({
    required this.employeeName,
    required this.groups,
    required this.onRemoveGroup,
  });

  final String employeeName;
  final List<CombinationGroup> groups;
  final void Function(int groupNumber)? onRemoveGroup;

  @override
  Widget build(BuildContext context) {
    final textTheme = Theme.of(context).textTheme;
    final colorScheme = Theme.of(context).colorScheme;

    return Card.outlined(
      margin: const EdgeInsets.only(bottom: AppSpacing.sm),
      child: Padding(
        padding: const EdgeInsets.all(AppSpacing.sm),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              employeeName,
              style: textTheme.titleSmall?.copyWith(
                color: colorScheme.primary,
              ),
            ),
            const SizedBox(height: AppSpacing.xs),
            ...groups.map(
              (group) => Padding(
                padding: const EdgeInsets.only(bottom: AppSpacing.xs),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        Text(
                          'Grupo ${group.groupNumber}',
                          style: textTheme.labelMedium?.copyWith(
                            color: colorScheme.onSurfaceVariant,
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                        if (onRemoveGroup != null) ...[
                          const SizedBox(width: AppSpacing.xs),
                          InkWell(
                            onTap: () => onRemoveGroup!(group.groupNumber),
                            borderRadius: BorderRadius.circular(12),
                            child: Padding(
                              padding: const EdgeInsets.all(2),
                              child: Icon(
                                Icons.close,
                                size: 14,
                                color: colorScheme.error,
                              ),
                            ),
                          ),
                        ],
                      ],
                    ),
                    ...group.units.map(
                      (unit) => Padding(
                        padding: const EdgeInsets.symmetric(
                          vertical: 2,
                          horizontal: AppSpacing.sm,
                        ),
                        child: Row(
                          children: [
                            Icon(
                              Icons.description_outlined,
                              size: 16,
                              color: colorScheme.onSurfaceVariant,
                            ),
                            const SizedBox(width: AppSpacing.xs),
                            Expanded(
                              child: Text(
                                '${unit.documentTemplateName} - ${unit.date}'
                                '${unit.period != null ? ' (${unit.period!.formattedPeriod})' : ''}',
                                style: textTheme.bodySmall,
                              ),
                            ),
                          ],
                        ),
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
