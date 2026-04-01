import 'package:flutter/material.dart';

import '../../../../core/theme/app_spacing.dart';
import '../viewmodel/batch_download_viewmodel.dart';

/// Step 3: Review selection and download.
///
/// Displays a summary of selected document units grouped by employee,
/// and provides the download action button.
class ReviewDownloadStep extends StatelessWidget {
  const ReviewDownloadStep({
    super.key,
    required this.viewModel,
    required this.onDownload,
  });

  /// The parent view model.
  final BatchDownloadViewModel viewModel;

  /// Callback invoked when the user taps "Baixar".
  final VoidCallback onDownload;

  @override
  Widget build(BuildContext context) {
    final vm = viewModel;
    final colorScheme = Theme.of(context).colorScheme;
    final textTheme = Theme.of(context).textTheme;
    final grouped = vm.selectedUnitsByEmployee;
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
                  Icons.download_rounded,
                  size: 32,
                  color: colorScheme.primary,
                ),
                const SizedBox(width: AppSpacing.md),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        '${vm.selectedUnitCount} documento(s) selecionado(s)',
                        style: textTheme.titleMedium,
                      ),
                      Text(
                        'de ${vm.selectedEmployeeNamesCount} funcionario(s)',
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
        // Grouped list
        Expanded(
          child: grouped.isEmpty
              ? Center(
                  child: Text(
                    'Nenhum documento selecionado',
                    style: textTheme.bodyLarge?.copyWith(
                      color: colorScheme.onSurfaceVariant,
                    ),
                  ),
                )
              : ListView.builder(
                  itemCount: grouped.length,
                  itemBuilder: (context, index) {
                    final entry = grouped.entries.elementAt(index);
                    return _EmployeeGroup(
                      employeeName: entry.key,
                      units: entry.value,
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
                      width: 140,
                      child: Center(child: CircularProgressIndicator()),
                    )
                  : FilledButton.icon(
                      onPressed:
                          vm.hasSelectedUnits ? onDownload : null,
                      icon: const Icon(Icons.download_rounded, size: 18),
                      label: const Text('Baixar'),
                    ),
            ],
          ),
        ),
      ],
    );
  }
}

/// Displays a group of selected units for a single employee.
class _EmployeeGroup extends StatelessWidget {
  const _EmployeeGroup({
    required this.employeeName,
    required this.units,
  });

  final String employeeName;
  final List units;

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
            ...units.map(
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
    );
  }
}
