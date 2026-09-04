import 'package:flutter/material.dart';

/// Shows a floating [SnackBar] with server error messages.
///
/// Joins all entries in [messages] into a single multi-line string.
/// Falls back to [fallbackMessage] when [messages] is empty.
void showErrorSnackBar(
  BuildContext context, {
  required List<String> messages,
  String fallbackMessage = 'Ocorreu um erro inesperado.',
}) {
  final text = messages.isNotEmpty ? messages.join('\n') : fallbackMessage;

  ScaffoldMessenger.of(context)
    ..hideCurrentSnackBar()
    ..showSnackBar(
      SnackBar(
        content: Text(text),
        behavior: SnackBarBehavior.floating,
        duration: const Duration(seconds: 6),
      ),
    );
}
