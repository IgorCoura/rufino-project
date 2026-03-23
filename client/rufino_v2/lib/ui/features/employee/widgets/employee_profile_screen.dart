import 'dart:typed_data';

import 'package:file_picker/file_picker.dart';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:mask_text_input_formatter/mask_text_input_formatter.dart';

import '../../../../core/theme/app_breakpoints.dart';
import '../../../../core/theme/app_spacing.dart';
import '../../../../domain/entities/address.dart';
import '../../../../domain/entities/employee.dart';
import '../../../../domain/entities/employee_id_card.dart';
import '../../../../domain/entities/employee_personal_info.dart';
import '../../../../domain/entities/employee_profile.dart';
import '../../../../domain/entities/personal_info_options.dart';
import '../../../../domain/entities/selection_option.dart';
import '../viewmodel/employee_profile_viewmodel.dart';

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
              _ContactSection(viewModel: viewModel),
              const SizedBox(height: AppSpacing.sm),
              _AddressSection(viewModel: viewModel),
              const SizedBox(height: AppSpacing.sm),
              _PersonalInfoSection(viewModel: viewModel),
              const SizedBox(height: AppSpacing.sm),
              _IdCardSection(viewModel: viewModel),
              const SizedBox(height: AppSpacing.sm),
              _VoteIdSection(viewModel: viewModel),
              if (profile.canMarkAsInactive) ...[
                const SizedBox(height: AppSpacing.lg),
                Align(
                  alignment: Alignment.centerLeft,
                  child: FilledButton.tonalIcon(
                    onPressed:
                        viewModel.isSaving ? null : () => onMarkAsInactive(),
                    icon: viewModel.isSaving
                        ? const SizedBox(
                            width: 18,
                            height: 18,
                            child: CircularProgressIndicator(strokeWidth: 2),
                          )
                        : const Icon(Icons.person_off_outlined),
                    label: const Text('Marcar como inativo'),
                  ),
                ),
              ],
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

// ─── Expandable section tile helper ──────────────────────────────────────────

/// A helper that wraps content in a card with an [ExpansionTile].
///
/// Triggers [onExpand] on first expansion so the section can lazy-load its data.
class _ExpandableSectionCard extends StatefulWidget {
  const _ExpandableSectionCard({
    required this.title,
    required this.child,
    required this.onExpand,
  });

  final String title;
  final Widget child;
  final VoidCallback onExpand;

  @override
  State<_ExpandableSectionCard> createState() => _ExpandableSectionCardState();
}

class _ExpandableSectionCardState extends State<_ExpandableSectionCard> {
  /// Whether [widget.onExpand] has been triggered at least once.
  bool _loadTriggered = false;

  @override
  Widget build(BuildContext context) {
    return Card.outlined(
      clipBehavior: Clip.antiAlias,
      child: ExpansionTile(
        title: Text(
          widget.title,
          style: Theme.of(context).textTheme.titleMedium,
        ),
        onExpansionChanged: (expanded) {
          if (expanded && !_loadTriggered) {
            _loadTriggered = true;
            widget.onExpand();
          }
        },
        childrenPadding: const EdgeInsets.fromLTRB(
          AppSpacing.md,
          0,
          AppSpacing.md,
          AppSpacing.md,
        ),
        children: [widget.child],
      ),
    );
  }
}

// ─── Contact section ──────────────────────────────────────────────────────────

/// Expandable card for viewing and editing employee contact information.
class _ContactSection extends StatefulWidget {
  const _ContactSection({required this.viewModel});

  final EmployeeProfileViewModel viewModel;

  @override
  State<_ContactSection> createState() => _ContactSectionState();
}

class _ContactSectionState extends State<_ContactSection> {
  final _formKey = GlobalKey<FormState>();
  final _cellphoneController = TextEditingController();
  final _emailController = TextEditingController();

  /// Mask formatter for Brazilian mobile numbers: `## #####-####`.
  ///
  /// The `+55` country code is shown via [InputDecoration.prefixText] so it is
  /// never part of the editable text, keeping the controller value clean.
  final _phoneMask = MaskTextInputFormatter(
    mask: '## #####-####',
    filter: {'#': RegExp(r'[0-9]')},
    type: MaskAutoCompletionType.lazy,
  );

  bool _isEditing = false;

  @override
  void dispose() {
    _cellphoneController.dispose();
    _emailController.dispose();
    super.dispose();
  }

  /// Formats raw digit string (e.g. `"11968608425"`) for display as
  /// `"+55 11 96860-8425"`.
  String _formatPhone(String raw) {
    final digits = raw.replaceAll(RegExp(r'[^\d]'), '');
    if (digits.isEmpty) return '';
    if (digits.length == 11) {
      return '+55 ${digits.substring(0, 2)} '
          '${digits.substring(2, 7)}-${digits.substring(7)}';
    }
    if (digits.length == 10) {
      return '+55 ${digits.substring(0, 2)} '
          '${digits.substring(2, 6)}-${digits.substring(6)}';
    }
    return digits;
  }

  void _startEdit() {
    final contact = widget.viewModel.contact;
    if (contact == null) return;

    // Apply the mask to the stored raw digits so the field shows formatted text.
    final rawDigits = contact.cellphone.replaceAll(RegExp(r'[^\d]'), '');
    _phoneMask.formatEditUpdate(
      TextEditingValue.empty,
      TextEditingValue(text: rawDigits),
    );
    _cellphoneController.text = _phoneMask.getMaskedText();
    _emailController.text = contact.email;
    setState(() => _isEditing = true);
  }

  Future<void> _save() async {
    if (_formKey.currentState?.validate() != true) return;

    // Strip mask characters — send only the 11 raw digits to the API.
    final rawDigits =
        _cellphoneController.text.replaceAll(RegExp(r'[^\d]'), '');
    await widget.viewModel.saveContact(rawDigits, _emailController.text.trim());

    if (mounted && widget.viewModel.contactStatus == SectionLoadStatus.loaded) {
      setState(() => _isEditing = false);
    }
  }

  void _cancel() => setState(() => _isEditing = false);

  String? _validatePhone(String? value) {
    if (value == null || value.trim().isEmpty) return null;
    final digits = value.replaceAll(RegExp(r'[^\d]'), '');
    if (digits.length != 10 && digits.length != 11) {
      return 'Número inválido (ex: 11 98765-4321)';
    }
    return null;
  }

  String? _validateEmail(String? value) {
    if (value == null || value.trim().isEmpty) return null;
    final emailRegex = RegExp(r'^[^@\s]+@[^@\s]+\.[^@\s]+$');
    if (!emailRegex.hasMatch(value.trim())) return 'E-mail inválido';
    return null;
  }

  @override
  Widget build(BuildContext context) {
    return ListenableBuilder(
      listenable: widget.viewModel,
      builder: (context, _) {
        final status = widget.viewModel.contactStatus;
        return _ExpandableSectionCard(
          title: 'Contato',
          onExpand: widget.viewModel.loadContact,
          child: Padding(
            padding: const EdgeInsets.all(8.0),
            child: _buildContent(context, status),
          ),
        );
      },
    );
  }

  Widget _buildContent(BuildContext context, SectionLoadStatus status) {
    if (status == SectionLoadStatus.notLoaded ||
        status == SectionLoadStatus.loading) {
      return const Padding(
        padding: EdgeInsets.symmetric(vertical: AppSpacing.md),
        child: Center(child: CircularProgressIndicator()),
      );
    }

    if (status == SectionLoadStatus.error) {
      return Padding(
        padding: const EdgeInsets.symmetric(vertical: AppSpacing.sm),
        child: Row(
          children: [
            Icon(
              Icons.error_outline,
              color: Theme.of(context).colorScheme.error,
              size: 20,
            ),
            const SizedBox(width: AppSpacing.sm),
            const Expanded(
              child: Text('Não foi possível carregar o contato.'),
            ),
          ],
        ),
      );
    }

    final contact = widget.viewModel.contact;
    final isSaving = status == SectionLoadStatus.saving;

    if (_isEditing) {
      return Form(
        key: _formKey,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            TextFormField(
              controller: _cellphoneController,
              enabled: !isSaving,
              decoration: const InputDecoration(
                labelText: 'Celular',
                prefixText: '+55 ',
                prefixIcon: Icon(Icons.phone_outlined),
                border: OutlineInputBorder(),
                helperText: 'Ex: 11 98765-4321',
              ),
              keyboardType: TextInputType.phone,
              inputFormatters: [_phoneMask],
              validator: _validatePhone,
            ),
            const SizedBox(height: AppSpacing.md),
            TextFormField(
              controller: _emailController,
              enabled: !isSaving,
              decoration: const InputDecoration(
                labelText: 'E-mail',
                prefixIcon: Icon(Icons.email_outlined),
                border: OutlineInputBorder(),
              ),
              keyboardType: TextInputType.emailAddress,
              validator: _validateEmail,
            ),
            const SizedBox(height: AppSpacing.md),
            Row(
              mainAxisAlignment: MainAxisAlignment.end,
              children: [
                TextButton(
                  onPressed: isSaving ? null : _cancel,
                  child: const Text('Cancelar'),
                ),
                const SizedBox(width: AppSpacing.sm),
                isSaving
                    ? const SizedBox(
                        width: 24,
                        height: 24,
                        child: CircularProgressIndicator(strokeWidth: 2),
                      )
                    : FilledButton(
                        onPressed: _save,
                        child: const Text('Salvar'),
                      ),
              ],
            ),
          ],
        ),
      );
    }

    // ── View mode ────────────────────────────────────────────────────────────
    final formattedPhone = contact?.cellphone.isNotEmpty == true
        ? _formatPhone(contact!.cellphone)
        : null;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        _ContactInfoRow(
          icon: Icons.phone_outlined,
          label: 'Celular',
          value: formattedPhone ?? 'Não informado',
        ),
        const Divider(height: AppSpacing.xl),
        _ContactInfoRow(
          icon: Icons.email_outlined,
          label: 'E-mail',
          value: contact?.email.isNotEmpty == true
              ? contact!.email
              : 'Não informado',
        ),
        const SizedBox(height: AppSpacing.sm),
        Align(
          alignment: Alignment.centerRight,
          child: TextButton.icon(
            onPressed: _startEdit,
            icon: const Icon(Icons.edit_outlined, size: 18),
            label: const Text('Editar'),
          ),
        ),
      ],
    );
  }
}

/// A labelled info row with a leading icon, used inside the contact view mode.
class _ContactInfoRow extends StatelessWidget {
  const _ContactInfoRow({
    required this.icon,
    required this.label,
    required this.value,
  });

  final IconData icon;
  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    return Row(
      crossAxisAlignment: CrossAxisAlignment.center,
      children: [
        Icon(icon, size: 22, color: cs.primary),
        const SizedBox(width: AppSpacing.md),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                label,
                style: Theme.of(context).textTheme.labelSmall?.copyWith(
                      color: cs.onSurfaceVariant,
                      letterSpacing: 0.4,
                    ),
              ),
              const SizedBox(height: 2),
              Text(
                value,
                style: Theme.of(context).textTheme.bodyMedium,
              ),
            ],
          ),
        ),
      ],
    );
  }
}

// ─── Address section ──────────────────────────────────────────────────────────

/// Expandable card for viewing and editing employee address information.
class _AddressSection extends StatefulWidget {
  const _AddressSection({required this.viewModel});

  final EmployeeProfileViewModel viewModel;

  @override
  State<_AddressSection> createState() => _AddressSectionState();
}

class _AddressSectionState extends State<_AddressSection> {
  final _formKey = GlobalKey<FormState>();
  final _zipCodeController = TextEditingController();
  final _streetController = TextEditingController();
  final _numberController = TextEditingController();
  final _complementController = TextEditingController();
  final _neighborhoodController = TextEditingController();
  final _cityController = TextEditingController();
  final _stateController = TextEditingController();
  final _countryController = TextEditingController();

  /// CEP mask: `#####-###`.
  final _cepMask = MaskTextInputFormatter(
    mask: '#####-###',
    filter: {'#': RegExp(r'[0-9]')},
    type: MaskAutoCompletionType.lazy,
  );

  bool _isEditing = false;

  @override
  void dispose() {
    _zipCodeController.dispose();
    _streetController.dispose();
    _numberController.dispose();
    _complementController.dispose();
    _neighborhoodController.dispose();
    _cityController.dispose();
    _stateController.dispose();
    _countryController.dispose();
    super.dispose();
  }

  void _startEdit() {
    final address = widget.viewModel.address;
    if (address == null) return;

    // Apply CEP mask to raw digits stored in the entity.
    final rawZip = address.zipCode.replaceAll(RegExp(r'[^\d]'), '');
    _cepMask.formatEditUpdate(
      TextEditingValue.empty,
      TextEditingValue(text: rawZip),
    );
    _zipCodeController.text = _cepMask.getMaskedText();

    _streetController.text = address.street;
    _numberController.text = address.number;
    _complementController.text = address.complement;
    _neighborhoodController.text = address.neighborhood;
    _cityController.text = address.city;
    _stateController.text = address.state.toUpperCase();
    _countryController.text = address.country;
    setState(() => _isEditing = true);
  }

  Future<void> _save() async {
    if (_formKey.currentState?.validate() != true) return;

    final address = Address(
      zipCode: _zipCodeController.text.trim(),
      street: _streetController.text.trim(),
      number: _numberController.text.trim(),
      complement: _complementController.text.trim(),
      neighborhood: _neighborhoodController.text.trim(),
      city: _cityController.text.trim(),
      state: _stateController.text.trim().toUpperCase(),
      country: _countryController.text.trim(),
    );
    await widget.viewModel.saveAddress(address);
    if (mounted && widget.viewModel.addressStatus == SectionLoadStatus.loaded) {
      setState(() => _isEditing = false);
    }
  }

  void _cancel() => setState(() => _isEditing = false);

  String? _validateCep(String? value) {
    if (value == null || value.trim().isEmpty) return 'CEP é obrigatório';
    final digits = value.replaceAll(RegExp(r'[^\d]'), '');
    if (digits.length != 8) return 'CEP inválido (ex: 01310-100)';
    return null;
  }

  String? _validateRequired(String? value, String label) {
    if (value == null || value.trim().isEmpty) return '$label é obrigatório';
    return null;
  }

  String? _validateState(String? value) {
    if (value == null || value.trim().isEmpty) return null;
    if (value.trim().length != 2) return 'Use a sigla de 2 letras (ex: SP)';
    return null;
  }

  @override
  Widget build(BuildContext context) {
    return ListenableBuilder(
      listenable: widget.viewModel,
      builder: (context, _) {
        final status = widget.viewModel.addressStatus;
        return _ExpandableSectionCard(
          title: 'Endereço',
          onExpand: widget.viewModel.loadAddress,
          child: _buildContent(context, status),
        );
      },
    );
  }

  Widget _buildContent(BuildContext context, SectionLoadStatus status) {
    if (status == SectionLoadStatus.notLoaded ||
        status == SectionLoadStatus.loading) {
      return const Padding(
        padding: EdgeInsets.symmetric(vertical: AppSpacing.md),
        child: Center(child: CircularProgressIndicator()),
      );
    }

    if (status == SectionLoadStatus.error) {
      return Padding(
        padding: const EdgeInsets.symmetric(vertical: AppSpacing.sm),
        child: Row(
          children: [
            Icon(
              Icons.error_outline,
              color: Theme.of(context).colorScheme.error,
              size: 20,
            ),
            const SizedBox(width: AppSpacing.sm),
            const Expanded(
              child: Text('Não foi possível carregar o endereço.'),
            ),
          ],
        ),
      );
    }

    final address = widget.viewModel.address;
    final isSaving = status == SectionLoadStatus.saving;

    if (_isEditing) {
      return _AddressEditForm(
        formKey: _formKey,
        zipCodeController: _zipCodeController,
        streetController: _streetController,
        numberController: _numberController,
        complementController: _complementController,
        neighborhoodController: _neighborhoodController,
        cityController: _cityController,
        stateController: _stateController,
        countryController: _countryController,
        cepMask: _cepMask,
        isSaving: isSaving,
        onSave: _save,
        onCancel: _cancel,
        validateCep: _validateCep,
        validateRequired: _validateRequired,
        validateState: _validateState,
      );
    }

    // ── View mode ────────────────────────────────────────────────────────────
    final hasAddress = address?.street.isNotEmpty == true;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Icon(
              Icons.location_on_outlined,
              size: 22,
              color: Theme.of(context).colorScheme.primary,
            ),
            const SizedBox(width: AppSpacing.md),
            Expanded(
              child: hasAddress
                  ? _AddressBlock(address: address!)
                  : Text(
                      'Não informado',
                      style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                            color:
                                Theme.of(context).colorScheme.onSurfaceVariant,
                          ),
                    ),
            ),
          ],
        ),
        const SizedBox(height: AppSpacing.sm),
        Align(
          alignment: Alignment.centerRight,
          child: TextButton.icon(
            onPressed: _startEdit,
            icon: const Icon(Icons.edit_outlined, size: 18),
            label: const Text('Editar'),
          ),
        ),
      ],
    );
  }
}

/// Renders a full address as a compact multi-line text block.
class _AddressBlock extends StatelessWidget {
  const _AddressBlock({required this.address});

  final Address address;

  @override
  Widget build(BuildContext context) {
    final tt = Theme.of(context).textTheme;
    final cs = Theme.of(context).colorScheme;
    final secondary = tt.bodySmall?.copyWith(color: cs.onSurfaceVariant);

    // Line 1: Street + number + optional complement
    final line1 = [
      address.street,
      if (address.number.isNotEmpty) address.number,
    ].join(', ') + (address.complement.isNotEmpty ? ' — ${address.complement}' : '');

    // Line 2: Neighborhood (only if present)
    final line2 = address.neighborhood;

    // Line 3: City – state, ZIP
    final rawZip = address.zipCode.replaceAll(RegExp(r'[^\d]'), '');
    final formattedZip = rawZip.length == 8
        ? '${rawZip.substring(0, 5)}-${rawZip.substring(5)}'
        : address.zipCode;
    final cityState = [
      if (address.city.isNotEmpty) address.city,
      if (address.state.isNotEmpty) address.state.toUpperCase(),
    ].join(' — ');
    final line3 = [
      if (cityState.isNotEmpty) cityState,
      if (formattedZip.isNotEmpty) formattedZip,
    ].join(', ');

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        if (line1.trim().isNotEmpty)
          Text(line1, style: tt.bodyMedium?.copyWith(fontWeight: FontWeight.w500)),
        if (line2.isNotEmpty) ...[
          const SizedBox(height: 2),
          Text(line2, style: secondary),
        ],
        if (line3.isNotEmpty) ...[
          const SizedBox(height: 2),
          Text(line3, style: secondary),
        ],
        if (address.country.isNotEmpty) ...[
          const SizedBox(height: 2),
          Text(address.country, style: secondary),
        ],
      ],
    );
  }
}

/// The address edit form, extracted to a [StatelessWidget] to keep
/// [_AddressSectionState] readable.
class _AddressEditForm extends StatelessWidget {
  const _AddressEditForm({
    required this.formKey,
    required this.zipCodeController,
    required this.streetController,
    required this.numberController,
    required this.complementController,
    required this.neighborhoodController,
    required this.cityController,
    required this.stateController,
    required this.countryController,
    required this.cepMask,
    required this.isSaving,
    required this.onSave,
    required this.onCancel,
    required this.validateCep,
    required this.validateRequired,
    required this.validateState,
  });

  final GlobalKey<FormState> formKey;
  final TextEditingController zipCodeController;
  final TextEditingController streetController;
  final TextEditingController numberController;
  final TextEditingController complementController;
  final TextEditingController neighborhoodController;
  final TextEditingController cityController;
  final TextEditingController stateController;
  final TextEditingController countryController;
  final MaskTextInputFormatter cepMask;
  final bool isSaving;
  final VoidCallback onSave;
  final VoidCallback onCancel;
  final String? Function(String?) validateCep;
  final String? Function(String?, String) validateRequired;
  final String? Function(String?) validateState;

  @override
  Widget build(BuildContext context) {
    return Form(
      key: formKey,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          // ── CEP ──────────────────────────────────────────────────────────
          TextFormField(
            controller: zipCodeController,
            enabled: !isSaving,
            decoration: const InputDecoration(
              labelText: 'CEP *',
              prefixIcon: Icon(Icons.markunread_mailbox_outlined),
              border: OutlineInputBorder(),
              helperText: 'Ex: 01310-100',
            ),
            keyboardType: TextInputType.number,
            inputFormatters: [cepMask],
            validator: validateCep,
          ),
          const SizedBox(height: AppSpacing.md),

          // ── Logradouro ───────────────────────────────────────────────────
          TextFormField(
            controller: streetController,
            enabled: !isSaving,
            decoration: const InputDecoration(
              labelText: 'Logradouro *',
              prefixIcon: Icon(Icons.signpost_outlined),
              border: OutlineInputBorder(),
              hintText: 'Rua, Avenida, Travessa…',
            ),
            textCapitalization: TextCapitalization.words,
            validator: (v) => validateRequired(v, 'Logradouro'),
          ),
          const SizedBox(height: AppSpacing.md),

          // ── Número + Complemento ─────────────────────────────────────────
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(
                flex: 2,
                child: TextFormField(
                  controller: numberController,
                  enabled: !isSaving,
                  decoration: const InputDecoration(
                    labelText: 'Número *',
                    border: OutlineInputBorder(),
                  ),
                  keyboardType: TextInputType.text,
                  validator: (v) => validateRequired(v, 'Número'),
                ),
              ),
              const SizedBox(width: AppSpacing.sm),
              Expanded(
                flex: 3,
                child: TextFormField(
                  controller: complementController,
                  enabled: !isSaving,
                  decoration: const InputDecoration(
                    labelText: 'Complemento',
                    border: OutlineInputBorder(),
                    hintText: 'Apto, Sala…',
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: AppSpacing.md),

          // ── Bairro ───────────────────────────────────────────────────────
          TextFormField(
            controller: neighborhoodController,
            enabled: !isSaving,
            decoration: const InputDecoration(
              labelText: 'Bairro',
              prefixIcon: Icon(Icons.holiday_village_outlined),
              border: OutlineInputBorder(),
            ),
            textCapitalization: TextCapitalization.words,
          ),
          const SizedBox(height: AppSpacing.md),

          // ── Cidade + Estado ──────────────────────────────────────────────
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(
                flex: 3,
                child: TextFormField(
                  controller: cityController,
                  enabled: !isSaving,
                  decoration: const InputDecoration(
                    labelText: 'Cidade *',
                    border: OutlineInputBorder(),
                  ),
                  textCapitalization: TextCapitalization.words,
                  validator: (v) => validateRequired(v, 'Cidade'),
                ),
              ),
              const SizedBox(width: AppSpacing.sm),
              Expanded(
                flex: 2,
                child: TextFormField(
                  controller: stateController,
                  enabled: !isSaving,
                  decoration: const InputDecoration(
                    labelText: 'UF',
                    border: OutlineInputBorder(),
                    hintText: 'SP',
                    counterText: '',
                  ),
                  textCapitalization: TextCapitalization.characters,
                  maxLength: 2,
                  validator: validateState,
                ),
              ),
            ],
          ),
          const SizedBox(height: AppSpacing.md),

          // ── País ─────────────────────────────────────────────────────────
          TextFormField(
            controller: countryController,
            enabled: !isSaving,
            decoration: const InputDecoration(
              labelText: 'País',
              prefixIcon: Icon(Icons.public_outlined),
              border: OutlineInputBorder(),
              hintText: 'Brasil',
            ),
            textCapitalization: TextCapitalization.words,
          ),
          const SizedBox(height: AppSpacing.md),

          // ── Actions ──────────────────────────────────────────────────────
          Row(
            mainAxisAlignment: MainAxisAlignment.end,
            children: [
              TextButton(
                onPressed: isSaving ? null : onCancel,
                child: const Text('Cancelar'),
              ),
              const SizedBox(width: AppSpacing.sm),
              isSaving
                  ? const SizedBox(
                      width: 24,
                      height: 24,
                      child: CircularProgressIndicator(strokeWidth: 2),
                    )
                  : FilledButton(
                      onPressed: onSave,
                      child: const Text('Salvar'),
                    ),
            ],
          ),
        ],
      ),
    );
  }
}

// ─── Personal info section ────────────────────────────────────────────────────

/// Returns [id] when it exists in [options]; otherwise null.
///
/// [DropdownButtonFormField] asserts that its `value` is either null or present
/// in the items list — this guard prevents that crash when the stored ID does
/// not match any option returned by the API.
String? _safeSelectionId(String? id, List<SelectionOption> options) {
  if (id == null || id.isEmpty) return null;
  return options.any((o) => o.id == id) ? id : null;
}

/// Returns the display name for [id] in [options], or `"Não informado"`.
String _selectionLabel(List<SelectionOption> options, String id) {
  if (id.isEmpty) return 'Não informado';
  return options.where((o) => o.id == id).firstOrNull?.name ?? 'Não informado';
}

/// Expandable card for viewing and editing employee personal information.
class _PersonalInfoSection extends StatefulWidget {
  const _PersonalInfoSection({required this.viewModel});

  final EmployeeProfileViewModel viewModel;

  @override
  State<_PersonalInfoSection> createState() => _PersonalInfoSectionState();
}

class _PersonalInfoSectionState extends State<_PersonalInfoSection> {
  String? _selectedGenderId;
  String? _selectedMaritalStatusId;
  String? _selectedEthnicityId;
  String? _selectedEducationLevelId;
  List<String> _selectedDisabilityIds = [];
  final _observationController = TextEditingController();
  final _formKey = GlobalKey<FormState>();
  bool _isEditing = false;

  @override
  void dispose() {
    _observationController.dispose();
    super.dispose();
  }

  void _startEdit() {
    final info = widget.viewModel.personalInfo;
    final options = widget.viewModel.personalInfoOptions;
    if (info == null || options == null) return;

    // Validate each stored ID against the loaded options list so the
    // dropdowns never receive a value that is absent from their items.
    _selectedGenderId = _safeSelectionId(info.genderId, options.genders);
    _selectedMaritalStatusId =
        _safeSelectionId(info.maritalStatusId, options.maritalStatuses);
    _selectedEthnicityId =
        _safeSelectionId(info.ethnicityId, options.ethnicities);
    _selectedEducationLevelId =
        _safeSelectionId(info.educationLevelId, options.educationLevels);
    _selectedDisabilityIds = info.disabilityIds
        .where((id) => options.disabilities.any((o) => o.id == id))
        .toList();
    _observationController.text = info.disabilityObservation;
    setState(() => _isEditing = true);
  }

  Future<void> _save() async {
    if (_formKey.currentState?.validate() != true) return;
    final personalInfo = EmployeePersonalInfo(
      genderId: _selectedGenderId ?? '',
      maritalStatusId: _selectedMaritalStatusId ?? '',
      ethnicityId: _selectedEthnicityId ?? '',
      educationLevelId: _selectedEducationLevelId ?? '',
      disabilityIds: _selectedDisabilityIds,
      disabilityObservation: _observationController.text.trim(),
    );
    await widget.viewModel.savePersonalInfo(personalInfo);
    if (mounted &&
        widget.viewModel.personalInfoStatus == SectionLoadStatus.loaded) {
      setState(() => _isEditing = false);
    }
  }

  void _cancel() => setState(() => _isEditing = false);

  /// Adds [id] to the selected disability list if not already present.
  void _addDisability(String id) {
    if (!_selectedDisabilityIds.contains(id)) {
      setState(() => _selectedDisabilityIds.add(id));
    }
  }

  /// Removes [id] from the selected disability list.
  void _removeDisability(String id) =>
      setState(() => _selectedDisabilityIds.remove(id));

  @override
  Widget build(BuildContext context) {
    return ListenableBuilder(
      listenable: widget.viewModel,
      builder: (context, _) {
        final status = widget.viewModel.personalInfoStatus;
        return _ExpandableSectionCard(
          title: 'Informações Pessoais',
          onExpand: widget.viewModel.loadPersonalInfo,
          child: _buildContent(context, status),
        );
      },
    );
  }

  Widget _buildContent(BuildContext context, SectionLoadStatus status) {
    if (status == SectionLoadStatus.notLoaded ||
        status == SectionLoadStatus.loading) {
      return const Padding(
        padding: EdgeInsets.symmetric(vertical: AppSpacing.md),
        child: Center(child: CircularProgressIndicator()),
      );
    }

    if (status == SectionLoadStatus.error) {
      return Padding(
        padding: const EdgeInsets.symmetric(vertical: AppSpacing.sm),
        child: Row(
          children: [
            Icon(
              Icons.error_outline,
              color: Theme.of(context).colorScheme.error,
              size: 20,
            ),
            const SizedBox(width: AppSpacing.sm),
            const Expanded(
              child:
                  Text('Não foi possível carregar as informações pessoais.'),
            ),
          ],
        ),
      );
    }

    final info = widget.viewModel.personalInfo;
    final options = widget.viewModel.personalInfoOptions;
    final isSaving = status == SectionLoadStatus.saving;

    if (_isEditing && options != null) {
      return _PersonalInfoEditForm(
        formKey: _formKey,
        selectedGenderId: _selectedGenderId,
        selectedMaritalStatusId: _selectedMaritalStatusId,
        selectedEthnicityId: _selectedEthnicityId,
        selectedEducationLevelId: _selectedEducationLevelId,
        selectedDisabilityIds: _selectedDisabilityIds,
        observationController: _observationController,
        options: options,
        isSaving: isSaving,
        onGenderChanged: (v) => setState(() => _selectedGenderId = v),
        onMaritalStatusChanged: (v) =>
            setState(() => _selectedMaritalStatusId = v),
        onEthnicityChanged: (v) => setState(() => _selectedEthnicityId = v),
        onEducationLevelChanged: (v) =>
            setState(() => _selectedEducationLevelId = v),
        onAddDisability: _addDisability,
        onRemoveDisability: _removeDisability,
        onSave: _save,
        onCancel: _cancel,
      );
    }

    // ── View mode ────────────────────────────────────────────────────────────
    final genderLabel = options != null
        ? _selectionLabel(options.genders, info?.genderId ?? '')
        : 'Não informado';
    final maritalLabel = options != null
        ? _selectionLabel(options.maritalStatuses, info?.maritalStatusId ?? '')
        : 'Não informado';
    final ethnicityLabel = options != null
        ? _selectionLabel(options.ethnicities, info?.ethnicityId ?? '')
        : 'Não informado';
    final educationLabel = options != null
        ? _selectionLabel(options.educationLevels, info?.educationLevelId ?? '')
        : 'Não informado';

    final disabilityLabels = (info?.disabilityIds.isNotEmpty == true &&
            options != null)
        ? info!.disabilityIds
            .map((id) => _selectionLabel(options.disabilities, id))
            .toList()
        : <String>[];

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        _ContactInfoRow(
          icon: Icons.wc_outlined,
          label: 'Gênero',
          value: genderLabel,
        ),
        const Divider(height: AppSpacing.xl),
        _ContactInfoRow(
          icon: Icons.favorite_border,
          label: 'Estado Civil',
          value: maritalLabel,
        ),
        const Divider(height: AppSpacing.xl),
        _ContactInfoRow(
          icon: Icons.people_outline,
          label: 'Etnia',
          value: ethnicityLabel,
        ),
        const Divider(height: AppSpacing.xl),
        _ContactInfoRow(
          icon: Icons.school_outlined,
          label: 'Escolaridade',
          value: educationLabel,
        ),
        const Divider(height: AppSpacing.xl),
        _PersonalInfoDisabilityRow(
          labels: disabilityLabels,
          observation: info?.disabilityObservation ?? '',
        ),
        const SizedBox(height: AppSpacing.sm),
        Align(
          alignment: Alignment.centerRight,
          child: TextButton.icon(
            onPressed: _isEditing
                ? null
                : (options != null ? _startEdit : null),
            icon: const Icon(Icons.edit_outlined, size: 18),
            label: const Text('Editar'),
          ),
        ),
      ],
    );
  }
}

// ─── Personal info helpers ─────────────────────────────────────────────────

/// View-mode row for the disabilities field with optional chip display.
class _PersonalInfoDisabilityRow extends StatelessWidget {
  const _PersonalInfoDisabilityRow({
    required this.labels,
    required this.observation,
  });

  final List<String> labels;
  final String observation;

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    final tt = Theme.of(context).textTheme;

    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Icon(Icons.accessibility_new_outlined, size: 22, color: cs.primary),
        const SizedBox(width: AppSpacing.md),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                'Deficiências',
                style: tt.labelSmall?.copyWith(
                  color: cs.onSurfaceVariant,
                  letterSpacing: 0.4,
                ),
              ),
              const SizedBox(height: AppSpacing.xs),
              if (labels.isEmpty)
                Text('Nenhuma declarada', style: tt.bodyMedium)
              else ...[
                Wrap(
                  spacing: AppSpacing.xs,
                  runSpacing: AppSpacing.xs,
                  children: labels
                      .map(
                        (l) => Chip(
                          label: Text(l),
                          padding: EdgeInsets.zero,
                          materialTapTargetSize:
                              MaterialTapTargetSize.shrinkWrap,
                          visualDensity: VisualDensity.compact,
                        ),
                      )
                      .toList(),
                ),
                if (observation.isNotEmpty) ...[
                  const SizedBox(height: AppSpacing.xs),
                  Text(observation,
                      style: tt.bodySmall
                          ?.copyWith(color: cs.onSurfaceVariant)),
                ],
              ],
            ],
          ),
        ),
      ],
    );
  }
}

/// Edit form for the personal info section, extracted to keep
/// [_PersonalInfoSectionState] concise.
///
/// Wraps all fields in a [Form] with validators for dropdowns (required) and
/// the observation field (max 100 characters). Disabilities are managed
/// via an add-dialog / remove-button pattern matching the rufino app UX.
class _PersonalInfoEditForm extends StatelessWidget {
  const _PersonalInfoEditForm({
    required this.formKey,
    required this.selectedGenderId,
    required this.selectedMaritalStatusId,
    required this.selectedEthnicityId,
    required this.selectedEducationLevelId,
    required this.selectedDisabilityIds,
    required this.observationController,
    required this.options,
    required this.isSaving,
    required this.onGenderChanged,
    required this.onMaritalStatusChanged,
    required this.onEthnicityChanged,
    required this.onEducationLevelChanged,
    required this.onAddDisability,
    required this.onRemoveDisability,
    required this.onSave,
    required this.onCancel,
  });

  final GlobalKey<FormState> formKey;
  final String? selectedGenderId;
  final String? selectedMaritalStatusId;
  final String? selectedEthnicityId;
  final String? selectedEducationLevelId;
  final List<String> selectedDisabilityIds;
  final TextEditingController observationController;
  final PersonalInfoOptions options;
  final bool isSaving;
  final ValueChanged<String?> onGenderChanged;
  final ValueChanged<String?> onMaritalStatusChanged;
  final ValueChanged<String?> onEthnicityChanged;
  final ValueChanged<String?> onEducationLevelChanged;

  /// Called with the id of the disability the user wants to add.
  final ValueChanged<String> onAddDisability;

  /// Called with the id of the disability the user wants to remove.
  final ValueChanged<String> onRemoveDisability;
  final VoidCallback onSave;
  final VoidCallback onCancel;

  /// Shows a dialog with a dropdown of unselected disabilities so the user
  /// can pick one to add.
  void _showAddDisabilityDialog(BuildContext context) {
    final available = options.disabilities
        .where((o) => !selectedDisabilityIds.contains(o.id))
        .toList();
    if (available.isEmpty) return;

    SelectionOption? chosen = available.first;
    showDialog<void>(
      context: context,
      builder: (dialogContext) => StatefulBuilder(
        builder: (ctx, setDialogState) => AlertDialog(
          title: const Text('Adicionar Deficiência'),
          content: SizedBox(
            width: 400,
            child: DropdownButtonFormField<SelectionOption>(
              value: chosen,
              isExpanded: true,
              decoration: const InputDecoration(
                labelText: 'Deficiência',
                border: OutlineInputBorder(),
              ),
              items: available
                  .map((o) => DropdownMenuItem(value: o, child: Text(o.name)))
                  .toList(),
              onChanged: (v) => setDialogState(() => chosen = v),
            ),
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.of(dialogContext).pop(),
              child: const Text('Cancelar'),
            ),
            FilledButton(
              onPressed: () {
                if (chosen != null) onAddDisability(chosen!.id);
                Navigator.of(dialogContext).pop();
              },
              child: const Text('Adicionar'),
            ),
          ],
        ),
      ),
    );
  }

  /// Builds a required dropdown field for a single enum property.
  DropdownButtonFormField<String> _dropdown({
    required String label,
    required IconData icon,
    required String? value,
    required List<SelectionOption> optionList,
    required ValueChanged<String?> onChanged,
  }) {
    return DropdownButtonFormField<String>(
      // Guard: pass null when the stored id is absent from the option list
      // to avoid Flutter's "value must be in items" assertion.
      value: _safeSelectionId(value, optionList),
      decoration: InputDecoration(
        labelText: label,
        prefixIcon: Icon(icon),
        border: const OutlineInputBorder(),
      ),
      hint: const Text('Não informado'),
      isExpanded: true,
      items: optionList
          .map((o) => DropdownMenuItem(value: o.id, child: Text(o.name)))
          .toList(),
      onChanged: isSaving ? null : onChanged,
      validator: (v) =>
          (v == null || v.isEmpty) ? 'Por favor, selecione uma opção.' : null,
    );
  }

  @override
  Widget build(BuildContext context) {
    final tt = Theme.of(context).textTheme;
    final cs = Theme.of(context).colorScheme;

    return Form(
      key: formKey,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          // ── Gênero ──────────────────────────────────────────────────────
          _dropdown(
            label: 'Gênero',
            icon: Icons.wc_outlined,
            value: selectedGenderId,
            optionList: options.genders,
            onChanged: onGenderChanged,
          ),
          const SizedBox(height: AppSpacing.md),

          // ── Estado Civil ────────────────────────────────────────────────
          _dropdown(
            label: 'Estado Civil',
            icon: Icons.favorite_border,
            value: selectedMaritalStatusId,
            optionList: options.maritalStatuses,
            onChanged: onMaritalStatusChanged,
          ),
          const SizedBox(height: AppSpacing.md),

          // ── Etnia ────────────────────────────────────────────────────────
          _dropdown(
            label: 'Etnia',
            icon: Icons.people_outline,
            value: selectedEthnicityId,
            optionList: options.ethnicities,
            onChanged: onEthnicityChanged,
          ),
          const SizedBox(height: AppSpacing.md),

          // ── Escolaridade ────────────────────────────────────────────────
          _dropdown(
            label: 'Escolaridade',
            icon: Icons.school_outlined,
            value: selectedEducationLevelId,
            optionList: options.educationLevels,
            onChanged: onEducationLevelChanged,
          ),
          const SizedBox(height: AppSpacing.lg),

          // ── Deficiências ─────────────────────────────────────────────────
          Row(
            children: [
              Icon(
                Icons.accessibility_new_outlined,
                size: 20,
                color: cs.onSurfaceVariant,
              ),
              const SizedBox(width: AppSpacing.sm),
              Text(
                'Deficiências',
                style: tt.labelLarge?.copyWith(color: cs.onSurfaceVariant),
              ),
            ],
          ),
          const SizedBox(height: AppSpacing.sm),
          if (selectedDisabilityIds.isEmpty)
            Padding(
              padding: const EdgeInsets.only(bottom: AppSpacing.sm),
              child: Text(
                'Nenhuma declarada',
                style: tt.bodyMedium?.copyWith(color: cs.onSurfaceVariant),
              ),
            )
          else
            ...selectedDisabilityIds.map((id) {
              final name = _selectionLabel(options.disabilities, id);
              return Padding(
                padding: const EdgeInsets.only(bottom: AppSpacing.xs),
                child: Row(
                  children: [
                    Expanded(child: Text(name, style: tt.bodyMedium)),
                    IconButton(
                      icon: Icon(Icons.close, size: 18, color: cs.error),
                      tooltip: 'Remover deficiência',
                      visualDensity: VisualDensity.compact,
                      onPressed:
                          isSaving ? null : () => onRemoveDisability(id),
                    ),
                  ],
                ),
              );
            }),
          const SizedBox(height: AppSpacing.sm),
          OutlinedButton.icon(
            onPressed: isSaving ||
                    selectedDisabilityIds.length == options.disabilities.length
                ? null
                : () => _showAddDisabilityDialog(context),
            icon: const Icon(Icons.add, size: 18),
            label: const Text('Adicionar Deficiência'),
          ),

          // Observation — shown only when at least one disability is selected.
          if (selectedDisabilityIds.isNotEmpty) ...[
            const SizedBox(height: AppSpacing.md),
            TextFormField(
              controller: observationController,
              enabled: !isSaving,
              decoration: const InputDecoration(
                labelText: 'Observações sobre a deficiência',
                prefixIcon: Icon(Icons.notes_outlined),
                border: OutlineInputBorder(),
                alignLabelWithHint: true,
                helperText: 'Máximo 100 caracteres',
                counterText: '',
              ),
              maxLines: 3,
              minLines: 2,
              maxLength: 100,
              textCapitalization: TextCapitalization.sentences,
              validator: (v) => (v != null && v.length > 100)
                  ? 'Observação não pode ultrapassar 100 caracteres.'
                  : null,
            ),
          ],
          const SizedBox(height: AppSpacing.md),

          // ── Actions ──────────────────────────────────────────────────────
          Row(
            mainAxisAlignment: MainAxisAlignment.end,
            children: [
              TextButton(
                onPressed: isSaving ? null : onCancel,
                child: const Text('Cancelar'),
              ),
              const SizedBox(width: AppSpacing.sm),
              isSaving
                  ? const SizedBox(
                      width: 24,
                      height: 24,
                      child: CircularProgressIndicator(strokeWidth: 2),
                    )
                  : FilledButton(
                      onPressed: onSave,
                      child: const Text('Salvar'),
                    ),
            ],
          ),
        ],
      ),
    );
  }
}

// ─── ID card section ──────────────────────────────────────────────────────────

/// Expandable card for viewing and editing employee ID card (Identidade) data.
class _IdCardSection extends StatefulWidget {
  const _IdCardSection({required this.viewModel});

  final EmployeeProfileViewModel viewModel;

  @override
  State<_IdCardSection> createState() => _IdCardSectionState();
}

class _IdCardSectionState extends State<_IdCardSection> {
  final _formKey = GlobalKey<FormState>();
  final _cpfController = TextEditingController();
  final _motherNameController = TextEditingController();
  final _fatherNameController = TextEditingController();
  final _dateOfBirthController = TextEditingController();
  final _birthCityController = TextEditingController();
  final _birthStateController = TextEditingController();
  final _nationalityController = TextEditingController();

  /// CPF mask: `###.###.###-##`.
  final _cpfMask = MaskTextInputFormatter(
    mask: '###.###.###-##',
    filter: {'#': RegExp(r'[0-9]')},
    type: MaskAutoCompletionType.lazy,
  );

  /// Date of birth mask: `##/##/####`.
  final _dobMask = MaskTextInputFormatter(
    mask: '##/##/####',
    filter: {'#': RegExp(r'[0-9]')},
    type: MaskAutoCompletionType.lazy,
  );

  bool _isEditing = false;

  @override
  void dispose() {
    _cpfController.dispose();
    _motherNameController.dispose();
    _fatherNameController.dispose();
    _dateOfBirthController.dispose();
    _birthCityController.dispose();
    _birthStateController.dispose();
    _nationalityController.dispose();
    super.dispose();
  }

  void _startEdit() {
    final idCard = widget.viewModel.idCard;
    if (idCard == null) return;

    // Apply CPF mask to the stored raw digits.
    final rawCpf = idCard.cpf.replaceAll(RegExp(r'[^\d]'), '');
    _cpfMask.formatEditUpdate(
      TextEditingValue.empty,
      TextEditingValue(text: rawCpf),
    );
    _cpfController.text = _cpfMask.getMaskedText();

    // Apply date-of-birth mask to the stored raw digits.
    final rawDob = idCard.dateOfBirth.replaceAll(RegExp(r'[^\d]'), '');
    _dobMask.formatEditUpdate(
      TextEditingValue.empty,
      TextEditingValue(text: rawDob),
    );
    _dateOfBirthController.text = _dobMask.getMaskedText();

    _motherNameController.text = idCard.motherName;
    _fatherNameController.text = idCard.fatherName;
    _birthCityController.text = idCard.birthCity;
    _birthStateController.text = idCard.birthState.toUpperCase();
    _nationalityController.text = idCard.nationality;
    setState(() => _isEditing = true);
  }

  Future<void> _save() async {
    if (_formKey.currentState?.validate() != true) return;
    final idCard = EmployeeIdCard(
      cpf: _cpfController.text.trim(),
      motherName: _motherNameController.text.trim(),
      fatherName: _fatherNameController.text.trim(),
      dateOfBirth: _dateOfBirthController.text.trim(),
      birthCity: _birthCityController.text.trim(),
      birthState: _birthStateController.text.trim().toUpperCase(),
      nationality: _nationalityController.text.trim(),
    );
    await widget.viewModel.saveIdCard(idCard);
    if (mounted && widget.viewModel.idCardStatus == SectionLoadStatus.loaded) {
      setState(() => _isEditing = false);
    }
  }

  void _cancel() => setState(() => _isEditing = false);

  /// Whether [cpf] passes the Brazilian CPF mathematical verification algorithm.
  ///
  /// Strips formatting before checking. Returns false for all-same-digit
  /// sequences (e.g. "111.111.111-11") and for any CPF whose check digits
  /// do not match the computed values.
  bool _isCpfValid(String cpf) {
    final digits = cpf.replaceAll(RegExp(r'[^\d]'), '');
    if (digits.length != 11) return false;
    // Reject sequences like "000.000.000-00", "111.111.111-11", etc.
    if (RegExp(r'^(\d)\1{10}$').hasMatch(digits)) return false;

    int firstSum = 0;
    for (int i = 0; i < 9; i++) {
      firstSum += int.parse(digits[i]) * (10 - i);
    }
    final mod1 = firstSum % 11;
    final d1 = mod1 < 2 ? 0 : 11 - mod1;
    if (d1 != int.parse(digits[9])) return false;

    int secondSum = 0;
    for (int i = 0; i < 10; i++) {
      secondSum += int.parse(digits[i]) * (11 - i);
    }
    final mod2 = secondSum % 11;
    final d2 = mod2 < 2 ? 0 : 11 - mod2;
    return d2 == int.parse(digits[10]);
  }

  String? _validateCpf(String? value) {
    if (value == null || value.trim().isEmpty) {
      return 'O CPF não pode ser vazio.';
    }
    if (value.trim().length > 100) {
      return 'O CPF não pode ser maior que 100 caracteres.';
    }
    if (!_isCpfValid(value)) {
      return 'O CPF não é válido.';
    }
    return null;
  }

  String? _validateDate(String? value) {
    if (value == null || value.trim().isEmpty) {
      return 'A Data de nascimento não pode ser vazia.';
    }
    final digits = value.replaceAll(RegExp(r'[^\d]'), '');
    if (digits.length != 8) {
      return 'Data inválida (ex: 15/06/1990)';
    }
    try {
      final parts = value.split('/');
      final isoDate = '${parts[2]}-${parts[1]}-${parts[0]}';
      final date = DateTime.tryParse(isoDate);
      final now = DateTime.now();
      final hundredYearsAgo = now.subtract(const Duration(days: 36500));
      if (date == null || date.isAfter(now) || date.isBefore(hundredYearsAgo)) {
        return 'A Data de nascimento é inválida.';
      }
    } catch (_) {
      return 'A Data de nascimento é inválida.';
    }
    return null;
  }

  String? _validateMotherName(String? value) {
    if (value == null || value.trim().isEmpty) {
      return 'O Nome da mãe não pode ser vazio.';
    }
    if (value.trim().length > 100) {
      return 'O Nome da mãe não pode ser maior que 100 caracteres.';
    }
    return null;
  }

  String? _validateFatherName(String? value) {
    if (value == null || value.trim().isEmpty) {
      return 'O Nome do pai não pode ser vazio.';
    }
    if (value.trim().length > 100) {
      return 'O Nome do pai não pode ser maior que 100 caracteres.';
    }
    return null;
  }

  String? _validateBirthCity(String? value) {
    if (value == null || value.trim().isEmpty) {
      return 'A Cidade de nascimento não pode ser vazia.';
    }
    if (value.trim().length > 100) {
      return 'A Cidade de nascimento não pode ser maior que 100 caracteres.';
    }
    return null;
  }

  String? _validateState(String? value) {
    if (value == null || value.trim().isEmpty) {
      return 'O Estado de nascimento não pode ser vazio.';
    }
    if (value.trim().length != 2) {
      return 'Use a sigla de 2 letras (ex: SP)';
    }
    return null;
  }

  String? _validateNationality(String? value) {
    if (value == null || value.trim().isEmpty) {
      return 'A Nacionalidade não pode ser vazia.';
    }
    if (value.trim().length > 100) {
      return 'A Nacionalidade não pode ser maior que 100 caracteres.';
    }
    return null;
  }

  @override
  Widget build(BuildContext context) {
    return ListenableBuilder(
      listenable: widget.viewModel,
      builder: (context, _) {
        final status = widget.viewModel.idCardStatus;
        return _ExpandableSectionCard(
          title: 'Documento (Identidade)',
          onExpand: widget.viewModel.loadIdCard,
          child: _buildContent(context, status),
        );
      },
    );
  }

  Widget _buildContent(BuildContext context, SectionLoadStatus status) {
    if (status == SectionLoadStatus.notLoaded ||
        status == SectionLoadStatus.loading) {
      return const Padding(
        padding: EdgeInsets.symmetric(vertical: AppSpacing.md),
        child: Center(child: CircularProgressIndicator()),
      );
    }

    if (status == SectionLoadStatus.error) {
      return Padding(
        padding: const EdgeInsets.symmetric(vertical: AppSpacing.sm),
        child: Row(
          children: [
            Icon(
              Icons.error_outline,
              color: Theme.of(context).colorScheme.error,
              size: 20,
            ),
            const SizedBox(width: AppSpacing.sm),
            const Expanded(
              child: Text('Não foi possível carregar os dados do documento.'),
            ),
          ],
        ),
      );
    }

    final idCard = widget.viewModel.idCard;
    final isSaving = status == SectionLoadStatus.saving;

    if (_isEditing) {
      return _IdCardEditForm(
        formKey: _formKey,
        cpfController: _cpfController,
        motherNameController: _motherNameController,
        fatherNameController: _fatherNameController,
        dateOfBirthController: _dateOfBirthController,
        birthCityController: _birthCityController,
        birthStateController: _birthStateController,
        nationalityController: _nationalityController,
        cpfMask: _cpfMask,
        dobMask: _dobMask,
        isSaving: isSaving,
        onSave: _save,
        onCancel: _cancel,
        validateCpf: _validateCpf,
        validateDate: _validateDate,
        validateMotherName: _validateMotherName,
        validateFatherName: _validateFatherName,
        validateBirthCity: _validateBirthCity,
        validateState: _validateState,
        validateNationality: _validateNationality,
      );
    }

    // ── View mode ─────────────────────────────────────────────────────────────
    final naturalidade = () {
      final city = idCard?.birthCity ?? '';
      final state = idCard?.birthState ?? '';
      if (city.isEmpty) return 'Não informado';
      return state.isNotEmpty ? '$city — ${state.toUpperCase()}' : city;
    }();

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        _ContactInfoRow(
          icon: Icons.badge_outlined,
          label: 'CPF',
          value: idCard?.cpf.isNotEmpty == true ? idCard!.cpf : 'Não informado',
        ),
        const Divider(height: AppSpacing.xl),
        _ContactInfoRow(
          icon: Icons.cake_outlined,
          label: 'Data de nascimento',
          value: idCard?.dateOfBirth.isNotEmpty == true
              ? idCard!.dateOfBirth
              : 'Não informado',
        ),
        const Divider(height: AppSpacing.xl),
        _ContactInfoRow(
          icon: Icons.person_outlined,
          label: 'Nome da mãe',
          value: idCard?.motherName.isNotEmpty == true
              ? idCard!.motherName
              : 'Não informado',
        ),
        const Divider(height: AppSpacing.xl),
        _ContactInfoRow(
          icon: Icons.person_outline,
          label: 'Nome do pai',
          value: idCard?.fatherName.isNotEmpty == true
              ? idCard!.fatherName
              : 'Não informado',
        ),
        const Divider(height: AppSpacing.xl),
        _ContactInfoRow(
          icon: Icons.location_city_outlined,
          label: 'Naturalidade',
          value: naturalidade,
        ),
        const Divider(height: AppSpacing.xl),
        _ContactInfoRow(
          icon: Icons.flag_outlined,
          label: 'Nacionalidade',
          value: idCard?.nationality.isNotEmpty == true
              ? idCard!.nationality
              : 'Não informado',
        ),
        const SizedBox(height: AppSpacing.sm),
        Align(
          alignment: Alignment.centerRight,
          child: TextButton.icon(
            onPressed: _startEdit,
            icon: const Icon(Icons.edit_outlined, size: 18),
            label: const Text('Editar'),
          ),
        ),
      ],
    );
  }
}

/// The ID card edit form, extracted to keep [_IdCardSectionState] readable.
class _IdCardEditForm extends StatelessWidget {
  const _IdCardEditForm({
    required this.formKey,
    required this.cpfController,
    required this.motherNameController,
    required this.fatherNameController,
    required this.dateOfBirthController,
    required this.birthCityController,
    required this.birthStateController,
    required this.nationalityController,
    required this.cpfMask,
    required this.dobMask,
    required this.isSaving,
    required this.onSave,
    required this.onCancel,
    required this.validateCpf,
    required this.validateDate,
    required this.validateMotherName,
    required this.validateFatherName,
    required this.validateBirthCity,
    required this.validateState,
    required this.validateNationality,
  });

  final GlobalKey<FormState> formKey;
  final TextEditingController cpfController;
  final TextEditingController motherNameController;
  final TextEditingController fatherNameController;
  final TextEditingController dateOfBirthController;
  final TextEditingController birthCityController;
  final TextEditingController birthStateController;
  final TextEditingController nationalityController;
  final MaskTextInputFormatter cpfMask;
  final MaskTextInputFormatter dobMask;
  final bool isSaving;
  final VoidCallback onSave;
  final VoidCallback onCancel;
  final String? Function(String?) validateCpf;
  final String? Function(String?) validateDate;
  final String? Function(String?) validateMotherName;
  final String? Function(String?) validateFatherName;
  final String? Function(String?) validateBirthCity;
  final String? Function(String?) validateState;
  final String? Function(String?) validateNationality;

  @override
  Widget build(BuildContext context) {
    return Form(
      key: formKey,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          // ── CPF ──────────────────────────────────────────────────────────
          TextFormField(
            controller: cpfController,
            enabled: !isSaving,
            decoration: const InputDecoration(
              labelText: 'CPF',
              prefixIcon: Icon(Icons.badge_outlined),
              border: OutlineInputBorder(),
              helperText: 'Ex: 123.456.789-00',
            ),
            keyboardType: TextInputType.number,
            inputFormatters: [cpfMask],
            validator: validateCpf,
          ),
          const SizedBox(height: AppSpacing.md),

          // ── Data de nascimento ────────────────────────────────────────────
          TextFormField(
            controller: dateOfBirthController,
            enabled: !isSaving,
            decoration: const InputDecoration(
              labelText: 'Data de nascimento',
              prefixIcon: Icon(Icons.cake_outlined),
              border: OutlineInputBorder(),
              helperText: 'Ex: 15/06/1990',
            ),
            keyboardType: TextInputType.datetime,
            inputFormatters: [dobMask],
            validator: validateDate,
          ),
          const SizedBox(height: AppSpacing.md),

          // ── Nome da mãe ───────────────────────────────────────────────────
          TextFormField(
            controller: motherNameController,
            enabled: !isSaving,
            decoration: const InputDecoration(
              labelText: 'Nome da mãe',
              prefixIcon: Icon(Icons.person_outlined),
              border: OutlineInputBorder(),
            ),
            textCapitalization: TextCapitalization.words,
            validator: validateMotherName,
          ),
          const SizedBox(height: AppSpacing.md),

          // ── Nome do pai ───────────────────────────────────────────────────
          TextFormField(
            controller: fatherNameController,
            enabled: !isSaving,
            decoration: const InputDecoration(
              labelText: 'Nome do pai',
              prefixIcon: Icon(Icons.person_outline),
              border: OutlineInputBorder(),
            ),
            textCapitalization: TextCapitalization.words,
            validator: validateFatherName,
          ),
          const SizedBox(height: AppSpacing.md),

          // ── Naturalidade ──────────────────────────────────────────────────
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(
                flex: 3,
                child: TextFormField(
                  controller: birthCityController,
                  enabled: !isSaving,
                  decoration: const InputDecoration(
                    labelText: 'Município de nascimento',
                    prefixIcon: Icon(Icons.location_city_outlined),
                    border: OutlineInputBorder(),
                  ),
                  textCapitalization: TextCapitalization.words,
                  validator: validateBirthCity,
                ),
              ),
              const SizedBox(width: AppSpacing.sm),
              Expanded(
                flex: 2,
                child: TextFormField(
                  controller: birthStateController,
                  enabled: !isSaving,
                  decoration: const InputDecoration(
                    labelText: 'UF',
                    border: OutlineInputBorder(),
                    hintText: 'SP',
                    counterText: '',
                  ),
                  textCapitalization: TextCapitalization.characters,
                  maxLength: 2,
                  validator: validateState,
                ),
              ),
            ],
          ),
          const SizedBox(height: AppSpacing.md),

          // ── Nacionalidade ─────────────────────────────────────────────────
          TextFormField(
            controller: nationalityController,
            enabled: !isSaving,
            decoration: const InputDecoration(
              labelText: 'Nacionalidade',
              prefixIcon: Icon(Icons.flag_outlined),
              border: OutlineInputBorder(),
            ),
            textCapitalization: TextCapitalization.words,
            validator: validateNationality,
          ),
          const SizedBox(height: AppSpacing.md),

          // ── Actions ───────────────────────────────────────────────────────
          Row(
            mainAxisAlignment: MainAxisAlignment.end,
            children: [
              TextButton(
                onPressed: isSaving ? null : onCancel,
                child: const Text('Cancelar'),
              ),
              const SizedBox(width: AppSpacing.sm),
              isSaving
                  ? const SizedBox(
                      width: 24,
                      height: 24,
                      child: CircularProgressIndicator(strokeWidth: 2),
                    )
                  : FilledButton(
                      onPressed: onSave,
                      child: const Text('Salvar'),
                    ),
            ],
          ),
        ],
      ),
    );
  }
}

// ─── Vote ID section ──────────────────────────────────────────────────────────

/// Expandable card for viewing and editing employee voter registration
/// (Título de Eleitor) data.
class _VoteIdSection extends StatefulWidget {
  const _VoteIdSection({required this.viewModel});

  final EmployeeProfileViewModel viewModel;

  @override
  State<_VoteIdSection> createState() => _VoteIdSectionState();
}

class _VoteIdSectionState extends State<_VoteIdSection> {
  final _formKey = GlobalKey<FormState>();
  final _numberController = TextEditingController();

  /// Vote ID mask: `####.####.####` (12 digits in three groups of four).
  final _voteIdMask = MaskTextInputFormatter(
    mask: '####.####.####',
    filter: {'#': RegExp(r'[0-9]')},
    type: MaskAutoCompletionType.lazy,
  );

  bool _isEditing = false;

  @override
  void dispose() {
    _numberController.dispose();
    super.dispose();
  }

  void _startEdit() {
    final voteId = widget.viewModel.voteId;
    if (voteId == null) return;

    // Apply mask to the stored raw digits so the field shows formatted text.
    final rawDigits = voteId.number.replaceAll(RegExp(r'[^\d]'), '');
    _voteIdMask.formatEditUpdate(
      TextEditingValue.empty,
      TextEditingValue(text: rawDigits),
    );
    _numberController.text = _voteIdMask.getMaskedText();
    setState(() => _isEditing = true);
  }

  Future<void> _save() async {
    if (_formKey.currentState?.validate() != true) return;
    await widget.viewModel.saveVoteId(_numberController.text.trim());
    if (mounted && widget.viewModel.voteIdStatus == SectionLoadStatus.loaded) {
      setState(() => _isEditing = false);
    }
  }

  void _cancel() => setState(() => _isEditing = false);

  /// Whether [number] passes the Brazilian voter registration (Título de
  /// Eleitor) mathematical verification algorithm.
  ///
  /// Strips formatting before checking. Returns false for all-same-digit
  /// sequences and for any number whose check digits do not match.
  bool _isVoteIdValid(String number) {
    final digits = number.replaceAll(RegExp(r'[^\d]'), '');
    if (digits.length != 12) return false;
    if (RegExp(r'^(\d)\1{11}$').hasMatch(digits)) return false;

    final uf = '${digits[8]}${digits[9]}';

    int sum = 0;
    const multiplierOne = [2, 3, 4, 5, 6, 7, 8, 9];
    for (int i = 0; i < 8; i++) {
      sum += int.parse(digits[i]) * multiplierOne[i];
    }
    int rest = sum % 11;
    if (rest > 9) {
      rest = 0;
    } else if (rest == 0 && (uf == '01' || uf == '02')) {
      rest = 1;
    }
    final z1 = rest.toString();

    sum = 0;
    const multiplierTwo = [7, 8, 9];
    final aux = '${digits[8]}${digits[9]}$z1';
    for (int i = 0; i < 3; i++) {
      sum += int.parse(aux[i]) * multiplierTwo[i];
    }
    rest = sum % 11;
    if (rest > 9) {
      rest = 0;
    } else if (rest == 0 && (uf == '01' || uf == '02')) {
      rest = 1;
    }
    final z2 = rest.toString();

    return digits.endsWith('$z1$z2');
  }

  String? _validateNumber(String? value) {
    final stripped = (value ?? '').replaceAll(RegExp(r'[^\d]'), '');
    if (stripped.isEmpty) {
      return 'O Número do título não pode ser vazio.';
    }
    if (stripped.length != 12) {
      return 'Número inválido (ex: 0000.0000.0000)';
    }
    if (!_isVoteIdValid(stripped)) {
      return 'O Número do título não é válido.';
    }
    return null;
  }

  @override
  Widget build(BuildContext context) {
    return ListenableBuilder(
      listenable: widget.viewModel,
      builder: (context, _) {
        final status = widget.viewModel.voteIdStatus;
        return _ExpandableSectionCard(
          title: 'Título de Eleitor',
          onExpand: widget.viewModel.loadVoteId,
          child: _buildContent(context, status),
        );
      },
    );
  }

  Widget _buildContent(BuildContext context, SectionLoadStatus status) {
    if (status == SectionLoadStatus.notLoaded ||
        status == SectionLoadStatus.loading) {
      return const Padding(
        padding: EdgeInsets.symmetric(vertical: AppSpacing.md),
        child: Center(child: CircularProgressIndicator()),
      );
    }

    if (status == SectionLoadStatus.error) {
      return Padding(
        padding: const EdgeInsets.symmetric(vertical: AppSpacing.sm),
        child: Row(
          children: [
            Icon(
              Icons.error_outline,
              color: Theme.of(context).colorScheme.error,
              size: 20,
            ),
            const SizedBox(width: AppSpacing.sm),
            const Expanded(
              child: Text('Não foi possível carregar o título de eleitor.'),
            ),
          ],
        ),
      );
    }

    final voteId = widget.viewModel.voteId;
    final isSaving = status == SectionLoadStatus.saving;

    if (_isEditing) {
      return Form(
        key: _formKey,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            TextFormField(
              controller: _numberController,
              enabled: !isSaving,
              decoration: const InputDecoration(
                labelText: 'Número do título',
                prefixIcon: Icon(Icons.how_to_vote_outlined),
                border: OutlineInputBorder(),
                helperText: 'Ex: 0000.0000.0000',
              ),
              keyboardType: TextInputType.number,
              inputFormatters: [_voteIdMask],
              validator: _validateNumber,
            ),
            const SizedBox(height: AppSpacing.md),
            Row(
              mainAxisAlignment: MainAxisAlignment.end,
              children: [
                TextButton(
                  onPressed: isSaving ? null : _cancel,
                  child: const Text('Cancelar'),
                ),
                const SizedBox(width: AppSpacing.sm),
                isSaving
                    ? const SizedBox(
                        width: 24,
                        height: 24,
                        child: CircularProgressIndicator(strokeWidth: 2),
                      )
                    : FilledButton(
                        onPressed: _save,
                        child: const Text('Salvar'),
                      ),
              ],
            ),
          ],
        ),
      );
    }

    // ── View mode ─────────────────────────────────────────────────────────────
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        _ContactInfoRow(
          icon: Icons.how_to_vote_outlined,
          label: 'Número do título',
          value: voteId?.number.isNotEmpty == true
              ? voteId!.number
              : 'Não informado',
        ),
        const SizedBox(height: AppSpacing.sm),
        Align(
          alignment: Alignment.centerRight,
          child: TextButton.icon(
            onPressed: _startEdit,
            icon: const Icon(Icons.edit_outlined, size: 18),
            label: const Text('Editar'),
          ),
        ),
      ],
    );
  }
}
