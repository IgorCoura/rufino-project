/// As páginas que o roteador constrói — cada uma dona do ciclo de vida do seu
/// ViewModel.
///
/// **Por que existir uma camada só para isso.** O `go_router` reexecuta o
/// builder da rota a cada mudança na pilha de navegação. Criar o ViewModel
/// dentro do builder faz nascer uma instância nova a cada `push`/`pop` — e
/// como o `State` da tela sobrevive ao rebuild, o `initState` que dispara o
/// carregamento **não roda de novo**. O resultado é uma tela que volta e fica
/// girando para sempre. Mesma disciplina do `tenant_pages.dart`.
library;

import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../domain/bill_repository.dart';
import '../domain/capture_item_repository.dart';
import '../domain/captured_message_repository.dart';
import '../domain/trusted_origin_repository.dart';
import '../domain/capture_source_repository.dart';
import '../domain/expectation_repository.dart';
import '../domain/payee_repository.dart';
import '../domain/payer_profile_repository.dart';
import '../domain/payment_repository.dart';
import 'bills/bill_detail_screen.dart';
import 'bills/bill_detail_viewmodel.dart';
import 'bills/bill_import_screen.dart';
import 'bills/bill_import_viewmodel.dart';
import 'bills/bill_list_screen.dart';
import 'bills/bill_list_viewmodel.dart';
import 'capture_items/capture_item_detail_screen.dart';
import 'capture_items/capture_item_detail_viewmodel.dart';
import 'shared/document_picker.dart';
import 'capture_items/capture_item_list_screen.dart';
import 'capture_items/capture_item_list_viewmodel.dart';
import 'captured_messages/captured_message_list_screen.dart';
import 'captured_messages/captured_message_list_viewmodel.dart';
import 'capture_sources/capture_source_connect_screen.dart';
import 'capture_sources/capture_source_connect_viewmodel.dart';
import 'capture_sources/capture_source_detail_screen.dart';
import 'capture_sources/capture_source_detail_viewmodel.dart';
import 'capture_sources/capture_source_list_screen.dart';
import 'capture_sources/capture_source_list_viewmodel.dart';
import 'expectations/expectation_detail_screen.dart';
import 'expectations/expectation_detail_viewmodel.dart';
import 'expectations/expectation_form_screen.dart';
import 'expectations/expectation_form_viewmodel.dart';
import 'expectations/expectation_list_screen.dart';
import 'expectations/expectation_list_viewmodel.dart';
import 'payees/payee_detail_screen.dart';
import 'payees/payee_detail_viewmodel.dart';
import 'payees/payee_form_screen.dart';
import 'payees/payee_form_viewmodel.dart';
import 'payees/payee_list_screen.dart';
import 'payees/payee_list_viewmodel.dart';
import 'payer_profile/payer_profile_screen.dart';
import 'payer_profile/payer_profile_viewmodel.dart';
import 'pending/pending_screen.dart';
import 'pending/pending_viewmodel.dart';
import 'shared/artifact_viewer_screen.dart';
import 'shared/email_viewer_screen.dart';
import 'shared/artifact_viewer_viewmodel.dart';
import 'trusted_origins/trusted_origin_list_screen.dart';
import 'trusted_origins/trusted_origin_list_viewmodel.dart';

/// Página da lista de origens confiáveis.
class TrustedOriginListPage extends StatefulWidget {
  /// Cria a página.
  const TrustedOriginListPage({super.key, required this.backFallback});

  /// Para onde o voltar leva quando não há pilha.
  final String backFallback;

  @override
  State<TrustedOriginListPage> createState() => _TrustedOriginListPageState();
}

class _TrustedOriginListPageState extends State<TrustedOriginListPage> {
  late final TrustedOriginListViewModel _viewModel;

  @override
  void initState() {
    super.initState();
    _viewModel = TrustedOriginListViewModel(
      repository: context.read<TrustedOriginRepository>(),
    );
  }

  @override
  void dispose() {
    _viewModel.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return TrustedOriginListScreen(
      viewModel: _viewModel,
      backFallback: widget.backFallback,
    );
  }
}

/// Página do painel de pendências.
class PendingPage extends StatefulWidget {
  /// Cria a página.
  const PendingPage({
    super.key,
    required this.backFallback,
    required this.onOpenApprovalQueue,
    required this.onOpenExpectation,
    required this.onOpenCaptureItem,
    required this.onOpenPayerProfile,
  });

  /// Para onde o voltar leva quando não há pilha.
  final String backFallback;

  /// Abre a fila de aprovação.
  final VoidCallback onOpenApprovalQueue;

  /// Abre uma expectativa.
  final void Function(String id) onOpenExpectation;

  /// Abre um item da quarentena.
  final void Function(String id) onOpenCaptureItem;

  /// Abre o perfil do pagador.
  final VoidCallback onOpenPayerProfile;

  @override
  State<PendingPage> createState() => _PendingPageState();
}

class _PendingPageState extends State<PendingPage> {
  late final PendingViewModel _viewModel;

  @override
  void initState() {
    super.initState();
    _viewModel = PendingViewModel(
      expectationRepository: context.read<ExpectationRepository>(),
      billRepository: context.read<BillRepository>(),
      payerProfileRepository: context.read<PayerProfileRepository>(),
    );
  }

  @override
  void dispose() {
    _viewModel.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return PendingScreen(
      viewModel: _viewModel,
      backFallback: widget.backFallback,
      onOpenApprovalQueue: widget.onOpenApprovalQueue,
      onOpenExpectation: widget.onOpenExpectation,
      onOpenCaptureItem: widget.onOpenCaptureItem,
      onOpenPayerProfile: widget.onOpenPayerProfile,
    );
  }
}

/// Página da lista de boletos.
class BillListPage extends StatefulWidget {
  /// Cria a página, opcionalmente já filtrada por [initialStatus].
  const BillListPage({
    super.key,
    required this.backFallback,
    required this.onOpenBill,
    required this.onImportBill,
    this.initialStatus,
  });

  /// Para onde o voltar leva quando não há pilha.
  final String backFallback;

  /// Abre um boleto.
  final void Function(String id) onOpenBill;

  /// Abre a importação manual.
  final VoidCallback onImportBill;

  /// Filtro de status inicial.
  final String? initialStatus;

  @override
  State<BillListPage> createState() => _BillListPageState();
}

class _BillListPageState extends State<BillListPage> {
  late final BillListViewModel _viewModel;

  @override
  void initState() {
    super.initState();
    _viewModel = BillListViewModel(
      repository: context.read<BillRepository>(),
      initialStatus: widget.initialStatus,
    );
  }

  @override
  void dispose() {
    _viewModel.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return BillListScreen(
      viewModel: _viewModel,
      backFallback: widget.backFallback,
      onOpenBill: widget.onOpenBill,
      onImportBill: widget.onImportBill,
    );
  }
}

/// Página do detalhe de um boleto.
class BillDetailPage extends StatefulWidget {
  /// Cria a página para [billId].
  const BillDetailPage({
    super.key,
    required this.billId,
    required this.backFallback,
    required this.onOpenArtifact,
    required this.onOpenEmail,
    this.onOpenReceipt,
  });

  /// O boleto sendo mostrado.
  final String billId;

  /// Para onde o voltar leva quando não há pilha.
  final String backFallback;

  /// Abre o documento original do boleto.
  final VoidCallback onOpenArtifact;

  /// Abre o e-mail que trouxe o boleto.
  final VoidCallback onOpenEmail;

  /// Abre o comprovante do pagamento (fase 3). Nulo esconde o botão.
  final VoidCallback? onOpenReceipt;

  @override
  State<BillDetailPage> createState() => _BillDetailPageState();
}

class _BillDetailPageState extends State<BillDetailPage> {
  late final BillDetailViewModel _viewModel;

  @override
  void initState() {
    super.initState();
    _viewModel = BillDetailViewModel(
      repository: context.read<BillRepository>(),
      // Nulo quando a casca ainda não registrou o repositório de pagamento:
      // a seção da execução simplesmente não carrega, e o resto da tela vive.
      paymentRepository: _maybeRead<PaymentRepository>(context),
      billId: widget.billId,
    );
  }

  static T? _maybeRead<T>(BuildContext context) {
    try {
      return context.read<T>();
    } on ProviderNotFoundException {
      return null;
    }
  }

  @override
  void dispose() {
    _viewModel.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return BillDetailScreen(
      viewModel: _viewModel,
      backFallback: widget.backFallback,
      onOpenArtifact: widget.onOpenArtifact,
      onOpenEmail: widget.onOpenEmail,
      onOpenReceipt: widget.onOpenReceipt,
    );
  }
}

/// Página do comprovante de pagamento de um boleto (fase 3).
class BillReceiptPage extends StatefulWidget {
  /// Cria a página para [billId].
  const BillReceiptPage({
    super.key,
    required this.billId,
    required this.backFallback,
  });

  /// O boleto cujo comprovante está sendo mostrado.
  final String billId;

  /// Para onde o voltar leva quando não há pilha.
  final String backFallback;

  @override
  State<BillReceiptPage> createState() => _BillReceiptPageState();
}

class _BillReceiptPageState extends State<BillReceiptPage> {
  late final ArtifactViewerViewModel _viewModel;

  @override
  void initState() {
    super.initState();
    final repository = context.read<PaymentRepository>();
    _viewModel = ArtifactViewerViewModel(
      load: () => repository.getReceiptForBill(widget.billId),
    );
  }

  @override
  void dispose() {
    _viewModel.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return ArtifactViewerScreen(
      viewModel: _viewModel,
      title: 'Comprovante de pagamento',
      backFallback: widget.backFallback,
    );
  }
}

/// Página da importação manual.
class BillImportPage extends StatefulWidget {
  /// Cria a página.
  const BillImportPage({
    super.key,
    required this.backFallback,
    required this.onImported,
    required this.onPickDocument,
  });

  /// Para onde o voltar leva quando não há pilha.
  final String backFallback;

  /// Chamada com o id do boleto recém-importado.
  final void Function(String id) onImported;

  /// Abre o seletor de arquivos do sistema. Vem da casca.
  final DocumentPicker onPickDocument;

  @override
  State<BillImportPage> createState() => _BillImportPageState();
}

class _BillImportPageState extends State<BillImportPage> {
  late final BillImportViewModel _viewModel;

  @override
  void initState() {
    super.initState();
    _viewModel =
        BillImportViewModel(repository: context.read<BillRepository>());
  }

  @override
  void dispose() {
    _viewModel.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return BillImportScreen(
      viewModel: _viewModel,
      backFallback: widget.backFallback,
      onImported: widget.onImported,
      onPickDocument: widget.onPickDocument,
    );
  }
}

/// Página da quarentena.
class CaptureItemListPage extends StatefulWidget {
  /// Cria a página.
  const CaptureItemListPage({
    super.key,
    required this.backFallback,
    required this.onOpenItem,
  });

  /// Para onde o voltar leva quando não há pilha.
  final String backFallback;

  /// Abre um item.
  final void Function(String id) onOpenItem;

  @override
  State<CaptureItemListPage> createState() => _CaptureItemListPageState();
}

class _CaptureItemListPageState extends State<CaptureItemListPage> {
  late final CaptureItemListViewModel _viewModel;

  @override
  void initState() {
    super.initState();
    _viewModel = CaptureItemListViewModel(
      repository: context.read<CaptureItemRepository>(),
    );
  }

  @override
  void dispose() {
    _viewModel.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return CaptureItemListScreen(
      viewModel: _viewModel,
      backFallback: widget.backFallback,
      onOpenItem: widget.onOpenItem,
    );
  }
}

/// Página do detalhe de um item da quarentena.
class CaptureItemDetailPage extends StatefulWidget {
  /// Cria a página para [itemId].
  const CaptureItemDetailPage({
    super.key,
    required this.itemId,
    required this.backFallback,
    required this.onOpenBill,
    required this.onOpenArtifact,
    required this.onOpenEmail,
    required this.onPickDocument,
    required this.onOpenLink,
  });

  /// O item sendo mostrado.
  final String itemId;

  /// Para onde o voltar leva quando não há pilha.
  final String backFallback;

  /// Abre um boleto ligado ao item.
  final void Function(String billId) onOpenBill;

  /// Abre o documento original do item.
  final VoidCallback onOpenArtifact;

  /// Abre o e-mail que trouxe o item.
  final VoidCallback onOpenEmail;

  /// Abre o seletor de arquivos para anexar o boleto obtido à mão.
  final DocumentPicker onPickDocument;

  /// Abre o endereço do documento no navegador do sistema.
  final LinkOpener onOpenLink;

  @override
  State<CaptureItemDetailPage> createState() => _CaptureItemDetailPageState();
}

class _CaptureItemDetailPageState extends State<CaptureItemDetailPage> {
  late final CaptureItemDetailViewModel _viewModel;

  @override
  void initState() {
    super.initState();
    _viewModel = CaptureItemDetailViewModel(
      repository: context.read<CaptureItemRepository>(),
      itemId: widget.itemId,
    );
  }

  @override
  void dispose() {
    _viewModel.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return CaptureItemDetailScreen(
      viewModel: _viewModel,
      backFallback: widget.backFallback,
      onOpenBill: widget.onOpenBill,
      onOpenArtifact: widget.onOpenArtifact,
      onOpenEmail: widget.onOpenEmail,
      onPickDocument: widget.onPickDocument,
      onOpenLink: widget.onOpenLink,
    );
  }
}

/// Página do documento original de um item da quarentena.
class CaptureItemArtifactPage extends StatefulWidget {
  /// Cria a página para [itemId].
  const CaptureItemArtifactPage({
    super.key,
    required this.itemId,
    required this.backFallback,
  });

  /// O item cujo documento está sendo mostrado.
  final String itemId;

  /// Para onde o voltar leva quando não há pilha.
  final String backFallback;

  @override
  State<CaptureItemArtifactPage> createState() =>
      _CaptureItemArtifactPageState();
}

class _CaptureItemArtifactPageState extends State<CaptureItemArtifactPage> {
  late final ArtifactViewerViewModel _viewModel;

  @override
  void initState() {
    super.initState();
    final repository = context.read<CaptureItemRepository>();
    _viewModel = ArtifactViewerViewModel(
      load: () => repository.getArtifact(widget.itemId),
    );
  }

  @override
  void dispose() {
    _viewModel.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return ArtifactViewerScreen(
      viewModel: _viewModel,
      title: 'Documento recebido',
      backFallback: widget.backFallback,
    );
  }
}

/// Página do e-mail que trouxe o item da quarentena.
class CaptureItemEmailPage extends StatefulWidget {
  /// Cria a página para [itemId].
  const CaptureItemEmailPage({
    super.key,
    required this.itemId,
    required this.backFallback,
  });

  /// O item cujo e-mail está sendo mostrado.
  final String itemId;

  /// Para onde o voltar leva quando não há pilha.
  final String backFallback;

  @override
  State<CaptureItemEmailPage> createState() => _CaptureItemEmailPageState();
}

class _CaptureItemEmailPageState extends State<CaptureItemEmailPage> {
  late final EmailViewerViewModel _viewModel;

  @override
  void initState() {
    super.initState();
    final repository = context.read<CaptureItemRepository>();
    _viewModel = EmailViewerViewModel(
      load: () => repository.getEmail(widget.itemId),
    );
  }

  @override
  void dispose() {
    _viewModel.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return EmailViewerScreen(
      viewModel: _viewModel,
      backFallback: widget.backFallback,
      title: 'E-mail do item',
    );
  }
}

/// Página do documento original de um boleto.
class BillArtifactPage extends StatefulWidget {
  /// Cria a página para [billId].
  const BillArtifactPage({
    super.key,
    required this.billId,
    required this.backFallback,
  });

  /// O boleto cujo documento está sendo mostrado.
  final String billId;

  /// Para onde o voltar leva quando não há pilha.
  final String backFallback;

  @override
  State<BillArtifactPage> createState() => _BillArtifactPageState();
}

class _BillArtifactPageState extends State<BillArtifactPage> {
  late final ArtifactViewerViewModel _viewModel;

  @override
  void initState() {
    super.initState();
    final repository = context.read<BillRepository>();
    _viewModel = ArtifactViewerViewModel(
      load: () => repository.getArtifact(widget.billId),
    );
  }

  @override
  void dispose() {
    _viewModel.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return ArtifactViewerScreen(
      viewModel: _viewModel,
      title: 'Documento do boleto',
      backFallback: widget.backFallback,
    );
  }
}

/// Página do e-mail que trouxe o boleto.
class BillEmailPage extends StatefulWidget {
  /// Cria a página para [billId].
  const BillEmailPage({
    super.key,
    required this.billId,
    required this.backFallback,
  });

  /// O boleto cujo e-mail está sendo mostrado.
  final String billId;

  /// Para onde o voltar leva quando não há pilha.
  final String backFallback;

  @override
  State<BillEmailPage> createState() => _BillEmailPageState();
}

class _BillEmailPageState extends State<BillEmailPage> {
  late final EmailViewerViewModel _viewModel;

  @override
  void initState() {
    super.initState();
    final repository = context.read<BillRepository>();
    _viewModel = EmailViewerViewModel(
      load: () => repository.getEmail(widget.billId),
    );
  }

  @override
  void dispose() {
    _viewModel.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return EmailViewerScreen(
      viewModel: _viewModel,
      backFallback: widget.backFallback,
    );
  }
}

/// Página do histórico de e-mails capturados.
class CapturedMessageListPage extends StatefulWidget {
  /// Cria a página.
  const CapturedMessageListPage({
    super.key,
    required this.backFallback,
    required this.onOpenBill,
    required this.onOpenCaptureItem,
  });

  /// Para onde o voltar leva quando não há pilha.
  final String backFallback;

  /// Abre o boleto que o e-mail produziu.
  final void Function(String billId) onOpenBill;

  /// Abre o item da quarentena.
  final void Function(String itemId) onOpenCaptureItem;

  @override
  State<CapturedMessageListPage> createState() =>
      _CapturedMessageListPageState();
}

class _CapturedMessageListPageState extends State<CapturedMessageListPage> {
  late final CapturedMessageListViewModel _viewModel;

  @override
  void initState() {
    super.initState();
    _viewModel = CapturedMessageListViewModel(
      repository: context.read<CapturedMessageRepository>(),
    );
  }

  @override
  void dispose() {
    _viewModel.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return CapturedMessageListScreen(
      viewModel: _viewModel,
      backFallback: widget.backFallback,
      onOpenBill: widget.onOpenBill,
      onOpenCaptureItem: widget.onOpenCaptureItem,
    );
  }
}

/// Página da lista de fontes de captura.
class CaptureSourceListPage extends StatefulWidget {
  /// Cria a página.
  const CaptureSourceListPage({
    super.key,
    required this.backFallback,
    required this.onOpenSource,
    required this.onConnectSource,
  });

  /// Para onde o voltar leva quando não há pilha.
  final String backFallback;

  /// Abre uma fonte.
  final void Function(String id) onOpenSource;

  /// Abre o formulário de conexão.
  final VoidCallback onConnectSource;

  @override
  State<CaptureSourceListPage> createState() => _CaptureSourceListPageState();
}

class _CaptureSourceListPageState extends State<CaptureSourceListPage> {
  late final CaptureSourceListViewModel _viewModel;

  @override
  void initState() {
    super.initState();
    _viewModel = CaptureSourceListViewModel(
      repository: context.read<CaptureSourceRepository>(),
    );
  }

  @override
  void dispose() {
    _viewModel.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return CaptureSourceListScreen(
      viewModel: _viewModel,
      backFallback: widget.backFallback,
      onOpenSource: widget.onOpenSource,
      onConnectSource: widget.onConnectSource,
    );
  }
}

/// Página do formulário de conexão de caixa.
class CaptureSourceConnectPage extends StatefulWidget {
  /// Cria a página.
  const CaptureSourceConnectPage({
    super.key,
    required this.backFallback,
    required this.onConnected,
  });

  /// Para onde o voltar leva quando não há pilha.
  final String backFallback;

  /// Chamada com o id da fonte recém-conectada.
  final void Function(String id) onConnected;

  @override
  State<CaptureSourceConnectPage> createState() =>
      _CaptureSourceConnectPageState();
}

class _CaptureSourceConnectPageState extends State<CaptureSourceConnectPage> {
  late final CaptureSourceConnectViewModel _viewModel;

  @override
  void initState() {
    super.initState();
    _viewModel = CaptureSourceConnectViewModel(
      repository: context.read<CaptureSourceRepository>(),
    );
  }

  @override
  void dispose() {
    _viewModel.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return CaptureSourceConnectScreen(
      viewModel: _viewModel,
      backFallback: widget.backFallback,
      onConnected: widget.onConnected,
    );
  }
}

/// Página do detalhe de uma fonte de captura.
class CaptureSourceDetailPage extends StatefulWidget {
  /// Cria a página para [sourceId].
  const CaptureSourceDetailPage({
    super.key,
    required this.sourceId,
    required this.backFallback,
    required this.onDisconnected,
  });

  /// A fonte sendo mostrada.
  final String sourceId;

  /// Para onde o voltar leva quando não há pilha.
  final String backFallback;

  /// Chamada após a desconexão.
  final VoidCallback onDisconnected;

  @override
  State<CaptureSourceDetailPage> createState() =>
      _CaptureSourceDetailPageState();
}

class _CaptureSourceDetailPageState extends State<CaptureSourceDetailPage> {
  late final CaptureSourceDetailViewModel _viewModel;

  @override
  void initState() {
    super.initState();
    _viewModel = CaptureSourceDetailViewModel(
      repository: context.read<CaptureSourceRepository>(),
      sourceId: widget.sourceId,
    );
  }

  @override
  void dispose() {
    _viewModel.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return CaptureSourceDetailScreen(
      viewModel: _viewModel,
      backFallback: widget.backFallback,
      onDisconnected: widget.onDisconnected,
    );
  }
}

/// Página da lista de beneficiários.
class PayeeListPage extends StatefulWidget {
  /// Cria a página.
  const PayeeListPage({
    super.key,
    required this.backFallback,
    required this.onOpenPayee,
    required this.onCreatePayee,
  });

  /// Para onde o voltar leva quando não há pilha.
  final String backFallback;

  /// Abre um beneficiário.
  final void Function(String id) onOpenPayee;

  /// Abre o cadastro.
  final VoidCallback onCreatePayee;

  @override
  State<PayeeListPage> createState() => _PayeeListPageState();
}

class _PayeeListPageState extends State<PayeeListPage> {
  late final PayeeListViewModel _viewModel;

  @override
  void initState() {
    super.initState();
    _viewModel =
        PayeeListViewModel(repository: context.read<PayeeRepository>());
  }

  @override
  void dispose() {
    _viewModel.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return PayeeListScreen(
      viewModel: _viewModel,
      backFallback: widget.backFallback,
      onOpenPayee: widget.onOpenPayee,
      onCreatePayee: widget.onCreatePayee,
    );
  }
}

/// Página do cadastro de beneficiário.
class PayeeFormPage extends StatefulWidget {
  /// Cria a página.
  const PayeeFormPage({
    super.key,
    required this.backFallback,
    required this.onRegistered,
  });

  /// Para onde o voltar leva quando não há pilha.
  final String backFallback;

  /// Chamada com o id do beneficiário recém-cadastrado.
  final void Function(String id) onRegistered;

  @override
  State<PayeeFormPage> createState() => _PayeeFormPageState();
}

class _PayeeFormPageState extends State<PayeeFormPage> {
  late final PayeeFormViewModel _viewModel;

  @override
  void initState() {
    super.initState();
    _viewModel =
        PayeeFormViewModel(repository: context.read<PayeeRepository>());
  }

  @override
  void dispose() {
    _viewModel.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return PayeeFormScreen(
      viewModel: _viewModel,
      backFallback: widget.backFallback,
      onRegistered: widget.onRegistered,
    );
  }
}

/// Página do detalhe de um beneficiário.
class PayeeDetailPage extends StatefulWidget {
  /// Cria a página para [payeeId].
  const PayeeDetailPage({
    super.key,
    required this.payeeId,
    required this.backFallback,
    required this.onDeleted,
  });

  /// O beneficiário sendo mostrado.
  final String payeeId;

  /// Para onde o voltar leva quando não há pilha.
  final String backFallback;

  /// Chamada após a exclusão.
  final VoidCallback onDeleted;

  @override
  State<PayeeDetailPage> createState() => _PayeeDetailPageState();
}

class _PayeeDetailPageState extends State<PayeeDetailPage> {
  late final PayeeDetailViewModel _viewModel;

  @override
  void initState() {
    super.initState();
    _viewModel = PayeeDetailViewModel(
      repository: context.read<PayeeRepository>(),
      payeeId: widget.payeeId,
    );
  }

  @override
  void dispose() {
    _viewModel.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return PayeeDetailScreen(
      viewModel: _viewModel,
      backFallback: widget.backFallback,
      onDeleted: widget.onDeleted,
    );
  }
}

/// Página do perfil do pagador.
class PayerProfilePage extends StatefulWidget {
  /// Cria a página.
  const PayerProfilePage({super.key, required this.backFallback});

  /// Para onde o voltar leva quando não há pilha.
  final String backFallback;

  @override
  State<PayerProfilePage> createState() => _PayerProfilePageState();
}

class _PayerProfilePageState extends State<PayerProfilePage> {
  late final PayerProfileViewModel _viewModel;

  @override
  void initState() {
    super.initState();
    _viewModel = PayerProfileViewModel(
      repository: context.read<PayerProfileRepository>(),
    );
  }

  @override
  void dispose() {
    _viewModel.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return PayerProfileScreen(
      viewModel: _viewModel,
      backFallback: widget.backFallback,
    );
  }
}

/// Página da lista de expectativas.
class ExpectationListPage extends StatefulWidget {
  /// Cria a página.
  const ExpectationListPage({
    super.key,
    required this.backFallback,
    required this.onOpenExpectation,
    required this.onCreateExpectation,
  });

  /// Para onde o voltar leva quando não há pilha.
  final String backFallback;

  /// Abre uma expectativa.
  final void Function(String id) onOpenExpectation;

  /// Abre o cadastro.
  final VoidCallback onCreateExpectation;

  @override
  State<ExpectationListPage> createState() => _ExpectationListPageState();
}

class _ExpectationListPageState extends State<ExpectationListPage> {
  late final ExpectationListViewModel _viewModel;

  @override
  void initState() {
    super.initState();
    _viewModel = ExpectationListViewModel(
      repository: context.read<ExpectationRepository>(),
    );
  }

  @override
  void dispose() {
    _viewModel.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return ExpectationListScreen(
      viewModel: _viewModel,
      backFallback: widget.backFallback,
      onOpenExpectation: widget.onOpenExpectation,
      onCreateExpectation: widget.onCreateExpectation,
    );
  }
}

/// Página do formulário de expectativa — cadastro e edição.
class ExpectationFormPage extends StatefulWidget {
  /// Cria a página. Com [expectationId], o formulário abre em modo edição.
  const ExpectationFormPage({
    super.key,
    required this.backFallback,
    required this.onSaved,
    this.expectationId,
  });

  /// Para onde o voltar leva quando não há pilha.
  final String backFallback;

  /// Chamada com o id da expectativa recém-salva.
  final void Function(String id) onSaved;

  /// A expectativa sendo editada, ou nulo no cadastro.
  final String? expectationId;

  @override
  State<ExpectationFormPage> createState() => _ExpectationFormPageState();
}

class _ExpectationFormPageState extends State<ExpectationFormPage> {
  late final ExpectationFormViewModel _viewModel;

  @override
  void initState() {
    super.initState();
    _viewModel = ExpectationFormViewModel(
      repository: context.read<ExpectationRepository>(),
      payeeRepository: context.read<PayeeRepository>(),
      expectationId: widget.expectationId,
    );
  }

  @override
  void dispose() {
    _viewModel.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return ExpectationFormScreen(
      viewModel: _viewModel,
      backFallback: widget.backFallback,
      onSaved: widget.onSaved,
    );
  }
}

/// Página do detalhe de uma expectativa.
class ExpectationDetailPage extends StatefulWidget {
  /// Cria a página para [expectationId].
  const ExpectationDetailPage({
    super.key,
    required this.expectationId,
    required this.backFallback,
    required this.onOpenBill,
    required this.onOpenCaptureItem,
    required this.onEdit,
    required this.onDeleted,
  });

  /// A expectativa sendo mostrada.
  final String expectationId;

  /// Para onde o voltar leva quando não há pilha.
  final String backFallback;

  /// Abre o boleto que cumpriu um ciclo.
  final void Function(String billId) onOpenBill;

  /// Abre o item da quarentena que bloqueia um ciclo.
  final void Function(String itemId) onOpenCaptureItem;

  /// Abre o formulário de edição desta expectativa.
  final VoidCallback onEdit;

  /// Chamada depois de a expectativa ser excluída — não há tela para voltar.
  final VoidCallback onDeleted;

  @override
  State<ExpectationDetailPage> createState() => _ExpectationDetailPageState();
}

class _ExpectationDetailPageState extends State<ExpectationDetailPage> {
  late final ExpectationDetailViewModel _viewModel;

  @override
  void initState() {
    super.initState();
    _viewModel = ExpectationDetailViewModel(
      repository: context.read<ExpectationRepository>(),
      expectationId: widget.expectationId,
    );
  }

  @override
  void dispose() {
    _viewModel.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return ExpectationDetailScreen(
      viewModel: _viewModel,
      backFallback: widget.backFallback,
      onOpenBill: widget.onOpenBill,
      onOpenCaptureItem: widget.onOpenCaptureItem,
      onEdit: widget.onEdit,
      onDeleted: widget.onDeleted,
    );
  }
}
