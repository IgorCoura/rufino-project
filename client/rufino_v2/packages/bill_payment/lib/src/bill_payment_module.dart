import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:http/http.dart' as http;
import 'package:provider/provider.dart';
import 'package:provider/single_child_widget.dart';
import 'package:rufino_core/rufino_core.dart';

import 'bill_payment_permissions.dart';
import 'data/bill_api_service.dart';
import 'data/bill_repository_impl.dart';
import 'data/capture_item_api_service.dart';
import 'data/capture_item_repository_impl.dart';
import 'data/capture_source_api_service.dart';
import 'data/capture_source_repository_impl.dart';
import 'data/captured_message_api_service.dart';
import 'data/captured_message_repository_impl.dart';
import 'data/expectation_api_service.dart';
import 'data/expectation_repository_impl.dart';
import 'data/payee_api_service.dart';
import 'data/payee_repository_impl.dart';
import 'data/payer_profile_api_service.dart';
import 'data/payer_profile_repository_impl.dart';
import 'data/payment_api_service.dart';
import 'data/payment_repository_impl.dart';
import 'data/trusted_origin_api_service.dart';
import 'data/trusted_origin_repository_impl.dart';
import 'domain/bill_repository.dart';
import 'domain/capture_item_repository.dart';
import 'domain/capture_source_repository.dart';
import 'domain/captured_message_repository.dart';
import 'domain/expectation_repository.dart';
import 'domain/payee_repository.dart';
import 'domain/payer_profile_repository.dart';
import 'domain/payment_repository.dart';
import 'domain/trusted_origin_repository.dart';
import 'ui/bill_payment_routes.dart';
import 'ui/shared/document_picker.dart';

/// Contas a pagar plugado na casca.
///
/// Tudo o que vem de fora chega pelo construtor. O `getTenantId` é o que
/// distingue este produto do de gestão de pessoas: aqui o tenant vai no
/// caminho da rota da API, e quem o resolve é o contexto da casca.
class BillPaymentModule extends AppModule {
  /// Cria o módulo.
  const BillPaymentModule({
    required this.client,
    required this.baseUrl,
    required this.getAuthHeader,
    required this.getTenantId,
    required this.errorReporter,
    required this.homeRoute,
    required this.onPickDocument,
    required this.onOpenLink,
  });

  /// O cliente HTTP da casca.
  final http.Client client;

  /// Origem da API deste produto.
  final String baseUrl;

  /// Devolve o cabeçalho `Authorization` já pronto.
  final Future<String> Function() getAuthHeader;

  /// Devolve o tenant corrente — vai no caminho de toda rota da API.
  final String Function() getTenantId;

  /// Para onde os erros vão.
  final ErrorReporter errorReporter;

  /// Rota do Home.
  final String homeRoute;

  /// Seletor de documento (plugin, mora na casca).
  final DocumentPicker onPickDocument;

  /// Abridor de link (plugin, mora na casca).
  final LinkOpener onOpenLink;

  @override
  String get menuTitle => 'CONTAS A PAGAR';

  @override
  List<RouteBase> routes() => billPaymentRoutes(
        homeRoute: homeRoute,
        onPickDocument: onPickDocument,
        onOpenLink: onOpenLink,
      );

  @override
  List<SingleChildWidget> providers() => [
        Provider<BillRepository>.value(
          value: BillRepositoryImpl(
            apiService: BillApiService(
              client: client,
              baseUrl: baseUrl,
              getAuthHeader: getAuthHeader,
              getTenantId: getTenantId,
            ),
            reporter: errorReporter,
          ),
        ),
        Provider<PaymentRepository>.value(
          value: PaymentRepositoryImpl(
            apiService: PaymentApiService(
              client: client,
              baseUrl: baseUrl,
              getAuthHeader: getAuthHeader,
              getTenantId: getTenantId,
            ),
            reporter: errorReporter,
          ),
        ),
        Provider<CaptureItemRepository>.value(
          value: CaptureItemRepositoryImpl(
            apiService: CaptureItemApiService(
              client: client,
              baseUrl: baseUrl,
              getAuthHeader: getAuthHeader,
              getTenantId: getTenantId,
            ),
            reporter: errorReporter,
          ),
        ),
        Provider<CapturedMessageRepository>.value(
          value: CapturedMessageRepositoryImpl(
            apiService: CapturedMessageApiService(
              client: client,
              baseUrl: baseUrl,
              getAuthHeader: getAuthHeader,
              getTenantId: getTenantId,
            ),
            reporter: errorReporter,
          ),
        ),
        Provider<CaptureSourceRepository>.value(
          value: CaptureSourceRepositoryImpl(
            apiService: CaptureSourceApiService(
              client: client,
              baseUrl: baseUrl,
              getAuthHeader: getAuthHeader,
              getTenantId: getTenantId,
            ),
            reporter: errorReporter,
          ),
        ),
        Provider<PayeeRepository>.value(
          value: PayeeRepositoryImpl(
            apiService: PayeeApiService(
              client: client,
              baseUrl: baseUrl,
              getAuthHeader: getAuthHeader,
              getTenantId: getTenantId,
            ),
            reporter: errorReporter,
          ),
        ),
        Provider<PayerProfileRepository>.value(
          value: PayerProfileRepositoryImpl(
            apiService: PayerProfileApiService(
              client: client,
              baseUrl: baseUrl,
              getAuthHeader: getAuthHeader,
              getTenantId: getTenantId,
            ),
            reporter: errorReporter,
          ),
        ),
        Provider<TrustedOriginRepository>.value(
          value: TrustedOriginRepositoryImpl(
            apiService: TrustedOriginApiService(
              client: client,
              baseUrl: baseUrl,
              getAuthHeader: getAuthHeader,
              getTenantId: getTenantId,
            ),
            reporter: errorReporter,
          ),
        ),
        Provider<ExpectationRepository>.value(
          value: ExpectationRepositoryImpl(
            apiService: ExpectationApiService(
              client: client,
              baseUrl: baseUrl,
              getAuthHeader: getAuthHeader,
              getTenantId: getTenantId,
            ),
            reporter: errorReporter,
          ),
        ),
      ];

  @override
  List<HomeEntry> visibleEntries(BuildContext context) {
    final tenant = context.watch<TenantContextNotifier>();
    if (!tenant.hasProduct(TenantProducts.billPayment)) return const [];

    final permissions = context.watch<BillPaymentPermissionNotifier>();
    return _entries
        .where((e) => permissions.hasAnyScope(e.resource))
        .toList();
  }
}

const _entries = <HomeEntry>[
  HomeEntry(
    icon: Icons.pending_actions_outlined,
    label: 'Painel de Contas',
    route: BillPaymentRoutes.pending,
    resource: BillPaymentResources.expectation,
  ),
  HomeEntry(
    icon: Icons.receipt_long_outlined,
    label: 'Boletos',
    route: BillPaymentRoutes.bills,
    resource: BillPaymentResources.bill,
  ),
  HomeEntry(
    icon: Icons.inbox_outlined,
    label: 'Quarentena',
    route: BillPaymentRoutes.captureItems,
    resource: BillPaymentResources.captureItem,
  ),
  HomeEntry(
    icon: Icons.storefront_outlined,
    label: 'Beneficiários',
    route: BillPaymentRoutes.payees,
    resource: BillPaymentResources.payee,
  ),
  HomeEntry(
    icon: Icons.notifications_active_outlined,
    label: 'Expectativas',
    route: BillPaymentRoutes.expectations,
    resource: BillPaymentResources.expectation,
  ),
  HomeEntry(
    icon: Icons.forward_to_inbox_outlined,
    label: 'E-mails Capturados',
    route: BillPaymentRoutes.capturedMessages,
    resource: BillPaymentResources.capturedMessage,
  ),
  HomeEntry(
    icon: Icons.mark_email_read_outlined,
    label: 'Fontes de Captura',
    route: BillPaymentRoutes.captureSources,
    resource: BillPaymentResources.captureSource,
  ),
  HomeEntry(
    icon: Icons.verified_user_outlined,
    label: 'Origens Confiáveis',
    route: BillPaymentRoutes.trustedOrigins,
    resource: BillPaymentResources.origin,
  ),
  HomeEntry(
    icon: Icons.account_balance_wallet_outlined,
    label: 'Perfil do Pagador',
    route: BillPaymentRoutes.payerProfile,
    resource: BillPaymentResources.payerProfile,
  ),
];
