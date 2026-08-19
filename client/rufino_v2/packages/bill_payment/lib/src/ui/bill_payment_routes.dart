import 'package:flutter/widgets.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';
import 'package:rufino_core/rufino_core.dart';

import '../bill_payment_permissions.dart';
import 'bill_payment_pages.dart';

/// Route paths this module owns.
///
/// They are constants so the shell can send the user here without spelling a
/// string that only breaks at runtime.
abstract final class BillPaymentRoutes {
  /// The operator's daily panel.
  static const String pending = '/bill-payment/pending';

  /// The bill listing.
  static const String bills = '/bill-payment/bills';

  /// The manual import form.
  static const String billImport = '/bill-payment/bills/import';

  /// One bill's approval screen.
  static String billDetail(String id) => '/bill-payment/bills/$id';

  /// The quarantine listing.
  static const String captureItems = '/bill-payment/capture-items';

  /// One quarantine item.
  static String captureItemDetail(String id) =>
      '/bill-payment/capture-items/$id';

  /// The capture source listing.
  static const String captureSources = '/bill-payment/capture-sources';

  /// The connect-mailbox flow.
  static const String captureSourceConnect =
      '/bill-payment/capture-sources/connect';

  /// One capture source.
  static String captureSourceDetail(String id) =>
      '/bill-payment/capture-sources/$id';

  /// The payee listing.
  static const String payees = '/bill-payment/payees';

  /// The payee register form.
  static const String payeeCreate = '/bill-payment/payees/create';

  /// One payee.
  static String payeeDetail(String id) => '/bill-payment/payees/$id';

  /// The payer profile (one per tenant).
  static const String payerProfile = '/bill-payment/payer-profile';

  /// The trusted origin listing.
  static const String trustedOrigins = '/bill-payment/trusted-origins';

  /// The expectation listing.
  static const String expectations = '/bill-payment/expectations';

  /// The expectation register form.
  static const String expectationCreate = '/bill-payment/expectations/create';

  /// One expectation.
  static String expectationDetail(String id) =>
      '/bill-payment/expectations/$id';
}

/// Builds the routes of this module.
///
/// The shell supplies what belongs to it — where "home" is. The module
/// supplies the screens. Neither knows the other's internals.
List<RouteBase> billPaymentRoutes({required String homeRoute}) {
  return [
    GoRoute(
      path: BillPaymentRoutes.pending,
      redirect: (context, state) => _requireBillScope(
        context,
        resource: BillPaymentResources.expectation,
        scope: BillPaymentScopes.view,
        fallback: homeRoute,
      ),
      builder: (context, state) => PendingPage(
        backFallback: homeRoute,
        onOpenApprovalQueue: () => context.push(BillPaymentRoutes.bills),
        onOpenExpectation: (id) =>
            context.push(BillPaymentRoutes.expectationDetail(id)),
        onOpenCaptureItem: (id) =>
            context.push(BillPaymentRoutes.captureItemDetail(id)),
        onOpenPayerProfile: () =>
            context.push(BillPaymentRoutes.payerProfile),
      ),
    ),
    // As rotas literais vêm ANTES das `:id` correspondentes: o go_router casa
    // na ordem, e um `:id` à frente engoliria `import`/`create`/`connect`
    // como se fossem identificadores.
    GoRoute(
      path: BillPaymentRoutes.billImport,
      redirect: (context, state) => _requireBillScope(
        context,
        resource: BillPaymentResources.bill,
        scope: BillPaymentScopes.import,
        fallback: BillPaymentRoutes.bills,
      ),
      builder: (context, state) => BillImportPage(
        backFallback: BillPaymentRoutes.bills,
        onImported: (id) =>
            context.pushReplacement(BillPaymentRoutes.billDetail(id)),
      ),
    ),
    GoRoute(
      path: BillPaymentRoutes.bills,
      redirect: (context, state) => _requireBillScope(
        context,
        resource: BillPaymentResources.bill,
        scope: BillPaymentScopes.view,
        fallback: homeRoute,
      ),
      builder: (context, state) => BillListPage(
        backFallback: homeRoute,
        // A fila do aprovador abre já filtrada — é a tela de trabalho.
        initialStatus: 'AwaitingApproval',
        onOpenBill: (id) => context.push(BillPaymentRoutes.billDetail(id)),
        onImportBill: () => context.push(BillPaymentRoutes.billImport),
      ),
    ),
    GoRoute(
      path: '/bill-payment/bills/:id',
      redirect: (context, state) => _requireBillScope(
        context,
        resource: BillPaymentResources.bill,
        scope: BillPaymentScopes.view,
        fallback: homeRoute,
      ),
      builder: (context, state) => BillDetailPage(
        billId: state.pathParameters['id']!,
        backFallback: BillPaymentRoutes.bills,
      ),
    ),
    GoRoute(
      path: BillPaymentRoutes.captureItems,
      redirect: (context, state) => _requireBillScope(
        context,
        resource: BillPaymentResources.captureItem,
        scope: BillPaymentScopes.view,
        fallback: homeRoute,
      ),
      builder: (context, state) => CaptureItemListPage(
        backFallback: homeRoute,
        onOpenItem: (id) =>
            context.push(BillPaymentRoutes.captureItemDetail(id)),
      ),
    ),
    GoRoute(
      path: '/bill-payment/capture-items/:id',
      redirect: (context, state) => _requireBillScope(
        context,
        resource: BillPaymentResources.captureItem,
        scope: BillPaymentScopes.view,
        fallback: homeRoute,
      ),
      builder: (context, state) => CaptureItemDetailPage(
        itemId: state.pathParameters['id']!,
        backFallback: BillPaymentRoutes.captureItems,
        onOpenBill: (billId) =>
            context.push(BillPaymentRoutes.billDetail(billId)),
      ),
    ),
    GoRoute(
      path: BillPaymentRoutes.captureSourceConnect,
      redirect: (context, state) => _requireBillScope(
        context,
        resource: BillPaymentResources.captureSource,
        scope: BillPaymentScopes.manage,
        fallback: BillPaymentRoutes.captureSources,
      ),
      builder: (context, state) => CaptureSourceConnectPage(
        backFallback: BillPaymentRoutes.captureSources,
        onConnected: (id) => context
            .pushReplacement(BillPaymentRoutes.captureSourceDetail(id)),
      ),
    ),
    GoRoute(
      path: BillPaymentRoutes.captureSources,
      redirect: (context, state) => _requireBillScope(
        context,
        resource: BillPaymentResources.captureSource,
        scope: BillPaymentScopes.view,
        fallback: homeRoute,
      ),
      builder: (context, state) => CaptureSourceListPage(
        backFallback: homeRoute,
        onOpenSource: (id) =>
            context.push(BillPaymentRoutes.captureSourceDetail(id)),
        onConnectSource: () =>
            context.push(BillPaymentRoutes.captureSourceConnect),
      ),
    ),
    GoRoute(
      path: '/bill-payment/capture-sources/:id',
      redirect: (context, state) => _requireBillScope(
        context,
        resource: BillPaymentResources.captureSource,
        scope: BillPaymentScopes.view,
        fallback: homeRoute,
      ),
      builder: (context, state) => CaptureSourceDetailPage(
        sourceId: state.pathParameters['id']!,
        backFallback: BillPaymentRoutes.captureSources,
        onDisconnected: () => context.canPop()
            ? context.pop()
            : context.go(BillPaymentRoutes.captureSources),
      ),
    ),
    GoRoute(
      path: BillPaymentRoutes.payeeCreate,
      redirect: (context, state) => _requireBillScope(
        context,
        resource: BillPaymentResources.payee,
        scope: BillPaymentScopes.manage,
        fallback: BillPaymentRoutes.payees,
      ),
      builder: (context, state) => PayeeFormPage(
        backFallback: BillPaymentRoutes.payees,
        onRegistered: (id) =>
            context.pushReplacement(BillPaymentRoutes.payeeDetail(id)),
      ),
    ),
    GoRoute(
      path: BillPaymentRoutes.payees,
      redirect: (context, state) => _requireBillScope(
        context,
        resource: BillPaymentResources.payee,
        scope: BillPaymentScopes.view,
        fallback: homeRoute,
      ),
      builder: (context, state) => PayeeListPage(
        backFallback: homeRoute,
        onOpenPayee: (id) => context.push(BillPaymentRoutes.payeeDetail(id)),
        onCreatePayee: () => context.push(BillPaymentRoutes.payeeCreate),
      ),
    ),
    GoRoute(
      path: '/bill-payment/payees/:id',
      redirect: (context, state) => _requireBillScope(
        context,
        resource: BillPaymentResources.payee,
        scope: BillPaymentScopes.view,
        fallback: homeRoute,
      ),
      builder: (context, state) => PayeeDetailPage(
        payeeId: state.pathParameters['id']!,
        backFallback: BillPaymentRoutes.payees,
        onDeleted: () => context.canPop()
            ? context.pop()
            : context.go(BillPaymentRoutes.payees),
      ),
    ),
    GoRoute(
      path: BillPaymentRoutes.payerProfile,
      redirect: (context, state) => _requireBillScope(
        context,
        resource: BillPaymentResources.payerProfile,
        scope: BillPaymentScopes.view,
        fallback: homeRoute,
      ),
      builder: (context, state) =>
          PayerProfilePage(backFallback: homeRoute),
    ),
    GoRoute(
      path: BillPaymentRoutes.trustedOrigins,
      redirect: (context, state) => _requireBillScope(
        context,
        resource: BillPaymentResources.origin,
        scope: BillPaymentScopes.view,
        fallback: homeRoute,
      ),
      builder: (context, state) =>
          TrustedOriginListPage(backFallback: homeRoute),
    ),
    GoRoute(
      path: BillPaymentRoutes.expectationCreate,
      redirect: (context, state) => _requireBillScope(
        context,
        resource: BillPaymentResources.expectation,
        scope: BillPaymentScopes.manage,
        fallback: BillPaymentRoutes.expectations,
      ),
      builder: (context, state) => ExpectationFormPage(
        backFallback: BillPaymentRoutes.expectations,
        onRegistered: (id) =>
            context.pushReplacement(BillPaymentRoutes.expectationDetail(id)),
      ),
    ),
    GoRoute(
      path: BillPaymentRoutes.expectations,
      redirect: (context, state) => _requireBillScope(
        context,
        resource: BillPaymentResources.expectation,
        scope: BillPaymentScopes.view,
        fallback: homeRoute,
      ),
      builder: (context, state) => ExpectationListPage(
        backFallback: homeRoute,
        onOpenExpectation: (id) =>
            context.push(BillPaymentRoutes.expectationDetail(id)),
        onCreateExpectation: () =>
            context.push(BillPaymentRoutes.expectationCreate),
      ),
    ),
    GoRoute(
      path: '/bill-payment/expectations/:id',
      redirect: (context, state) => _requireBillScope(
        context,
        resource: BillPaymentResources.expectation,
        scope: BillPaymentScopes.view,
        fallback: homeRoute,
      ),
      builder: (context, state) => ExpectationDetailPage(
        expectationId: state.pathParameters['id']!,
        backFallback: BillPaymentRoutes.expectations,
        onOpenBill: (billId) =>
            context.push(BillPaymentRoutes.billDetail(billId)),
        onOpenCaptureItem: (itemId) =>
            context.push(BillPaymentRoutes.captureItemDetail(itemId)),
      ),
    ),
  ];
}

/// Guards a route behind a scope of the **bill payment** audience.
///
/// A guard in the widget tree hides a button; it does nothing about a URL
/// typed into the browser. Returns the path to redirect to, or `null` to let
/// the navigation through.
///
/// While permissions have not loaded yet the navigation is **allowed**: on
/// the web the router opens straight at the deep link, without passing
/// through the splash, and refusing at that moment would throw the operator
/// out on every refresh. The server decides anyway.
String? _requireBillScope(
  BuildContext context, {
  required String resource,
  required String scope,
  required String fallback,
}) {
  final permissions = context.read<BillPaymentPermissionNotifier>();
  if (permissions.status != PermissionStatus.loaded) return null;
  if (permissions.hasPermission(resource, scope)) return null;
  return fallback;
}
