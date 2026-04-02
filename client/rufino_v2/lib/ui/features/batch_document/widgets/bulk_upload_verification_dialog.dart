/// Full-screen dialog for verifying bulk file-to-employee assignments.
///
/// Displays each uploaded file with its matched employee, confidence level,
/// and an inline PDF preview. Supports manual reassignment via a searchable
/// dropdown and confirms staging when the user is satisfied.
library;

import 'package:flutter/material.dart';
import 'package:syncfusion_flutter_pdfviewer/pdfviewer.dart';

import '../../../../core/theme/app_breakpoints.dart';
import '../../../../core/theme/app_spacing.dart';
import '../../../../core/utils/fuzzy_name_matcher.dart';
import '../../../../domain/entities/batch_document_unit.dart';
import '../../../../domain/entities/bulk_upload_match.dart';
import '../viewmodel/batch_document_viewmodel.dart';

/// Displays the bulk upload verification dialog.
///
/// Shows a split view on wide screens (file list + preview) and a single
/// column with expandable cards on narrow screens. The user reviews each
/// file's assignment and can reassign or remove entries before confirming.
class BulkUploadVerificationDialog extends StatefulWidget {
  /// Creates the verification dialog.
  const BulkUploadVerificationDialog({super.key, required this.viewModel});

  /// The view model managing bulk upload state.
  final BatchDocumentViewModel viewModel;

  @override
  State<BulkUploadVerificationDialog> createState() =>
      _BulkUploadVerificationDialogState();
}

class _BulkUploadVerificationDialogState
    extends State<BulkUploadVerificationDialog> {
  int _selectedIndex = 0;

  BatchDocumentViewModel get _vm => widget.viewModel;

  @override
  void initState() {
    super.initState();
    _vm.addListener(_onChanged);
  }

  @override
  void dispose() {
    _vm.removeListener(_onChanged);
    super.dispose();
  }

  void _onChanged() {
    if (!mounted) return;
    setState(() {
      if (_selectedIndex >= _vm.bulkMatches.length) {
        _selectedIndex = _vm.bulkMatches.isEmpty ? 0 : _vm.bulkMatches.length - 1;
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    final width = MediaQuery.sizeOf(context).width;
    final isWide = width >= AppBreakpoints.tablet;

    return Dialog.fullscreen(
      child: Scaffold(
        appBar: AppBar(
          leading: IconButton(
            icon: const Icon(Icons.close),
            onPressed: () {
              _vm.cancelBulkMatches();
              Navigator.of(context).pop();
            },
          ),
          title: const Text('Verificação de Upload em Lote'),
          centerTitle: false,
          actions: [
            TextButton(
              onPressed: () {
                _vm.cancelBulkMatches();
                Navigator.of(context).pop();
              },
              child: const Text('Cancelar'),
            ),
            const SizedBox(width: AppSpacing.sm),
            FilledButton.icon(
              onPressed: _vm.bulkMatchedCount == 0
                  ? null
                  : () {
                      _vm.confirmBulkMatches();
                      Navigator.of(context).pop();
                    },
              icon: const Icon(Icons.check, size: 18),
              label: Text('Confirmar (${_vm.bulkMatchedCount})'),
            ),
            const SizedBox(width: AppSpacing.md),
          ],
        ),
        body: Column(
          children: [
            Expanded(
              child: _vm.bulkMatches.isEmpty
                  ? const Center(
                      child: Text('Nenhum arquivo para verificar.'),
                    )
                  : isWide
                      ? _buildWideLayout(context)
                      : _buildNarrowLayout(context),
            ),
            _SummaryBar(vm: _vm),
          ],
        ),
      ),
    );
  }

  Widget _buildWideLayout(BuildContext context) {
    return Row(
      children: [
        SizedBox(
          width: 380,
          child: _FileList(
            matches: _vm.bulkMatches,
            selectedIndex: _selectedIndex,
            onSelect: (index) => setState(() => _selectedIndex = index),
            onRemove: (index) => _vm.removeBulkMatch(index),
          ),
        ),
        const VerticalDivider(width: 1),
        Expanded(
          child: _selectedIndex < _vm.bulkMatches.length
              ? _PreviewPanel(
                  match: _vm.bulkMatches[_selectedIndex],
                  index: _selectedIndex,
                  pendingUnits: _vm.pendingUnits,
                  bulkMatches: _vm.bulkMatches,
                  onReassign: (unit) =>
                      _vm.reassignBulkMatch(_selectedIndex, unit),
                )
              : const SizedBox.shrink(),
        ),
      ],
    );
  }

  Widget _buildNarrowLayout(BuildContext context) {
    return ListView.builder(
      padding: const EdgeInsets.all(AppSpacing.md),
      itemCount: _vm.bulkMatches.length,
      itemBuilder: (context, index) {
        final match = _vm.bulkMatches[index];
        return _NarrowMatchCard(
          match: match,
          index: index,
          pendingUnits: _vm.pendingUnits,
          bulkMatches: _vm.bulkMatches,
          onReassign: (unit) => _vm.reassignBulkMatch(index, unit),
          onRemove: () => _vm.removeBulkMatch(index),
        );
      },
    );
  }
}

// ─── File List (wide layout, left panel) ──────────────────

/// Scrollable list of matched files for the wide layout.
class _FileList extends StatelessWidget {
  const _FileList({
    required this.matches,
    required this.selectedIndex,
    required this.onSelect,
    required this.onRemove,
  });

  final List<BulkUploadMatch> matches;
  final int selectedIndex;
  final ValueChanged<int> onSelect;
  final ValueChanged<int> onRemove;

  @override
  Widget build(BuildContext context) {
    return ListView.separated(
      padding: const EdgeInsets.symmetric(vertical: AppSpacing.sm),
      itemCount: matches.length,
      separatorBuilder: (_, __) => const Divider(height: 1),
      itemBuilder: (context, index) {
        final match = matches[index];
        final isSelected = index == selectedIndex;
        final colorScheme = Theme.of(context).colorScheme;

        return ListTile(
          selected: isSelected,
          selectedTileColor: colorScheme.primaryContainer.withValues(alpha: 0.15),
          leading: _ConfidenceIndicator(level: match.confidenceLevel),
          title: Text(
            match.fileName,
            overflow: TextOverflow.ellipsis,
            style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                  fontWeight: FontWeight.w600,
                ),
          ),
          subtitle: Text(
            match.isMatched
                ? match.matchedEmployeeName!
                : 'Sem correspondência',
            overflow: TextOverflow.ellipsis,
            style: Theme.of(context).textTheme.bodySmall?.copyWith(
                  color: match.isMatched
                      ? colorScheme.onSurfaceVariant
                      : colorScheme.error,
                ),
          ),
          trailing: Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              if (match.isMatched)
                Text(
                  '${(match.confidence * 100).round()}%',
                  style: Theme.of(context).textTheme.labelSmall?.copyWith(
                        color: _confidenceColor(match.confidenceLevel, colorScheme),
                      ),
                ),
              const SizedBox(width: AppSpacing.xs),
              IconButton(
                icon: const Icon(Icons.close, size: 18),
                tooltip: 'Remover',
                onPressed: () => onRemove(index),
                visualDensity: VisualDensity.compact,
              ),
            ],
          ),
          onTap: () => onSelect(index),
        );
      },
    );
  }
}

// ─── Preview Panel (wide layout, right panel) ─────────────

/// Shows the selected file's details with a PDF or text preview.
class _PreviewPanel extends StatefulWidget {
  const _PreviewPanel({
    required this.match,
    required this.index,
    required this.pendingUnits,
    required this.bulkMatches,
    required this.onReassign,
  });

  final BulkUploadMatch match;
  final int index;
  final List<BatchDocumentUnitItem> pendingUnits;
  final List<BulkUploadMatch> bulkMatches;
  final ValueChanged<BatchDocumentUnitItem> onReassign;

  @override
  State<_PreviewPanel> createState() => _PreviewPanelState();
}

class _PreviewPanelState extends State<_PreviewPanel> {
  bool _showPdf = true;

  @override
  Widget build(BuildContext context) {
    final match = widget.match;
    final colorScheme = Theme.of(context).colorScheme;
    final textTheme = Theme.of(context).textTheme;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        // ── Pinned employee header ──
        Container(
          padding: const EdgeInsets.all(AppSpacing.md),
          color: colorScheme.surfaceContainerHigh,
          child: Row(
            children: [
              _ConfidenceIndicator(level: match.confidenceLevel, size: 32),
              const SizedBox(width: AppSpacing.md),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      match.fileName,
                      style: textTheme.titleMedium,
                      overflow: TextOverflow.ellipsis,
                    ),
                    const SizedBox(height: AppSpacing.xs),
                    Text(
                      match.isMatched
                          ? 'Funcionário: ${match.matchedEmployeeName}'
                          : 'Sem correspondência',
                      style: textTheme.bodyMedium?.copyWith(
                        fontWeight: FontWeight.w600,
                        color: match.isMatched
                            ? colorScheme.onSurface
                            : colorScheme.error,
                      ),
                    ),
                    if (match.isMatched)
                      Text(
                        'Confiança: ${(match.confidence * 100).round()}%',
                        style: textTheme.bodySmall?.copyWith(
                          color: _confidenceColor(
                              match.confidenceLevel, colorScheme),
                        ),
                      ),
                  ],
                ),
              ),
            ],
          ),
        ),
        // ── View mode toggle ──
        Padding(
          padding: const EdgeInsets.fromLTRB(
            AppSpacing.md,
            AppSpacing.sm,
            AppSpacing.md,
            0,
          ),
          child: Row(
            children: [
              SegmentedButton<bool>(
                segments: const [
                  ButtonSegment(
                    value: true,
                    icon: Icon(Icons.picture_as_pdf_outlined, size: 18),
                    label: Text('PDF'),
                  ),
                  ButtonSegment(
                    value: false,
                    icon: Icon(Icons.text_snippet_outlined, size: 18),
                    label: Text('Texto'),
                  ),
                ],
                selected: {_showPdf},
                onSelectionChanged: (v) =>
                    setState(() => _showPdf = v.first),
                style: const ButtonStyle(
                  visualDensity: VisualDensity.compact,
                  tapTargetSize: MaterialTapTargetSize.shrinkWrap,
                ),
              ),
            ],
          ),
        ),
        // ── Preview content ──
        Expanded(
          child: Padding(
            padding: const EdgeInsets.all(AppSpacing.sm),
            child: _showPdf
                ? Container(
                    decoration: BoxDecoration(
                      color: Colors.white,
                      border: Border.all(color: colorScheme.outlineVariant),
                      borderRadius: BorderRadius.circular(AppSpacing.sm),
                    ),
                    clipBehavior: Clip.antiAlias,
                    child: SfPdfViewer.memory(
                      match.fileBytes,
                      key: ValueKey(match.fileName),
                      canShowScrollHead: false,
                      canShowPaginationDialog: false,
                      pageSpacing: 4,
                    ),
                  )
                : Container(
                    width: double.infinity,
                    padding: const EdgeInsets.all(AppSpacing.md),
                    decoration: BoxDecoration(
                      color: colorScheme.surfaceContainerLow,
                      borderRadius: BorderRadius.circular(AppSpacing.sm),
                      border: Border.all(color: colorScheme.outlineVariant),
                    ),
                    child: match.extractedText.isEmpty
                        ? Center(
                            child: Text(
                              'Texto não encontrado no PDF.\nSelecione o funcionário manualmente.',
                              textAlign: TextAlign.center,
                              style: textTheme.bodyMedium?.copyWith(
                                color: colorScheme.onSurfaceVariant,
                              ),
                            ),
                          )
                        : SingleChildScrollView(
                            child: SelectableText(
                              match.extractedText,
                              style: textTheme.bodySmall?.copyWith(
                                fontFamily: 'monospace',
                                color: colorScheme.onSurface,
                              ),
                            ),
                          ),
                  ),
          ),
        ),
        // ── Reassignment dropdown ──
        Padding(
          padding: const EdgeInsets.fromLTRB(
            AppSpacing.md,
            0,
            AppSpacing.md,
            AppSpacing.md,
          ),
          child: _EmployeeReassignDropdown(
            pendingUnits: widget.pendingUnits,
            bulkMatches: widget.bulkMatches,
            currentUnitId: match.matchedDocumentUnitId,
            onSelected: widget.onReassign,
          ),
        ),
      ],
    );
  }
}

// ─── Narrow Match Card (mobile layout) ────────────────────

/// Expandable card for narrow screens showing match details inline.
class _NarrowMatchCard extends StatefulWidget {
  const _NarrowMatchCard({
    required this.match,
    required this.index,
    required this.pendingUnits,
    required this.bulkMatches,
    required this.onReassign,
    required this.onRemove,
  });

  final BulkUploadMatch match;
  final int index;
  final List<BatchDocumentUnitItem> pendingUnits;
  final List<BulkUploadMatch> bulkMatches;
  final ValueChanged<BatchDocumentUnitItem> onReassign;
  final VoidCallback onRemove;

  @override
  State<_NarrowMatchCard> createState() => _NarrowMatchCardState();
}

class _NarrowMatchCardState extends State<_NarrowMatchCard> {
  bool _isExpanded = false;
  bool _showPdf = true;

  @override
  Widget build(BuildContext context) {
    final match = widget.match;
    final colorScheme = Theme.of(context).colorScheme;
    final textTheme = Theme.of(context).textTheme;
    final borderColor = _confidenceColor(match.confidenceLevel, colorScheme);

    return Card.outlined(
      margin: const EdgeInsets.only(bottom: AppSpacing.sm),
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(AppSpacing.sm),
        side: BorderSide(color: borderColor, width: 2),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          // Header — always visible.
          InkWell(
            onTap: () => setState(() => _isExpanded = !_isExpanded),
            borderRadius: BorderRadius.circular(AppSpacing.sm),
            child: Padding(
              padding: const EdgeInsets.all(AppSpacing.md),
              child: Row(
                children: [
                  _ConfidenceIndicator(level: match.confidenceLevel),
                  const SizedBox(width: AppSpacing.sm),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          match.fileName,
                          style: textTheme.bodyMedium?.copyWith(
                            fontWeight: FontWeight.w600,
                          ),
                          overflow: TextOverflow.ellipsis,
                        ),
                        const SizedBox(height: 2),
                        Text(
                          match.isMatched
                              ? '${match.matchedEmployeeName} · ${(match.confidence * 100).round()}%'
                              : 'Sem correspondência',
                          style: textTheme.bodySmall?.copyWith(
                            color: match.isMatched
                                ? colorScheme.onSurfaceVariant
                                : colorScheme.error,
                          ),
                          overflow: TextOverflow.ellipsis,
                        ),
                      ],
                    ),
                  ),
                  IconButton(
                    icon: const Icon(Icons.close, size: 18),
                    tooltip: 'Remover',
                    onPressed: widget.onRemove,
                    visualDensity: VisualDensity.compact,
                  ),
                  Icon(
                    _isExpanded ? Icons.expand_less : Icons.expand_more,
                    color: colorScheme.onSurfaceVariant,
                  ),
                ],
              ),
            ),
          ),
          // Expanded content.
          if (_isExpanded) ...[
            const Divider(height: 1),
            Padding(
              padding: const EdgeInsets.all(AppSpacing.md),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      SegmentedButton<bool>(
                        segments: const [
                          ButtonSegment(
                            value: true,
                            icon: Icon(Icons.picture_as_pdf_outlined, size: 18),
                            label: Text('PDF'),
                          ),
                          ButtonSegment(
                            value: false,
                            icon: Icon(Icons.text_snippet_outlined, size: 18),
                            label: Text('Texto'),
                          ),
                        ],
                        selected: {_showPdf},
                        onSelectionChanged: (v) =>
                            setState(() => _showPdf = v.first),
                        style: const ButtonStyle(
                          visualDensity: VisualDensity.compact,
                          tapTargetSize: MaterialTapTargetSize.shrinkWrap,
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: AppSpacing.sm),
                  if (_showPdf)
                    Container(
                      width: double.infinity,
                      height: 300,
                      decoration: BoxDecoration(
                        color: Colors.white,
                        border: Border.all(color: colorScheme.outlineVariant),
                        borderRadius: BorderRadius.circular(AppSpacing.xs),
                      ),
                      clipBehavior: Clip.antiAlias,
                      child: SfPdfViewer.memory(
                        match.fileBytes,
                        key: ValueKey(match.fileName),
                        canShowScrollHead: false,
                        canShowPaginationDialog: false,
                        pageSpacing: 4,
                      ),
                    )
                  else
                    Container(
                      width: double.infinity,
                      constraints: const BoxConstraints(maxHeight: 300),
                      padding: const EdgeInsets.all(AppSpacing.sm),
                      decoration: BoxDecoration(
                        color: colorScheme.surfaceContainerLow,
                        borderRadius: BorderRadius.circular(AppSpacing.xs),
                        border: Border.all(color: colorScheme.outlineVariant),
                      ),
                      child: match.extractedText.isEmpty
                          ? Text(
                              'Texto não encontrado no PDF.',
                              style: textTheme.bodySmall?.copyWith(
                                color: colorScheme.onSurfaceVariant,
                              ),
                            )
                          : SingleChildScrollView(
                              child: SelectableText(
                                match.extractedText,
                                style: textTheme.bodySmall?.copyWith(
                                  fontFamily: 'monospace',
                                ),
                              ),
                            ),
                    ),
                  const SizedBox(height: AppSpacing.md),
                  _EmployeeReassignDropdown(
                    pendingUnits: widget.pendingUnits,
                    bulkMatches: widget.bulkMatches,
                    currentUnitId: match.matchedDocumentUnitId,
                    onSelected: widget.onReassign,
                  ),
                ],
              ),
            ),
          ],
        ],
      ),
    );
  }
}

// ─── Employee Reassignment Dropdown ───────────────────────

/// Searchable dropdown for assigning a file to a different employee.
class _EmployeeReassignDropdown extends StatefulWidget {
  const _EmployeeReassignDropdown({
    required this.pendingUnits,
    required this.bulkMatches,
    required this.currentUnitId,
    required this.onSelected,
  });

  final List<BatchDocumentUnitItem> pendingUnits;
  final List<BulkUploadMatch> bulkMatches;
  final String? currentUnitId;
  final ValueChanged<BatchDocumentUnitItem> onSelected;

  @override
  State<_EmployeeReassignDropdown> createState() =>
      _EmployeeReassignDropdownState();
}

class _EmployeeReassignDropdownState extends State<_EmployeeReassignDropdown> {
  String _searchQuery = '';

  /// Returns pending units not already assigned to other bulk matches.
  List<BatchDocumentUnitItem> get _availableUnits {
    final assignedIds = widget.bulkMatches
        .where((m) =>
            m.matchedDocumentUnitId != null &&
            m.matchedDocumentUnitId != widget.currentUnitId)
        .map((m) => m.matchedDocumentUnitId!)
        .toSet();

    return widget.pendingUnits
        .where((u) => !assignedIds.contains(u.documentUnitId))
        .where((u) => _searchQuery.isEmpty ||
            u.employeeName.toLowerCase().contains(_searchQuery.toLowerCase()))
        .toList();
  }

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;
    final textTheme = Theme.of(context).textTheme;
    final units = _availableUnits;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Text(
          'Atribuir a funcionário:',
          style: textTheme.labelMedium?.copyWith(
            color: colorScheme.onSurfaceVariant,
          ),
        ),
        const SizedBox(height: AppSpacing.sm),
        TextField(
          decoration: const InputDecoration(
            hintText: 'Buscar funcionário...',
            prefixIcon: Icon(Icons.search, size: 20),
            isDense: true,
            border: OutlineInputBorder(),
            contentPadding: EdgeInsets.symmetric(
              horizontal: AppSpacing.sm,
              vertical: AppSpacing.sm,
            ),
          ),
          onChanged: (v) => setState(() => _searchQuery = v),
        ),
        const SizedBox(height: AppSpacing.sm),
        ConstrainedBox(
          constraints: const BoxConstraints(maxHeight: 180),
          child: units.isEmpty
              ? Padding(
                  padding: const EdgeInsets.all(AppSpacing.md),
                  child: Text(
                    'Nenhum funcionário disponível.',
                    style: textTheme.bodySmall?.copyWith(
                      color: colorScheme.onSurfaceVariant,
                    ),
                  ),
                )
              : ListView.builder(
                  shrinkWrap: true,
                  itemCount: units.length,
                  itemBuilder: (context, index) {
                    final unit = units[index];
                    final isCurrentMatch =
                        unit.documentUnitId == widget.currentUnitId;

                    return ListTile(
                      dense: true,
                      selected: isCurrentMatch,
                      selectedTileColor:
                          colorScheme.primaryContainer.withValues(alpha: 0.2),
                      leading: CircleAvatar(
                        radius: 14,
                        backgroundColor: colorScheme.primaryContainer,
                        child: Text(
                          unit.employeeName.isNotEmpty
                              ? unit.employeeName[0].toUpperCase()
                              : '?',
                          style: textTheme.labelSmall?.copyWith(
                            color: colorScheme.onPrimaryContainer,
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                      ),
                      title: Text(
                        unit.employeeName,
                        overflow: TextOverflow.ellipsis,
                        style: textTheme.bodySmall,
                      ),
                      trailing: isCurrentMatch
                          ? Icon(
                              Icons.check_circle,
                              size: 18,
                              color: colorScheme.primary,
                            )
                          : null,
                      onTap: () => widget.onSelected(unit),
                    );
                  },
                ),
        ),
      ],
    );
  }
}

// ─── Summary Bar ──────────────────────────────────────────

/// Displays match statistics at the bottom of the dialog.
class _SummaryBar extends StatelessWidget {
  const _SummaryBar({required this.vm});

  final BatchDocumentViewModel vm;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;
    final textTheme = Theme.of(context).textTheme;

    final total = vm.bulkMatches.length;
    final matched = vm.bulkMatchedCount;
    final needsReview =
        vm.bulkMatches.where((m) => m.isMatched && m.needsReview).length;
    final unmatched = vm.bulkUnmatchedCount;

    return Container(
      padding: const EdgeInsets.symmetric(
        horizontal: AppSpacing.md,
        vertical: AppSpacing.sm,
      ),
      decoration: BoxDecoration(
        color: colorScheme.surfaceContainerHigh,
        border: Border(top: BorderSide(color: colorScheme.outlineVariant)),
      ),
      child: Wrap(
        spacing: AppSpacing.md,
        runSpacing: AppSpacing.xs,
        alignment: WrapAlignment.center,
        children: [
          _summaryChip(
            '$total arquivo${total != 1 ? 's' : ''}',
            colorScheme.onSurface,
            textTheme,
          ),
          _summaryChip(
            '$matched correspondido${matched != 1 ? 's' : ''}',
            colorScheme.primary,
            textTheme,
          ),
          if (needsReview > 0)
            _summaryChip(
              '$needsReview revisão${needsReview != 1 ? 'ões' : ''}',
              Colors.amber.shade700,
              textTheme,
            ),
          if (unmatched > 0)
            _summaryChip(
              '$unmatched sem correspondência',
              colorScheme.error,
              textTheme,
            ),
        ],
      ),
    );
  }

  Widget _summaryChip(String label, Color color, TextTheme textTheme) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Container(
          width: 8,
          height: 8,
          decoration: BoxDecoration(color: color, shape: BoxShape.circle),
        ),
        const SizedBox(width: AppSpacing.xs),
        Text(
          label,
          style: textTheme.labelSmall?.copyWith(color: color),
        ),
      ],
    );
  }
}

// ─── Shared Widgets ───────────────────────────────────────

/// Circular confidence indicator with colour coding.
class _ConfidenceIndicator extends StatelessWidget {
  const _ConfidenceIndicator({required this.level, this.size = 24});

  final MatchConfidenceLevel level;
  final double size;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;
    final color = _confidenceColor(level, colorScheme);

    final IconData icon;
    switch (level) {
      case MatchConfidenceLevel.high:
        icon = Icons.check_circle;
      case MatchConfidenceLevel.medium:
        icon = Icons.help;
      case MatchConfidenceLevel.low:
        icon = Icons.warning;
      case MatchConfidenceLevel.none:
        icon = Icons.error;
    }

    return Icon(icon, color: color, size: size);
  }
}

/// Returns the colour associated with a [MatchConfidenceLevel].
Color _confidenceColor(MatchConfidenceLevel level, ColorScheme colorScheme) {
  switch (level) {
    case MatchConfidenceLevel.high:
      return colorScheme.primary;
    case MatchConfidenceLevel.medium:
      return Colors.amber.shade700;
    case MatchConfidenceLevel.low:
      return colorScheme.error.withValues(alpha: 0.7);
    case MatchConfidenceLevel.none:
      return colorScheme.error;
  }
}
