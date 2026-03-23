import 'dart:typed_data';

import 'package:file_picker/file_picker.dart';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/theme/app_breakpoints.dart';
import '../../../../core/theme/app_spacing.dart';
import '../../../../domain/entities/employee.dart';
import '../../../../domain/entities/employee_profile.dart';
import '../viewmodel/employee_profile_viewmodel.dart';
import 'components/address_section.dart';
import 'components/contact_section.dart';
import 'components/contract_section.dart';
import 'components/dependent_section.dart';
import 'components/id_card_section.dart';
import 'components/medical_exam_section.dart';
import 'components/military_document_section.dart';
import 'components/personal_info_section.dart';
import 'components/role_info_section.dart';
import 'components/signing_options_section.dart';
import 'components/vote_id_section.dart';
import 'components/workplace_section.dart';

/// Displays a detailed employee profile with photo upload, name editing,
/// status badge, and assignment summary.
class EmployeeProfileScreen extends StatefulWidget {
  const EmployeeProfileScreen({
    super.key,
    required this.viewModel,
    required this.employeeId,
  });

  final EmployeeProfileViewModel viewModel;
  final String employeeId;

  @override
  State<EmployeeProfileScreen> createState() => _EmployeeProfileScreenState();
}

class _EmployeeProfileScreenState extends State<EmployeeProfileScreen> {
  /// Controller owned at screen level so it survives every [ListenableBuilder]
  /// rebuild without losing the text the user is actively typing.
  final TextEditingController _nameController = TextEditingController();

  @override
  void initState() {
    super.initState();
    widget.viewModel.addListener(_onViewModelChanged);
    widget.viewModel.load(widget.employeeId);
  }

  @override
  void didUpdateWidget(EmployeeProfileScreen oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.viewModel != widget.viewModel) {
      oldWidget.viewModel.removeListener(_onViewModelChanged);
      widget.viewModel.addListener(_onViewModelChanged);
    }
    if (oldWidget.employeeId != widget.employeeId ||
        oldWidget.viewModel != widget.viewModel) {
      widget.viewModel.load(widget.employeeId);
    }
  }

  @override
  void dispose() {
    _nameController.dispose();
    widget.viewModel.removeListener(_onViewModelChanged);
    super.dispose();
  }

  /// Reacts to transient ViewModel state changes such as snack messages.
  ///
  /// Also keeps [_nameController] in sync with the profile name whenever the
  /// screen is not in editing mode, so the field is always pre-filled with the
  /// current name when the user opens the editor.
  void _onViewModelChanged() {
    if (!mounted) return;

    // Sync controller with the authoritative name whenever we are not editing.
    if (!widget.viewModel.isEditingName) {
      _nameController.text = widget.viewModel.profile?.name ?? '';
    }

    final snackMessage = widget.viewModel.snackMessage;
    if (snackMessage != null && snackMessage.isNotEmpty) {
      ScaffoldMessenger.of(context)
        ..hideCurrentSnackBar()
        ..showSnackBar(
          SnackBar(
            content: Text(snackMessage),
            behavior: SnackBarBehavior.floating,
          ),
        );
      widget.viewModel.consumeSnackMessage();
    }

    if (widget.viewModel.hasError && widget.viewModel.profile != null) {
      ScaffoldMessenger.of(context)
        ..hideCurrentSnackBar()
        ..showSnackBar(
          SnackBar(
            content:
                Text(widget.viewModel.errorMessage ?? 'Erro desconhecido.'),
            behavior: SnackBarBehavior.floating,
          ),
        );
    }
  }

  Future<void> _confirmMarkAsInactive() async {
    final confirmed = await showDialog<bool>(
          context: context,
          builder: (context) => AlertDialog(
            title: const Text('Confirmar ação'),
            content: const Text(
              'Tem certeza que deseja marcar este funcionário como inativo?',
            ),
            actions: [
              TextButton(
                onPressed: () => Navigator.of(context).pop(false),
                child: const Text('Cancelar'),
              ),
              FilledButton(
                onPressed: () => Navigator.of(context).pop(true),
                child: const Text('Confirmar'),
              ),
            ],
          ),
        ) ??
        false;

    if (!confirmed) return;
    await widget.viewModel.markAsInactive();
  }

  /// Opens a file picker for the user to select a new profile photo.
  Future<void> _pickAndUploadAvatar() async {
    final result = await FilePicker.platform.pickFiles(
      type: FileType.image,
      withData: true,
    );
    if (result == null || result.files.single.bytes == null) return;
    final file = result.files.single;
    await widget.viewModel.uploadAvatar(file.bytes!, file.name);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          tooltip: 'Voltar para funcionários',
          onPressed: () => context.pop(),
        ),
        title: const Text('Perfil do Funcionário'),
      ),
      body: SafeArea(
        child: ListenableBuilder(
          listenable: widget.viewModel,
          builder: (context, _) {
            if (widget.viewModel.isLoading) {
              return const Center(child: CircularProgressIndicator());
            }

            if (widget.viewModel.hasError && widget.viewModel.profile == null) {
              return _EmployeeProfileErrorState(
                message: widget.viewModel.errorMessage ??
                    'Não foi possível carregar o funcionário.',
                onRetry: () => widget.viewModel.load(widget.employeeId),
              );
            }

            final profile = widget.viewModel.profile;
            if (profile == null) {
              return _EmployeeProfileErrorState(
                message: 'Funcionário não encontrado.',
                onRetry: () => widget.viewModel.load(widget.employeeId),
              );
            }

            return _EmployeeProfileBody(
              viewModel: widget.viewModel,
              profile: profile,
              nameController: _nameController,
              onMarkAsInactive: _confirmMarkAsInactive,
              onPickAvatar: _pickAndUploadAvatar,
            );
          },
        ),
      ),
    );
  }
}

/// Renders the employee profile summary content.
class _EmployeeProfileBody extends StatelessWidget {
  const _EmployeeProfileBody({
    required this.viewModel,
    required this.profile,
    required this.nameController,
    required this.onMarkAsInactive,
    required this.onPickAvatar,
  });

  final EmployeeProfileViewModel viewModel;
  final EmployeeProfile profile;

  /// Controller owned by [_EmployeeProfileScreenState] so it survives rebuilds.
  final TextEditingController nameController;
  final Future<void> Function() onMarkAsInactive;
  final Future<void> Function() onPickAvatar;

  @override
  Widget build(BuildContext context) {
    final width = MediaQuery.sizeOf(context).width;
    final horizontalPadding = width >= AppBreakpoints.tablet
        ? AppSpacing.xl
        : width >= AppBreakpoints.mobile
            ? AppSpacing.lg
            : AppSpacing.md;

    return SingleChildScrollView(
      padding: EdgeInsets.fromLTRB(
        horizontalPadding,
        AppSpacing.md,
        horizontalPadding,
        AppSpacing.xl,
      ),
      child: Center(
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 960),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              _EmployeeHeroCard(
                profile: profile,
                imageBytes: viewModel.imageBytes,
                isSaving: viewModel.isSaving,
                onPickAvatar: onPickAvatar,
              ),
              const SizedBox(height: AppSpacing.md),
              _EmployeeNameCard(
                viewModel: viewModel,
                profile: profile,
                controller: nameController,
              ),
              const SizedBox(height: AppSpacing.md),
              LayoutBuilder(
                builder: (context, constraints) {
                  final isWide = constraints.maxWidth >= AppBreakpoints.mobile;

                  return isWide
                      ? Row(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Expanded(
                              child: _ProfileSectionCard(
                                title: 'Dados do funcionário',
                                children: [
                                  _ProfileFactRow(
                                    label: 'Registro',
                                    value: profile.registration.isNotEmpty
                                        ? profile.registration
                                        : 'Não informado',
                                  ),
                                  _ProfileStatusRow(status: profile.status),
                                ],
                              ),
                            ),
                            const SizedBox(width: AppSpacing.md),
                            Expanded(
                              child: _ProfileSectionCard(
                                title: 'Vínculos',
                                children: [
                                  _ProfileFactRow(
                                    label: 'Função',
                                    value: viewModel.roleLabel,
                                  ),
                                  _ProfileFactRow(
                                    label: 'Local de trabalho',
                                    value: viewModel.workplaceLabel,
                                  ),
                                ],
                              ),
                            ),
                          ],
                        )
                      : Column(
                          children: [
                            _ProfileSectionCard(
                              title: 'Dados do funcionário',
                              children: [
                                _ProfileFactRow(
                                  label: 'Registro',
                                  value: profile.registration.isNotEmpty
                                      ? profile.registration
                                      : 'Não informado',
                                ),
                                _ProfileStatusRow(status: profile.status),
                              ],
                            ),
                            const SizedBox(height: AppSpacing.md),
                            _ProfileSectionCard(
                              title: 'Vínculos',
                              children: [
                                _ProfileFactRow(
                                  label: 'Função',
                                  value: viewModel.roleLabel,
                                ),
                                _ProfileFactRow(
                                  label: 'Local de trabalho',
                                  value: viewModel.workplaceLabel,
                                ),
                              ],
                            ),
                          ],
                        );
                },
              ),
              const SizedBox(height: AppSpacing.md),
              ContactSection(viewModel: viewModel),
              const SizedBox(height: AppSpacing.sm),
              AddressSection(viewModel: viewModel),
              const SizedBox(height: AppSpacing.sm),
              PersonalInfoSection(viewModel: viewModel),
              const SizedBox(height: AppSpacing.sm),
              IdCardSection(viewModel: viewModel),
              const SizedBox(height: AppSpacing.sm),
              VoteIdSection(viewModel: viewModel),
              const SizedBox(height: AppSpacing.sm),
              DependentSection(viewModel: viewModel),
              const SizedBox(height: AppSpacing.sm),
              MilitaryDocumentSection(viewModel: viewModel),
              const SizedBox(height: AppSpacing.sm),
              MedicalExamSection(viewModel: viewModel),
              const SizedBox(height: AppSpacing.sm),
              RoleInfoSection(viewModel: viewModel),
              const SizedBox(height: AppSpacing.sm),
              WorkplaceSection(viewModel: viewModel),
              const SizedBox(height: AppSpacing.sm),
              SigningOptionsSection(viewModel: viewModel),
              const SizedBox(height: AppSpacing.sm),
              ContractSection(
                viewModel: viewModel,
                canMarkAsInactive: profile.canMarkAsInactive,
                onMarkAsInactive: onMarkAsInactive,
              ),
            ],
          ),
        ),
      ),
    );
  }
}

/// Highlights the employee identity at the top of the profile screen with
/// an editable avatar.
class _EmployeeHeroCard extends StatelessWidget {
  const _EmployeeHeroCard({
    required this.profile,
    required this.imageBytes,
    required this.isSaving,
    required this.onPickAvatar,
  });

  final EmployeeProfile profile;
  final Uint8List? imageBytes;
  final bool isSaving;
  final Future<void> Function() onPickAvatar;

  @override
  Widget build(BuildContext context) {
    final hasImage = imageBytes != null;
    final colorScheme = Theme.of(context).colorScheme;

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(AppSpacing.lg),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.center,
          children: [
            Stack(
              children: [
                CircleAvatar(
                  radius: 40,
                  backgroundImage: hasImage ? MemoryImage(imageBytes!) : null,
                  backgroundColor: colorScheme.primaryContainer,
                  child: hasImage
                      ? null
                      : Text(
                          profile.name.isNotEmpty
                              ? profile.name[0].toUpperCase()
                              : '?',
                          style: Theme.of(context)
                              .textTheme
                              .headlineMedium
                              ?.copyWith(
                                color: colorScheme.onPrimaryContainer,
                              ),
                        ),
                ),
                Positioned(
                  bottom: 0,
                  right: 0,
                  child: Tooltip(
                    message: 'Alterar foto',
                    child: GestureDetector(
                      onTap: isSaving ? null : onPickAvatar,
                      child: Container(
                        width: 26,
                        height: 26,
                        decoration: BoxDecoration(
                          color: colorScheme.primary,
                          shape: BoxShape.circle,
                          border: Border.all(
                            color: colorScheme.surface,
                            width: 2,
                          ),
                        ),
                        child: Icon(
                          Icons.camera_alt,
                          size: 14,
                          color: colorScheme.onPrimary,
                        ),
                      ),
                    ),
                  ),
                ),
              ],
            ),
            const SizedBox(width: AppSpacing.md),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    profile.name,
                    style: Theme.of(context).textTheme.headlineSmall,
                  ),
                  const SizedBox(height: AppSpacing.xs),
                  Text(
                    profile.registration.isNotEmpty
                        ? 'Registro ${profile.registration}'
                        : 'Registro não informado',
                    style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                          color: colorScheme.onSurfaceVariant,
                        ),
                  ),
                  const SizedBox(height: AppSpacing.sm),
                  _StatusBadge(status: profile.status),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

/// Inline name editing card for the employee's display name.
///
/// Stateless by design — the [controller] is owned by the parent
/// [_EmployeeProfileScreenState] so its text survives every [ListenableBuilder]
/// rebuild triggered by the ViewModel.
class _EmployeeNameCard extends StatelessWidget {
  const _EmployeeNameCard({
    required this.viewModel,
    required this.profile,
    required this.controller,
  });

  final EmployeeProfileViewModel viewModel;
  final EmployeeProfile profile;

  /// Text controller owned by the enclosing [_EmployeeProfileScreenState].
  final TextEditingController controller;

  @override
  Widget build(BuildContext context) {
    final vm = viewModel;
    return Card.outlined(
      child: Padding(
        padding: const EdgeInsets.all(AppSpacing.md),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(
                  child: Text(
                    'Nome do funcionário',
                    style: Theme.of(context).textTheme.titleMedium,
                  ),
                ),
                if (!vm.isEditingName)
                  TextButton.icon(
                    onPressed: vm.isSaving
                        ? null
                        : () {
                            controller.text = profile.name;
                            vm.startEditingName();
                          },
                    icon: const Icon(Icons.edit_outlined, size: 18),
                    label: const Text('Editar'),
                  ),
              ],
            ),
            const SizedBox(height: AppSpacing.sm),
            if (vm.isEditingName) ...[
              TextField(
                controller: controller,
                enabled: !vm.isSaving,
                autofocus: true,
                decoration: const InputDecoration(
                  labelText: 'Nome',
                  border: OutlineInputBorder(),
                ),
                textCapitalization: TextCapitalization.words,
                onSubmitted: (_) => vm.saveName(controller.text),
              ),
              const SizedBox(height: AppSpacing.sm),
              Row(
                mainAxisAlignment: MainAxisAlignment.end,
                children: [
                  TextButton(
                    onPressed:
                        vm.isSaving ? null : () => vm.cancelEditingName(),
                    child: const Text('Cancelar'),
                  ),
                  const SizedBox(width: AppSpacing.sm),
                  vm.isSaving
                      ? const SizedBox(
                          width: 24,
                          height: 24,
                          child: CircularProgressIndicator(strokeWidth: 2),
                        )
                      : FilledButton(
                          onPressed: () => vm.saveName(controller.text),
                          child: const Text('Salvar'),
                        ),
                ],
              ),
            ] else ...[
              Text(
                profile.name,
                style: Theme.of(context).textTheme.bodyLarge,
              ),
            ],
          ],
        ),
      ),
    );
  }
}

/// Groups a section of related profile facts inside a Material card.
class _ProfileSectionCard extends StatelessWidget {
  const _ProfileSectionCard({
    required this.title,
    required this.children,
  });

  final String title;
  final List<Widget> children;

  @override
  Widget build(BuildContext context) {
    return Card.outlined(
      child: Padding(
        padding: const EdgeInsets.all(AppSpacing.md),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              title,
              style: Theme.of(context).textTheme.titleMedium,
            ),
            const SizedBox(height: AppSpacing.sm),
            ...children,
          ],
        ),
      ),
    );
  }
}

/// Displays a single profile label/value pair.
class _ProfileFactRow extends StatelessWidget {
  const _ProfileFactRow({
    required this.label,
    required this.value,
  });

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;

    return Padding(
      padding: const EdgeInsets.symmetric(vertical: AppSpacing.sm),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            label,
            style: Theme.of(context).textTheme.labelLarge?.copyWith(
                  color: colorScheme.onSurfaceVariant,
                ),
          ),
          const SizedBox(height: AppSpacing.xs),
          Text(value),
        ],
      ),
    );
  }
}

/// Displays the employment status as a coloured label/chip row.
class _ProfileStatusRow extends StatelessWidget {
  const _ProfileStatusRow({required this.status});

  final EmployeeStatus status;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;

    return Padding(
      padding: const EdgeInsets.symmetric(vertical: AppSpacing.sm),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            'Status',
            style: Theme.of(context).textTheme.labelLarge?.copyWith(
                  color: colorScheme.onSurfaceVariant,
                ),
          ),
          const SizedBox(height: AppSpacing.xs),
          _StatusBadge(status: status),
        ],
      ),
    );
  }
}

/// Renders the employment [status] as a colour-coded chip.
class _StatusBadge extends StatelessWidget {
  const _StatusBadge({required this.status});

  final EmployeeStatus status;

  Color _backgroundColor(ColorScheme cs) => switch (status) {
        EmployeeStatus.active => cs.primaryContainer,
        EmployeeStatus.inactive => cs.errorContainer,
        EmployeeStatus.pending => cs.secondaryContainer,
        EmployeeStatus.vacation => cs.tertiaryContainer,
        EmployeeStatus.away => cs.secondaryContainer,
        EmployeeStatus.none => cs.surfaceContainerHighest,
      };

  Color _foregroundColor(ColorScheme cs) => switch (status) {
        EmployeeStatus.active => cs.onPrimaryContainer,
        EmployeeStatus.inactive => cs.onErrorContainer,
        EmployeeStatus.pending => cs.onSecondaryContainer,
        EmployeeStatus.vacation => cs.onTertiaryContainer,
        EmployeeStatus.away => cs.onSecondaryContainer,
        EmployeeStatus.none => cs.onSurfaceVariant,
      };

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    return Container(
      padding: const EdgeInsets.symmetric(
        horizontal: AppSpacing.sm,
        vertical: AppSpacing.xs,
      ),
      decoration: BoxDecoration(
        color: _backgroundColor(cs),
        borderRadius: BorderRadius.circular(AppSpacing.sm),
      ),
      child: Text(
        status.label,
        style: Theme.of(context).textTheme.labelMedium?.copyWith(
              color: _foregroundColor(cs),
              fontWeight: FontWeight.w600,
            ),
      ),
    );
  }
}

/// Displays an error state when the employee profile cannot be loaded.
class _EmployeeProfileErrorState extends StatelessWidget {
  const _EmployeeProfileErrorState({
    required this.message,
    required this.onRetry,
  });

  final String message;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: ConstrainedBox(
        constraints: const BoxConstraints(maxWidth: 420),
        child: Card.outlined(
          margin: const EdgeInsets.all(AppSpacing.md),
          child: Padding(
            padding: const EdgeInsets.all(AppSpacing.xl),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                const Icon(Icons.error_outline, size: 48),
                const SizedBox(height: AppSpacing.md),
                Text(
                  'Não foi possível carregar o perfil.',
                  style: Theme.of(context).textTheme.titleMedium,
                  textAlign: TextAlign.center,
                ),
                const SizedBox(height: AppSpacing.sm),
                Text(
                  message,
                  textAlign: TextAlign.center,
                ),
                const SizedBox(height: AppSpacing.lg),
                FilledButton.tonal(
                  onPressed: onRetry,
                  child: const Text('Tentar novamente'),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

// Section components are in the `components/` subdirectory.
