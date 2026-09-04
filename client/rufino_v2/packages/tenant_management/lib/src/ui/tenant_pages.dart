/// As páginas que o roteador constrói — cada uma dona do ciclo de vida do seu
/// ViewModel.
///
/// **Por que existir uma camada só para isso.** O `go_router` reexecuta o
/// builder da rota a cada mudança na pilha de navegação. Criar o ViewModel
/// dentro do builder faz nascer uma instância nova a cada `push`/`pop` — e como
/// o `State` da tela sobrevive ao rebuild, o `initState` que dispara o
/// carregamento **não roda de novo**. O resultado é uma tela que volta e fica
/// girando para sempre, com um ViewModel recém-criado que ninguém mandou
/// carregar. Foi exatamente o que aconteceu ao voltar do cadastro para o
/// seletor.
///
/// Mesma disciplina do `DocumentDashboardPage` do People Management.
library;

import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:rufino_core/rufino_core.dart';

import '../domain/tenant_repository.dart';
import 'create/tenant_form_screen.dart';
import 'create/tenant_form_viewmodel.dart';
import 'detail/tenant_detail_screen.dart';
import 'detail/tenant_detail_viewmodel.dart';
import 'list/tenant_list_screen.dart';
import 'list/tenant_list_viewmodel.dart';
import 'select/tenant_selection_screen.dart';
import 'select/tenant_selection_viewmodel.dart';


/// Página do seletor de cliente.
class TenantSelectionPage extends StatefulWidget {
  /// Cria a página.
  const TenantSelectionPage({
    super.key,
    required this.onTenantSelected,
    required this.onSelected,
    required this.onOpenBackOffice,
    required this.onLogout,
    required this.homeRoute,
  });

  /// O que a casca faz quando um tenant vira o contexto.
  final TenantSelectedCallback onTenantSelected;

  /// Navegação após a escolha.
  final VoidCallback onSelected;

  /// Abre o back-office.
  final VoidCallback onOpenBackOffice;

  /// Encerra a sessão.
  final Future<void> Function() onLogout;

  /// Para onde o voltar leva quando já existe um cliente em contexto.
  final String homeRoute;

  @override
  State<TenantSelectionPage> createState() => _TenantSelectionPageState();
}

class _TenantSelectionPageState extends State<TenantSelectionPage> {
  late final TenantSelectionViewModel _viewModel;

  @override
  void initState() {
    super.initState();
    _viewModel = TenantSelectionViewModel(
      repository: context.read<TenantRepository>(),
      tenantContext: context.read<TenantContextNotifier>(),
      onTenantSelected: widget.onTenantSelected,
    );
  }

  @override
  void dispose() {
    _viewModel.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return TenantSelectionScreen(
      viewModel: _viewModel,
      backFallback: widget.homeRoute,
      onSelected: widget.onSelected,
      onOpenBackOffice: widget.onOpenBackOffice,
      onLogout: widget.onLogout,
    );
  }
}

/// Página da listagem do back-office.
class TenantListPage extends StatefulWidget {
  /// Cria a página.
  const TenantListPage({
    super.key,
    required this.onOpenTenant,
    required this.onCreateTenant,
    required this.backFallback,
  });

  /// Abre um tenant da lista.
  final void Function(String id) onOpenTenant;

  /// Abre o cadastro de um cliente novo.
  final VoidCallback onCreateTenant;

  /// Para onde o voltar leva quando não há pilha.
  final String backFallback;

  @override
  State<TenantListPage> createState() => _TenantListPageState();
}

class _TenantListPageState extends State<TenantListPage> {
  late final TenantListViewModel _viewModel;

  @override
  void initState() {
    super.initState();
    _viewModel = TenantListViewModel(repository: context.read<TenantRepository>());
  }

  @override
  void dispose() {
    _viewModel.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return TenantListScreen(
      viewModel: _viewModel,
      backFallback: widget.backFallback,
      onOpenTenant: widget.onOpenTenant,
      onCreateTenant: widget.onCreateTenant,
    );
  }
}

/// Página do cadastro completo de um tenant.
class TenantDetailPage extends StatefulWidget {
  /// Cria a página para [tenantId].
  const TenantDetailPage({
    super.key,
    required this.tenantId,
    required this.backFallback,
    this.cepService,
  });

  /// O tenant sendo mostrado.
  final String tenantId;

  /// Consulta de CEP, quando a casca a fornece.
  final CepLookupService? cepService;

  /// Para onde o voltar leva quando não há pilha.
  final String backFallback;

  @override
  State<TenantDetailPage> createState() => _TenantDetailPageState();
}

class _TenantDetailPageState extends State<TenantDetailPage> {
  late final TenantDetailViewModel _viewModel;

  @override
  void initState() {
    super.initState();
    _viewModel = TenantDetailViewModel(
      repository: context.read<TenantRepository>(),
      tenantId: widget.tenantId,
      cepService: widget.cepService,
    );
  }

  @override
  void dispose() {
    _viewModel.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return TenantDetailScreen(
      viewModel: _viewModel,
      backFallback: widget.backFallback,
    );
  }
}

/// Página do cadastro de um cliente novo.
class TenantFormPage extends StatefulWidget {
  /// Cria a página.
  const TenantFormPage({
    super.key,
    required this.onRegistered,
    required this.backFallback,
    this.cepService,
  });

  /// Consulta de CEP, quando a casca a fornece.
  final CepLookupService? cepService;

  /// Chamada com o id do tenant recém-cadastrado.
  final void Function(String id) onRegistered;

  /// Para onde o voltar leva quando não há pilha.
  final String backFallback;

  @override
  State<TenantFormPage> createState() => _TenantFormPageState();
}

class _TenantFormPageState extends State<TenantFormPage> {
  late final TenantFormViewModel _viewModel;

  @override
  void initState() {
    super.initState();
    _viewModel = TenantFormViewModel(
      repository: context.read<TenantRepository>(),
      cepService: widget.cepService,
    );
  }

  @override
  void dispose() {
    _viewModel.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return TenantFormScreen(
      viewModel: _viewModel,
      backFallback: widget.backFallback,
      onRegistered: widget.onRegistered,
    );
  }
}
