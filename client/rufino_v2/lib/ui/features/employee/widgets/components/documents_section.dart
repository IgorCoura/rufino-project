import 'package:flutter/material.dart';
import 'package:mask_text_input_formatter/mask_text_input_formatter.dart';

import '../../../../../core/theme/app_spacing.dart';
import '../../../../../domain/entities/employee_document.dart';
import '../../viewmodel/employee_profile_viewmodel.dart';
import 'profile_shared_widgets.dart';

/// Expandable card that displays the employee's required documents with nested
/// expansion for document units, status badges, pagination, and basic CRUD
/// actions (edit date, mark as not applicable, add new unit).
class DocumentsSection extends StatefulWidget {
  const DocumentsSection({super.key, required this.viewModel});

  /// The profile view model that owns the documents state and exposes
  /// load / mutate operations.
  final EmployeeProfileViewModel viewModel;

  @override
  State<DocumentsSection> createState() => _DocumentsSectionState();
}

class _DocumentsSectionState extends State<DocumentsSection> {
  /// Tracks which document ids are currently expanded.
  final Set<String> _expandedDocIds = {};

  /// Tracks the current pagination page per document id.
  final Map<String, int> _pageMap = {};

  // ─── Status badge helpers ──────────────────────────────────────────────────

  /// Returns a display label and color for a document-level status.
  ({String label, Color color}) _documentStatus(String statusId) {
    return switch (statusId) {
      '1' => (label: 'Pendente', color: Colors.orange),
      '2' => (label: 'Requer Validacao', color: Colors.amber),
      '3' => (label: 'OK', color: Colors.green),
      '4' => (label: 'Obsoleto', color: Colors.grey),
      '5' => (label: 'Aguardando Assinatura', color: Colors.blue),
      _ => (label: statusId, color: Colors.grey),
    };
  }

  /// Returns a color for a document-unit-level status.
  Color _unitStatusColor(String statusId) {
    return switch (statusId) {
      '1' => Colors.orange,
      '2' => Colors.green,
      '3' => Colors.grey,
      '4' => Colors.red,
      '5' => Colors.amber,
      '6' => Colors.blueGrey,
      '7' => Colors.blue,
      _ => Colors.grey,
    };
  }

  // ─── Dialogs ───────────────────────────────────────────────────────────────

  /// Shows a dialog to edit the date of a document unit.
  Future<void> _showEditDateDialog(
    EmployeeDocument doc,
    DocumentUnit unit,
  ) async {
    final dateCtrl = TextEditingController(text: unit.date);
    final dateMask = MaskTextInputFormatter(
      mask: '##/##/####',
      filter: {'#': RegExp(r'[0-9]')},
      type: MaskAutoCompletionType.lazy,
    );

    // Pre-fill the mask with the existing raw digits.
    final rawDigits = unit.date.replaceAll(RegExp(r'[^\d]'), '');
    dateMask.formatEditUpdate(
      TextEditingValue.empty,
      TextEditingValue(text: rawDigits),
    );
    dateCtrl.text = dateMask.getMaskedText();

    final formKey = GlobalKey<FormState>();

    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) {
        return AlertDialog(
          title: const Text('Editar data'),
          content: Form(
            key: formKey,
            child: TextFormField(
              controller: dateCtrl,
              decoration: const InputDecoration(
                labelText: 'Data',
                prefixIcon: Icon(Icons.event_outlined),
                border: OutlineInputBorder(),
                helperText: 'Ex: 15/03/2026',
              ),
              keyboardType: TextInputType.number,
              inputFormatters: [dateMask],
              validator: widget.viewModel.validateContractDate,
            ),
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.of(ctx).pop(false),
              child: const Text('Cancelar'),
            ),
            FilledButton(
              onPressed: () {
                if (formKey.currentState?.validate() == true) {
                  Navigator.of(ctx).pop(true);
                }
              },
              child: const Text('Salvar'),
            ),
          ],
        );
      },
    );

    if (confirmed == true && mounted) {
      await widget.viewModel.editDocumentUnitDate(
        doc.id,
        unit.id,
        dateCtrl.text.trim(),
      );
    }

    dateCtrl.dispose();
  }

  /// Shows a confirmation dialog before marking a unit as not applicable.
  Future<void> _showNotApplicableDialog(
    EmployeeDocument doc,
    DocumentUnit unit,
  ) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) {
        return AlertDialog(
          title: const Text('Confirmar'),
          content: const Text(
            'Deseja marcar este documento como nao aplicavel?',
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.of(ctx).pop(false),
              child: const Text('Cancelar'),
            ),
            FilledButton(
              onPressed: () => Navigator.of(ctx).pop(true),
              child: const Text('Confirmar'),
            ),
          ],
        );
      },
    );

    if (confirmed == true && mounted) {
      await widget.viewModel.setDocumentUnitNotApplicable(doc.id, unit.id);
    }
  }

  // ─── Build ─────────────────────────────────────────────────────────────────

  @override
  Widget build(BuildContext context) {
    return ListenableBuilder(
      listenable: widget.viewModel,
      builder: (context, _) {
        final status = widget.viewModel.documentsStatus;
        return ExpandableSectionCard(
          title: 'Documentos',
          onExpand: widget.viewModel.loadDocuments,
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
              child: Text('Nao foi possivel carregar os documentos.'),
            ),
          ],
        ),
      );
    }

    final documents = widget.viewModel.documents;

    if (documents.isEmpty) {
      return const Padding(
        padding: EdgeInsets.symmetric(vertical: AppSpacing.md),
        child: Center(child: Text('Nenhum documento encontrado.')),
      );
    }

    return Column(
      children: documents.map((doc) => _buildDocumentTile(context, doc)).toList(),
    );
  }

  Widget _buildDocumentTile(BuildContext context, EmployeeDocument doc) {
    final badgeInfo = _documentStatus(doc.statusId);
    final isExpanded = _expandedDocIds.contains(doc.id);

    return Padding(
      padding: const EdgeInsets.only(bottom: AppSpacing.sm),
      child: Card.outlined(
        clipBehavior: Clip.antiAlias,
        child: ExpansionTile(
          title: Text(
            doc.name,
            style: Theme.of(context).textTheme.titleSmall,
          ),
          trailing: Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              _StatusBadge(label: badgeInfo.label, color: badgeInfo.color),
              const SizedBox(width: AppSpacing.xs),
              Icon(
                isExpanded
                    ? Icons.expand_less
                    : Icons.expand_more,
              ),
            ],
          ),
          onExpansionChanged: (expanded) {
            setState(() {
              if (expanded) {
                _expandedDocIds.add(doc.id);
                widget.viewModel.loadDocumentUnits(doc.id);
              } else {
                _expandedDocIds.remove(doc.id);
              }
            });
          },
          childrenPadding: const EdgeInsets.fromLTRB(
            AppSpacing.md,
            0,
            AppSpacing.md,
            AppSpacing.md,
          ),
          children: [
            _buildUnitsList(context, doc),
          ],
        ),
      ),
    );
  }

  Widget _buildUnitsList(BuildContext context, EmployeeDocument doc) {
    final units = doc.units;

    if (units.isEmpty) {
      return const Padding(
        padding: EdgeInsets.symmetric(vertical: AppSpacing.sm),
        child: Center(child: Text('Nenhuma unidade encontrada.')),
      );
    }

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        ...units.map((unit) => _buildUnitRow(context, doc, unit)),
        const SizedBox(height: AppSpacing.sm),
        Align(
          alignment: Alignment.centerLeft,
          child: TextButton.icon(
            onPressed: () => widget.viewModel.createDocumentUnit(doc.id),
            icon: const Icon(Icons.add, size: 18),
            label: const Text('Adicionar'),
          ),
        ),
        if (doc.totalUnitsCount > units.length) ...[
          const SizedBox(height: AppSpacing.sm),
          _buildPaginationRow(context, doc),
        ],
      ],
    );
  }

  Widget _buildUnitRow(
    BuildContext context,
    EmployeeDocument doc,
    DocumentUnit unit,
  ) {
    final statusColor = _unitStatusColor(unit.statusId);
    final cs = Theme.of(context).colorScheme;

    return Padding(
      padding: const EdgeInsets.only(bottom: AppSpacing.xs),
      child: Card.outlined(
        child: Padding(
          padding: const EdgeInsets.symmetric(
            horizontal: AppSpacing.md,
            vertical: AppSpacing.sm,
          ),
          child: Row(
            children: [
              Container(
                width: 12,
                height: 12,
                decoration: BoxDecoration(
                  color: statusColor,
                  shape: BoxShape.circle,
                ),
              ),
              const SizedBox(width: AppSpacing.md),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      unit.statusName,
                      style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                            fontWeight: FontWeight.w600,
                          ),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      'Data: ${unit.date}',
                      style: Theme.of(context).textTheme.bodySmall?.copyWith(
                            color: cs.onSurfaceVariant,
                          ),
                    ),
                    if (unit.validity.isNotEmpty)
                      Text(
                        'Validade: ${unit.validity}',
                        style: Theme.of(context).textTheme.bodySmall?.copyWith(
                              color: cs.onSurfaceVariant,
                            ),
                      ),
                  ],
                ),
              ),
              PopupMenuButton<String>(
                onSelected: (action) async {
                  switch (action) {
                    case 'edit_date':
                      await _showEditDateDialog(doc, unit);
                    case 'not_applicable':
                      await _showNotApplicableDialog(doc, unit);
                  }
                },
                itemBuilder: (_) => const [
                  PopupMenuItem(
                    value: 'edit_date',
                    child: Text('Editar data'),
                  ),
                  PopupMenuItem(
                    value: 'not_applicable',
                    child: Text('Nao aplicavel'),
                  ),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildPaginationRow(BuildContext context, EmployeeDocument doc) {
    final cs = Theme.of(context).colorScheme;

    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: [
        Text(
          'Mostrando ${doc.units.length} de ${doc.totalUnitsCount}',
          style: Theme.of(context).textTheme.bodySmall?.copyWith(
                color: cs.onSurfaceVariant,
              ),
        ),
        TextButton(
          onPressed: () {
            final currentPage = _pageMap[doc.id] ?? 1;
            _pageMap[doc.id] = currentPage + 1;
            widget.viewModel.loadDocumentUnits(
              doc.id,
              pageNumber: currentPage + 1,
            );
          },
          child: const Text('Carregar mais'),
        ),
      ],
    );
  }
}

/// Small coloured badge used to display a document or unit status.
class _StatusBadge extends StatelessWidget {
  const _StatusBadge({required this.label, required this.color});

  final String label;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(
        horizontal: AppSpacing.sm,
        vertical: AppSpacing.xs,
      ),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.15),
        borderRadius: BorderRadius.circular(12),
      ),
      child: Text(
        label,
        style: Theme.of(context).textTheme.labelSmall?.copyWith(
              color: color,
              fontWeight: FontWeight.w600,
            ),
      ),
    );
  }
}
