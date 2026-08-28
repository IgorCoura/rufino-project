import 'package:bill_payment/bill_payment.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  group('CaptureSource folders', () {
    test('another folder fits while under the cap of twenty', () {
      final source = _source(folderCount: 19);

      expect(source.canAddFolder, isTrue);
    });

    test('the twentieth folder closes the door', () {
      final source = _source(folderCount: 20);

      expect(source.canAddFolder, isFalse);
    });

    test('the last folder cannot be removed', () {
      expect(_source(folderCount: 1).canRemoveFolder, isFalse);
      expect(_source(folderCount: 2).canRemoveFolder, isTrue);
    });

    test('a folder without a path labels itself as the inbox', () {
      const inbox = MonitoredFolder(id: 'f1', hasSyncCursor: false);
      const named =
          MonitoredFolder(id: 'f2', hasSyncCursor: true, path: 'Contas');

      expect(inbox.label, 'Caixa de entrada');
      expect(named.label, 'Contas');
    });
  });

  group('CaptureItem actions', () {
    test('claim applies only to unrouted items', () {
      expect(_item(CaptureItemStatuses.unrouted).acceptsClaim, isTrue);
      expect(_item(CaptureItemStatuses.foreignPayer).acceptsClaim, isFalse);
    });

    test('reprocess applies to the retriable quarantine states', () {
      expect(_item(CaptureItemStatuses.unrecognized).acceptsReprocess, isTrue);
      expect(_item(CaptureItemStatuses.locked).acceptsReprocess, isTrue);
      expect(_item(CaptureItemStatuses.promoted).acceptsReprocess, isFalse);
    });
  });

  group('PayerProfile', () {
    test('only a company supports CNPJ-root matching', () {
      expect(
        _profile(kind: PayerKinds.company).supportsCnpjRootMatching,
        isTrue,
      );
      expect(
        _profile(kind: PayerKinds.individual).supportsCnpjRootMatching,
        isFalse,
      );
    });
  });

  group('TrustedOrigin', () {
    test('knows when it blocks', () {
      expect(_origin(TrustDecisions.blocked).isBlocked, isTrue);
      expect(_origin(TrustDecisions.trusted).isBlocked, isFalse);
    });
  });

  group('Expectation', () {
    test('is paused exactly while now is before the paused-until date', () {
      final expectation = _expectation(pausedUntil: DateTime(2026, 12, 1));

      expect(expectation.isPausedAt(DateTime(2026, 11, 30)), isTrue);
      expect(expectation.isPausedAt(DateTime(2026, 12, 1)), isFalse);
      expect(_expectation().isPausedAt(DateTime(2026, 11, 30)), isFalse);
    });

    test('a cycle is open while waiting, partially captured or missing', () {
      expect(_cycle(CycleStatuses.waiting).isOpen, isTrue);
      expect(_cycle(CycleStatuses.partiallyCaptured).isOpen, isTrue);
      expect(_cycle(CycleStatuses.missing).isOpen, isTrue);

      expect(_cycle(CycleStatuses.fulfilled).isOpen, isFalse);
      expect(_cycle(CycleStatuses.waived).isOpen, isFalse);
    });
  });

  group('PendingExpectationsView', () {
    // Vencida tambem conta como acao: ha encargos correndo. So "vence em
    // breve" e antecedencia, e por isso fica de fora da contagem.
    test('counts action on missing, overdue and capture-failed lists', () {
      final view = PendingExpectationsView(
        missing: [_pending()],
        overdue: [_pending()],
        captureFailed: [_pending(), _pending()],
        dueSoon: [_pending()],
      );

      expect(view.actionableCount, 4);
      expect(view.isEmpty, isFalse);
      expect(const PendingExpectationsView.empty().isEmpty, isTrue);
    });
  });

  // A redação da política é da UI, e vive em ui/shared/amount_policy_view.dart
  // — o domínio guarda os fatos. Coberta por payee_amount_policy_test.dart.
}

CaptureSource _source({required int folderCount}) {
  return CaptureSource(
    id: 'src-1',
    kind: 'MicrosoftGraphMailbox',
    displayName: 'Contas a pagar',
    address: 'contas@empresa.com.br',
    folders: List.generate(
      folderCount,
      (i) => MonitoredFolder(id: 'f$i', hasSyncCursor: false, path: 'P$i'),
    ),
    hasCredential: true,
    isEnabled: true,
    createdAt: DateTime(2026, 1, 1),
  );
}

CaptureItem _item(String status) {
  return CaptureItem(
    id: 'item-1',
    sourceId: 'src-1',
    receivedAt: DateTime(2026, 1, 1),
    status: status,
  );
}

PayerProfile _profile({required String kind}) {
  return PayerProfile(
    id: 'profile-1',
    kind: kind,
    legalName: 'Padaria do Zé LTDA',
    primaryTaxId: '11.222.333/0001-81',
    primaryTaxIdKind: 'CNPJ',
    additionalTaxIds: const [],
    matchByCnpjRoot: false,
    canSchedulePayments: false,
  );
}

TrustedOrigin _origin(String decision) {
  return TrustedOrigin(
    id: 'origin-1',
    kind: OriginKinds.emailDomain,
    value: 'fornecedor.com.br',
    decision: decision,
    decidedBy: 'user-1',
    decidedAt: DateTime(2026, 1, 1),
  );
}

Expectation _expectation({DateTime? pausedUntil}) {
  return Expectation(
    id: 'exp-1',
    payeeId: 'payee-1',
    label: 'EDP — Casa Florentino',
    recurrence: Recurrences.monthly,
    expectedDueDay: 10,
    observedLeadDays: 7,
    alertLeadDays: 5,
    origin: ExpectationOrigins.manual,
    observationCount: 0,
    isActive: true,
    cycles: const [],
    pausedUntil: pausedUntil,
  );
}

ExpectationCycle _cycle(String status) {
  return ExpectationCycle(
    id: 'cycle-1',
    competence: '2026-08',
    expectedDueDate: DateTime(2026, 8, 20),
    alertAt: DateTime(2026, 8, 15),
    status: status,
  );
}

PendingExpectation _pending() {
  return PendingExpectation(
    expectationId: 'exp-1',
    cycleId: 'cycle-1',
    label: 'EDP — Casa Florentino',
    competence: '2026-08',
    expectedDueDate: DateTime(2026, 8, 20),
    status: CycleStatuses.missing,
  );
}
