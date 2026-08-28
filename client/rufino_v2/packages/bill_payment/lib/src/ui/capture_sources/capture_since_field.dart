import 'package:flutter/material.dart';
import 'package:rufino_core/rufino_core.dart';

import '../shared/formats.dart';

/// How far back a brand-new source reads by default.
///
/// The field is pre-filled with it so the common case — "do not drag years
/// of history in" — needs no discovery. A measured mailbox took ~7 minutes
/// and 404 attachments on its first full sweep; most of that was not a bill.
/// Someone who does want the whole archive clears the field.
const int kDefaultCaptureSinceDays = 90;

/// The default floor for a source being connected now.
DateTime defaultCaptureSince(DateTime today) =>
    DateUtils.dateOnly(today).subtract(
      const Duration(days: kDefaultCaptureSinceDays),
    );

/// Picks the time floor of a capture source: read nothing received before
/// this date.
///
/// Empty means the whole mailbox, and that is a legitimate choice — the
/// field never forces a date. The provider is what applies the cut
/// (`receivedDateTime ge {date}`), so an empty value costs nothing extra.
class CaptureSinceField extends StatelessWidget {
  /// Creates the field.
  const CaptureSinceField({
    super.key,
    required this.value,
    required this.onChanged,
    this.enabled = true,
    this.helperText,
  });

  /// The floor in force, or `null` for the whole mailbox.
  final DateTime? value;

  /// Called with the new floor. `null` clears it.
  final ValueChanged<DateTime?> onChanged;

  /// Whether the field accepts interaction.
  final bool enabled;

  /// Overrides the explanation shown under the field.
  final String? helperText;

  Future<void> _pick(BuildContext context) async {
    final today = DateUtils.dateOnly(DateTime.now());

    final picked = await showDatePicker(
      context: context,
      initialDate: value ?? defaultCaptureSince(today),
      // A data mais antiga é arbitrária; o teto NÃO é: piso no futuro
      // descreve uma fonte que não captura nada, e o servidor recusa com
      // BLP.CPS20. Impedir aqui evita levar o usuário a um erro previsível.
      firstDate: DateTime(today.year - 10),
      lastDate: today,
      helpText: 'Ler e-mails a partir de',
    );

    if (picked != null) onChanged(DateUtils.dateOnly(picked));
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        InputDecorator(
          decoration: InputDecoration(
            labelText: 'Ler e-mails a partir de',
            border: const OutlineInputBorder(),
            enabled: enabled,
            suffixIcon: Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                if (value != null)
                  IconButton(
                    icon: const Icon(Icons.clear),
                    tooltip: 'Ler a caixa inteira',
                    onPressed: enabled ? () => onChanged(null) : null,
                  ),
                IconButton(
                  icon: const Icon(Icons.calendar_today),
                  tooltip: 'Escolher data',
                  onPressed: enabled ? () => _pick(context) : null,
                ),
              ],
            ),
          ),
          child: Text(
            value == null ? 'Caixa inteira' : formatDate(value!),
            style: theme.textTheme.bodyLarge,
          ),
        ),
        const SizedBox(height: AppSpacing.xs),
        Text(
          helperText ??
              'Nada recebido antes desta data é lido. Deixe em branco para '
                  'ler a caixa inteira.',
          style: theme.textTheme.bodySmall?.copyWith(
            color: theme.colorScheme.onSurfaceVariant,
          ),
        ),
      ],
    );
  }
}
