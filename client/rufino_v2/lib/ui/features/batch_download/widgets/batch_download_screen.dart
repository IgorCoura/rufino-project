import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/theme/app_breakpoints.dart';
import '../../../../core/theme/app_spacing.dart';
import '../../../../core/utils/file_saver.dart';
import '../viewmodel/batch_download_viewmodel.dart';
import 'employee_selection_step.dart';
import 'review_download_step.dart';
import 'unit_selection_step.dart';

/// Main screen for batch document download.
///
/// Implements a 3-step wizard: select employees, select document units,
/// review and download as ZIP.
class BatchDownloadScreen extends StatefulWidget {
  const BatchDownloadScreen({super.key, required this.viewModel});

  /// The view model that manages state for this screen.
  final BatchDownloadViewModel viewModel;

  @override
  State<BatchDownloadScreen> createState() => _BatchDownloadScreenState();
}

class _BatchDownloadScreenState extends State<BatchDownloadScreen> {
  @override
  void initState() {
    super.initState();
    widget.viewModel.addListener(_onViewModelChanged);
    widget.viewModel.initialize();
  }

  @override
  void dispose() {
    widget.viewModel.removeListener(_onViewModelChanged);
    super.dispose();
  }

  void _onViewModelChanged() {
    if (!mounted) return;
    setState(() {});
    final vm = widget.viewModel;
    if (vm.status == BatchDownloadStatus.error && vm.errorMessage != null) {
      ScaffoldMessenger.of(context)
        ..hideCurrentSnackBar()
        ..showSnackBar(
          SnackBar(
            content: Text(vm.errorMessage!),
            behavior: SnackBarBehavior.floating,
          ),
        );
    }
  }

  Future<void> _handleDownload() async {
    final bytes = await widget.viewModel.downloadSelected();
    if (bytes != null && mounted) {
      await saveFile(fileName: 'documentos.zip', bytes: bytes);
      if (mounted) {
        ScaffoldMessenger.of(context)
          ..hideCurrentSnackBar()
          ..showSnackBar(
            const SnackBar(
              content: Text('Download concluido com sucesso!'),
              behavior: SnackBarBehavior.floating,
            ),
          );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final vm = widget.viewModel;
    final width = MediaQuery.sizeOf(context).width;
    final horizontalPadding = width >= AppBreakpoints.tablet
        ? AppSpacing.xl
        : width >= AppBreakpoints.mobile
            ? AppSpacing.lg
            : AppSpacing.md;

    return Scaffold(
      appBar: AppBar(
        title: const Text('Download em Lote'),
        centerTitle: false,
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          onPressed: vm.currentStep == BatchDownloadStep.selectEmployees
              ? () => context.go('/home')
              : vm.goBack,
        ),
      ),
      body: SafeArea(
        child: Column(
          children: [
            _StepIndicator(currentStep: vm.currentStep),
            const Divider(height: 1),
            Expanded(
              child: Padding(
                padding:
                    EdgeInsets.symmetric(horizontal: horizontalPadding),
                child: switch (vm.currentStep) {
                  BatchDownloadStep.selectEmployees =>
                    EmployeeSelectionStep(
                      viewModel: vm,
                      onNext: vm.proceedToUnitSelection,
                    ),
                  BatchDownloadStep.selectUnits => UnitSelectionStep(
                      viewModel: vm,
                      onNext: vm.proceedToReview,
                    ),
                  BatchDownloadStep.review => ReviewDownloadStep(
                      viewModel: vm,
                      onDownload: _handleDownload,
                    ),
                },
              ),
            ),
          ],
        ),
      ),
    );
  }
}

/// Horizontal step indicator for the wizard.
class _StepIndicator extends StatelessWidget {
  const _StepIndicator({required this.currentStep});

  final BatchDownloadStep currentStep;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;
    final textTheme = Theme.of(context).textTheme;

    return Padding(
      padding: const EdgeInsets.symmetric(
        horizontal: AppSpacing.md,
        vertical: AppSpacing.sm,
      ),
      child: Row(
        children: [
          _StepChip(
            label: '1. Funcionarios',
            isActive:
                currentStep == BatchDownloadStep.selectEmployees,
            isCompleted:
                currentStep.index > BatchDownloadStep.selectEmployees.index,
            colorScheme: colorScheme,
            textTheme: textTheme,
          ),
          const Expanded(child: Divider(indent: 8, endIndent: 8)),
          _StepChip(
            label: '2. Documentos',
            isActive: currentStep == BatchDownloadStep.selectUnits,
            isCompleted:
                currentStep.index > BatchDownloadStep.selectUnits.index,
            colorScheme: colorScheme,
            textTheme: textTheme,
          ),
          const Expanded(child: Divider(indent: 8, endIndent: 8)),
          _StepChip(
            label: '3. Revisar',
            isActive: currentStep == BatchDownloadStep.review,
            isCompleted: false,
            colorScheme: colorScheme,
            textTheme: textTheme,
          ),
        ],
      ),
    );
  }
}

class _StepChip extends StatelessWidget {
  const _StepChip({
    required this.label,
    required this.isActive,
    required this.isCompleted,
    required this.colorScheme,
    required this.textTheme,
  });

  final String label;
  final bool isActive;
  final bool isCompleted;
  final ColorScheme colorScheme;
  final TextTheme textTheme;

  @override
  Widget build(BuildContext context) {
    final bgColor = isActive
        ? colorScheme.primaryContainer
        : isCompleted
            ? colorScheme.secondaryContainer
            : colorScheme.surfaceContainerHigh;
    final textColor = isActive
        ? colorScheme.onPrimaryContainer
        : isCompleted
            ? colorScheme.onSecondaryContainer
            : colorScheme.onSurface;

    return Container(
      padding: const EdgeInsets.symmetric(
        horizontal: AppSpacing.sm,
        vertical: AppSpacing.xs,
      ),
      decoration: BoxDecoration(
        color: bgColor,
        borderRadius: BorderRadius.circular(16),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          if (isCompleted)
            Padding(
              padding: const EdgeInsets.only(right: 4),
              child: Icon(Icons.check, size: 16, color: textColor),
            ),
          Text(
            label,
            style: textTheme.labelMedium?.copyWith(color: textColor),
          ),
        ],
      ),
    );
  }
}
