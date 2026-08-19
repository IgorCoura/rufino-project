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
}
