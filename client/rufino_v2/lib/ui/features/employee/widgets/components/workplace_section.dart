import 'package:flutter/material.dart';

import '../../../../../core/theme/app_spacing.dart';
import '../../../../../domain/entities/workplace.dart';
import '../../viewmodel/employee_profile_viewmodel.dart';
import 'profile_shared_widgets.dart';

/// Expandable card for viewing and editing the employee's workplace assignment
/// (Local de Trabalho).
///
/// View mode displays the current workplace name and address. Edit mode shows
/// a dropdown to select from all available workplaces with an address preview.
class WorkplaceSection extends StatefulWidget {
  const WorkplaceSection({super.key, required this.viewModel});

  final EmployeeProfileViewModel viewModel;

  @override
  State<WorkplaceSection> createState() => _WorkplaceSectionState();
}

class _WorkplaceSectionState extends State<WorkplaceSection> {
  bool _isEditing = false;
  String? _selectedWorkplaceId;

  void _startEdit() {
    final workplaceId = widget.viewModel.profile?.workplaceId ?? '';
    setState(() {
      _isEditing = true;
      _selectedWorkplaceId = workplaceId.isNotEmpty ? workplaceId : null;
    });
  }

  Future<void> _save() async {
    if (_selectedWorkplaceId == null || _selectedWorkplaceId!.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
            content: Text('Por favor, selecione um local de trabalho.')),
      );
      return;
    }
    await widget.viewModel.saveEmployeeWorkplace(_selectedWorkplaceId!);
    if (mounted &&
        widget.viewModel.workplaceInfoStatus == SectionLoadStatus.loaded) {
      setState(() => _isEditing = false);
    }
  }

  void _cancel() => setState(() => _isEditing = false);

  @override
  Widget build(BuildContext context) {
    return ListenableBuilder(
      listenable: widget.viewModel,
      builder: (context, _) {
        final status = widget.viewModel.workplaceInfoStatus;
        return ExpandableSectionCard(
          title: 'Local de Trabalho',
          onExpand: widget.viewModel.loadWorkplaceInfo,
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
            Icon(Icons.error_outline,
                color: Theme.of(context).colorScheme.error, size: 20),
            const SizedBox(width: AppSpacing.sm),
            const Expanded(
              child:
                  Text('Não foi possível carregar o local de trabalho.'),
            ),
          ],
        ),
      );
    }

    final isSaving = status == SectionLoadStatus.saving;
    final workplaces = widget.viewModel.allWorkplaces;
    final workplaceId = widget.viewModel.profile?.workplaceId ?? '';

    if (_isEditing) {
      return _buildEditMode(context, workplaces, isSaving);
    }

    return _buildViewMode(context, workplaces, workplaceId);
  }

  Widget _buildViewMode(
    BuildContext context,
    List<Workplace> workplaces,
    String workplaceId,
  ) {
    final current =
        workplaces.where((w) => w.id == workplaceId).firstOrNull;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        ContactInfoRow(
          icon: Icons.business_outlined,
          label: 'Local de trabalho',
          value: current?.name ?? 'Não informado',
        ),
        if (current != null) ...[
          const SizedBox(height: AppSpacing.xs),
          ContactInfoRow(
            icon: Icons.location_on_outlined,
            label: 'Endereço',
            value: current.address.inlineSummary,
          ),
        ],
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

  Widget _buildEditMode(
    BuildContext context,
    List<Workplace> workplaces,
    bool isSaving,
  ) {
    final selectedWorkplace = workplaces
        .where((w) => w.id == _selectedWorkplaceId)
        .firstOrNull;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        DropdownButtonFormField<String>(
          initialValue: _selectedWorkplaceId,
          decoration: const InputDecoration(
            labelText: 'Local de trabalho',
            prefixIcon: Icon(Icons.business_outlined),
            border: OutlineInputBorder(),
          ),
          items: workplaces
              .map<DropdownMenuItem<String>>((w) =>
                  DropdownMenuItem<String>(value: w.id, child: Text(w.name)))
              .toList(),
          onChanged: isSaving
              ? null
              : (id) => setState(() => _selectedWorkplaceId = id),
        ),
        if (selectedWorkplace != null) ...[
          const SizedBox(height: AppSpacing.sm),
          Card.filled(
            child: Padding(
              padding: const EdgeInsets.all(AppSpacing.sm),
              child: ContactInfoRow(
                icon: Icons.location_on_outlined,
                label: 'Endereço',
                value: selectedWorkplace.address.inlineSummary,
              ),
            ),
          ),
        ],
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
    );
  }
}
