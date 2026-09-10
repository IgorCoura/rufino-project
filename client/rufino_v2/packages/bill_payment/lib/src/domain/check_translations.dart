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

  /// Whether this bill was one the system was waiting for — the inverse of
  /// the expectation alert: there the system says an expected bill never
  /// arrived; here it says a bill nobody expected did.
  static const String expectationMatch = 'ExpectationMatch';

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
        expectationMatch => 'Conta esperada',
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
        'Não foi possível verificar este boleto: a consulta oficial não '
            'respondeu. Não há indício contra ele — há ausência de qualquer '
            'confirmação sobre quem recebe, quanto e quando, e é isso que o '
            'coloca em Extremo Perigo. Revalide mais tarde: respondendo a '
            'consulta, a classificação cai sozinha.',
      'lookup_unresolved' =>
        'O provedor oficial não reconhece este documento. Título registrado é '
            'sempre reconhecido, então revalidar não muda este resultado — e um '
            'documento que ninguém registrou é o sinal mais forte de fabricação '
            'que o sistema consegue dar.',
      'lookup_not_configured' =>
        'Esta conta ainda não vinculou a chave do provedor de pagamentos, '
            'então nenhum boleto pode ser verificado. Revalidar não resolve: '
            'vincule a conta no Perfil do Pagador e revalide depois.',
      'lookup_bank_mismatch' =>
        'O banco da consulta diverge do impresso no documento.',
      'lookup_amount_mismatch' =>
        'O valor da consulta diverge do impresso no documento.',
      'lookup_due_date_mismatch' =>
        'O vencimento da consulta diverge do impresso no documento.',

      // Beneficiário.
      'payee_not_registered' => 'Beneficiário não cadastrado.',
      'payee_inactive' => 'Beneficiário desativado no cadastro.',
      'payee_blacklisted' =>
        'BLOQUEADO: o beneficiário está na sua lista de bloqueio.',
      'payee_lookalike' =>
        'O nome parece o de um beneficiário conhecido, mas o documento é de '
            'outro. Possível golpe.',
      'payee_same_cnpj_root' =>
        'A cobrança veio de outra filial do beneficiário cadastrado — mesma '
            'raiz de CNPJ. Confira e, se for o caso, cadastre a filial.',
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
      'document_payee_from_email_body' =>
        'O beneficiário divergente foi lido no CORPO DO E-MAIL, não no '
            'documento — e o corpo é escrito por quem enviou a mensagem. '
            'Confira o documento antes de decidir.',
      'document_payee_suspicion' =>
        'O beneficiário impresso no documento não é o que o Pix vai pagar. '
            'Pode ser erro de leitura ou documento adulterado — confira o '
            'documento antes de aprovar.',
      'document_payee_is_the_payer' =>
        'O único beneficiário lido no documento é o CNPJ do próprio pagador — '
            'leitura descartada, sem nada a confrontar.',
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
      // Não diz mais "impresso no documento": desde o ADR-024 a contradição também pode vir da
      // consulta oficial do Pix, e o texto antigo apontaria a fonte errada para quem aprova.
      'payer_mismatch' =>
        'O pagador desta cobrança não é este cliente.',
      'payer_not_extractable' =>
        'Não foi possível determinar de quem é este documento.',
      // O ÚNICO passe forte do check 8 (ADR-025), e por isso tem texto próprio: a cobrança foi
      // registrada contra o documento fiscal do cliente. Um passe SEM motivo neste check significa
      // outra coisa — o PDF afirmou e nada contradisse —, e mostrar os dois com o mesmo selo seria
      // prometer uma verificação que não houve.
      'payer_confirmed_by_lookup' =>
        'A consulta oficial confirma que esta cobrança foi emitida contra este cliente.',
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
      // Sem produtor desde 2026-09-10 (o corte de 14h do check 10 foi removido por medicao).
      // A traducao FICA: boletos verificados antes disso guardam o codigo, e a tela de aprovacao
      // ainda precisa saber le-lo.
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

      // Conta esperada. Nenhum destes desmente nada — todos têm teto de
      // Atenção, e existem para separar "chegou o que eu esperava" de
      // "chegou uma conta que ninguém estava esperando".
      'expectation_cycle_opens_on_arrival' =>
        'A conta era esperada; o acompanhamento deste mês começa nesta '
            'chegada.',
      'expectation_not_registered' =>
        'Este beneficiário não tem conta esperada cadastrada — ninguém '
            'estava aguardando esta cobrança.',
      'expectation_ambiguous' =>
        'Mais de uma conta deste beneficiário poderia ser esta. Confira de '
            'qual delas se trata.',
      'expectation_paused' =>
        'A conta esperada deste beneficiário está pausada ou desativada.',
      'expectation_payee_unresolved' =>
        'Sem beneficiário identificado não há conta esperada a conferir.',
      'expectation_due_date_unavailable' =>
        'Sem vencimento legível não foi possível conferir a conta esperada.',
      _ => null,
    };
