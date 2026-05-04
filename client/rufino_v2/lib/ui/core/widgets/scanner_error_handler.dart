import 'package:flutter/material.dart';
import 'package:permission_handler/permission_handler.dart';

import '../../../core/errors/document_scanner_exception.dart';

/// Presents a user-facing error for a [DocumentScannerException].
///
/// For [ScannerPermissionPermanentlyDeniedException] shows a dialog with
/// an "Abrir Ajustes" action that takes the user directly to the iOS
/// (or Android) app settings. Other variants surface as a snackbar so
/// they do not block the flow.
///
/// All UI strings are in Brazilian Portuguese per the project's
/// language convention.
Future<void> presentScannerError(
  BuildContext context,
  DocumentScannerException exception,
) async {
  if (exception is ScannerPermissionPermanentlyDeniedException) {
    await _showOpenSettingsDialog(context);
    return;
  }
  ScaffoldMessenger.of(context)
    ..hideCurrentSnackBar()
    ..showSnackBar(
      SnackBar(
        content: Text(_messageFor(exception)),
        behavior: SnackBarBehavior.floating,
      ),
    );
}

String _messageFor(DocumentScannerException e) => switch (e) {
      ScannerPermissionDeniedException() =>
        'Permita o acesso à câmera para digitalizar documentos.',
      ScannerPermissionPermanentlyDeniedException() =>
        'Acesso à câmera bloqueado. Habilite em Ajustes > Rufino.',
      ScannerPluginFailureException() =>
        'Não foi possível abrir o digitalizador. Tente novamente.',
      ScannerFileReadException() =>
        'Falha ao ler a imagem digitalizada. Tente novamente.',
    };

Future<void> _showOpenSettingsDialog(BuildContext context) async {
  final shouldOpen = await showDialog<bool>(
    context: context,
    builder: (ctx) => AlertDialog(
      title: const Text('Permissão de câmera necessária'),
      content: const Text(
        'O Rufino precisa da câmera para digitalizar documentos. '
        'Habilite o acesso em Ajustes > Rufino.',
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.of(ctx).pop(false),
          child: const Text('Cancelar'),
        ),
        FilledButton(
          onPressed: () => Navigator.of(ctx).pop(true),
          child: const Text('Abrir Ajustes'),
        ),
      ],
    ),
  );
  if (shouldOpen ?? false) {
    await openAppSettings();
  }
}
