import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../../../core/config/app_config.dart';
import '../../../../core/theme/app_breakpoints.dart';
import '../../../../core/theme/app_spacing.dart';
import '../../auth/viewmodel/permission_notifier.dart';

/// A screen with developer tools used to diagnose issues in production.
///
/// Access is gated by the `debug` Keycloak resource — anyone reaching this
/// screen has been granted at least one scope on `debug`, so the tools
/// below assume an authorized operator.
class DebugScreen extends StatelessWidget {
  const DebugScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          tooltip: 'Voltar para o início',
          onPressed: () => context.go('/home'),
        ),
        title: const Text('Debug'),
      ),
      body: SafeArea(
        child: LayoutBuilder(
          builder: (context, constraints) {
            final isWide = constraints.maxWidth >= AppBreakpoints.tablet;
            return Center(
              child: ConstrainedBox(
                constraints: const BoxConstraints(maxWidth: 720),
                child: ListView(
                  padding: EdgeInsets.all(
                    isWide ? AppSpacing.lg : AppSpacing.md,
                  ),
                  children: const [
                    _ToolTile(
                      icon: Icons.bug_report_outlined,
                      title: 'Verificar integração com o Sentry',
                      description:
                          'Lança uma exceção proposital para confirmar que '
                          'erros estão chegando ao painel de monitoramento.',
                      child: _VerifySentryButton(),
                    ),
                    SizedBox(height: AppSpacing.md),
                    _ToolTile(
                      icon: Icons.lock_outline,
                      title: 'Permissões do usuário',
                      description:
                          'Lista todos os recursos e escopos concedidos ao '
                          'usuário pelo Keycloak.',
                      child: _PermissionsViewer(),
                    ),
                    SizedBox(height: AppSpacing.md),
                    _ToolTile(
                      icon: Icons.settings_outlined,
                      title: 'Configuração do app (AppConfig)',
                      description:
                          'Mostra todos os valores compilados em '
                          '[AppConfig], incluindo endpoints, ambiente e '
                          'flags de monitoramento.',
                      child: _AppConfigViewer(),
                    ),
                  ],
                ),
              ),
            );
          },
        ),
      ),
    );
  }
}

class _ToolTile extends StatelessWidget {
  const _ToolTile({
    required this.icon,
    required this.title,
    required this.description,
    required this.child,
  });

  final IconData icon;
  final String title;
  final String description;
  final Widget child;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(AppSpacing.md),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Icon(icon, color: theme.colorScheme.primary),
                const SizedBox(width: AppSpacing.sm),
                Expanded(
                  child: Text(title, style: theme.textTheme.titleMedium),
                ),
              ],
            ),
            const SizedBox(height: AppSpacing.sm),
            Text(description, style: theme.textTheme.bodyMedium),
            const SizedBox(height: AppSpacing.md),
            Align(alignment: Alignment.centerLeft, child: child),
          ],
        ),
      ),
    );
  }
}

class _VerifySentryButton extends StatelessWidget {
  const _VerifySentryButton();

  @override
  Widget build(BuildContext context) {
    return ElevatedButton(
      onPressed: () {
        throw StateError('This is test exception');
      },
      child: const Text('Verify Sentry Setup'),
    );
  }
}

class _PermissionsViewer extends StatelessWidget {
  const _PermissionsViewer();

  @override
  Widget build(BuildContext context) {
    final notifier = context.watch<PermissionNotifier>();
    final theme = Theme.of(context);

    if (notifier.status == PermissionStatus.loading) {
      return const Padding(
        padding: EdgeInsets.symmetric(vertical: AppSpacing.sm),
        child: SizedBox(
          height: 24,
          width: 24,
          child: CircularProgressIndicator(strokeWidth: 2),
        ),
      );
    }

    final permissions = notifier.permissions;
    if (permissions.isEmpty) {
      return Text(
        notifier.status == PermissionStatus.error
            ? 'Falha ao carregar permissões.'
            : 'Nenhuma permissão concedida.',
        style: theme.textTheme.bodyMedium,
      );
    }

    final sorted = [...permissions]
      ..sort((a, b) => a.resource.compareTo(b.resource));

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        for (final p in sorted)
          Padding(
            padding: const EdgeInsets.symmetric(vertical: AppSpacing.xs),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(p.resource, style: theme.textTheme.titleSmall),
                const SizedBox(height: AppSpacing.xs),
                Wrap(
                  spacing: AppSpacing.xs,
                  runSpacing: AppSpacing.xs,
                  children: [
                    for (final scope in p.scopes)
                      Chip(
                        label: Text(scope),
                        visualDensity: VisualDensity.compact,
                      ),
                  ],
                ),
              ],
            ),
          ),
        const SizedBox(height: AppSpacing.sm),
        Align(
          alignment: Alignment.centerLeft,
          child: TextButton.icon(
            onPressed: () => context.read<PermissionNotifier>().loadPermissions(),
            icon: const Icon(Icons.sync),
            label: const Text('Recarregar'),
          ),
        ),
      ],
    );
  }
}

class _AppConfigViewer extends StatelessWidget {
  const _AppConfigViewer();

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final entries = <MapEntry<String, String>>[
      const MapEntry('environment', AppConfig.environment),
      MapEntry('isDevelop', '${AppConfig.isDevelop}'),
      const MapEntry(
          'useDirectAccessGrants', '${AppConfig.useDirectAccessGrants}'),
      MapEntry(
          'useAuthorizationCodeFlow', '${AppConfig.useAuthorizationCodeFlow}'),
      const MapEntry('peopleManagementUrl', AppConfig.peopleManagementUrl),
      const MapEntry('identifier', AppConfig.identifier),
      const MapEntry('secret', AppConfig.secret),
      const MapEntry('authorizationEndpoint', AppConfig.authorizationEndpoint),
      const MapEntry('endSessionEndpoint', AppConfig.endSessionEndpoint),
      const MapEntry('authCodeAuthorizationEndpoint',
          AppConfig.authCodeAuthorizationEndpoint),
      const MapEntry(
          'authCodeTokenEndpoint', AppConfig.authCodeTokenEndpoint),
      const MapEntry('authCodeMobileRedirectScheme',
          AppConfig.authCodeMobileRedirectScheme),
      MapEntry(
          'authCodeMobileRedirectUri', AppConfig.authCodeMobileRedirectUri),
      const MapEntry(
          'authCodeDesktopRedirectPath', AppConfig.authCodeDesktopRedirectPath),
      const MapEntry('authCodeDesktopRedirectPort',
          '${AppConfig.authCodeDesktopRedirectPort}'),
      const MapEntry(
          'authCodeWebRedirectPath', AppConfig.authCodeWebRedirectPath),
      const MapEntry(
          'errorMonitoringEnabled', '${AppConfig.errorMonitoringEnabled}'),
      const MapEntry('errorMonitoringDsn', AppConfig.errorMonitoringDsn),
      MapEntry(
          'errorMonitoringEnvironment', AppConfig.errorMonitoringEnvironment),
      MapEntry('errorMonitoringTracesSampleRate',
          '${AppConfig.errorMonitoringTracesSampleRate}'),
    ];

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        for (final entry in entries)
          Padding(
            padding: const EdgeInsets.symmetric(vertical: AppSpacing.xs),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(entry.key, style: theme.textTheme.labelSmall),
                const SizedBox(height: 2),
                SelectableText(
                  entry.value.isEmpty ? '(vazio)' : entry.value,
                  style: theme.textTheme.bodyMedium?.copyWith(
                    fontFamily: 'monospace',
                  ),
                ),
              ],
            ),
          ),
        const SizedBox(height: AppSpacing.sm),
        Align(
          alignment: Alignment.centerLeft,
          child: TextButton.icon(
            onPressed: () {
              final dump = entries
                  .map((e) => '${e.key}=${e.value}')
                  .join('\n');
              Clipboard.setData(ClipboardData(text: dump));
              ScaffoldMessenger.of(context)
                ..hideCurrentSnackBar()
                ..showSnackBar(
                  const SnackBar(
                    content: Text('AppConfig copiado para a área de transferência.'),
                    behavior: SnackBarBehavior.floating,
                  ),
                );
            },
            icon: const Icon(Icons.copy_all_outlined),
            label: const Text('Copiar tudo'),
          ),
        ),
      ],
    );
  }
}
