import 'package:flutter/material.dart';
import 'package:mask_text_input_formatter/mask_text_input_formatter.dart';

import '../../../../../core/theme/app_spacing.dart';
import '../../viewmodel/employee_profile_viewmodel.dart';
import 'profile_shared_widgets.dart';

/// Expandable card for viewing and editing employee voter registration
/// (Titulo de Eleitor) data.
class VoteIdSection extends StatefulWidget {
  const VoteIdSection({super.key, required this.viewModel});

  final EmployeeProfileViewModel viewModel;

  @override
  State<VoteIdSection> createState() => _VoteIdSectionState();
}

class _VoteIdSectionState extends State<VoteIdSection> {
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

  @override
  Widget build(BuildContext context) {
    return ListenableBuilder(
      listenable: widget.viewModel,
      builder: (context, _) {
        final status = widget.viewModel.voteIdStatus;
        return ExpandableSectionCard(
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
              validator: widget.viewModel.validateVoteIdNumber,
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
        ContactInfoRow(
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
