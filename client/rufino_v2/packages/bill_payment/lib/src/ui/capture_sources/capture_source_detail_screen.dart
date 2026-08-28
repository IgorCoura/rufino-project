import 'package:flutter/material.dart';
import 'package:material_symbols_icons/symbols.dart';
import 'package:provider/provider.dart';
import 'package:rufino_core/rufino_core.dart';

import '../../bill_payment_permissions.dart';
import '../bill_payment_back_button.dart';
import '../shared/formats.dart';
import '../shared/message_panel.dart';
import '../shared/status_badge.dart';
import 'capture_since_field.dart';
import 'capture_source_detail_viewmodel.dart';

/// The capture source detail: folders, sync actions and the credential
/// replacement.
class CaptureSourceDetailScreen extends StatefulWidget {
  /// Creates the screen.
  const CaptureSourceDetailScreen({
    super.key,
    required this.viewModel,
    required this.backFallback,
    required this.onDisconnected,
  });

  /// Drives the screen.
  final CaptureSourceDetailViewModel viewModel;

  /// Para onde o voltar leva quando não há pilha.
  final String backFallback;

  /// Called after the source is disconnected.
  final VoidCallback onDisconnected;

  @override
  State<CaptureSourceDetailScreen> createState() =>
      _CaptureSourceDetailScreenState();
}

class _CaptureSourceDetailScreenState
    extends State<CaptureSourceDetailScreen> {
  String? _lastInfoMessage;

  @override
  void initState() {
    super.initState();
    widget.viewModel.addListener(_onViewModelChanged);
    widget.viewModel.load();
  }

  @override
  void dispose() {
    widget.viewModel.removeListener(_onViewModelChanged);
    super.dispose();
  }

  void _onViewModelChanged() {
    final message = widget.viewModel.infoMessage;
    if (message != null && message != _lastInfoMessage && mounted) {
      _lastInfoMessage = message;
      ScaffoldMessenger.of(context)
          .showSnackBar(SnackBar(content: Text(message)));
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Fonte de captura'),
        leading: BillPaymentBackButton(fallback: widget.backFallback),
      ),
      body: SafeArea(
        child: ListenableBuilder(
          listenable: widget.viewModel,
          builder: (context, _) {
            final viewModel = widget.viewModel;
            switch (viewModel.status) {
              case CaptureSourceDetailStatus.loading:
                return const Center(child: CircularProgressIndicator());
              case CaptureSourceDetailStatus.error:
                return MessagePanel(
                  icon: Symbols.error,
                  title: viewModel.errorMessage ??
                      'Não foi possível carregar a fonte.',
                  action: FilledButton.tonal(
                    onPressed: viewModel.load,
                    child: const Text('Tentar novamente'),
                  ),
                );
              case CaptureSourceDetailStatus.loaded:
                return _Body(
                  viewModel: viewModel,
                  onDisconnected: widget.onDisconnected,
                );
            }
          },
        ),
      ),
    );
  }
}

class _Body extends StatefulWidget {
  const _Body({required this.viewModel, required this.onDisconnected});

  final CaptureSourceDetailViewModel viewModel;
  final VoidCallback onDisconnected;

  @override
  State<_Body> createState() => _BodyState();
}

class _BodyState extends State<_Body> {
  final _folderController = TextEditingController();

  @override
  void dispose() {
    _folderController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final viewModel = widget.viewModel;
    final source = viewModel.source!;
    final permissions = context.watch<BillPaymentPermissionNotifier>();
    final canManage = permissions.hasPermission(
      BillPaymentResources.captureSource,
      BillPaymentScopes.manage,
    );
    final canSync = permissions.hasPermission(
      BillPaymentResources.captureSource,
      BillPaymentScopes.sync,
    );

    return Center(
      child: ConstrainedBox(
        constraints: const BoxConstraints(maxWidth: AppBreakpoints.tablet),
        child: ListView(
          padding: const EdgeInsets.all(AppSpacing.md),
          children: [
            if (viewModel.errorMessage != null)
              Padding(
                padding: const EdgeInsets.only(bottom: AppSpacing.md),
                child: Text(
                  viewModel.errorMessage!,
                  style:
                      TextStyle(color: Theme.of(context).colorScheme.error),
                ),
              ),
            SectionCard(
              title: 'Caixa',
              child: Column(
                children: [
                  InfoRow(
                    icon: Symbols.inbox,
                    label: 'Nome',
                    value: source.displayName,
                  ),
                  InfoRow(
                    icon: Symbols.alternate_email,
                    label: 'Endereço',
                    value: source.address,
                  ),
                  InfoRow(
                    icon: Symbols.key,
                    label: 'Credencial',
                    value: source.hasCredential
                        ? 'Guardada no cofre'
                        : 'Ausente',
                  ),
                  InfoRow(
                    icon: Symbols.schedule,
                    label: 'Última sincronização',
                    value: source.lastSyncError ??
                        formatDateTime(source.lastSyncAt),
                  ),
                  const SizedBox(height: AppSpacing.md),
                  CaptureSinceField(
                    value: source.captureSince,
                    enabled: !viewModel.isMutating,
                    helperText:
                        'Nada recebido antes desta data é lido. Alterar '
                        'faz a caixa ser relida desde a data nova — sem '
                        'duplicar o que já entrou.',
                    onChanged: (date) => viewModel.changeCaptureSince(date),
                  ),
                ],
              ),
            ),
            const SizedBox(height: AppSpacing.md),
            SectionCard(
              title: 'Pastas monitoradas',
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  for (final folder in source.folders)
                    ListTile(
                      contentPadding: EdgeInsets.zero,
                      leading: const Icon(Symbols.folder),
                      title: Text(folder.label),
                      subtitle: folder.lastSyncError != null
                          ? Text(
                              folder.lastSyncError!,
                              style: TextStyle(
                                color:
                                    Theme.of(context).colorScheme.error,
                              ),
                            )
                          : Text(
                              'Sincronizada em '
                              '${formatDateTime(folder.lastSyncAt)}',
                            ),
                      trailing: canManage && source.canRemoveFolder
                          ? IconButton(
                              icon: const Icon(Symbols.delete),
                              tooltip: 'Remover pasta',
                              onPressed: viewModel.isMutating
                                  ? null
                                  : () =>
                                      viewModel.removeFolder(folder.path),
                            )
                          : null,
                    ),
                  if (canManage && source.canAddFolder)
                    Row(
                      children: [
                        Expanded(
                          child: TextField(
                            controller: _folderController,
                            decoration: const InputDecoration(
                              hintText:
                                  'Nova pasta (vazio = caixa de entrada)',
                              border: OutlineInputBorder(),
                              isDense: true,
                            ),
                          ),
                        ),
                        const SizedBox(width: AppSpacing.sm),
                        IconButton.filledTonal(
                          icon: const Icon(Symbols.add),
                          tooltip: 'Adicionar pasta',
                          onPressed: viewModel.isMutating
                              ? null
                              : () async {
                                  final added = await viewModel
                                      .addFolder(_folderController.text);
                                  if (added && mounted) {
                                    _folderController.clear();
                                  }
                                },
                        ),
                      ],
                    ),
                ],
              ),
            ),
            const SizedBox(height: AppSpacing.md),
            SectionCard(
              title: 'Situação',
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      StatusBadge(
                        label: source.isEnabled ? 'Ativa' : 'Desativada',
                        tone: source.isEnabled
                            ? BadgeTone.positive
                            : BadgeTone.neutral,
                      ),
                    ],
                  ),
                  const SizedBox(height: AppSpacing.sm),
                  Wrap(
                    spacing: AppSpacing.sm,
                    runSpacing: AppSpacing.xs,
                    children: [
                      if (canSync)
                        FilledButton.tonal(
                          onPressed: viewModel.isMutating
                              ? null
                              : viewModel.syncNow,
                          child: const Text('Sincronizar agora'),
                        ),
                      if (canSync)
                        OutlinedButton(
                          onPressed: viewModel.isMutating
                              ? null
                              : () => _confirmRescan(context),
                          child: const Text('Reler caixa inteira'),
                        ),
                      if (canManage)
                        OutlinedButton(
                          onPressed: viewModel.isMutating
                              ? null
                              : () => viewModel.setActivation(
                                  isEnabled: !source.isEnabled),
                          child: Text(
                            source.isEnabled ? 'Desativar' : 'Reativar',
                          ),
                        ),
                      if (canManage)
                        OutlinedButton(
                          onPressed: viewModel.isMutating
                              ? null
                              : () => _replaceCredential(context),
                          child: const Text('Substituir credencial'),
                        ),
                      if (canManage)
                        TextButton(
                          onPressed: viewModel.isMutating
                              ? null
                              : () => _confirmDisconnect(context),
                          child: const Text('Desconectar'),
                        ),
                    ],
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  Future<void> _confirmRescan(BuildContext context) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: const Text('Reler a caixa inteira?'),
        content: const Text(
          'Os cursores são descartados e a próxima varredura relê tudo que '
          'ainda está na caixa. Nada duplica — o que já virou item continua '
          'o mesmo.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(dialogContext).pop(false),
            child: const Text('Cancelar'),
          ),
          FilledButton.tonal(
            onPressed: () => Navigator.of(dialogContext).pop(true),
            child: const Text('Reler'),
          ),
        ],
      ),
    );
    if (confirmed == true) await widget.viewModel.rescan();
  }

  Future<void> _confirmDisconnect(BuildContext context) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: const Text('Desconectar a caixa?'),
        content: const Text(
          'A captura para de ler esta caixa. Reconectar depois exige '
          'digitar a credencial de novo.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(dialogContext).pop(false),
            child: const Text('Cancelar'),
          ),
          FilledButton.tonal(
            onPressed: () => Navigator.of(dialogContext).pop(true),
            child: const Text('Desconectar'),
          ),
        ],
      ),
    );
    if (confirmed != true) return;
    final disconnected = await widget.viewModel.disconnect();
    if (disconnected) widget.onDisconnected();
  }

  Future<void> _replaceCredential(BuildContext context) async {
    final directoryController = TextEditingController();
    final clientIdController = TextEditingController();
    final secretController = TextEditingController();

    final confirmed = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: const Text('Substituir credencial'),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            TextField(
              controller: directoryController,
              decoration: const InputDecoration(
                labelText: 'Directory (tenant) ID',
              ),
            ),
            TextField(
              controller: clientIdController,
              decoration: const InputDecoration(
                labelText: 'Application (client) ID',
              ),
            ),
            TextField(
              controller: secretController,
              obscureText: true,
              decoration: const InputDecoration(labelText: 'Client secret'),
            ),
          ],
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(dialogContext).pop(false),
            child: const Text('Cancelar'),
          ),
          FilledButton.tonal(
            onPressed: () => Navigator.of(dialogContext).pop(true),
            child: const Text('Substituir'),
          ),
        ],
      ),
    );

    if (confirmed == true) {
      await widget.viewModel.replaceCredential(
        directoryId: directoryController.text,
        clientId: clientIdController.text,
        clientSecret: secretController.text,
      );
    }
    directoryController.dispose();
    clientIdController.dispose();
    secretController.dispose();
  }
}
