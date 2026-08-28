/// Translation of the verification contract: check types and reason codes.
///
/// `CheckReasons` is a UI contract on the server (ADR-003): the screen
/// translates the **code**, never the message. A code missing here is a new
/// reason, not an error — the check's own evidence text is the fallback.
library;

/// Wire values of the backend's `CheckType` smart enum, with labels.
abstract final class CheckTypes {
  /// The digitable line's four check digits.
  static const String barcodeIntegrity = 'BarcodeIntegrity';

  /// Whether this instrument is already under management.
  static const String duplicate = 'Duplicate';

  /// Whether the official lookup answered.
  static const String lookupAvailability = 'LookupAvailability';

  /// Whether the lookup agrees with the printed instrument.
  static const String lookupConsistency = 'LookupConsistency';

  /// Whether the beneficiary is a registered payee.
  static const String payeeMatch = 'PayeeMatch';

  /// Whether the receiving bank is one the payee accepts.
  static const String receivingBankMatch = 'ReceivingBankMatch';

  /// Whether the amount fits the payee's policy.
  static const String amountMatch = 'AmountMatch';

  /// Whether the extracted payer is this tenant.
  static const String payerMatch = 'PayerMatch';

  /// Whether the sender is trusted, blocked or unknown.
  static const String originTrust = 'OriginTrust';

  /// Whether the due date makes sense for scheduling.
  static const String dueDateSanity = 'DueDateSanity';

  /// How the bill was routed to this tenant.
  static const String tenantRouting = 'TenantRouting';

  /// Whether the QR and the barcode describe the same payment — the most
  /// direct fraud vector in circulation.
  static const String pixBarcodeConsistency = 'PixBarcodeConsistency';

  /// Whether what is printed on the document (as the AI read it) matches
  /// the official lookup.
  static const String documentConsistency = 'DocumentConsistency';

  /// The label to show for [type].
  static String label(String type) => switch (type) {
        barcodeIntegrity => 'Integridade do código',
        duplicate => 'Duplicidade',
        lookupAvailability => 'Consulta oficial',
        lookupConsistency => 'Consistência da consulta',
        payeeMatch => 'Beneficiário',
        receivingBankMatch => 'Banco recebedor',
        amountMatch => 'Valor',
        payerMatch => 'Pagador',
        originTrust => 'Origem',
        dueDateSanity => 'Vencimento',
        tenantRouting => 'Roteamento',
        pixBarcodeConsistency => 'Pix × código de barras',
        documentConsistency => 'Documento × consulta oficial',
        _ => type,
      };
}

/// Translates a check's `reasonCode` into Portuguese.
///
/// Returns `null` for an unknown or absent code — the caller falls back to
/// the check's `evidence`, which the server writes for humans. A `Passed`
/// without a reason is legitimate and needs no explanation.
String? checkReasonMessage(String? reasonCode) => switch (reasonCode) {
      // Duplicata.
      'duplicate_same_tenant' => 'Este boleto já está cadastrado nesta conta.',
      'duplicate_other_tenant' =>
        'Este boleto já está sob gestão de outra conta.',
      'duplicate_key_unavailable' =>
        'Documento sem chave de uso único — a duplicidade não pôde ser '
            'verificada.',

      // Consulta oficial.
      'lookup_unavailable' =>
        'A consulta oficial estava indisponível. Revalide mais tarde.',
      'lookup_unresolved' => 'O provedor não reconheceu este documento.',
      'lookup_bank_mismatch' =>
        'O banco da consulta diverge do impresso no documento.',
      'lookup_amount_mismatch' =>
        'O valor da consulta diverge do impresso no documento.',
      'lookup_due_date_mismatch' =>
        'O vencimento da consulta diverge do impresso no documento.',

      // Beneficiário.
      'payee_not_registered' => 'Beneficiário não cadastrado.',
      'payee_inactive' => 'Beneficiário desativado no cadastro.',
      'payee_lookalike' =>
        'O nome parece o de um beneficiário conhecido, mas o documento é de '
            'outro. Possível golpe.',
      'payee_name_divergence' =>
        'O nome na consulta diverge do cadastro do beneficiário.',
      'payee_not_identified' =>
        'A consulta não identificou o beneficiário.',
      'matched_by_name_only' =>
        'Casou apenas pelo nome, sem documento — verificação parcial.',

      // Banco recebedor.
      'bank_expectation_not_set' =>
        'O cadastro do beneficiário não define bancos aceitos.',
      'bank_not_accepted' =>
        'O banco recebedor não está entre os aceitos para este beneficiário.',
      'bank_unknown' => 'Banco recebedor desconhecido.',
      'bank_outside_compe' => 'Banco fora da tabela COMPE.',
      'bank_source_conflict' =>
        'Duas fontes oficiais divergem sobre o banco recebedor.',
      'bank_not_available_for_utility' =>
        'Arrecadação não carrega banco — verificação não se aplica.',
      'ispb_without_compe_code' =>
        'A instituição Pix não tem código COMPE correspondente.',
      'bank_not_available' => 'O banco recebedor não pôde ser determinado.',

      // Documento × consulta oficial (check 13).
      'reading_not_available' =>
        'Sem leitura por IA para comparar com a consulta oficial.',
      'document_payee_mismatch' =>
        'O beneficiário impresso no documento NÃO é o que a consulta oficial '
            'devolveu. Forte indício de boleto adulterado — confira antes de '
            'qualquer coisa.',
      'document_amount_divergence' =>
        'O valor impresso no documento diverge do valor registrado.',
      'document_due_date_divergence' =>
        'O vencimento impresso diverge do registrado.',
      'official_identity_not_available' =>
        'A consulta oficial não trouxe identidade para confrontar com o '
            'documento.',
      'nothing_comparable' =>
        'A leitura existe, mas não há campo oficial correspondente para '
            'confrontar.',

      // Valor.
      'amount_outside_policy' =>
        'O valor está fora da política definida para este beneficiário.',
      'amount_policy_unbounded' =>
        'A política de valor deste beneficiário não limita — verificação '
            'inconclusiva.',
      'amount_open' => 'Documento de valor em aberto.',
      'amount_not_available' => 'O valor não pôde ser determinado.',

      // Pagador.
      'payer_mismatch' =>
        'O pagador impresso no documento não é este cliente.',
      'payer_not_extractable' =>
        'O pagador não pôde ser lido do documento.',
      'payer_profile_missing' =>
        'Perfil do pagador não cadastrado — verificação impossível.',
      // Os dois bloqueiam o pagamento. O texto diz o que houve E o que fazer, porque
      // "verificação falhou" sem motivo deixa quem aprova sem saída.
      'payee_is_the_payer' =>
        'BLOQUEADO: o beneficiário da cobrança é este próprio cliente — '
            'ninguém emite boleto contra si mesmo. Confira o documento antes de pagar.',
      'payer_only_inside_barcode' =>
        'BLOQUEADO: o documento do pagador só aparece dentro do código de barras, '
            'não impresso no boleto — isso é coincidência de dígitos, não identificação.',

      // Origem.
      'origin_unknown' => 'Remetente desconhecido.',
      'origin_blocked' => 'Remetente bloqueado no cadastro.',
      'origin_manual_upload' =>
        'Importação manual — não há remetente a verificar.',

      // Vencimento e agendamento.
      'overdue' => 'O documento está vencido.',
      'same_day_after_cutoff' =>
        'Vence hoje, após o horário-limite de agendamento.',
      'cannot_schedule_before_due' =>
        'Não é possível agendar antes do prazo mínimo do provedor.',
      'pix_expires_before_schedule' =>
        'O QR Pix expira antes da data de agendamento.',
      'due_date_not_available' => 'O vencimento não pôde ser determinado.',

      // Roteamento.
      'routing_manual_import' => 'Importado manualmente por uma pessoa.',
      'routing_inferred' =>
        'Atribuído por vínculo de cadastro, sem prova direta.',
      'routing_not_recorded' => 'O roteamento não foi registrado.',

      // Consistência Pix × código de barras.
      'pix_barcode_payee_mismatch' =>
        'O QR Pix e o código de barras apontam beneficiários diferentes. '
            'Possível fraude.',
      'pix_barcode_amount_mismatch' =>
        'O QR Pix e o código de barras trazem valores diferentes.',
      'pix_barcode_due_date_mismatch' =>
        'O QR Pix e o código de barras trazem vencimentos diferentes.',
      'single_rail_document' =>
        'Documento com um trilho só — não há o que comparar.',
      'pix_qr_not_payable' => 'O QR Pix não é pagável.',
      'static_qr_without_amount' => 'QR Pix estático sem valor definido.',
      _ => null,
    };
