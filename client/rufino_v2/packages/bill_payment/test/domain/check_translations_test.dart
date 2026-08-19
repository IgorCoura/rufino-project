import 'package:bill_payment/bill_payment.dart';
import 'package:flutter_test/flutter_test.dart';

/// Every reason code the server declares in `CheckReasons.cs`. A code
/// missing from the translation map would surface as raw snake_case to the
/// approver.
const _serverReasonCodes = [
  'duplicate_same_tenant',
  'duplicate_other_tenant',
  'duplicate_key_unavailable',
  'lookup_unavailable',
  'lookup_unresolved',
  'lookup_bank_mismatch',
  'lookup_amount_mismatch',
  'lookup_due_date_mismatch',
  'payee_not_registered',
  'payee_inactive',
  'payee_lookalike',
  'payee_name_divergence',
  'payee_not_identified',
  'matched_by_name_only',
  'bank_expectation_not_set',
  'bank_not_accepted',
  'bank_unknown',
  'bank_outside_compe',
  'bank_source_conflict',
  'bank_not_available_for_utility',
  'ispb_without_compe_code',
  'bank_not_available',
  'amount_outside_policy',
  'amount_policy_unbounded',
  'amount_open',
  'amount_not_available',
  'payer_mismatch',
  'payer_not_extractable',
  'payer_profile_missing',
  'origin_unknown',
  'origin_blocked',
  'origin_manual_upload',
  'overdue',
  'same_day_after_cutoff',
  'cannot_schedule_before_due',
  'pix_expires_before_schedule',
  'due_date_not_available',
  'routing_manual_import',
  'routing_inferred',
  'routing_not_recorded',
  'pix_barcode_payee_mismatch',
  'pix_barcode_amount_mismatch',
  'pix_barcode_due_date_mismatch',
  'single_rail_document',
  'pix_qr_not_payable',
  'static_qr_without_amount',
];

void main() {
  group('checkReasonMessage', () {
    test('translates every reason code the server declares', () {
      for (final code in _serverReasonCodes) {
        expect(
          checkReasonMessage(code),
          isNotNull,
          reason: 'missing translation for $code',
        );
      }
    });

    test('returns null for an unknown code so the evidence can take over',
        () {
      expect(checkReasonMessage('brand_new_reason'), isNull);
    });

    test('returns null for an absent code — a clean pass explains nothing',
        () {
      expect(checkReasonMessage(null), isNull);
    });
  });

  group('CheckTypes', () {
    test('translates the twelve types and echoes unknown ones', () {
      const twelve = [
        CheckTypes.barcodeIntegrity,
        CheckTypes.duplicate,
        CheckTypes.lookupAvailability,
        CheckTypes.lookupConsistency,
        CheckTypes.payeeMatch,
        CheckTypes.receivingBankMatch,
        CheckTypes.amountMatch,
        CheckTypes.payerMatch,
        CheckTypes.originTrust,
        CheckTypes.dueDateSanity,
        CheckTypes.tenantRouting,
        CheckTypes.pixBarcodeConsistency,
      ];

      for (final type in twelve) {
        expect(CheckTypes.label(type), isNot(type),
            reason: 'missing label for $type');
      }
      expect(CheckTypes.label('NewCheck'), 'NewCheck');
    });
  });

  group('BillCheck', () {
    test('prefers the translated reason code over the evidence', () {
      final check = _check(
        reasonCode: 'payee_lookalike',
        evidence: 'nome parecido: PADARIA S JOSE',
      );

      expect(check.reasonMessage, contains('golpe'));
    });

    test('falls back to the evidence when the code is unknown', () {
      final check = _check(
        reasonCode: 'brand_new_reason',
        evidence: 'evidência escrita pelo servidor',
      );

      expect(check.reasonMessage, 'evidência escrita pelo servidor');
    });

    test('shows nothing for a clean pass', () {
      final check = _check(outcome: CheckOutcomes.passed);

      expect(check.reasonMessage, isNull);
      expect(check.requiresAttention, isFalse);
    });
  });
}

BillCheck _check({
  String outcome = CheckOutcomes.failed,
  String? reasonCode,
  String? evidence,
}) {
  return BillCheck(
    type: CheckTypes.payeeMatch,
    outcome: outcome,
    severity: CheckSeverities.blocking,
    isBlockingFailure: outcome == CheckOutcomes.failed,
    evaluatedAt: DateTime(2026, 1, 1),
    reasonCode: reasonCode,
    evidence: evidence,
  );
}
