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
  'lookup_not_configured',
  'lookup_bank_mismatch',
  'lookup_amount_mismatch',
  'lookup_due_date_mismatch',
  'payee_not_registered',
  'payee_inactive',
  'payee_blacklisted',
  'payee_lookalike',
  'payee_same_cnpj_root',
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
  'payee_is_the_payer',
  'payer_only_inside_barcode',
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
  // Documento × consulta (check 13) — ficaram fora da varredura quando o
  // check nasceu; a lista abaixo paga essa dívida.
  'reading_not_available',
  'document_payee_mismatch',
  'document_payee_is_the_payer',
  'document_payee_suspicion',
  'document_payee_from_email_body',
  'document_amount_divergence',
  'document_due_date_divergence',
  'official_identity_not_available',
  'nothing_comparable',
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
    test('translates the thirteen types and echoes unknown ones', () {
      const thirteen = [
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
        CheckTypes.documentConsistency,
      ];

      for (final type in thirteen) {
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

  group('blocking payer reasons', () {
    // Os dois motivos novos bloqueiam o pagamento; a tela precisa dizer POR QUE, senão quem
    // aprova vê "verificação falhou" e não tem o que fazer com isso.
    test('the beneficiary being the payer explains itself and says it is blocked', () {
      final text = checkReasonMessage('payee_is_the_payer')!;

      expect(text, contains('BLOQUEADO'));
      expect(text, contains('beneficiário'));
    });

    test('a tax id found only inside the barcode explains why it means nothing', () {
      final text = checkReasonMessage('payer_only_inside_barcode')!;

      expect(text, contains('BLOQUEADO'));
      expect(text, contains('código de barras'));
    });
  });

  group('expectation check', () {
    // O rótulo da verificação 14 é o que a tela mostra na lista de checks —
    // sem ele, o tipo cru vazaria para a tela do aprovador.
    test('check 14 has a label of its own', () {
      expect(CheckTypes.label(CheckTypes.expectationMatch), 'Conta esperada');
    });

    // Todo motivo da verificação 14 tem tradução: um código sem texto cai na
    // evidência do servidor, que é escrita para diagnóstico e não para a tela.
    test('every expectation reason code is translated', () {
      const codes = [
        'expectation_cycle_opens_on_arrival',
        'expectation_not_registered',
        'expectation_ambiguous',
        'expectation_paused',
        'expectation_payee_unresolved',
        'expectation_due_date_unavailable',
      ];

      for (final code in codes) {
        expect(checkReasonMessage(code), isNotNull, reason: code);
      }
    });

    // O caso que o recurso existe para expor: chegou uma cobrança que ninguém
    // estava esperando, e a frase precisa dizer isso a quem vai aprovar.
    test('a bill nobody expected says so in plain words', () {
      final text = checkReasonMessage('expectation_not_registered')!;

      expect(text, contains('não tem conta esperada'));
      expect(text, contains('aguardando'));
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
