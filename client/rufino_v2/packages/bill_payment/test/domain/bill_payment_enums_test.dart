import 'package:bill_payment/bill_payment.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  group('BillStatuses', () {
    test('marks exactly denied, paid and cancelled as terminal', () {
      expect(BillStatuses.isTerminal(BillStatuses.denied), isTrue);
      expect(BillStatuses.isTerminal(BillStatuses.paid), isTrue);
      expect(BillStatuses.isTerminal(BillStatuses.cancelled), isTrue);

      expect(BillStatuses.isTerminal(BillStatuses.captured), isFalse);
      expect(BillStatuses.isTerminal(BillStatuses.awaitingApproval), isFalse);
      expect(BillStatuses.isTerminal(BillStatuses.rejected), isFalse);
      expect(BillStatuses.isTerminal(BillStatuses.approved), isFalse);
      expect(BillStatuses.isTerminal(BillStatuses.scheduled), isFalse);
      expect(BillStatuses.isTerminal(BillStatuses.failed), isFalse);
    });

    test('accepts validation up to approved and never from scheduled on', () {
      expect(BillStatuses.acceptsValidation(BillStatuses.captured), isTrue);
      expect(BillStatuses.acceptsValidation(BillStatuses.rejected), isTrue);
      expect(
        BillStatuses.acceptsValidation(BillStatuses.awaitingApproval),
        isTrue,
      );
      expect(BillStatuses.acceptsValidation(BillStatuses.approved), isTrue);

      expect(BillStatuses.acceptsValidation(BillStatuses.scheduled), isFalse);
      expect(BillStatuses.acceptsValidation(BillStatuses.paid), isFalse);
      expect(BillStatuses.acceptsValidation(BillStatuses.denied), isFalse);
      expect(BillStatuses.acceptsValidation(BillStatuses.cancelled), isFalse);
    });

    test('only awaiting approval accepts a decision', () {
      expect(
        BillStatuses.acceptsDecision(BillStatuses.awaitingApproval),
        isTrue,
      );
      expect(BillStatuses.acceptsDecision(BillStatuses.approved), isFalse);
      expect(BillStatuses.acceptsDecision(BillStatuses.captured), isFalse);
    });

    test('translates every status and echoes unknown ones', () {
      expect(BillStatuses.label(BillStatuses.awaitingApproval),
          'Aguardando aprovação');
      expect(BillStatuses.label('SomethingNew'), 'SomethingNew');
    });
  });

  group('CaptureItemStatuses', () {
    test('only unrouted items can be claimed', () {
      expect(
        CaptureItemStatuses.acceptsClaim(CaptureItemStatuses.unrouted),
        isTrue,
      );
      expect(
        CaptureItemStatuses.acceptsClaim(CaptureItemStatuses.foreignPayer),
        isFalse,
      );
      expect(
        CaptureItemStatuses.acceptsClaim(CaptureItemStatuses.unrecognized),
        isFalse,
      );
    });

    test('reprocess applies to unrecognized, locked and link-failed items',
        () {
      expect(
        CaptureItemStatuses.acceptsReprocess(CaptureItemStatuses.unrecognized),
        isTrue,
      );
      expect(
        CaptureItemStatuses.acceptsReprocess(CaptureItemStatuses.locked),
        isTrue,
      );
      expect(
        CaptureItemStatuses.acceptsReprocess(CaptureItemStatuses.linkFailed),
        isTrue,
      );

      expect(
        CaptureItemStatuses.acceptsReprocess(CaptureItemStatuses.parsed),
        isFalse,
      );
      expect(
        CaptureItemStatuses.acceptsReprocess(CaptureItemStatuses.promoted),
        isFalse,
      );
      expect(
        CaptureItemStatuses.acceptsReprocess(CaptureItemStatuses.discarded),
        isFalse,
      );
    });
  });

  group('CheckOutcomes', () {
    test('only failed reproves', () {
      expect(CheckOutcomes.isFailure(CheckOutcomes.failed), isTrue);
      expect(CheckOutcomes.isFailure(CheckOutcomes.warning), isFalse);
      expect(CheckOutcomes.isFailure(CheckOutcomes.inconclusive), isFalse);
      expect(CheckOutcomes.isFailure(CheckOutcomes.passed), isFalse);
      expect(CheckOutcomes.isFailure(CheckOutcomes.skipped), isFalse);
    });

    test('failed, inconclusive and warning require attention', () {
      expect(CheckOutcomes.requiresAttention(CheckOutcomes.failed), isTrue);
      expect(
        CheckOutcomes.requiresAttention(CheckOutcomes.inconclusive),
        isTrue,
      );
      expect(CheckOutcomes.requiresAttention(CheckOutcomes.warning), isTrue);

      expect(CheckOutcomes.requiresAttention(CheckOutcomes.passed), isFalse);
      expect(CheckOutcomes.requiresAttention(CheckOutcomes.skipped), isFalse);
    });
  });

  group('MissReasons', () {
    test('separates what never arrived from what arrived broken', () {
      expect(MissReasons.arrived(MissReasons.neverArrived), isFalse);
      expect(MissReasons.arrived(MissReasons.portalUnavailable), isFalse);

      expect(MissReasons.arrived(MissReasons.captureFailed), isTrue);
      expect(MissReasons.arrived(MissReasons.locked), isTrue);
      expect(MissReasons.arrived(MissReasons.linkFailed), isTrue);
      expect(MissReasons.arrived(MissReasons.unrouted), isTrue);
    });
  });

  group('ReadingStatuses', () {
    test('speaks only for the states the user can act on', () {
      expect(ReadingStatuses.speaks(ReadingStatuses.queued), isTrue);
      expect(ReadingStatuses.speaks(ReadingStatuses.unavailable), isTrue);

      // Silent on purpose: "não há o que ler" is an absence nobody can act on,
      // and a finished reading speaks through the fields it filled in.
      expect(ReadingStatuses.speaks(ReadingStatuses.notApplicable), isFalse);
      expect(ReadingStatuses.speaks(ReadingStatuses.done), isFalse);
    });

    test('the queued label names the AI, not a generic analysis', () {
      expect(ReadingStatuses.label(ReadingStatuses.queued), contains('IA'));
      expect(ReadingStatuses.label(ReadingStatuses.queued), contains('fila'));
    });

    test('every state that speaks also explains itself', () {
      for (final status in [ReadingStatuses.queued, ReadingStatuses.unavailable]) {
        expect(ReadingStatuses.detail(status), isNotEmpty);
      }

      expect(ReadingStatuses.detail(ReadingStatuses.done), isEmpty);
      expect(ReadingStatuses.detail(ReadingStatuses.notApplicable), isEmpty);
    });

    test('an unknown state coming from the server says nothing', () {
      expect(ReadingStatuses.speaks('SomethingNew'), isFalse);
      expect(ReadingStatuses.label('SomethingNew'), isEmpty);
    });
  });

  group('RiskLevels', () {
    // A escala é ordenada e nível desconhecido vale 0 — nunca "seguro por padrão".
    test('the tier scale orders the four levels and zeroes the unknown', () {
      expect(RiskLevels.tier(RiskLevels.safe), 1);
      expect(RiskLevels.tier(RiskLevels.attention), 2);
      expect(RiskLevels.tier(RiskLevels.danger), 3);
      expect(RiskLevels.tier(RiskLevels.extremeDanger), 4);
      expect(RiskLevels.tier('SomethingNew'), 0);
      expect(RiskLevels.tier(null), 0);
    });

    // O aceite explícito vale para Perigo E Extremo Perigo (ADR-015).
    test('danger and extreme danger require the acknowledgement', () {
      expect(RiskLevels.requiresAcknowledgement(RiskLevels.danger), isTrue);
      expect(
        RiskLevels.requiresAcknowledgement(RiskLevels.extremeDanger),
        isTrue,
      );
      expect(RiskLevels.requiresAcknowledgement(RiskLevels.safe), isFalse);
      expect(RiskLevels.requiresAcknowledgement(RiskLevels.attention), isFalse);
    });
  });

  group('BillStatuses reopen (phase 3)', () {
    // Só o pagamento falhado reabre — reabrir não é atalho para desfazer aprovação.
    test('only a failed payment accepts the reopen action', () {
      expect(BillStatuses.acceptsReopen(BillStatuses.failed), isTrue);
      expect(BillStatuses.acceptsReopen(BillStatuses.approved), isFalse);
      expect(BillStatuses.acceptsReopen(BillStatuses.scheduled), isFalse);
      expect(BillStatuses.acceptsReopen(BillStatuses.paid), isFalse);
    });
  });

  group('PaymentOrderStatuses', () {
    // A janela de reação: rascunho, aceito e em processamento ainda cancelam;
    // o que já se resolveu, não.
    test('the reaction window covers draft, pending and bank processing', () {
      expect(PaymentOrderStatuses.canCancel(PaymentOrderStatuses.draft), isTrue);
      expect(
        PaymentOrderStatuses.canCancel(PaymentOrderStatuses.pending),
        isTrue,
      );
      expect(
        PaymentOrderStatuses.canCancel(PaymentOrderStatuses.bankProcessing),
        isTrue,
      );
      expect(PaymentOrderStatuses.canCancel(PaymentOrderStatuses.paid), isFalse);
      expect(
        PaymentOrderStatuses.canCancel(PaymentOrderStatuses.failed),
        isFalse,
      );
      expect(
        PaymentOrderStatuses.canCancel(PaymentOrderStatuses.cancelled),
        isFalse,
      );
    });

    // Status desconhecido ecoa o nome de arame — servidor mais novo nunca é
    // pintado como um desfecho conhecido.
    test('an unknown status echoes the wire name instead of guessing', () {
      expect(PaymentOrderStatuses.label('SomethingNew'), 'SomethingNew');
      expect(PaymentOrderStatuses.label(PaymentOrderStatuses.paid), 'Pago');
    });
  });

  group('PaymentOrderHolds', () {
    // Retenção é estado visível: só a ausência dela cala.
    test('only the absent hold produces no line', () {
      expect(PaymentOrderHolds.label(PaymentOrderHolds.none), isNull);
      expect(
        PaymentOrderHolds.label(PaymentOrderHolds.awaitingAccount),
        isNotNull,
      );
      expect(
        PaymentOrderHolds.label(PaymentOrderHolds.awaitingConfirmation),
        isNotNull,
      );
      expect(PaymentOrderHolds.label('SomethingNew'), 'SomethingNew');
    });
  });
}
