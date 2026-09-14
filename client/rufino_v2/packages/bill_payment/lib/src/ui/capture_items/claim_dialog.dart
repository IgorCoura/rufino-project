import 'package:flutter/material.dart';
import 'package:rufino_core/rufino_core.dart';

/// What happened when the dialog asked to claim.
class ClaimAttempt {
  /// The claim went through, or failed for a reason the dialog cannot fix.
  const ClaimAttempt.finished() : accountRefusal = null;

  /// The server refused the account to remember; the dialog stays open.
  const ClaimAttempt.accountRefused(String? message)
      : accountRefusal = message ?? 'O número da conta não foi aceito.';

  /// Why the account number was refused, when it was.
  final String? accountRefusal;
}

/// Confirms a claim and offers to remember the account number (ADR-026).
///
/// Owns its own [TextEditingController]: the dialog stays alive through the
/// exit animation, and a controller disposed by the caller would break the
/// rebuild that follows the claim.
class ClaimDialog extends StatefulWidget {
  /// Creates the dialog.
  const ClaimDialog({
    super.key,
    required this.onClaim,
    this.suggestedAccountReference,
  });

  /// The account number the server found in the document, when there is one.
  final String? suggestedAccountReference;

  /// Runs the claim with the account to remember, or `null` when unticked.
  final Future<ClaimAttempt> Function(String? rememberAccountReference) onClaim;

  @override
  State<ClaimDialog> createState() => _ClaimDialogState();
}

class _ClaimDialogState extends State<ClaimDialog> {
  late final TextEditingController _account =
      TextEditingController(text: widget.suggestedAccountReference ?? '');
  final _formKey = GlobalKey<FormState>();

  // Marcada por padrão só quando há sugestão confirmada no documento: sem ela,
  // marcar por padrão pediria à pessoa um número que ninguém conferiu.
  late bool _remember = widget.suggestedAccountReference != null;
  bool _submitting = false;
  String? _refusal;

  @override
  void dispose() {
    _account.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;

    setState(() {
      _submitting = true;
      _refusal = null;
    });

    final attempt = await widget.onClaim(_remember ? _account.text.trim() : null);
    if (!mounted) return;

    if (attempt.accountRefusal != null) {
      setState(() {
        _submitting = false;
        _refusal = attempt.accountRefusal;
      });
      return;
    }

    Navigator.of(context).pop(true);
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return AlertDialog(
      title: const Text('Reivindicar este boleto?'),
      content: Form(
        key: _formKey,
        child: SingleChildScrollView(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Text(
                'O documento passa a ser deste cliente e vira um boleto na fila '
                'de verificação. O sistema relê o artefato pelos mesmos dígitos '
                'verificadores do caminho automático.',
              ),
              const SizedBox(height: AppSpacing.md),
              CheckboxListTile(
                key: const Key('claim-remember-account'),
                contentPadding: EdgeInsets.zero,
                value: _remember,
                onChanged: _submitting
                    ? null
                    : (value) => setState(() {
                          _remember = value ?? false;
                          _refusal = null;
                        }),
                title: const Text('Lembrar desta conta'),
                subtitle: const Text(
                  'Os próximos boletos com este número de conta serão '
                  'atribuídos automaticamente a este cliente.',
                ),
              ),
              if (_remember)
                TextFormField(
                  key: const Key('claim-account-reference'),
                  controller: _account,
                  enabled: !_submitting,
                  keyboardType: TextInputType.text,
                  decoration: const InputDecoration(
                    labelText: 'Número da conta',
                    helperText: 'Como aparece no boleto: nº da conta, instalação, '
                        'matrícula. Precisa estar no documento.',
                    helperMaxLines: 2,
                  ),
                  validator: (value) => value == null || value.trim().isEmpty
                      ? 'Informe o número da conta ou desmarque a opção.'
                      : null,
                ),
              if (_refusal != null)
                Padding(
                  padding: const EdgeInsets.only(top: AppSpacing.sm),
                  child: Text(
                    _refusal!,
                    key: const Key('claim-account-refusal'),
                    style: theme.textTheme.bodyMedium
                        ?.copyWith(color: theme.colorScheme.error),
                  ),
                ),
            ],
          ),
        ),
      ),
      actions: [
        TextButton(
          onPressed: _submitting ? null : () => Navigator.of(context).pop(false),
          child: const Text('Cancelar'),
        ),
        FilledButton.tonal(
          key: const Key('claim-confirm'),
          onPressed: _submitting ? null : _submit,
          child: const Text('Reivindicar'),
        ),
      ],
    );
  }
}
