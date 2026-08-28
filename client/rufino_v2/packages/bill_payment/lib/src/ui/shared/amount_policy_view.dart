import 'package:flutter/material.dart';
import 'package:material_symbols_icons/symbols.dart';
import 'package:rufino_core/rufino_core.dart';

import '../../domain/bill_payment_enums.dart';
import '../../domain/payee.dart';
import 'formats.dart';

/// The accepted window of a fixed policy, or `null` for the other kinds.
///
/// **Mirrors `AmountPolicy.Matches` on the server** — there the rule is
/// `|actual − expected| <= |expected| * tolerance / 100`. If that formula
/// changes, this changes with it: showing a window the server would refuse is
/// worse than showing no window at all.
({double from, double to})? amountPolicyWindow(AmountPolicy policy) {
  if (policy.kind != AmountPolicyKinds.fixed) return null;

  final expected = policy.expectedAmount;
  final tolerance = policy.tolerancePercent;
  if (expected == null || tolerance == null) return null;

  final allowed = expected.abs() * (tolerance / 100);
  return (from: expected - allowed, to: expected + allowed);
}

/// One line describing [policy], for list rows and badges.
///
/// Carries the numbers the person informed: a badge reading only "Faixa de
/// valores" says nothing that the payee's own row did not already say.
String amountPolicySummary(AmountPolicy policy) => switch (policy.kind) {
      AmountPolicyKinds.fixed => policy.expectedAmount == null
          ? 'Valor fixo'
          : 'Fixo ${formatMoney(policy.expectedAmount)}'
              '${_toleranceSuffix(policy)}',
      AmountPolicyKinds.range =>
        policy.minAmount == null || policy.maxAmount == null
            ? 'Faixa de valores'
            : 'Faixa ${formatMoney(policy.minAmount)} – '
                '${formatMoney(policy.maxAmount)}',
      AmountPolicyKinds.unbounded => 'Sem limite de valor',
      _ => policy.kind,
    };

String _toleranceSuffix(AmountPolicy policy) {
  final tolerance = policy.tolerancePercent;
  if (tolerance == null) return '';
  return tolerance == 0 ? ' (exato)' : ' ±${_percent(tolerance)}';
}

/// Formats a percentage without trailing zeros — `5%`, not `5.0%`.
String _percent(double value) {
  final text = value.toStringAsFixed(2);
  return '${text.replaceAll(RegExp(r'\.?0+$'), '')}%';
}

/// The full reading of [policy], as the rows of a detail card.
///
/// Shows every field the person informed — the tolerance of a fixed policy and
/// both bounds of a range one — plus, for a fixed policy, **the window the
/// tolerance actually produces**: "±5%" alone leaves the reader doing the
/// arithmetic to know whether the next bill will pass.
class AmountPolicyDetails extends StatelessWidget {
  /// Creates the rows for [policy].
  const AmountPolicyDetails({super.key, required this.policy});

  /// The policy being read.
  final AmountPolicy policy;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        InfoRow(
          icon: Symbols.payments,
          label: 'Tipo',
          value: AmountPolicyKinds.label(policy.kind),
        ),
        ..._fieldsOf(policy),
        if (!policy.isConclusive) ...[
          const SizedBox(height: AppSpacing.sm),
          Text(
            'Sem uma expectativa de valor, a verificação de valor do boleto '
            'fica inconclusiva — qualquer valor passa sem alerta.',
            style: Theme.of(context).textTheme.bodySmall?.copyWith(
                  color: Theme.of(context).colorScheme.tertiary,
                ),
          ),
        ],
      ],
    );
  }

  List<Widget> _fieldsOf(AmountPolicy policy) => switch (policy.kind) {
        AmountPolicyKinds.fixed => [
            InfoRow(
              icon: Symbols.attach_money,
              label: 'Valor esperado',
              value: formatMoney(policy.expectedAmount),
            ),
            InfoRow(
              icon: Symbols.percent,
              label: 'Tolerância',
              value: _toleranceValue(policy),
            ),
          ],
        AmountPolicyKinds.range => [
            InfoRow(
              icon: Symbols.arrow_downward,
              label: 'Valor mínimo',
              value: formatMoney(policy.minAmount),
            ),
            InfoRow(
              icon: Symbols.arrow_upward,
              label: 'Valor máximo',
              value: formatMoney(policy.maxAmount),
            ),
          ],
        _ => const [],
      };

  String _toleranceValue(AmountPolicy policy) {
    final tolerance = policy.tolerancePercent;
    if (tolerance == null) return '—';

    // Zero é tolerância válida no domínio, e "±0%" não diz o que ela significa.
    if (tolerance == 0) return 'Sem tolerância — o valor tem que bater exato';

    final window = amountPolicyWindow(policy);
    if (window == null) return '±${_percent(tolerance)}';

    return '±${_percent(tolerance)} — aceita de ${formatMoney(window.from)} '
        'a ${formatMoney(window.to)}';
  }
}
