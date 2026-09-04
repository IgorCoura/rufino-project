import 'package:flutter/material.dart';
import 'package:mask_text_input_formatter/mask_text_input_formatter.dart';
import 'package:rufino_core/rufino_core.dart';

import '../../data/tenant_api_models.dart';
import '../../domain/tenant.dart';
import '../../domain/tenant_enums.dart';
import '../tenant_back_button.dart';
import 'tenant_form_viewmodel.dart';

const _cpfMaskPattern = '###.###.###-##';
const _cnpjMaskPattern = '##.###.###/####-##';

/// Cadastro of a new tenant — the platform's single door for a new customer.
///
/// Registering and granting the owner's access are one act: a cadastro nobody
/// can open is the state nobody notices until they need it.
class TenantFormScreen extends StatefulWidget {
  /// Creates the screen.
  const TenantFormScreen({
    super.key,
    required this.viewModel,
    required this.backFallback,
    required this.onRegistered,
  });

  /// Drives the screen.
  final TenantFormViewModel viewModel;

  /// Para onde o voltar leva quando não há pilha.
  final String backFallback;

  /// Called with the id of the tenant that was just registered.
  final void Function(String id) onRegistered;

  @override
  State<TenantFormScreen> createState() => _TenantFormScreenState();
}

class _TenantFormScreenState extends State<TenantFormScreen> {
  final _formKey = GlobalKey<FormState>();

  final _legalNameController = TextEditingController();
  final _tradeNameController = TextEditingController();
  final _taxIdController = TextEditingController();
  final _emailController = TextEditingController();
  final _phoneController = TextEditingController();
  final _ownerEmailController = TextEditingController();
  final _idController = TextEditingController();

  final _zipController = TextEditingController();
  final _streetController = TextEditingController();
  final _numberController = TextEditingController();
  final _complementController = TextEditingController();
  final _neighborhoodController = TextEditingController();
  final _cityController = TextEditingController();
  final _stateController = TextEditingController();

  final _phoneMask = MaskTextInputFormatter(
    mask: '(##) #####-####',
    filter: {'#': RegExp(r'[0-9]')},
    type: MaskAutoCompletionType.lazy,
  );
  final _zipMask = MaskTextInputFormatter(
    mask: '#####-###',
    filter: {'#': RegExp(r'[0-9]')},
    type: MaskAutoCompletionType.lazy,
  );
  final _taxIdMask = MaskTextInputFormatter(
    mask: _cnpjMaskPattern,
    filter: {'#': RegExp(r'[0-9]')},
    type: MaskAutoCompletionType.lazy,
  );

  bool _isLookingUpCep = false;

  @override
  void initState() {
    super.initState();
    widget.viewModel.addListener(_onKindChanged);
  }

  @override
  void dispose() {
    widget.viewModel.removeListener(_onKindChanged);
    for (final controller in [
      _legalNameController,
      _tradeNameController,
      _taxIdController,
      _emailController,
      _phoneController,
      _ownerEmailController,
      _idController,
      _zipController,
      _streetController,
      _numberController,
      _complementController,
      _neighborhoodController,
      _cityController,
      _stateController,
    ]) {
      controller.dispose();
    }
    super.dispose();
  }

  void _onKindChanged() {
    final pattern =
        widget.viewModel.isIndividual ? _cpfMaskPattern : _cnpjMaskPattern;
    if (_taxIdMask.getMask() == pattern) return;

    // Trocar o tipo troca o documento pedido: manter os dígitos do outro
    // formato só produziria um erro de validação que a pessoa não pediu.
    _taxIdMask.updateMask(mask: pattern);
    _taxIdController.clear();
    if (widget.viewModel.isIndividual) _tradeNameController.clear();
  }

  Future<void> _lookupCep() async {
    final digits = _zipController.text.replaceAll(RegExp(r'[^\d]'), '');
    if (digits.length != 8) return;

    setState(() => _isLookingUpCep = true);
    final lookup = await widget.viewModel.lookupCep(digits);
    if (!mounted) return;

    if (lookup != null) {
      _streetController.text = lookup.street;
      _neighborhoodController.text = lookup.neighborhood;
      _cityController.text = lookup.city;
      _stateController.text = lookup.state;
    }
    setState(() => _isLookingUpCep = false);
  }

  Future<void> _submit() async {
    if (_formKey.currentState?.validate() != true) return;

    final id = await widget.viewModel.submit(
      RegisterTenantInput(
        kind: widget.viewModel.kind,
        legalName: _legalNameController.text,
        tradeName:
            widget.viewModel.isIndividual ? '' : _tradeNameController.text,
        primaryTaxId: _taxIdController.text.replaceAll(RegExp(r'[^\d]'), ''),
        contactEmail: _emailController.text,
        contactPhone: _phoneController.text.replaceAll(RegExp(r'[^\d]'), ''),
        address: TenantAddress(
          zipCode: _zipController.text.replaceAll(RegExp(r'[^\d]'), ''),
          street: _streetController.text,
          number: _numberController.text,
          complement: _complementController.text,
          neighborhood: _neighborhoodController.text,
          city: _cityController.text,
          state: _stateController.text.toUpperCase(),
        ),
        ownerEmail: _ownerEmailController.text,
        products: widget.viewModel.products.toList(),
        id: _idController.text.trim().isEmpty ? null : _idController.text.trim(),
      ),
    );

    if (!mounted || id == null) return;
    widget.onRegistered(id);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Cadastrar cliente'),
        leading: TenantBackButton(fallback: widget.backFallback),
      ),
      body: SafeArea(
        child: ListenableBuilder(
          listenable: widget.viewModel,
          builder: (context, _) => Center(
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: AppBreakpoints.tablet),
              child: Form(
                key: _formKey,
                child: ListView(
                  padding: const EdgeInsets.all(AppSpacing.md),
                  children: [
                    _KindSection(viewModel: widget.viewModel),
                    const SizedBox(height: AppSpacing.md),
                    _IdentificationSection(
                      viewModel: widget.viewModel,
                      legalNameController: _legalNameController,
                      tradeNameController: _tradeNameController,
                      taxIdController: _taxIdController,
                      taxIdMask: _taxIdMask,
                    ),
                    const SizedBox(height: AppSpacing.md),
                    _ContactSection(
                      emailController: _emailController,
                      phoneController: _phoneController,
                      phoneMask: _phoneMask,
                      isSaving: widget.viewModel.isSaving,
                    ),
                    const SizedBox(height: AppSpacing.md),
                    _AddressSection(
                      zipController: _zipController,
                      zipMask: _zipMask,
                      streetController: _streetController,
                      numberController: _numberController,
                      complementController: _complementController,
                      neighborhoodController: _neighborhoodController,
                      cityController: _cityController,
                      stateController: _stateController,
                      isSaving: widget.viewModel.isSaving,
                      isLookingUpCep: _isLookingUpCep,
                      onLookupCep: _lookupCep,
                    ),
                    const SizedBox(height: AppSpacing.md),
                    _OwnerSection(
                      controller: _ownerEmailController,
                      isSaving: widget.viewModel.isSaving,
                    ),
                    const SizedBox(height: AppSpacing.md),
                    _ProductsSection(viewModel: widget.viewModel),
                    const SizedBox(height: AppSpacing.md),
                    _AdvancedSection(controller: _idController),
                    if (widget.viewModel.errorMessage != null) ...[
                      const SizedBox(height: AppSpacing.md),
                      _ErrorBanner(message: widget.viewModel.errorMessage!),
                    ],
                    const SizedBox(height: AppSpacing.lg),
                    if (widget.viewModel.isSaving)
                      const Center(child: CircularProgressIndicator())
                    else
                      FilledButton(
                        onPressed: _submit,
                        child: const Text('Cadastrar cliente'),
                      ),
                    const SizedBox(height: AppSpacing.xl),
                  ],
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}

class _KindSection extends StatelessWidget {
  const _KindSection({required this.viewModel});

  final TenantFormViewModel viewModel;

  @override
  Widget build(BuildContext context) {
    return SectionCard(
      title: 'Tipo de cliente',
      child: SegmentedButton<String>(
        segments: const [
          ButtonSegment(
            value: TenantKinds.company,
            label: Text('Pessoa jurídica'),
          ),
          ButtonSegment(
            value: TenantKinds.individual,
            label: Text('Pessoa física'),
          ),
        ],
        selected: {viewModel.kind},
        onSelectionChanged: viewModel.isSaving
            ? null
            : (selection) => viewModel.setKind(selection.first),
      ),
    );
  }
}

class _IdentificationSection extends StatelessWidget {
  const _IdentificationSection({
    required this.viewModel,
    required this.legalNameController,
    required this.tradeNameController,
    required this.taxIdController,
    required this.taxIdMask,
  });

  final TenantFormViewModel viewModel;
  final TextEditingController legalNameController;
  final TextEditingController tradeNameController;
  final TextEditingController taxIdController;
  final MaskTextInputFormatter taxIdMask;

  @override
  Widget build(BuildContext context) {
    final isIndividual = viewModel.isIndividual;

    return SectionCard(
      title: 'Identificação',
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          TextFormField(
            controller: legalNameController,
            enabled: !viewModel.isSaving,
            decoration: InputDecoration(
              labelText: isIndividual ? 'Nome completo' : 'Razão social',
              border: const OutlineInputBorder(),
            ),
            validator: Tenant.validateLegalName,
          ),
          if (!isIndividual) ...[
            const SizedBox(height: AppSpacing.md),
            TextFormField(
              controller: tradeNameController,
              enabled: !viewModel.isSaving,
              decoration: const InputDecoration(
                labelText: 'Nome fantasia (opcional)',
                border: OutlineInputBorder(),
              ),
              validator: (value) =>
                  Tenant.validateTradeName(viewModel.kind, value),
            ),
          ],
          const SizedBox(height: AppSpacing.md),
          TextFormField(
            controller: taxIdController,
            enabled: !viewModel.isSaving,
            decoration: InputDecoration(
              labelText: isIndividual ? 'CPF' : 'CNPJ',
              border: const OutlineInputBorder(),
            ),
            keyboardType: TextInputType.number,
            inputFormatters: [taxIdMask],
            validator: (value) => Tenant.validateTaxId(viewModel.kind, value),
          ),
        ],
      ),
    );
  }
}

class _ContactSection extends StatelessWidget {
  const _ContactSection({
    required this.emailController,
    required this.phoneController,
    required this.phoneMask,
    required this.isSaving,
  });

  final TextEditingController emailController;
  final TextEditingController phoneController;
  final MaskTextInputFormatter phoneMask;
  final bool isSaving;

  @override
  Widget build(BuildContext context) {
    return SectionCard(
      title: 'Contato',
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          TextFormField(
            controller: emailController,
            enabled: !isSaving,
            decoration: const InputDecoration(
              labelText: 'E-mail',
              border: OutlineInputBorder(),
            ),
            keyboardType: TextInputType.emailAddress,
            validator: Tenant.validateEmail,
          ),
          const SizedBox(height: AppSpacing.md),
          TextFormField(
            controller: phoneController,
            enabled: !isSaving,
            decoration: const InputDecoration(
              labelText: 'Telefone (opcional)',
              border: OutlineInputBorder(),
            ),
            keyboardType: TextInputType.phone,
            inputFormatters: [phoneMask],
            validator: Tenant.validatePhone,
          ),
        ],
      ),
    );
  }
}

class _AddressSection extends StatelessWidget {
  const _AddressSection({
    required this.zipController,
    required this.zipMask,
    required this.streetController,
    required this.numberController,
    required this.complementController,
    required this.neighborhoodController,
    required this.cityController,
    required this.stateController,
    required this.isSaving,
    required this.isLookingUpCep,
    required this.onLookupCep,
  });

  final TextEditingController zipController;
  final MaskTextInputFormatter zipMask;
  final TextEditingController streetController;
  final TextEditingController numberController;
  final TextEditingController complementController;
  final TextEditingController neighborhoodController;
  final TextEditingController cityController;
  final TextEditingController stateController;
  final bool isSaving;
  final bool isLookingUpCep;
  final Future<void> Function() onLookupCep;

  @override
  Widget build(BuildContext context) {
    return SectionCard(
      title: 'Endereço',
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          TextFormField(
            controller: zipController,
            enabled: !isSaving,
            decoration: InputDecoration(
              labelText: 'CEP',
              border: const OutlineInputBorder(),
              suffixIcon: isLookingUpCep
                  ? const Padding(
                      padding: EdgeInsets.all(AppSpacing.sm),
                      child: SizedBox(
                        width: 20,
                        height: 20,
                        child: CircularProgressIndicator(strokeWidth: 2),
                      ),
                    )
                  : IconButton(
                      tooltip: 'Buscar endereço pelo CEP',
                      icon: const Icon(Icons.search),
                      onPressed: onLookupCep,
                    ),
            ),
            keyboardType: TextInputType.number,
            inputFormatters: [zipMask],
            validator: Tenant.validateZipCode,
            onEditingComplete: onLookupCep,
          ),
          const SizedBox(height: AppSpacing.md),
          TextFormField(
            controller: streetController,
            enabled: !isSaving,
            decoration: const InputDecoration(
              labelText: 'Logradouro',
              border: OutlineInputBorder(),
            ),
            validator: Tenant.validateRequired,
          ),
          const SizedBox(height: AppSpacing.md),
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(
                child: TextFormField(
                  controller: numberController,
                  enabled: !isSaving,
                  decoration: const InputDecoration(
                    labelText: 'Número',
                    border: OutlineInputBorder(),
                  ),
                  validator: Tenant.validateRequired,
                ),
              ),
              const SizedBox(width: AppSpacing.md),
              Expanded(
                flex: 2,
                child: TextFormField(
                  controller: complementController,
                  enabled: !isSaving,
                  decoration: const InputDecoration(
                    labelText: 'Complemento (opcional)',
                    border: OutlineInputBorder(),
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: AppSpacing.md),
          TextFormField(
            controller: neighborhoodController,
            enabled: !isSaving,
            decoration: const InputDecoration(
              labelText: 'Bairro',
              border: OutlineInputBorder(),
            ),
            validator: Tenant.validateRequired,
          ),
          const SizedBox(height: AppSpacing.md),
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(
                flex: 3,
                child: TextFormField(
                  controller: cityController,
                  enabled: !isSaving,
                  decoration: const InputDecoration(
                    labelText: 'Cidade',
                    border: OutlineInputBorder(),
                  ),
                  validator: Tenant.validateRequired,
                ),
              ),
              const SizedBox(width: AppSpacing.md),
              Expanded(
                child: TextFormField(
                  controller: stateController,
                  enabled: !isSaving,
                  textCapitalization: TextCapitalization.characters,
                  maxLength: 2,
                  decoration: const InputDecoration(
                    labelText: 'UF',
                    border: OutlineInputBorder(),
                    counterText: '',
                  ),
                  validator: Tenant.validateState,
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _OwnerSection extends StatelessWidget {
  const _OwnerSection({required this.controller, required this.isSaving});

  final TextEditingController controller;
  final bool isSaving;

  @override
  Widget build(BuildContext context) {
    return SectionCard(
      title: 'Responsável',
      child: TextFormField(
        controller: controller,
        enabled: !isSaving,
        decoration: const InputDecoration(
          labelText: 'E-mail do responsável',
          border: OutlineInputBorder(),
          helperText: 'Este e-mail recebe o convite de acesso e passa a '
              'responder pelo cliente.',
          helperMaxLines: 3,
        ),
        keyboardType: TextInputType.emailAddress,
        validator: Tenant.validateEmail,
      ),
    );
  }
}

class _ProductsSection extends StatelessWidget {
  const _ProductsSection({required this.viewModel});

  final TenantFormViewModel viewModel;

  @override
  Widget build(BuildContext context) {
    return SectionCard(
      title: 'Produtos',
      child: Column(
        children: [
          for (final product in const [
            TenantProducts.peopleManagement,
            TenantProducts.billPayment,
          ])
            CheckboxListTile(
              contentPadding: EdgeInsets.zero,
              title: Text(TenantProductLabels.label(product)),
              value: viewModel.products.contains(product),
              onChanged: viewModel.isSaving
                  ? null
                  : (_) => viewModel.toggleProduct(product),
            ),
        ],
      ),
    );
  }
}

class _AdvancedSection extends StatelessWidget {
  const _AdvancedSection({required this.controller});

  final TextEditingController controller;

  @override
  Widget build(BuildContext context) {
    return Card.outlined(
      clipBehavior: Clip.antiAlias,
      child: ExpansionTile(
        title: const Text('Avançado'),
        childrenPadding: const EdgeInsets.all(AppSpacing.md),
        children: [
          TextFormField(
            controller: controller,
            decoration: const InputDecoration(
              labelText: 'Informar Id manualmente',
              border: OutlineInputBorder(),
              helperText: 'Só para migrar um cadastro que já tem identidade '
                  'em outro lugar. Em branco, o sistema emite um novo.',
              helperMaxLines: 3,
            ),
          ),
        ],
      ),
    );
  }
}

class _ErrorBanner extends StatelessWidget {
  const _ErrorBanner({required this.message});

  final String message;

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    return Card.filled(
      color: cs.errorContainer,
      child: Padding(
        padding: const EdgeInsets.all(AppSpacing.md),
        child: Row(
          children: [
            Icon(Icons.error_outline, color: cs.onErrorContainer),
            const SizedBox(width: AppSpacing.md),
            Expanded(
              child: Text(
                message,
                style: Theme.of(context)
                    .textTheme
                    .bodyMedium
                    ?.copyWith(color: cs.onErrorContainer),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
