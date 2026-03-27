import 'package:flutter/material.dart';

import '../../../../core/utils/file_saver_stub.dart'
    if (dart.library.io) '../../../../core/utils/file_saver.dart';
import 'package:flutter_json_view/flutter_json_view.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/theme/app_spacing.dart';
import '../../../../domain/entities/document_template.dart';
import '../../../core/widgets/error_dialog.dart';
import '../viewmodel/document_template_form_viewmodel.dart';

/// Form screen for creating or editing a document template.
///
/// When [templateId] is provided, [DocumentTemplateFormViewModel.loadTemplate]
/// is called on init to populate the fields from the API.
class DocumentTemplateFormScreen extends StatefulWidget {
  const DocumentTemplateFormScreen({
    super.key,
    required this.viewModel,
    this.templateId,
  });

  /// The view model that manages form state and validation.
  final DocumentTemplateFormViewModel viewModel;

  /// The id of the template to edit. Null when creating a new template.
  final String? templateId;

  @override
  State<DocumentTemplateFormScreen> createState() =>
      _DocumentTemplateFormScreenState();
}

class _DocumentTemplateFormScreenState
    extends State<DocumentTemplateFormScreen> {
  final _formKey = GlobalKey<FormState>();

  @override
  void initState() {
    super.initState();
    widget.viewModel.addListener(_onViewModelChanged);
    if (widget.templateId != null) {
      widget.viewModel.loadTemplate(widget.templateId!);
    } else {
      widget.viewModel.loadOptions();
    }
  }

  @override
  void didUpdateWidget(covariant DocumentTemplateFormScreen oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.viewModel != widget.viewModel) {
      oldWidget.viewModel.removeListener(_onViewModelChanged);
      widget.viewModel.addListener(_onViewModelChanged);
      if (widget.templateId != null) {
        widget.viewModel.loadTemplate(widget.templateId!);
      } else {
        widget.viewModel.loadOptions();
      }
    }
  }

  @override
  void dispose() {
    widget.viewModel.removeListener(_onViewModelChanged);
    super.dispose();
  }

  void _onViewModelChanged() {
    if (!mounted) return;
    switch (widget.viewModel.status) {
      case DocumentTemplateFormStatus.saved:
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Template salvo com sucesso!'),
            behavior: SnackBarBehavior.floating,
          ),
        );
        context.pop();
      case DocumentTemplateFormStatus.error:
        showErrorSnackBar(
          context,
          messages: widget.viewModel.serverErrors,
          fallbackMessage: widget.viewModel.errorMessage ?? 'Erro ao salvar.',
        );
      default:
        break;
    }
  }

  void _onSave() {
    if (!(_formKey.currentState?.validate() ?? false)) return;
    widget.viewModel.save();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: ListenableBuilder(
          listenable: widget.viewModel,
          builder: (context, _) => Text(
            widget.viewModel.isNew ? 'Criar Template' : 'Editar Template',
          ),
        ),
      ),
      body: ListenableBuilder(
        listenable: widget.viewModel,
        builder: (context, _) {
          if (widget.viewModel.isLoading) {
            return const Center(child: CircularProgressIndicator());
          }
          return _DocumentTemplateFormBody(
            viewModel: widget.viewModel,
            formKey: _formKey,
            onSave: _onSave,
            onCancel: () => context.pop(),
          );
        },
      ),
    );
  }
}

/// The scrollable form body containing all template fields.
class _DocumentTemplateFormBody extends StatelessWidget {
  const _DocumentTemplateFormBody({
    required this.viewModel,
    required this.formKey,
    required this.onSave,
    required this.onCancel,
  });

  final DocumentTemplateFormViewModel viewModel;
  final GlobalKey<FormState> formKey;
  final VoidCallback onSave;
  final VoidCallback onCancel;

  @override
  Widget build(BuildContext context) {
    return SingleChildScrollView(
      padding: const EdgeInsets.all(AppSpacing.md),
      child: Center(
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 800),
          child: Form(
            key: formKey,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                // ─── Informações Básicas ──────────────────────────────
                _SectionCard(
                  icon: Icons.description_outlined,
                  label: 'Informações Básicas',
                  children: [
                    TextFormField(
                      controller: viewModel.nameController,
                      decoration: const InputDecoration(
                        labelText: 'Nome *',
                        prefixIcon: Icon(Icons.label_outline),
                        border: OutlineInputBorder(),
                      ),
                      textCapitalization: TextCapitalization.sentences,
                      maxLength: 100,
                      validator: viewModel.validateName,
                    ),
                    const SizedBox(height: AppSpacing.md),
                    TextFormField(
                      controller: viewModel.descriptionController,
                      decoration: const InputDecoration(
                        labelText: 'Descrição *',
                        prefixIcon: Icon(Icons.notes_outlined),
                        alignLabelWithHint: true,
                        border: OutlineInputBorder(),
                      ),
                      maxLines: 3,
                      maxLength: 500,
                      textCapitalization: TextCapitalization.sentences,
                      validator: viewModel.validateDescription,
                    ),
                    const SizedBox(height: AppSpacing.md),
                    Row(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Expanded(
                          child: TextFormField(
                            controller: viewModel.validityController,
                            decoration: const InputDecoration(
                              labelText: 'Validade (dias)',
                              prefixIcon: Icon(Icons.calendar_today_outlined),
                              border: OutlineInputBorder(),
                              hintText: '0–999',
                            ),
                            keyboardType: TextInputType.number,
                            inputFormatters: [viewModel.validityFormatter],
                            validator: viewModel.validateValidity,
                          ),
                        ),
                        const SizedBox(width: AppSpacing.md),
                        Expanded(
                          child: TextFormField(
                            controller: viewModel.workloadController,
                            decoration: const InputDecoration(
                              labelText: 'Carga horária (h)',
                              prefixIcon: Icon(Icons.schedule_outlined),
                              border: OutlineInputBorder(),
                              hintText: '0–999',
                            ),
                            keyboardType: TextInputType.number,
                            inputFormatters: [viewModel.workloadFormatter],
                            validator: viewModel.validateWorkload,
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: AppSpacing.md),
                    DropdownButtonFormField<String>(
                      initialValue: viewModel.safeDocumentGroupId,
                      decoration: const InputDecoration(
                        labelText: 'Grupo de Documento',
                        prefixIcon: Icon(Icons.folder_outlined),
                        border: OutlineInputBorder(),
                      ),
                      items: viewModel.documentGroups
                          .map<DropdownMenuItem<String>>((g) =>
                              DropdownMenuItem<String>(
                                  value: g.id, child: Text(g.name)))
                          .toList(),
                      onChanged: (v) => viewModel.setDocumentGroupId(v),
                    ),
                  ],
                ),
                const SizedBox(height: AppSpacing.md),

                // ─── Configurações ──────────────────────────────────
                _SectionCard(
                  icon: Icons.settings_outlined,
                  label: 'Configurações',
                  children: [
                    SwitchListTile(
                      title: const Text('Competência de Período Anterior'),
                      subtitle: const Text(
                        'Usa a competência do período anterior para '
                        'agrupamento e depreciação.',
                      ),
                      value: viewModel.usePreviousPeriod,
                      onChanged: viewModel.setUsePreviousPeriod,
                      contentPadding: EdgeInsets.zero,
                    ),
                    const Divider(height: 1),
                    SwitchListTile(
                      title: const Text('Aceita Assinatura'),
                      subtitle: const Text(
                        'Documentos gerados a partir deste template '
                        'poderão ser assinados.',
                      ),
                      value: viewModel.acceptsSignature,
                      onChanged: viewModel.setAcceptsSignature,
                      contentPadding: EdgeInsets.zero,
                    ),
                  ],
                ),
                const SizedBox(height: AppSpacing.md),

                // ─── Arquivos ────────────────────────────────────────
                _SectionCard(
                  icon: Icons.insert_drive_file_outlined,
                  label: 'Arquivos do Template',
                  children: [
                    Text(
                      'Tipos de Recuperação de Dados',
                      style: Theme.of(context).textTheme.titleSmall,
                    ),
                    const SizedBox(height: AppSpacing.sm),
                    Wrap(
                      spacing: AppSpacing.sm,
                      runSpacing: AppSpacing.xs,
                      children: viewModel.recoverDataTypes
                          .where((t) => t.id != '0')
                          .map((t) {
                        final typeId = int.tryParse(t.id) ?? 0;
                        final selected = viewModel
                            .selectedRecoverDataTypeIds
                            .contains(typeId);
                        return FilterChip(
                          label: Text(t.name),
                          selected: selected,
                          onSelected: (_) =>
                              viewModel.toggleRecoverDataType(typeId),
                        );
                      }).toList(),
                    ),
                    const SizedBox(height: AppSpacing.lg),
                    TextFormField(
                      controller: viewModel.bodyFileNameController,
                      decoration: const InputDecoration(
                        labelText: 'Arquivo do corpo',
                        prefixIcon: Icon(Icons.article_outlined),
                        border: OutlineInputBorder(),
                        hintText: 'corpo.html',
                        helperText: 'Máx. 20 caracteres, extensão .html',
                      ),
                      maxLength: 20,
                      validator: viewModel.validateFileName,
                    ),
                    const SizedBox(height: AppSpacing.md),
                    TextFormField(
                      controller: viewModel.headerFileNameController,
                      decoration: const InputDecoration(
                        labelText: 'Arquivo do cabeçalho',
                        prefixIcon: Icon(Icons.vertical_align_top_outlined),
                        border: OutlineInputBorder(),
                        hintText: 'cabecalho.html',
                        helperText: 'Máx. 20 caracteres, extensão .html',
                      ),
                      maxLength: 20,
                      validator: viewModel.validateFileName,
                    ),
                    const SizedBox(height: AppSpacing.md),
                    TextFormField(
                      controller: viewModel.footerFileNameController,
                      decoration: const InputDecoration(
                        labelText: 'Arquivo do rodapé',
                        prefixIcon: Icon(Icons.vertical_align_bottom_outlined),
                        border: OutlineInputBorder(),
                        hintText: 'rodape.html',
                        helperText: 'Máx. 20 caracteres, extensão .html',
                      ),
                      maxLength: 20,
                      validator: viewModel.validateFileName,
                    ),
                  ],
                ),
                const SizedBox(height: AppSpacing.md),

                // ─── Upload/Download (only when editing) ─────────────
                if (!viewModel.isNew) ...[
                  _FileSection(viewModel: viewModel),
                  const SizedBox(height: AppSpacing.md),
                ],

                // ─── Locais das Assinaturas ──────────────────────────
                if (viewModel.acceptsSignature) ...[
                  _PlaceSignaturesSection(viewModel: viewModel),
                  const SizedBox(height: AppSpacing.md),
                ],

                // ─── Detalhes dos Modelos de Dados ───────────────────
                _DataModelsSection(viewModel: viewModel),
                const SizedBox(height: AppSpacing.xl),

                _FormActions(
                  isSaving: viewModel.isSaving,
                  onSave: onSave,
                  onCancel: onCancel,
                ),
                const SizedBox(height: AppSpacing.lg),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

/// A labelled card section for grouping related form fields.
class _SectionCard extends StatelessWidget {
  const _SectionCard({
    required this.icon,
    required this.label,
    required this.children,
  });

  final IconData icon;
  final String label;
  final List<Widget> children;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Card.outlined(
      child: Padding(
        padding: const EdgeInsets.all(AppSpacing.md),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Row(
              children: [
                Icon(icon, size: 20, color: theme.colorScheme.primary),
                const SizedBox(width: AppSpacing.sm),
                Text(
                  label,
                  style: theme.textTheme.titleMedium?.copyWith(
                    color: theme.colorScheme.primary,
                  ),
                ),
              ],
            ),
            const SizedBox(height: AppSpacing.md),
            ...children,
          ],
        ),
      ),
    );
  }
}

/// File upload/download buttons shown when editing an existing template.
class _FileSection extends StatelessWidget {
  const _FileSection({required this.viewModel});

  final DocumentTemplateFormViewModel viewModel;

  Future<void> _uploadAndShowFeedback(BuildContext context) async {
    final success = await viewModel.pickAndUploadFile();
    if (!context.mounted) return;
    if (success) {
      ScaffoldMessenger.of(context)
        ..hideCurrentSnackBar()
        ..showSnackBar(
          const SnackBar(
            content: Text('Arquivo alterado com sucesso.'),
            behavior: SnackBarBehavior.floating,
          ),
        );
    } else if (viewModel.status == DocumentTemplateFormStatus.error) {
      showErrorSnackBar(
        context,
        messages: viewModel.serverErrors,
        fallbackMessage:
            viewModel.errorMessage ?? 'Falha ao enviar arquivo.',
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    return Card.outlined(
      child: Padding(
        padding: const EdgeInsets.all(AppSpacing.md),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Row(
              children: [
                Icon(
                  Icons.cloud_outlined,
                  size: 20,
                  color: Theme.of(context).colorScheme.primary,
                ),
                const SizedBox(width: AppSpacing.sm),
                Text(
                  'Arquivo do Template',
                  style: Theme.of(context).textTheme.titleMedium?.copyWith(
                        color: Theme.of(context).colorScheme.primary,
                      ),
                ),
              ],
            ),
            const SizedBox(height: AppSpacing.md),
            if (viewModel.hasFile)
              Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  OutlinedButton.icon(
                    onPressed: () async {
                      final bytes = await viewModel.downloadFile();
                      if (bytes != null && context.mounted) {
                        await saveFile(
                          fileName: 'files.zip',
                          bytes: bytes,
                        );
                        if (context.mounted) {
                          ScaffoldMessenger.of(context).showSnackBar(
                            const SnackBar(
                              content: Text('Arquivo baixado com sucesso.'),
                              behavior: SnackBarBehavior.floating,
                            ),
                          );
                        }
                      }
                    },
                    icon: const Icon(Icons.download_outlined, size: 20),
                    label: const Text('Baixar Arquivo'),
                  ),
                  const SizedBox(width: AppSpacing.sm),
                  FilledButton.icon(
                    onPressed: () => _uploadAndShowFeedback(context),
                    icon: const Icon(Icons.upload_outlined, size: 20),
                    label: const Text('Alterar Arquivo'),
                  ),
                ],
              )
            else
              Center(
                child: FilledButton.tonalIcon(
                  onPressed: () => _uploadAndShowFeedback(context),
                  icon: const Icon(Icons.upload_outlined, size: 20),
                  label: const Text('Enviar Arquivo'),
                ),
              ),
          ],
        ),
      ),
    );
  }
}

/// Section for managing signature placements on the document template.
class _PlaceSignaturesSection extends StatelessWidget {
  const _PlaceSignaturesSection({required this.viewModel});

  final DocumentTemplateFormViewModel viewModel;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final signatures = viewModel.placeSignatures;

    return Card.outlined(
      child: Padding(
        padding: const EdgeInsets.all(AppSpacing.md),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Row(
              children: [
                Icon(
                  Icons.draw_outlined,
                  size: 20,
                  color: theme.colorScheme.primary,
                ),
                const SizedBox(width: AppSpacing.sm),
                Text(
                  'Locais das Assinaturas',
                  style: theme.textTheme.titleMedium?.copyWith(
                    color: theme.colorScheme.primary,
                  ),
                ),
              ],
            ),
            const SizedBox(height: AppSpacing.md),
            for (int i = 0; i < signatures.length; i++)
              _SignatureCard(
                key: ValueKey('sig-$i-${signatures.length}'),
                index: i,
                signature: signatures[i],
                viewModel: viewModel,
              ),
            Align(
              alignment: Alignment.centerLeft,
              child: TextButton.icon(
                onPressed: viewModel.addPlaceSignature,
                icon: const Icon(Icons.add, size: 18),
                label: const Text('Adicionar Assinatura'),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

/// A single signature placement card with its own form fields.
class _SignatureCard extends StatelessWidget {
  const _SignatureCard({
    super.key,
    required this.index,
    required this.signature,
    required this.viewModel,
  });

  final int index;
  final PlaceSignatureData signature;
  final DocumentTemplateFormViewModel viewModel;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Padding(
      padding: const EdgeInsets.only(bottom: AppSpacing.md),
      child: Card(
        elevation: 0,
        color: theme.colorScheme.surfaceContainerLow,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(12),
          side: BorderSide(color: theme.colorScheme.outlineVariant),
        ),
        child: Padding(
          padding: const EdgeInsets.all(AppSpacing.md),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Row(
                children: [
                  Icon(
                    Icons.gesture_outlined,
                    size: 18,
                    color: theme.colorScheme.onSurfaceVariant,
                  ),
                  const SizedBox(width: AppSpacing.sm),
                  Text(
                    'Assinatura ${index + 1}',
                    style: theme.textTheme.titleSmall,
                  ),
                  const Spacer(),
                  IconButton(
                    icon: Icon(
                      Icons.delete_outline,
                      color: theme.colorScheme.error,
                      size: 20,
                    ),
                    tooltip: 'Remover assinatura',
                    onPressed: () => viewModel.removePlaceSignature(index),
                  ),
                ],
              ),
              const SizedBox(height: AppSpacing.sm),
              DropdownButtonFormField<String>(
                initialValue: signature.typeSignatureId.isNotEmpty &&
                        viewModel.typeSignatures.any(
                            (t) => t.id == signature.typeSignatureId)
                    ? signature.typeSignatureId
                    : null,
                decoration: const InputDecoration(
                  labelText: 'Tipo de Assinatura',
                  border: OutlineInputBorder(),
                ),
                items: viewModel.typeSignatures
                    .map<DropdownMenuItem<String>>((t) =>
                        DropdownMenuItem<String>(
                            value: t.id, child: Text(t.name)))
                    .toList(),
                onChanged: (v) => viewModel.updatePlaceSignatureAndNotify(
                  index,
                  viewModel.placeSignatures[index]
                      .copyWith(typeSignatureId: v ?? ''),
                ),
              ),
              const SizedBox(height: AppSpacing.sm),
              TextFormField(
                initialValue: signature.page,
                decoration: const InputDecoration(
                  labelText: 'Página',
                  border: OutlineInputBorder(),
                  hintText: '0–100',
                ),
                keyboardType: TextInputType.number,
                validator: (v) =>
                    viewModel.validateSignatureNumber(v, 'Página'),
                onChanged: (v) => viewModel.updatePlaceSignature(
                  index,
                  viewModel.placeSignatures[index].copyWith(page: v),
                ),
              ),
              const SizedBox(height: AppSpacing.sm),
              Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Expanded(
                    child: TextFormField(
                      initialValue: signature.positionBottom,
                      decoration: const InputDecoration(
                        labelText: 'Pos. Inferior',
                        border: OutlineInputBorder(),
                        hintText: '0–100',
                      ),
                      keyboardType: TextInputType.number,
                      validator: (v) => viewModel.validateSignatureNumber(
                          v, 'Pos. Inferior'),
                      onChanged: (v) => viewModel.updatePlaceSignature(
                        index,
                        viewModel.placeSignatures[index]
                            .copyWith(positionBottom: v),
                      ),
                    ),
                  ),
                  const SizedBox(width: AppSpacing.sm),
                  Expanded(
                    child: TextFormField(
                      initialValue: signature.positionLeft,
                      decoration: const InputDecoration(
                        labelText: 'Pos. Esquerda',
                        border: OutlineInputBorder(),
                        hintText: '0–100',
                      ),
                      keyboardType: TextInputType.number,
                      validator: (v) => viewModel.validateSignatureNumber(
                          v, 'Pos. Esquerda'),
                      onChanged: (v) => viewModel.updatePlaceSignature(
                        index,
                        viewModel.placeSignatures[index]
                            .copyWith(positionLeft: v),
                      ),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: AppSpacing.sm),
              Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Expanded(
                    child: TextFormField(
                      initialValue: signature.sizeX,
                      decoration: const InputDecoration(
                        labelText: 'Tam. Horizontal',
                        border: OutlineInputBorder(),
                        hintText: '0–100',
                      ),
                      keyboardType: TextInputType.number,
                      validator: (v) => viewModel.validateSignatureNumber(
                          v, 'Tam. Horizontal'),
                      onChanged: (v) => viewModel.updatePlaceSignature(
                        index,
                        viewModel.placeSignatures[index].copyWith(sizeX: v),
                      ),
                    ),
                  ),
                  const SizedBox(width: AppSpacing.sm),
                  Expanded(
                    child: TextFormField(
                      initialValue: signature.sizeY,
                      decoration: const InputDecoration(
                        labelText: 'Tam. Vertical',
                        border: OutlineInputBorder(),
                        hintText: '0–100',
                      ),
                      keyboardType: TextInputType.number,
                      validator: (v) => viewModel.validateSignatureNumber(
                          v, 'Tam. Vertical'),
                      onChanged: (v) => viewModel.updatePlaceSignature(
                        index,
                        viewModel.placeSignatures[index].copyWith(sizeY: v),
                      ),
                    ),
                  ),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }
}

/// Expandable section that lazily loads and displays the recover data models
/// as an interactive JSON tree viewer.
class _DataModelsSection extends StatefulWidget {
  const _DataModelsSection({required this.viewModel});

  final DocumentTemplateFormViewModel viewModel;

  @override
  State<_DataModelsSection> createState() => _DataModelsSectionState();
}

class _DataModelsSectionState extends State<_DataModelsSection> {
  bool _loaded = false;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Card.outlined(
      clipBehavior: Clip.antiAlias,
      child: ExpansionTile(
        leading: Icon(
          Icons.data_object_outlined,
          size: 20,
          color: theme.colorScheme.primary,
        ),
        title: Text(
          'Modelos de Dados',
          style: theme.textTheme.titleMedium?.copyWith(
            color: theme.colorScheme.primary,
          ),
        ),
        onExpansionChanged: (expanded) {
          if (expanded && !_loaded) {
            _loaded = true;
            widget.viewModel.loadRecoverDataModels();
          }
        },
        children: [
          ListenableBuilder(
            listenable: widget.viewModel,
            builder: (context, _) {
              final raw = widget.viewModel.recoverDataModels;
              if (raw == '{}') {
                return const Padding(
                  padding: EdgeInsets.all(AppSpacing.md),
                  child: Center(child: CircularProgressIndicator()),
                );
              }

              return SizedBox(
                width: double.infinity,
                height: 400,
                child: Padding(
                  padding: const EdgeInsets.all(AppSpacing.sm),
                  child: SingleChildScrollView(
                    child: JsonView.string(
                      raw,
                      theme: Theme.of(context).brightness == Brightness.dark
                          ? const JsonViewTheme()
                          : JsonViewTheme(
                              backgroundColor:
                                  Theme.of(context).colorScheme.surfaceContainer,
                              defaultTextStyle: TextStyle(
                                fontSize: 16,
                                fontWeight: FontWeight.w500,
                                color: Theme.of(context).colorScheme.onSurface,
                              ),
                              keyStyle: TextStyle(
                                color: Theme.of(context).colorScheme.primary,
                              ),
                              intStyle: TextStyle(
                                color: Theme.of(context).colorScheme.tertiary,
                              ),
                              doubleStyle: TextStyle(
                                color: Theme.of(context).colorScheme.tertiary,
                              ),
                              boolStyle: TextStyle(
                                color: Theme.of(context)
                                    .colorScheme
                                    .secondary,
                              ),
                              stringStyle: TextStyle(
                                color: Theme.of(context)
                                    .colorScheme
                                    .error,
                              ),
                              openIcon: Icon(
                                Icons.arrow_drop_down,
                                size: 18,
                                color: Theme.of(context).colorScheme.onSurface,
                              ),
                              closeIcon: Icon(
                                Icons.arrow_drop_up,
                                size: 18,
                                color: Theme.of(context).colorScheme.onSurface,
                              ),
                            ),
                    ),
                  ),
                ),
              );
            },
          ),
        ],
      ),
    );
  }
}

/// Save and cancel action buttons for the form.
class _FormActions extends StatelessWidget {
  const _FormActions({
    required this.isSaving,
    required this.onSave,
    required this.onCancel,
  });

  final bool isSaving;
  final VoidCallback onSave;
  final VoidCallback onCancel;

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisAlignment: MainAxisAlignment.end,
      children: [
        OutlinedButton(
          onPressed: isSaving ? null : onCancel,
          child: const Text('Cancelar'),
        ),
        const SizedBox(width: AppSpacing.md),
        FilledButton.icon(
          onPressed: isSaving ? null : onSave,
          icon: isSaving
              ? const SizedBox(
                  width: 18,
                  height: 18,
                  child: CircularProgressIndicator(strokeWidth: 2),
                )
              : const Icon(Icons.save_outlined, size: 20),
          label: Text(isSaving ? 'Salvando...' : 'Salvar'),
        ),
      ],
    );
  }
}
