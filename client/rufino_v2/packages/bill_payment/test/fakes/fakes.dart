import 'dart:typed_data';

import 'package:bill_payment/bill_payment.dart';
import 'package:flutter/widgets.dart';
import 'package:http/http.dart' as http;
import 'package:rufino_core/rufino_core.dart';

/// Records captures like production would, mirroring the short-circuit on
/// [ExpectedFailure] — assertions about [capturedErrors] reflect exactly
/// what would reach the error monitor.
class FakeErrorReporter implements ErrorReporter {
  /// The errors that would have been reported.
  final List<Object> capturedErrors = [];

  @override
  Future<void> init() async {}

  @override
  void capture(
    Object error,
    StackTrace? stackTrace, {
    Map<String, Object?>? context,
  }) {
    if (error is ExpectedFailure) return;
    final cause = error is BillPaymentNetworkException ? error.cause : null;
    if (cause is ExpectedFailure) return;
    capturedErrors.add(error);
  }

  @override
  void addBreadcrumb(
    String message, {
    String? category,
    Map<String, Object?>? data,
  }) {}

  @override
  void setUser({required String? userId, String? companyId}) {}

  @override
  void clearUser() {}

  @override
  http.Client wrapHttpClient(http.Client base) => base;

  @override
  NavigatorObserver get navigatorObserver => NavigatorObserver();
}

/// In-memory [PermissionRepository] for tests.
class FakePermissionRepository implements PermissionRepository {
  List<Permission> _permissions = const [];

  /// Replaces the permission set the fake will answer with.
  void setPermissions(List<Permission> permissions) =>
      _permissions = permissions;

  @override
  Future<Result<List<Permission>>> fetchPermissions() async =>
      Result.success(_permissions);

  @override
  Future<List<Permission>?> getCachedPermissions() async => null;

  @override
  Future<void> cachePermissions(List<Permission> permissions) async {}

  @override
  Future<void> clearCachedPermissions() async {}
}

/// Builds a bill payment permission notifier already loaded with
/// [permissions].
Future<BillPaymentPermissionNotifier> billPaymentPermissions(
  List<Permission> permissions,
) async {
  final repo = FakePermissionRepository()..setPermissions(permissions);
  final notifier = BillPaymentPermissionNotifier(permissionRepository: repo);
  await notifier.loadPermissions();
  return notifier;
}

/// A payee with coherent defaults, overridable per test.
Payee payee({
  String id = 'payee-1',
  String legalName = 'EDP SAO PAULO SA',
  String taxId = '02.302.100/0001-06',
  String taxIdKind = 'CNPJ',
  List<String> aliases = const [],
  List<String> acceptedBanks = const [],
  AmountPolicy? amountPolicy,
  bool isActive = true,
}) {
  return Payee(
    id: id,
    legalName: legalName,
    taxId: taxId,
    taxIdKind: taxIdKind,
    aliases: aliases,
    acceptedBanks: acceptedBanks,
    amountPolicy: amountPolicy ??
        const AmountPolicy(
          kind: AmountPolicyKinds.unbounded,
          isConclusive: false,
        ),
    isActive: isActive,
  );
}

/// In-memory [PayeeRepository] with configurable failure and call capture.
class FakePayeeRepository implements PayeeRepository {
  /// The payees the fake serves, in order.
  List<Payee> payees = [];

  /// The page size used to slice [payees] into cursor pages.
  int pageSize = 50;

  bool _shouldFail = false;

  /// The writes performed, in order (e.g. `addAlias:EDP`).
  final List<String> calls = [];

  /// The document of the last register — what actually crossed the form.
  String? lastRegisteredTaxId;

  /// The last policy written — the fields, not just the kind.
  AmountPolicyInput? lastPolicy;

  /// Makes every call fail with a rule exception.
  // ignore: avoid_positional_boolean_parameters
  void setShouldFail(bool value) => _shouldFail = value;

  Result<T> _fail<T>() => Result.error(
        const BillPaymentRuleException('regra disse não', code: 'BLP.TST'),
      );

  Future<Result<void>> _write(String call) async {
    if (_shouldFail) return _fail();
    calls.add(call);
    return const Result.success(null);
  }

  @override
  Future<Result<PayeePage>> listPayees({String? cursor, int limit = 50}) async {
    if (_shouldFail) return _fail();
    final start = cursor == null ? 0 : int.parse(cursor);
    final slice = payees.skip(start).take(pageSize).toList();
    final next = start + pageSize;
    return Result.success(
      PayeePage(
        items: slice,
        nextCursor: next < payees.length ? '$next' : null,
      ),
    );
  }

  @override
  Future<Result<Payee>> getPayee(String id) async {
    if (_shouldFail) return _fail();
    return Result.success(payees.firstWhere((p) => p.id == id));
  }

  @override
  Future<Result<Payee?>> findByTaxId(String taxId) async {
    if (_shouldFail) return _fail();
    final digits = taxId.replaceAll(RegExp(r'\D'), '');
    for (final p in payees) {
      if (p.taxId.replaceAll(RegExp(r'\D'), '') == digits) {
        return Result.success(p);
      }
    }
    return const Result.success(null);
  }

  @override
  Future<Result<String>> registerPayee({
    required String legalName,
    required String taxId,
    required AmountPolicyInput amountPolicy,
  }) async {
    if (_shouldFail) return _fail();
    calls.add('registerPayee:$legalName');
    lastRegisteredTaxId = taxId;
    return const Result.success('payee-new');
  }

  @override
  Future<Result<void>> changeLegalName(String id, String legalName) =>
      _write('changeLegalName:$legalName');

  @override
  Future<Result<void>> changeAmountPolicy(
    String id,
    AmountPolicyInput policy,
  ) {
    lastPolicy = policy;
    return _write('changeAmountPolicy:${policy.kind}');
  }

  @override
  Future<Result<void>> addAlias(String id, String alias) =>
      _write('addAlias:$alias');

  @override
  Future<Result<void>> removeAlias(String id, String alias) =>
      _write('removeAlias:$alias');

  @override
  Future<Result<void>> addAcceptedBank(String id, String bankCode) =>
      _write('addAcceptedBank:$bankCode');

  @override
  Future<Result<void>> removeAcceptedBank(String id, String bankCode) =>
      _write('removeAcceptedBank:$bankCode');

  @override
  Future<Result<void>> setActivation(String id, {required bool isActive}) =>
      _write('setActivation:$isActive');

  @override
  Future<Result<void>> deletePayee(String id) => _write('deletePayee:$id');
}

/// A payer profile with coherent defaults, overridable per test.
PayerProfile payerProfile({
  String id = 'profile-1',
  String kind = PayerKinds.company,
  String legalName = 'Padaria do Zé LTDA',
  String primaryTaxId = '11.222.333/0001-81',
  String primaryTaxIdKind = 'CNPJ',
  List<PayerTaxId> additionalTaxIds = const [],
  bool matchByCnpjRoot = false,
  bool canSchedulePayments = false,
}) {
  return PayerProfile(
    id: id,
    kind: kind,
    legalName: legalName,
    primaryTaxId: primaryTaxId,
    primaryTaxIdKind: primaryTaxIdKind,
    additionalTaxIds: additionalTaxIds,
    matchByCnpjRoot: matchByCnpjRoot,
    canSchedulePayments: canSchedulePayments,
  );
}

/// In-memory [PayerProfileRepository] with configurable failure.
class FakePayerProfileRepository implements PayerProfileRepository {
  /// The profile served, or `null` for the onboarding state.
  PayerProfile? profile;

  bool _shouldFail = false;

  /// The writes performed, in order.
  final List<String> calls = [];

  /// Makes every call fail with a rule exception.
  // ignore: avoid_positional_boolean_parameters
  void setShouldFail(bool value) => _shouldFail = value;

  Result<T> _fail<T>() => Result.error(
        const BillPaymentRuleException('regra disse não', code: 'BLP.TST'),
      );

  Future<Result<void>> _write(String call) async {
    if (_shouldFail) return _fail();
    calls.add(call);
    return const Result.success(null);
  }

  @override
  Future<Result<PayerProfile?>> getProfile() async {
    if (_shouldFail) return _fail();
    return Result.success(profile);
  }

  @override
  Future<Result<String>> registerProfile({
    required String kind,
    required String legalName,
    required String primaryTaxId,
  }) async {
    if (_shouldFail) return _fail();
    calls.add('registerProfile:$legalName');
    profile = payerProfile(kind: kind, legalName: legalName);
    return const Result.success('profile-new');
  }

  @override
  Future<Result<void>> changeLegalName(String legalName) =>
      _write('changeLegalName:$legalName');

  @override
  Future<Result<void>> addTaxId(String taxId) => _write('addTaxId:$taxId');

  @override
  Future<Result<void>> removeTaxId(String taxId) =>
      _write('removeTaxId:$taxId');

  @override
  Future<Result<void>> setCnpjRootMatching({required bool enabled}) =>
      _write('setCnpjRootMatching:$enabled');

  @override
  Future<Result<bool>> linkAsaasAccount(String? accountRef) async {
    if (_shouldFail) return _fail();
    calls.add('linkAsaasAccount:${accountRef ?? '-'}');
    return const Result.success(true);
  }
}

/// A trusted origin with coherent defaults, overridable per test.
TrustedOrigin trustedOrigin({
  String id = 'origin-1',
  String kind = OriginKinds.emailDomain,
  String value = 'fornecedor.com.br',
  String decision = TrustDecisions.trusted,
  String decidedBy = 'user-1',
  DateTime? decidedAt,
  String? note,
}) {
  return TrustedOrigin(
    id: id,
    kind: kind,
    value: value,
    decision: decision,
    decidedBy: decidedBy,
    decidedAt: decidedAt ?? DateTime(2026, 1, 1),
    note: note,
  );
}

/// In-memory [TrustedOriginRepository] with configurable failure.
class FakeTrustedOriginRepository implements TrustedOriginRepository {
  /// The origins served, in order.
  List<TrustedOrigin> origins = [];

  /// The page size used to slice [origins] into cursor pages.
  int pageSize = 50;

  bool _shouldFail = false;

  /// The writes performed, in order.
  final List<String> calls = [];

  /// Makes every call fail with a rule exception.
  // ignore: avoid_positional_boolean_parameters
  void setShouldFail(bool value) => _shouldFail = value;

  Result<T> _fail<T>() => Result.error(
        const BillPaymentRuleException('regra disse não', code: 'BLP.TST'),
      );

  @override
  Future<Result<TrustedOriginPage>> listOrigins({
    String? cursor,
    int limit = 50,
  }) async {
    if (_shouldFail) return _fail();
    final start = cursor == null ? 0 : int.parse(cursor);
    final slice = origins.skip(start).take(pageSize).toList();
    final next = start + pageSize;
    return Result.success(
      TrustedOriginPage(
        items: slice,
        nextCursor: next < origins.length ? '$next' : null,
      ),
    );
  }

  @override
  Future<Result<TrustedOrigin>> getOrigin(String id) async {
    if (_shouldFail) return _fail();
    return Result.success(origins.firstWhere((o) => o.id == id));
  }

  @override
  Future<Result<TrustedOrigin?>> resolveSender(String sender) async {
    if (_shouldFail) return _fail();
    final normalized = sender.trim().toLowerCase();
    for (final origin in origins) {
      if (normalized == origin.value ||
          normalized.endsWith('@${origin.value}')) {
        return Result.success(origin);
      }
    }
    return const Result.success(null);
  }

  @override
  Future<Result<String>> registerOrigin({
    required String kind,
    required String value,
    required String decision,
    String? note,
  }) async {
    if (_shouldFail) return _fail();
    calls.add('registerOrigin:$value');
    return const Result.success('origin-new');
  }

  @override
  Future<Result<void>> changeDecision(
    String id, {
    required String decision,
    String? note,
  }) async {
    if (_shouldFail) return _fail();
    calls.add('changeDecision:$id:$decision');
    return const Result.success(null);
  }

  @override
  Future<Result<void>> deleteOrigin(String id) async {
    if (_shouldFail) return _fail();
    calls.add('deleteOrigin:$id');
    return const Result.success(null);
  }
}

/// A capture source with coherent defaults, overridable per test.
CaptureSource captureSource({
  String id = 'src-1',
  String displayName = 'Contas a pagar',
  String address = 'contas@empresa.com.br',
  List<MonitoredFolder>? folders,
  bool hasCredential = true,
  bool isEnabled = true,
  DateTime? captureSince,
  DateTime? lastSyncAt,
  String? lastSyncError,
}) {
  return CaptureSource(
    id: id,
    kind: 'MicrosoftGraphMailbox',
    displayName: displayName,
    address: address,
    folders: folders ??
        const [MonitoredFolder(id: 'folder-1', hasSyncCursor: true)],
    hasCredential: hasCredential,
    isEnabled: isEnabled,
    captureSince: captureSince,
    lastSyncAt: lastSyncAt,
    lastSyncError: lastSyncError,
    createdAt: DateTime(2026, 1, 1),
  );
}

/// In-memory [CaptureSourceRepository] with configurable failure.
class FakeCaptureSourceRepository implements CaptureSourceRepository {
  /// The sources served, in order.
  List<CaptureSource> sources = [];

  /// The sync outcome served by [syncSource].
  SyncOutcome syncOutcome = const SyncOutcome(
    id: 'src-1',
    status: SyncStatuses.ok,
    ingestedItems: 3,
    skippedAsAlreadyIngested: 1,
  );

  bool _shouldFail = false;

  /// The writes performed, in order.
  final List<String> calls = [];

  /// Makes every call fail with a rule exception.
  // ignore: avoid_positional_boolean_parameters
  void setShouldFail(bool value) => _shouldFail = value;

  Result<T> _fail<T>() => Result.error(
        const BillPaymentRuleException('regra disse não', code: 'BLP.TST'),
      );

  Future<Result<void>> _write(String call) async {
    if (_shouldFail) return _fail();
    calls.add(call);
    return const Result.success(null);
  }

  @override
  Future<Result<CaptureSourcePage>> listSources({
    String? cursor,
    int limit = 50,
  }) async {
    if (_shouldFail) return _fail();
    return Result.success(
      CaptureSourcePage(items: sources, nextCursor: null),
    );
  }

  @override
  Future<Result<CaptureSource>> getSource(String id) async {
    if (_shouldFail) return _fail();
    return Result.success(sources.firstWhere((s) => s.id == id));
  }

  @override
  Future<Result<ConnectOutcome>> connectSource({
    required String displayName,
    required String address,
    required GraphCredentialInput credential,
    String? folderPath,
    DateTime? captureSince,
  }) async {
    if (_shouldFail) return _fail();
    lastCaptureSince = captureSince;
    calls.add('connectSource:$address');
    return const Result.success(ConnectOutcome(id: 'src-new'));
  }

  /// The floor handed to the last `connectSource`/`changeCaptureSince` call.
  DateTime? lastCaptureSince;

  @override
  Future<Result<void>> changeCaptureSince(
    String id,
    DateTime? captureSince,
  ) async {
    lastCaptureSince = captureSince;
    return _write('changeCaptureSince:$captureSince');
  }

  @override
  Future<Result<void>> renameSource(String id, String displayName) =>
      _write('renameSource:$displayName');

  @override
  Future<Result<void>> setActivation(String id, {required bool isEnabled}) =>
      _write('setActivation:$isEnabled');

  @override
  Future<Result<void>> replaceCredential(
    String id,
    GraphCredentialInput credential,
  ) =>
      _write('replaceCredential');

  @override
  Future<Result<SyncOutcome>> syncSource(String id) async {
    if (_shouldFail) return _fail();
    calls.add('syncSource:$id');
    return Result.success(syncOutcome);
  }

  @override
  Future<Result<void>> addFolder(String id, String? folderPath) =>
      _write('addFolder:${folderPath ?? '-'}');

  @override
  Future<Result<void>> removeFolder(String id, String? folderPath) =>
      _write('removeFolder:${folderPath ?? '-'}');

  @override
  Future<Result<RescanOutcome>> rescanSource(String id) async {
    if (_shouldFail) return _fail();
    calls.add('rescanSource:$id');
    return const Result.success(RescanOutcome(id: 'src-1', foldersReset: 2));
  }

  @override
  Future<Result<void>> disconnectSource(String id) =>
      _write('disconnectSource:$id');
}

/// A capture item with coherent defaults, overridable per test.
CaptureItem captureItem({
  String id = 'item-1',
  String status = CaptureItemStatuses.unrouted,
  String? sender = 'contador@escritorio.com.br',
  String? subject = 'Boleto sindicato',
  String? reason,
  String? billId,
  bool hasArtifact = false,
  String? sourceUrl,
  String? linkHost,
}) {
  return CaptureItem(
    id: id,
    sourceId: 'src-1',
    sender: sender,
    subject: subject,
    receivedAt: DateTime(2026, 8, 1, 9),
    status: status,
    reason: reason,
    billId: billId,
    hasArtifact: hasArtifact,
    sourceUrl: sourceUrl,
    linkHost: linkHost,
  );
}

/// In-memory [CaptureItemRepository] with configurable failure.
class FakeCaptureItemRepository implements CaptureItemRepository {
  /// The items served — filtered by status like the server does.
  List<CaptureItem> items = [];

  bool _shouldFail = false;

  /// The writes performed, in order.
  final List<String> calls = [];

  /// The last status filter the list was asked with.
  String? lastStatusFilter;

  /// Makes every call fail with a rule exception.
  // ignore: avoid_positional_boolean_parameters
  void setShouldFail(bool value) => _shouldFail = value;

  Result<T> _fail<T>() => Result.error(
        const BillPaymentRuleException('regra disse não', code: 'BLP.TST'),
      );

  @override
  Future<Result<CaptureItemPage>> listItems({
    String? status,
    String? cursor,
    int limit = 50,
  }) async {
    if (_shouldFail) return _fail();
    lastStatusFilter = status;
    final filtered = status == null
        ? items
        : items.where((i) => i.status == status).toList();
    return Result.success(
      CaptureItemPage(items: filtered, nextCursor: null),
    );
  }

  @override
  Future<Result<CaptureItem>> getItem(String id) async {
    if (_shouldFail) return _fail();
    return Result.success(items.firstWhere((i) => i.id == id));
  }

  @override
  Future<Result<void>> reprocessItem(String id) async {
    if (_shouldFail) return _fail();
    calls.add('reprocessItem:$id');
    return const Result.success(null);
  }

  @override
  Future<Result<ClaimOutcome>> claimItem(String id) async {
    if (_shouldFail) return _fail();
    calls.add('claimItem:$id');
    return const Result.success(
      ClaimOutcome(id: 'item-1', billId: 'bill-7', status: 'Promoted'),
    );
  }

  @override
  Future<Result<void>> dismissItem(String id, {String? note}) async {
    if (_shouldFail) return _fail();
    calls.add('dismissItem:$id:${note ?? ''}');
    return const Result.success(null);
  }

  @override
  Future<Result<void>> attachArtifact(
    String id,
    List<int> bytes, {
    required String fileName,
    required String contentType,
  }) async {
    if (_shouldFail) return _fail();
    calls.add('attachArtifact:$id:$fileName:${bytes.length}');
    return const Result.success(null);
  }

  @override
  Future<Result<CapturedArtifact>> getArtifact(String id) async {
    if (_shouldFail) return _fail();
    calls.add('getArtifact:$id');
    return Result.success(artifact());
  }

  @override
  Future<Result<EmailMessage>> getEmail(String id) async {
    if (_shouldFail) return _fail();
    calls.add('getEmail:$id');
    return Result.success(
      EmailMessage(
        id: 'message-1',
        sender: 'faturas@fornecedor.com.br',
        subject: 'Sua fatura chegou',
        receivedAt: DateTime.utc(2026, 8, 27, 8),
        contentType: 'text/html',
        content: '<p>Sua fatura chegou.</p>',
      ),
    );
  }
}

/// A tiny document, enough for the viewer to have something to render.
///
/// The bytes are not a real PDF on purpose — no test renders them, and a
/// valid file would only make the fixture heavier.
CapturedArtifact artifact({
  String contentType = 'application/pdf',
  String? fileName = 'boleto.pdf',
}) {
  return CapturedArtifact(
    bytes: Uint8List.fromList('%PDF-1.4 documento'.codeUnits),
    contentType: contentType,
    fileName: fileName,
  );
}

/// Um e-mail do livro-caixa, com padrões coerentes.
CapturedMessage capturedMessage({
  String id = 'msg-1',
  String sender = 'faturas@enel.com.br',
  String? subject = 'Sua fatura chegou',
  String outcome = ArtifactOutcomes.discarded,
  int artifactCount = 1,
  bool canRecapture = true,
  bool processed = true,
  String? billId,
  String? captureItemId,
  DateTime? receivedAt,
}) {
  return CapturedMessage(
    id: id,
    sourceId: 'src-1',
    sender: sender,
    subject: subject,
    receivedAt: receivedAt ?? DateTime(2026, 8, 19, 15, 36),
    firstSeenAt: DateTime(2026, 8, 19, 15, 49),
    processedAt: processed ? DateTime(2026, 8, 19, 16, 1) : null,
    outcome: outcome,
    artifactCount: artifactCount,
    canRecapture: canRecapture,
    artifacts: [
      CapturedArtifactOutcome(
        fileName: 'boleto.pdf',
        contentType: 'application/pdf',
        outcome: outcome,
        billId: billId,
        captureItemId: captureItemId,
      ),
    ],
  );
}

/// In-memory [CapturedMessageRepository] com falha configurável.
class FakeCapturedMessageRepository implements CapturedMessageRepository {
  /// Os e-mails servidos.
  List<CapturedMessage> messages = [];

  /// O cabeçalho servido.
  CaptureSyncStatus syncStatus = CaptureSyncStatus(
    lastSyncAt: DateTime(2026, 8, 19, 16, 56),
    sourceCount: 1,
  );

  /// A política servida.
  CaptureRetentionPolicy retention = const CaptureRetentionPolicy(
    isEnabled: false,
    windowDays: 90,
    availableWindowDays: [7, 30, 90, 180],
  );

  bool _shouldFail = false;

  /// As escritas realizadas, em ordem.
  final List<String> calls = [];

  /// O último filtro pedido.
  CapturedMessageFilter? lastFilter;

  /// Faz toda chamada falhar com uma exceção de regra.
  // ignore: avoid_positional_boolean_parameters
  void setShouldFail(bool value) => _shouldFail = value;

  Result<T> _fail<T>() => Result.error(
        const BillPaymentRuleException('regra disse não', code: 'BLP.TST'),
      );

  @override
  Future<Result<CapturedMessagePage>> listMessages({
    CapturedMessageFilter filter = const CapturedMessageFilter(),
    String? cursor,
    int limit = 50,
  }) async {
    if (_shouldFail) return _fail();
    lastFilter = filter;

    final filtered = filter.outcome == null
        ? messages
        : messages.where((m) => m.outcome == filter.outcome).toList();

    return Result.success(
      CapturedMessagePage(items: filtered, nextCursor: null),
    );
  }

  @override
  Future<Result<CaptureSyncStatus>> getSyncStatus() async {
    if (_shouldFail) return _fail();
    return Result.success(syncStatus);
  }

  /// What the next [recapture] resolves to.
  RecaptureOutcome recaptureOutcome = const RecaptureOutcome(
    id: 'msg-1',
    artifactsReingested: 1,
    billsCancelled: 0,
    previouslyDeniedBillIds: [],
  );

  @override
  Future<Result<RecaptureOutcome>> recapture(String id) async {
    if (_shouldFail) return _fail();
    calls.add('recapture:$id');
    return Result.success(recaptureOutcome);
  }

  @override
  Future<Result<CaptureRetentionPolicy>> getRetentionPolicy() async {
    if (_shouldFail) return _fail();
    return Result.success(retention);
  }

  @override
  Future<Result<CaptureRetentionPolicy>> configureRetention({
    required bool isEnabled,
    required int windowDays,
  }) async {
    if (_shouldFail) return _fail();
    calls.add('configureRetention:$isEnabled:$windowDays');
    retention = CaptureRetentionPolicy(
      isEnabled: isEnabled,
      windowDays: windowDays,
      availableWindowDays: retention.availableWindowDays,
    );
    return Result.success(retention);
  }
}

/// A bill list projection with coherent defaults, overridable per test.
Bill bill({
  String id = 'bill-1',
  String status = BillStatuses.awaitingApproval,
  String kind = BillKinds.bankSlip,
  String rail = PaymentRails.boleto,
  double? amount = 615.07,
}) {
  return Bill(
    id: id,
    status: status,
    kind: kind,
    rail: rail,
    amount: amount,
    dueDate: DateTime(2026, 8, 20),
    bankCode: '033',
    origin: BillOrigin(
      sourceKind: BillSourceKinds.manualUpload,
      receivedAt: DateTime(2026, 8, 1),
    ),
    createdAt: DateTime(2026, 8, 1),
  );
}

/// A bill detail with coherent defaults, overridable per test.
BillDetail billDetail({
  String id = 'bill-1',
  String status = BillStatuses.awaitingApproval,
  DateTime? lastConsultedAt,
  DateTime? minimumScheduleDate,
  List<BillCheck> checks = const [],
  bool hasArtifact = false,
  String readingStatus = ReadingStatuses.notApplicable,
}) {
  return BillDetail(
    id: id,
    status: status,
    kind: BillKinds.bankSlip,
    rail: PaymentRails.boleto,
    checks: checks,
    readingStatus: readingStatus,
    origin: BillOrigin(
      sourceKind: BillSourceKinds.manualUpload,
      receivedAt: DateTime(2026, 8, 1),
      hasArtifact: hasArtifact,
    ),
    createdAt: DateTime(2026, 8, 1),
    lastConsultedAt: lastConsultedAt,
    minimumScheduleDate: minimumScheduleDate,
  );
}

/// In-memory [BillRepository] with configurable failure.
class FakeBillRepository implements BillRepository {
  /// The list projections served, filtered by status like the server does.
  List<Bill> bills = [];

  /// The detail served by [getBillDetail].
  BillDetail? detail;

  bool _shouldFail = false;

  /// The writes performed, in order.
  final List<String> calls = [];

  /// The last status filter the list was asked with.
  String? lastStatusFilter;

  /// Makes every call fail with a rule exception.
  // ignore: avoid_positional_boolean_parameters
  void setShouldFail(bool value) => _shouldFail = value;

  Result<T> _fail<T>() => Result.error(
        const BillPaymentRuleException('regra disse não', code: 'BLP.TST'),
      );

  @override
  Future<Result<BillPage>> listBills({
    String? status,
    String? cursor,
    int limit = 50,
  }) async {
    if (_shouldFail) return _fail();
    lastStatusFilter = status;
    final filtered = status == null
        ? bills
        : bills.where((b) => b.status == status).toList();
    return Result.success(BillPage(items: filtered, nextCursor: null));
  }

  @override
  Future<Result<Bill>> getBill(String id) async {
    if (_shouldFail) return _fail();
    return Result.success(bills.firstWhere((b) => b.id == id));
  }

  @override
  Future<Result<BillDetail>> getBillDetail(String id) async {
    if (_shouldFail) return _fail();
    return Result.success(detail!);
  }

  @override
  Future<Result<ImportOutcome>> importBill({
    String? digitableLine,
    String? pixPayload,
    List<int>? documentBytes,
    String? documentFileName,
    String? documentContentType,
  }) async {
    if (_shouldFail) return _fail();

    // Registra o que o ViewModel repassou: os testes afirmam sobre o arquivo
    // ter chegado (ou não), que é o que a tela decide.
    lastImport = (
      digitableLine: digitableLine,
      pixPayload: pixPayload,
      documentBytes: documentBytes,
      documentFileName: documentFileName,
      documentContentType: documentContentType,
    );
    calls.add('importBill');
    return const Result.success(
      ImportOutcome(id: 'bill-new', kind: 'BankSlip', rail: 'Boleto'),
    );
  }

  /// O que a última chamada de [importBill] recebeu.
  ({
    String? digitableLine,
    String? pixPayload,
    List<int>? documentBytes,
    String? documentFileName,
    String? documentContentType,
  })? lastImport;

  @override
  Future<Result<ValidationRunOutcome>> revalidateBill(String id) async {
    if (_shouldFail) return _fail();
    calls.add('revalidateBill:$id');
    return const Result.success(
      ValidationRunOutcome(
        id: 'bill-1',
        status: BillStatuses.awaitingApproval,
        blockingFailures: 0,
        attentionItems: 0,
      ),
    );
  }

  @override
  Future<Result<void>> approveBill(
    String id, {
    required DateTime scheduleFor,
    String? note,
    bool acknowledgeRisk = false,
  }) async {
    if (_shouldFail) return _fail();
    calls.add('approveBill:$id');
    return const Result.success(null);
  }

  @override
  Future<Result<void>> denyBill(String id, String reason) async {
    if (_shouldFail) return _fail();
    calls.add('denyBill:$reason');
    return const Result.success(null);
  }

  @override
  Future<Result<void>> cancelBill(String id, String reason) async {
    if (_shouldFail) return _fail();
    calls.add('cancelBill:$reason');
    return const Result.success(null);
  }

  @override
  Future<Result<CapturedArtifact>> getArtifact(String id) async {
    if (_shouldFail) return _fail();
    calls.add('getArtifact:$id');
    return Result.success(artifact());
  }

  @override
  Future<Result<EmailMessage>> getEmail(String id) async {
    if (_shouldFail) return _fail();
    calls.add('getEmail:$id');
    return Result.success(
      EmailMessage(
        id: 'message-1',
        sender: 'faturas@fornecedor.com.br',
        subject: 'Sua fatura chegou',
        receivedAt: DateTime.utc(2026, 8, 27, 8),
        contentType: 'text/html',
        content: '<p>Sua fatura chegou.</p>',
      ),
    );
  }}

/// An expectation with coherent defaults, overridable per test.
Expectation expectation({
  String id = 'exp-1',
  String label = 'EDP — Casa Florentino',
  bool isActive = true,
  List<ExpectationCycle> cycles = const [],
}) {
  return Expectation(
    id: id,
    payeeId: 'payee-1',
    label: label,
    recurrence: Recurrences.monthly,
    expectedDueDay: 20,
    observedLeadDays: 7,
    alertLeadDays: 5,
    origin: ExpectationOrigins.manual,
    observationCount: 0,
    isActive: isActive,
    cycles: cycles,
  );
}

/// A pending line with coherent defaults, overridable per test.
PendingExpectation pendingExpectation({
  String expectationId = 'exp-1',
  String status = CycleStatuses.missing,
  String? blockedByCaptureItemId,
}) {
  return PendingExpectation(
    expectationId: expectationId,
    cycleId: 'cycle-1',
    label: 'EDP — Casa Florentino',
    competence: '2026-08',
    expectedDueDate: DateTime(2026, 8, 20),
    status: status,
    blockedByCaptureItemId: blockedByCaptureItemId,
  );
}

/// In-memory [ExpectationRepository] with configurable failure.
class FakeExpectationRepository implements ExpectationRepository {
  /// The expectations served, in order.
  List<Expectation> expectations = [];

  /// The pending panel served.
  PendingExpectationsView pending = const PendingExpectationsView.empty();

  bool _shouldFail = false;

  /// The writes performed, in order.
  final List<String> calls = [];

  /// Makes every call fail with a rule exception.
  // ignore: avoid_positional_boolean_parameters
  void setShouldFail(bool value) => _shouldFail = value;

  Result<T> _fail<T>() => Result.error(
        const BillPaymentRuleException('regra disse não', code: 'BLP.TST'),
      );

  @override
  Future<Result<ExpectationPage>> listExpectations({
    String? cursor,
    int limit = 20,
  }) async {
    if (_shouldFail) return _fail();
    return Result.success(
      ExpectationPage(items: expectations, nextCursor: null),
    );
  }

  @override
  Future<Result<PendingExpectationsView>> getPending({
    int dueSoonWindowDays = 7,
  }) async {
    if (_shouldFail) return _fail();
    return Result.success(pending);
  }

  @override
  Future<Result<Expectation>> getExpectation(String id) async {
    if (_shouldFail) return _fail();
    return Result.success(expectations.firstWhere((e) => e.id == id));
  }

  @override
  Future<Result<String>> registerExpectation({
    required String payeeId,
    required String label,
    required String recurrence,
    required int expectedDueDay,
    required int observedLeadDays,
    String? accountReference,
    int? alertLeadDays,
  }) async {
    if (_shouldFail) return _fail();
    calls.add('registerExpectation:$label');
    return const Result.success('exp-new');
  }

  @override
  Future<Result<void>> editExpectation(
    String id, {
    required String label,
    required String recurrence,
    required int expectedDueDay,
    required int observedLeadDays,
    String? accountReference,
    int? alertLeadDays,
  }) async {
    if (_shouldFail) return _fail();
    calls.add('editExpectation:$id:$label:$recurrence:$expectedDueDay');
    return const Result.success(null);
  }

  @override
  Future<Result<void>> deleteExpectation(String id) async {
    if (_shouldFail) return _fail();
    calls.add('deleteExpectation:$id');
    return const Result.success(null);
  }

  @override
  Future<Result<void>> alterWatch(
    String id, {
    required bool isActive,
    DateTime? pausedUntil,
    String? reason,
  }) async {
    if (_shouldFail) return _fail();
    calls.add('alterWatch:$isActive:${pausedUntil?.toIso8601String() ?? '-'}');
    return const Result.success(null);
  }

  @override
  Future<Result<void>> waiveCycle(
    String id,
    String cycleId, {
    String? reason,
  }) async {
    if (_shouldFail) return _fail();
    calls.add('waiveCycle:$cycleId');
    return const Result.success(null);
  }
}
