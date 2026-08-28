import 'package:flutter/services.dart';
import 'package:mask_text_input_formatter/mask_text_input_formatter.dart';

/// Formats a Brazilian fiscal document that may be either a CPF or a CNPJ,
/// switching between the two as the person types.
///
/// **Why this exists instead of a plain [MaskTextInputFormatter].** A mask
/// caps the field at its own length: with `###.###.###-##` mounted, the 12th
/// digit is swallowed before any `onChanged` runs, so the code that would
/// grow the mask into a CNPJ never gets to see it — the field simply refuses
/// to accept a company document. Deciding the mask *before* masking is the
/// only order that works, and it has to happen inside the formatter, because
/// that is the one place that sees the keystroke while it is still raw.
///
/// The masking itself is still [MaskTextInputFormatter]'s job — this class
/// picks which mask it wears.
class TaxIdInputFormatter extends TextInputFormatter {
  /// Creates the formatter, mounted with the CPF mask.
  TaxIdInputFormatter()
      : _mask = MaskTextInputFormatter(
          mask: cpfMask,
          filter: {'#': RegExp(r'[0-9]')},
        );

  /// The mask of a CPF — 11 digits.
  static const String cpfMask = '###.###.###-##';

  /// The mask of a CNPJ — 14 digits.
  static const String cnpjMask = '##.###.###/####-##';

  /// Where a CPF ends and a CNPJ begins.
  static const int cpfLength = 11;

  final MaskTextInputFormatter _mask;

  /// The mask currently mounted, for tests and for callers that mirror it.
  String? get currentMask => _mask.getMask();

  @override
  TextEditingValue formatEditUpdate(
    TextEditingValue oldValue,
    TextEditingValue newValue,
  ) {
    final digits = newValue.text.replaceAll(RegExp(r'\D'), '');
    final wanted = digits.length > cpfLength ? cnpjMask : cpfMask;

    if (_mask.getMask() == wanted) return _mask.formatEditUpdate(oldValue, newValue);

    // `updateMask` re-formats the incoming value from scratch under the new
    // mask, which is exactly what a grown — or shrunk — document needs: the
    // digits stay, the grouping changes.
    return _mask.updateMask(mask: wanted, newValue: newValue);
  }
}
