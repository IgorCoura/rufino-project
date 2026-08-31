namespace BillPayment.Domain.Bills.Checks;

/// <summary>
/// Códigos de motivo dos checks.
/// </summary>
/// <remarks>
/// <para>
/// <strong>É contrato de UI</strong> (ADR-003): a tela traduz o código, não a mensagem. Por
/// isso <strong>mudar um código existente é mudança quebrante</strong>; código novo é aditivo.
/// Vivem como constantes para que um erro de digitação seja erro de compilação e para que
/// <c>grep</c> encontre quem produz cada motivo.
/// </para>
/// <para>
/// Ausência de motivo é legítima: um <c>Passed</c> limpo não precisa explicar nada.
/// </para>
/// </remarks>
public static class CheckReasons
{
    // Duplicata — a evidência muda conforme o dono do boleto original (ADR-008).
    public const string DUPLICATE_SAME_TENANT = "duplicate_same_tenant";
    public const string DUPLICATE_OTHER_TENANT = "duplicate_other_tenant";

    /// <summary>
    /// Documento sem chave de uso único (só QR Pix estático). A defesa por
    /// (beneficiário, valor, vencimento) que o doc 02 exige ainda não existe — este motivo é o
    /// que impede a lacuna de passar por verificação bem-sucedida.
    /// </summary>
    public const string DUPLICATE_KEY_UNAVAILABLE = "duplicate_key_unavailable";

    // Consulta oficial.
    public const string LOOKUP_UNAVAILABLE = "lookup_unavailable";
    public const string LOOKUP_UNRESOLVED = "lookup_unresolved";
    public const string LOOKUP_BANK_MISMATCH = "lookup_bank_mismatch";
    public const string LOOKUP_AMOUNT_MISMATCH = "lookup_amount_mismatch";
    public const string LOOKUP_DUE_DATE_MISMATCH = "lookup_due_date_mismatch";

    // Beneficiário.
    public const string PAYEE_NOT_REGISTERED = "payee_not_registered";
    public const string PAYEE_INACTIVE = "payee_inactive";

    /// <summary>O tenant marcou o beneficiário na blacklist. Falha bloqueante — o boleto nasce Perigo.</summary>
    public const string PAYEE_BLACKLISTED = "payee_blacklisted";
    public const string PAYEE_LOOKALIKE = "payee_lookalike";
    public const string PAYEE_NAME_DIVERGENCE = "payee_name_divergence";
    public const string PAYEE_NOT_IDENTIFIED = "payee_not_identified";

    /// <summary>Casou por nome, sem documento. Verificação <strong>parcial</strong> — a tela não pode mostrar como "verificado".</summary>
    public const string MATCHED_BY_NAME_ONLY = "matched_by_name_only";

    // Banco recebedor.
    public const string BANK_EXPECTATION_NOT_SET = "bank_expectation_not_set";
    public const string BANK_NOT_ACCEPTED = "bank_not_accepted";
    public const string BANK_UNKNOWN = "bank_unknown";
    public const string BANK_OUTSIDE_COMPE = "bank_outside_compe";
    public const string BANK_SOURCE_CONFLICT = "bank_source_conflict";
    public const string BANK_NOT_AVAILABLE_FOR_UTILITY = "bank_not_available_for_utility";
    public const string ISPB_WITHOUT_COMPE_CODE = "ispb_without_compe_code";
    public const string BANK_NOT_AVAILABLE = "bank_not_available";

    // Valor.
    public const string AMOUNT_OUTSIDE_POLICY = "amount_outside_policy";
    public const string AMOUNT_POLICY_UNBOUNDED = "amount_policy_unbounded";
    public const string AMOUNT_OPEN = "amount_open";
    public const string AMOUNT_NOT_AVAILABLE = "amount_not_available";

    // Pagador — a assimetria do ADR-004 mora nestes três.
    public const string PAYER_MISMATCH = "payer_mismatch";
    public const string PAYER_NOT_EXTRACTABLE = "payer_not_extractable";
    public const string PAYER_PROFILE_MISSING = "payer_profile_missing";

    /// <summary>
    /// O documento fiscal atribuído ao pagador está <strong>dentro</strong> do código de barras,
    /// e não impresso como campo. Coincidência de dígitos, não identificação — a atribuição do
    /// boleto se apoiou em nada.
    /// </summary>
    public const string PAYER_ONLY_INSIDE_BARCODE = "payer_only_inside_barcode";

    /// <summary>
    /// O beneficiário da cobrança é o próprio pagador. Ninguém emite boleto contra si mesmo:
    /// ou a consulta oficial descreve outro título, ou o documento foi adulterado.
    /// </summary>
    public const string PAYEE_IS_THE_PAYER = "payee_is_the_payer";

    // Origem.
    public const string ORIGIN_UNKNOWN = "origin_unknown";
    public const string ORIGIN_BLOCKED = "origin_blocked";
    public const string ORIGIN_MANUAL_UPLOAD = "origin_manual_upload";

    // Vencimento e agendamento.
    public const string OVERDUE = "overdue";
    public const string SAME_DAY_AFTER_CUTOFF = "same_day_after_cutoff";
    public const string CANNOT_SCHEDULE_BEFORE_DUE = "cannot_schedule_before_due";
    public const string PIX_EXPIRES_BEFORE_SCHEDULE = "pix_expires_before_schedule";
    public const string DUE_DATE_NOT_AVAILABLE = "due_date_not_available";

    // Roteamento entre tenants.
    public const string ROUTING_MANUAL_IMPORT = "routing_manual_import";
    public const string ROUTING_INFERRED = "routing_inferred";
    public const string ROUTING_NOT_RECORDED = "routing_not_recorded";

    // Consistência entre trilhos — o vetor de fraude mais direto em circulação.
    public const string PIX_BARCODE_PAYEE_MISMATCH = "pix_barcode_payee_mismatch";
    public const string PIX_BARCODE_AMOUNT_MISMATCH = "pix_barcode_amount_mismatch";
    public const string PIX_BARCODE_DUE_DATE_MISMATCH = "pix_barcode_due_date_mismatch";
    public const string SINGLE_RAIL_DOCUMENT = "single_rail_document";
    public const string PIX_QR_NOT_PAYABLE = "pix_qr_not_payable";
    public const string STATIC_QR_WITHOUT_AMOUNT = "static_qr_without_amount";
    // 13. DocumentConsistency
    public const string READING_NOT_AVAILABLE = "reading_not_available";
    public const string DOCUMENT_PAYEE_MISMATCH = "document_payee_mismatch";
    public const string DOCUMENT_AMOUNT_DIVERGENCE = "document_amount_divergence";
    public const string DOCUMENT_DUE_DATE_DIVERGENCE = "document_due_date_divergence";
    public const string OFFICIAL_IDENTITY_NOT_AVAILABLE = "official_identity_not_available";
    public const string NOTHING_COMPARABLE = "nothing_comparable";
}
