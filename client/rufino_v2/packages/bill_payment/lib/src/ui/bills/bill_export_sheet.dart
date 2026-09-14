import 'package:flutter/material.dart';
import 'package:material_symbols_icons/symbols.dart';
import 'package:rufino_core/rufino_core.dart';

import '../../domain/bill.dart';
import '../../domain/bill_payment_enums.dart';
import 'bill_list_viewmodel.dart';

/// Abre a folha de opções do download em lote e devolve o que foi escolhido —
/// nulo quando a pessoa desiste.
Future<BillDocumentExportOptions?> showBillExportSheet(
  BuildContext context,
  List<Bill> bills,
) {
  return showModalBottomSheet<BillDocumentExportOptions>(
    context: context,
    isScrollControlled: true,
    showDragHandle: true,
    builder: (_) => BillExportSheet(bills: bills),
  );
}

/// As três escolhas do download: quanto do boleto, se anexa os comprovantes e
/// se sai num PDF só ou num PDF por boleto.
class BillExportSheet extends StatefulWidget {
  /// Cria a folha para os [bills] selecionados.
  const BillExportSheet({super.key, required this.bills});

  /// Os boletos selecionados, na ordem em que foram marcados.
  final List<Bill> bills;

  @override
  State<BillExportSheet> createState() => _BillExportSheetState();
}

class _BillExportSheetState extends State<BillExportSheet> {
  String _pages = BillDocumentPages.all;
  bool _includeReceipts = false;
  String _packaging = BillDocumentPackagings.singlePdf;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final count = widget.bills.length;
    final withoutDocument =
        widget.bills.where((b) => !b.origin.hasArtifact).length;
    final paid =
        widget.bills.where((b) => b.status == BillStatuses.paid).length;

    return SafeArea(
      child: SingleChildScrollView(
        padding: const EdgeInsets.fromLTRB(
          AppSpacing.lg,
          0,
          AppSpacing.lg,
          AppSpacing.lg,
        ),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              count == 1
                  ? 'Baixar documentos de 1 boleto'
                  : 'Baixar documentos de $count boletos',
              style: theme.textTheme.titleLarge,
            ),
            const SizedBox(height: AppSpacing.md),
            Text('Conteúdo do boleto', style: theme.textTheme.titleSmall),
            RadioGroup<String>(
              groupValue: _pages,
              onChanged: (value) => setState(() => _pages = value!),
              child: const Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  RadioListTile<String>(
                    value: BillDocumentPages.all,
                    title: Text('Documento inteiro'),
                    contentPadding: EdgeInsets.zero,
                  ),
                  RadioListTile<String>(
                    value: BillDocumentPages.firstPage,
                    title: Text('Somente a primeira página'),
                    contentPadding: EdgeInsets.zero,
                  ),
                ],
              ),
            ),
            const Divider(),
            CheckboxListTile(
              value: _includeReceipts,
              onChanged: (value) =>
                  setState(() => _includeReceipts = value ?? false),
              title: const Text('Anexar comprovantes de pagamento'),
              subtitle: Text(
                '$paid de $count ${count == 1 ? 'boleto está pago' : 'boletos estão pagos'}. '
                'O comprovante entra inteiro, logo depois do boleto.',
              ),
              controlAffinity: ListTileControlAffinity.leading,
              contentPadding: EdgeInsets.zero,
            ),
            const Divider(),
            Text('Formato', style: theme.textTheme.titleSmall),
            RadioGroup<String>(
              groupValue: _packaging,
              onChanged: (value) => setState(() => _packaging = value!),
              child: const Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  RadioListTile<String>(
                    value: BillDocumentPackagings.singlePdf,
                    title: Text('Um PDF único'),
                    subtitle: Text('Na ordem em que os boletos foram marcados.'),
                    contentPadding: EdgeInsets.zero,
                  ),
                  RadioListTile<String>(
                    value: BillDocumentPackagings.pdfPerBill,
                    title: Text('Um PDF por boleto'),
                    subtitle: Text('Baixa um arquivo .zip.'),
                    contentPadding: EdgeInsets.zero,
                  ),
                ],
              ),
            ),
            if (withoutDocument > 0) ...[
              const SizedBox(height: AppSpacing.sm),
              _Notice(
                text: withoutDocument == 1
                    ? '1 boleto não tem documento: entra uma página de aviso '
                        'com a origem e o código para pagamento.'
                    : '$withoutDocument boletos não têm documento: entra uma '
                        'página de aviso com a origem e o código para pagamento.',
              ),
            ],
            const SizedBox(height: AppSpacing.lg),
            Row(
              mainAxisAlignment: MainAxisAlignment.end,
              children: [
                TextButton(
                  onPressed: () => Navigator.of(context).pop(),
                  child: const Text('Cancelar'),
                ),
                const SizedBox(width: AppSpacing.sm),
                FilledButton.icon(
                  onPressed: () => Navigator.of(context).pop(
                    (
                      pages: _pages,
                      includeReceipts: _includeReceipts,
                      packaging: _packaging,
                    ),
                  ),
                  icon: const Icon(Symbols.download),
                  label: const Text('Baixar'),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

class _Notice extends StatelessWidget {
  const _Notice({required this.text});

  final String text;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Icon(Symbols.info, size: 20, color: theme.colorScheme.tertiary),
        const SizedBox(width: AppSpacing.sm),
        Expanded(child: Text(text, style: theme.textTheme.bodySmall)),
      ],
    );
  }
}
