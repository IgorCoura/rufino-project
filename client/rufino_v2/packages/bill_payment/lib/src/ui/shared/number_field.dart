import 'package:flutter/material.dart';

/// A decimal field that accepts both `1234.56` and `1234,56`.
///
/// Lives here, and not privately in a screen, because the register form and
/// the detail editor collect the very same numbers — two copies of this
/// validator would drift, and the drift would show up as "the form accepted it
/// and the detail refused it".
class NumberField extends StatelessWidget {
  /// Creates the field.
  const NumberField({
    super.key,
    required this.controller,
    required this.label,
    this.requiredField = false,
    this.helperText,
  });

  /// Holds the typed text.
  final TextEditingController controller;

  /// The field name.
  final String label;

  /// Whether leaving it blank is refused.
  final bool requiredField;

  /// Optional hint shown under the field.
  final String? helperText;

  /// Reads [controller] as a number, or `null` when it is blank or invalid.
  static double? read(TextEditingController controller) =>
      double.tryParse(controller.text.replaceAll(',', '.'));

  @override
  Widget build(BuildContext context) {
    return TextFormField(
      controller: controller,
      decoration: InputDecoration(
        labelText: label,
        helperText: helperText,
        helperMaxLines: 2,
        border: const OutlineInputBorder(),
      ),
      keyboardType: const TextInputType.numberWithOptions(decimal: true),
      validator: (value) {
        final text = value?.trim() ?? '';
        if (text.isEmpty) {
          return requiredField ? 'Informe o valor.' : null;
        }
        return double.tryParse(text.replaceAll(',', '.')) == null
            ? 'Valor inválido.'
            : null;
      },
    );
  }
}
