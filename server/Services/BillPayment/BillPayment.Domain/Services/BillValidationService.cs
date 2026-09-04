namespace BillPayment.Domain.Services;

using System.Globalization;
using BillPayment.Domain.Bills;
using BillPayment.Domain.Bills.Checks;
using BillPayment.Domain.Instruments;
using BillPayment.Domain.Lookups;
using BillPayment.Domain.Payees;
using BillPayment.Domain.SharedKernel;

/// <summary>
/// Apura as doze verificações do catálogo (<c>03-bill-validation.md</c>) cruzando o boleto com
/// os cadastros do tenant e com o que a consulta oficial devolveu.
/// </summary>
/// <remarks>
/// <para>
/// É Domain Service porque a pergunta cruza quatro Aggregates — <c>Bill</c>, <c>Payee</c>,
/// <c>TrustedOrigin</c> e <c>PayerProfile</c>. Recebe os agregados já carregados, devolve
/// <strong>valores</strong>, e nunca muta nada: quem grava é <c>Bill.RecordChecks</c>, e quem
/// decide o status é o próprio agregado.
/// </para>
/// <para>
/// <strong>Puro e síncrono.</strong> Nenhum I/O, nenhum relógio: a data e a hora entram pelo
/// contexto. É o que torna a apuração inteira testável sem banco e sem rede — e a apuração é o
/// que decide se um pagamento pode acontecer.
/// </para>
/// <para>
/// Sempre devolve <strong>as doze</strong>. Verificação que não se aplica sai <c>Skipped</c>
/// com motivo; omitir deixaria pergunta sem resposta parecendo respondida, e
/// <c>RecordChecks</c> recusa conjunto parcial.
/// </para>
/// </remarks>
public static class BillValidationService
{
    /// <summary>
    /// Requisição depois desta hora é processada no dia útil seguinte pelo provedor. Espelha a
    /// regra do Asaas descrita em <c>04-integrations.md</c>.
    /// </summary>
    public const int PROVIDER_CUTOFF_HOUR = 14;

    /// <summary>Tolerância de vencimento entre fontes, em dias. Cobre fuso e arredondamento.</summary>
    public const int DUE_DATE_TOLERANCE_DAYS = 1;

    public static IReadOnlyCollection<CheckResult> Evaluate(BillValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return
        [
            EvaluateBarcodeIntegrity(context),
            EvaluateDuplicate(context),
            EvaluateLookupAvailability(context),
            EvaluateLookupConsistency(context),
            EvaluatePayeeMatch(context),
            EvaluateReceivingBankMatch(context),
            EvaluateAmountMatch(context),
            EvaluatePayerMatch(context),
            EvaluateOriginTrust(context),
            EvaluateDueDateSanity(context),
            EvaluateTenantRouting(context),
            EvaluatePixBarcodeConsistency(context),
            EvaluateDocumentConsistency(context),
        ];
    }

    // 1. A integridade estrutural é provada pela construção: DigitableLine e PixPayload não
    // existem em estado inválido, então um Bill que chegou aqui já passou. O check é gravado
    // mesmo assim para a auditoria ficar completa (doc 03, §1).
    private static CheckResult EvaluateBarcodeIntegrity(BillValidationContext context)
    {
        var instruments = string.Join(
            " + ",
            context.Bill.Instruments.Select(i => i.Kind.Name));

        return CheckResult.Passed(CheckType.BarcodeIntegrity, evidence: $"Instrumentos validados: {instruments}.");
    }

    private static CheckResult EvaluateDuplicate(BillValidationContext context)
    {
        if (context.Duplicate == DuplicateFinding.SameTenant)
            return CheckResult.Failed(
                CheckType.Duplicate,
                CheckReasons.DUPLICATE_SAME_TENANT,
                context.DuplicateOf is { } original
                    ? $"Já existe um boleto ativo com este mesmo instrumento: {original.Value}."
                    : "Já existe um boleto ativo com este mesmo instrumento.");

        // Aviso genérico de propósito: dizer de quem é o boleto original vazaria conta alheia.
        if (context.Duplicate == DuplicateFinding.OtherTenant)
            return CheckResult.Failed(
                CheckType.Duplicate,
                CheckReasons.DUPLICATE_OTHER_TENANT,
                "Este documento de cobrança já está sob gestão de outra conta do sistema.");

        if (context.Bill.DedupKey is null)
            return CheckResult.Inconclusive(
                CheckType.Duplicate,
                CheckReasons.DUPLICATE_KEY_UNAVAILABLE,
                "O documento só traz QR Pix estático, que é reutilizável e não serve de chave de duplicidade.");

        return CheckResult.Passed(CheckType.Duplicate);
    }

    // 3. A consulta é obrigatória e nunca cai para "aprova sem consulta". Indisponibilidade e
    // "não conheço este título" reprovam igual — mas o motivo distingue os dois, porque só um
    // deles melhora com retentativa.
    private static CheckResult EvaluateLookupAvailability(BillValidationContext context)
    {
        var failures = new List<string>();

        foreach (var (rail, result) in RailResults(context))
        {
            if (result is null || result.IsResolved)
                continue;

            failures.Add($"{rail}: {result.ReasonCode}");
        }

        if (failures.Count == 0)
            return CheckResult.Passed(CheckType.LookupAvailability);

        var retryable = RailResults(context).Any(r => r.Result is { IsResolved: false, IsRetryable: true });

        return CheckResult.Failed(
            CheckType.LookupAvailability,
            retryable ? CheckReasons.LOOKUP_UNAVAILABLE : CheckReasons.LOOKUP_UNRESOLVED,
            $"A consulta oficial não devolveu o documento — {string.Join("; ", failures)}.");
    }

    private static CheckResult EvaluateLookupConsistency(BillValidationContext context)
    {
        var barcode = Barcode(context.Bill);
        var snapshot = context.Bill.Lookup;

        if (barcode is null)
            return EvaluatePixLookupConsistency(context);

        if (snapshot is null)
            return CheckResult.Skipped(
                CheckType.LookupConsistency,
                CheckReasons.LOOKUP_UNAVAILABLE,
                "Sem retrato da consulta oficial não há contra o que comparar o código de barras.");

        var line = barcode.DigitableLine;
        var divergences = new List<string>();

        if (line.Kind.CarriesBankCode && snapshot.BankCode is not null
            && !line.BankCode.Equals(snapshot.BankCode))
        {
            divergences.Add($"banco {line.BankCode.Value} no código de barras, {snapshot.BankCode.Value} na consulta");
        }

        // Valor em aberto (típico de arrecadação) pula a comparação: não há valor registrado
        // contra o qual comparar o que está embutido no código de barras.
        if (!snapshot.AllowChangeValue && snapshot.OriginalAmount is not null
            && line.Amount.Amount != snapshot.OriginalAmount.Amount)
        {
            divergences.Add(Invariant(
                $"valor {line.Amount.Amount:0.00} no código de barras, {snapshot.OriginalAmount.Amount:0.00} na consulta"));
        }

        if (line.DueDate is { } embedded && snapshot.DueDate is { } official
            && !WithinDueDateTolerance(DateOnly.FromDateTime(embedded), official))
        {
            divergences.Add($"vencimento {embedded:yyyy-MM-dd} no código de barras, {official:yyyy-MM-dd} na consulta");
        }

        return divergences.Count == 0
            ? CheckResult.Passed(CheckType.LookupConsistency)
            : CheckResult.Failed(
                CheckType.LookupConsistency,
                ConsistencyReason(divergences),
                string.Join("; ", divergences) + ".");
    }

    private static CheckResult EvaluatePixLookupConsistency(BillValidationContext context)
    {
        var pix = PixInstrument(context.Bill);
        var snapshot = context.Bill.PixLookup;

        if (pix is null || snapshot is null)
            return CheckResult.Skipped(
                CheckType.LookupConsistency,
                CheckReasons.LOOKUP_UNAVAILABLE,
                "Sem retrato do decode não há contra o que comparar o QR.");

        var declared = pix.PixPayload.Amount;
        if (declared is null || snapshot.CanBePaidWithDifferentValue || snapshot.Amount is null)
            return CheckResult.Skipped(
                CheckType.LookupConsistency,
                CheckReasons.AMOUNT_OPEN,
                "O QR não declara valor fechado — nada a comparar contra o decode.");

        return declared.Amount == snapshot.Amount.Amount
            ? CheckResult.Passed(CheckType.LookupConsistency)
            : CheckResult.Failed(
                CheckType.LookupConsistency,
                CheckReasons.LOOKUP_AMOUNT_MISMATCH,
                Invariant($"valor {declared.Amount:0.00} no QR, {snapshot.Amount.Amount:0.00} no decode."));
    }

    private static CheckResult EvaluatePayeeMatch(BillValidationContext context)
    {
        var resolution = context.PayeeResolution;
        var beneficiary = Beneficiary(context.Bill);

        if (beneficiary is null)
            return CheckResult.Inconclusive(
                CheckType.PayeeMatch,
                CheckReasons.PAYEE_NOT_IDENTIFIED,
                "A consulta oficial não identificou o beneficiário.");

        // O sósia é o cenário de fraude: nome conhecido, documento de outra pessoa.
        if (resolution.Kind == PayeeMatchKind.Lookalike)
            return CheckResult.Failed(
                CheckType.PayeeMatch,
                CheckReasons.PAYEE_LOOKALIKE,
                $"O nome se parece com o beneficiário cadastrado \"{resolution.LookalikeName}\", "
                + "mas o documento fiscal é outro.");

        if (resolution.Kind == PayeeMatchKind.NotFound)
            return CheckResult.Inconclusive(
                CheckType.PayeeMatch,
                CheckReasons.PAYEE_NOT_REGISTERED,
                $"\"{beneficiary.DisplayName}\" ainda não está cadastrado como beneficiário.");

        var payee = resolution.Payee!;

        // A blacklist vence qualquer outro desfecho, inclusive o casamento exato por documento:
        // é o tenant dizendo "não pague este beneficiário", e um documento que confere só
        // confirma que o boleto é mesmo de quem ele mandou não pagar. Critical → Extremo Perigo.
        if (payee.Standing == PayeeStanding.Blacklisted)
            return CheckResult.Failed(
                CheckType.PayeeMatch,
                CheckReasons.PAYEE_BLACKLISTED,
                $"O beneficiário \"{payee.LegalName}\" está marcado na lista de bloqueio deste tenant.",
                CheckSeverity.Critical);

        if (!payee.IsActive)
            return CheckResult.Failed(
                CheckType.PayeeMatch,
                CheckReasons.PAYEE_INACTIVE,
                $"O beneficiário \"{payee.LegalName}\" está inativo no cadastro.");

        // Casou só por nome: sem documento fiscal não há como GARANTIR o beneficiário, então
        // não é Verde — é Atenção (decisão do usuário, 2026-08-31). A conta de concessionária
        // híbrida escapa disto pelo trilho Pix, cujo decode devolve o CNPJ que o código de
        // barras de arrecadação não carrega.
        if (resolution.Kind == PayeeMatchKind.ByName)
            return CheckResult.Inconclusive(
                CheckType.PayeeMatch,
                CheckReasons.MATCHED_BY_NAME_ONLY,
                $"Casou por nome com \"{payee.LegalName}\"; a consulta não devolveu documento fiscal. "
                + "Verificação parcial.");

        // Casou por documento. Nome diferente do cadastro é rotina — razão social muda, CNPJ
        // não —, mas em arrecadação é a única evidência que existe e não pode ser silenciada.
        var consultedName = beneficiary.DisplayName;
        if (consultedName is not null && !payee.MatchesName(consultedName))
            return CheckResult.Warning(
                CheckType.PayeeMatch,
                CheckReasons.PAYEE_NAME_DIVERGENCE,
                $"Documento fiscal confere, mas a consulta devolveu \"{consultedName}\" "
                + $"e o cadastro diz \"{payee.LegalName}\".");

        return CheckResult.Passed(CheckType.PayeeMatch);
    }

    private static CheckResult EvaluateReceivingBankMatch(BillValidationContext context)
    {
        var bill = context.Bill;

        // Arrecadação não tem campo de banco em posição nenhuma — não é escolha de desenho,
        // é ausência estrutural de dado. Trocar de provedor não muda isso.
        if (bill.Kind == BillKind.Utility)
            return CheckResult.Skipped(
                CheckType.ReceivingBankMatch,
                CheckReasons.BANK_NOT_AVAILABLE_FOR_UTILITY,
                "O código de barras de arrecadação não carrega banco recebedor.");

        var barcode = Barcode(bill);
        var fromBarcode = barcode?.DigitableLine.BankCode;

        // Duas fontes autoritativas discordando sobre o destino do dinheiro não é evento
        // legítimo — e é exatamente o campo que a fraude clássica precisa trocar.
        if (fromBarcode is not null && bill.Lookup?.BankCode is { } fromLookup && !fromBarcode.Equals(fromLookup))
            return CheckResult.Failed(
                CheckType.ReceivingBankMatch,
                CheckReasons.BANK_SOURCE_CONFLICT,
                $"O código de barras aponta o banco {fromBarcode.Value} e a consulta oficial o {fromLookup.Value}.",
                CheckSeverity.Blocking);

        var bank = fromBarcode ?? BankFromPix(context);
        if (bank is null)
            return CheckResult.Inconclusive(
                CheckType.ReceivingBankMatch,
                bill.Rail == PaymentRail.Pix ? CheckReasons.ISPB_WITHOUT_COMPE_CODE : CheckReasons.BANK_NOT_AVAILABLE,
                bill.Rail == PaymentRail.Pix
                    ? "A instituição do recebedor não tem código de três dígitos correspondente."
                    : "Não foi possível determinar o banco recebedor.");

        if (!context.BankDirectory.IsKnown(bank))
            return CheckResult.Failed(
                CheckType.ReceivingBankMatch,
                CheckReasons.BANK_UNKNOWN,
                $"O código {bank.Value} não corresponde a nenhuma instituição registrada no Banco Central.");

        var payee = context.PayeeResolution.Payee;
        var accepted = payee?.AcceptsBank(bank);

        if (accepted == false)
            return CheckResult.Failed(
                CheckType.ReceivingBankMatch,
                CheckReasons.BANK_NOT_ACCEPTED,
                $"O boleto liquida no banco {bank.Value} ({context.BankDirectory.NameOf(bank)}), "
                + "que não está entre os aceitos para este beneficiário.");

        // A tabela do Bacen pode estar mais velha que a realidade, então instituição fora da
        // Compe chama atenção sem bloquear.
        if (!context.BankDirectory.ParticipatesInCompe(bank))
            return CheckResult.Warning(
                CheckType.ReceivingBankMatch,
                CheckReasons.BANK_OUTSIDE_COMPE,
                $"{context.BankDirectory.NameOf(bank)} não consta como participante da Compe.");

        if (accepted is null)
            return CheckResult.Inconclusive(
                CheckType.ReceivingBankMatch,
                CheckReasons.BANK_EXPECTATION_NOT_SET,
                $"O boleto liquida no banco {bank.Value}; o beneficiário ainda não tem bancos aceitos cadastrados.");

        return CheckResult.Passed(
            CheckType.ReceivingBankMatch,
            evidence: $"Banco {bank.Value} ({context.BankDirectory.NameOf(bank)}) está entre os aceitos.");
    }

    private static CheckResult EvaluateAmountMatch(BillValidationContext context)
    {
        var payable = PayableAmount(context.Bill);
        if (payable is null)
            return CheckResult.Inconclusive(
                CheckType.AmountMatch,
                CheckReasons.AMOUNT_NOT_AVAILABLE,
                "A consulta oficial não devolveu valor a pagar.");

        if (IsOpenAmount(context.Bill))
            return CheckResult.Skipped(
                CheckType.AmountMatch,
                CheckReasons.AMOUNT_OPEN,
                Invariant($"O emissor permite alterar o valor; a consulta devolveu {payable.Amount:0.00}."));

        var payee = context.PayeeResolution.Payee;
        if (payee is null)
            return CheckResult.Inconclusive(
                CheckType.AmountMatch,
                CheckReasons.PAYEE_NOT_REGISTERED,
                Invariant($"Sem beneficiário cadastrado não há política de valor. Cobrado: {payable.Amount:0.00}."));

        // Unbounded passa em tudo, e por isso o resultado é inconclusivo: nada foi provado.
        if (!payee.AmountPolicy.IsConclusive)
            return CheckResult.Inconclusive(
                CheckType.AmountMatch,
                CheckReasons.AMOUNT_POLICY_UNBOUNDED,
                Invariant($"O beneficiário não tem expectativa de valor. Cobrado: {payable.Amount:0.00}."));

        return payee.AmountPolicy.Matches(payable)
            ? CheckResult.Passed(CheckType.AmountMatch, evidence: AmountEvidence(context.Bill, payable))
            : CheckResult.Failed(
                CheckType.AmountMatch,
                CheckReasons.AMOUNT_OUTSIDE_POLICY,
                AmountEvidence(context.Bill, payable));
    }

    /// <summary>
    /// A assimetria é o ponto do check: presença de contradição bloqueia, ausência de
    /// confirmação não libera. Um <c>Passed</c> aqui não prova propriedade — prova só que nada
    /// contradisse, num dado que ninguém certifica (ADR-004).
    /// </summary>
    private static CheckResult EvaluatePayerMatch(BillValidationContext context)
    {
        var profile = context.PayerProfile;
        if (profile is null)
            return CheckResult.Skipped(
                CheckType.PayerMatch,
                CheckReasons.PAYER_PROFILE_MISSING,
                "O tenant ainda não tem cadastro fiscal — não há contra o que comparar o pagador.");

        // O beneficiário NÃO pode ser o próprio pagador. Ninguém emite boleto contra si mesmo,
        // então isto é ou consulta descrevendo outro título, ou documento adulterado. A comparação
        // é por documento EXATO, nunca por raiz de CNPJ: filiais do mesmo grupo cobram umas às
        // outras legitimamente, e comparar por raiz barraria essa cobrança.
        if (context.Bill.Beneficiary?.TaxId is { } beneficiary
            && AllTenantTaxIds(context).Any(own => own.Equals(beneficiary)))
        {
            return CheckResult.Failed(
                CheckType.PayerMatch,
                CheckReasons.PAYEE_IS_THE_PAYER,
                $"O beneficiário da cobrança ({beneficiary.Formatted()}) é o próprio pagador — "
                + "esta conta seria paga para a própria conta que a está pagando.",
                CheckSeverity.Blocking);
        }

        var extracted = context.Bill.ExtractedPayer;
        if (extracted?.TaxId is { } taxId)
        {
            // O documento tem de estar IMPRESSO como campo, não ser um trecho do código de barras.
            // A varredura procura os documentos do cadastro diretamente no texto, e um código de
            // 44 posições pode, em tese, conter um deles por coincidência — 3 chances em 10
            // trilhões, medida como zero em 915 boletos reais. A guarda existe porque a atribuição
            // do boleto se apoia nesse documento: sem ela, uma coincidência viraria prova.
            if (IsOnlyInsideBarcode(context.Bill, taxId))
            {
                return CheckResult.Failed(
                    CheckType.PayerMatch,
                    CheckReasons.PAYER_ONLY_INSIDE_BARCODE,
                    $"O documento {taxId.Formatted()} aparece apenas dentro do código de barras, "
                    + "não impresso como documento do pagador — não identifica ninguém.",
                    CheckSeverity.Blocking);
            }

            return profile.Owns(taxId) || profile.OwnsByCnpjRoot(taxId)
                ? CheckResult.Passed(
                    CheckType.PayerMatch,
                    evidence: $"O documento identifica o pagador como {taxId.Formatted()}, que é do tenant.")
                : CheckResult.Failed(
                    CheckType.PayerMatch,
                    CheckReasons.PAYER_MISMATCH,
                    $"O documento identifica o pagador como {taxId.Formatted()}, "
                    + "que não pertence ao cadastro fiscal desta conta.",
                    CheckSeverity.Blocking);
        }

        // O decode do Pix devolve o pagador mascarado. Máscara não identifica ninguém, mas
        // pode contradizer — e contradição basta para bloquear.
        if (context.Bill.PixLookup?.Payer is { } masked && masked.VisibleDigitCount > 0
            && !masked.IsCompatibleWithAny(AllTenantTaxIds(context)))
        {
            return CheckResult.Failed(
                CheckType.PayerMatch,
                CheckReasons.PAYER_MISMATCH,
                "O pagador do QR Pix não pode ser nenhum dos documentos fiscais desta conta.",
                CheckSeverity.Blocking);
        }

        return CheckResult.Inconclusive(
            CheckType.PayerMatch,
            CheckReasons.PAYER_NOT_EXTRACTABLE,
            "O documento não traz o documento fiscal do pagador.");
    }

    private static CheckResult EvaluateOriginTrust(BillValidationContext context)
    {
        var origin = context.Bill.Origin;

        if (origin.SourceKind == BillSourceKind.ManualUpload)
            return CheckResult.Passed(
                CheckType.OriginTrust,
                CheckReasons.ORIGIN_MANUAL_UPLOAD,
                "Importado à mão por usuário autenticado.");

        if (context.Origin is null)
            return CheckResult.Inconclusive(
                CheckType.OriginTrust,
                CheckReasons.ORIGIN_UNKNOWN,
                origin.SenderAddress is null
                    ? "A origem não registrou remetente."
                    : $"O remetente {origin.SenderAddress} nunca foi visto antes.");

        // Critical, não Blocking: bloqueio é declaração explícita do tenant, e leva o boleto a
        // Extremo Perigo — um degrau acima da suspeita derivada.
        return context.Origin.Decision == TrustedOrigins.TrustDecision.Blocked
            ? CheckResult.Failed(
                CheckType.OriginTrust,
                CheckReasons.ORIGIN_BLOCKED,
                $"A origem {context.Origin.Value} está explicitamente bloqueada.",
                CheckSeverity.Critical)
            : CheckResult.Passed(
                CheckType.OriginTrust,
                evidence: $"Origem {context.Origin.Value} marcada como confiável.");
    }

    private static CheckResult EvaluateDueDateSanity(BillValidationContext context)
    {
        var bill = context.Bill;

        // O QR dinâmico expira, e expirar antes do agendamento é situação sem equivalente no
        // boleto — o pagamento simplesmente não acontece na data pedida.
        if (bill.PixLookup is { ExpirationDate: not null } pix
            && DateOnly.FromDateTime(pix.ExpirationDate!.Value.UtcDateTime) < context.Today)
        {
            return CheckResult.Failed(
                CheckType.DueDateSanity,
                CheckReasons.PIX_EXPIRES_BEFORE_SCHEDULE,
                $"O QR Pix expirou em {pix.ExpirationDate:yyyy-MM-dd}.");
        }

        if (bill.Lookup?.IsOverdue == true)
            return CheckResult.Failed(
                CheckType.DueDateSanity,
                CheckReasons.OVERDUE,
                DueDateEvidence(bill, "O documento está vencido"));

        // A leitura por IA é a última reserva (decisão de 2026-08-27): QR estático sem data
        // oficial usa a impressa no documento — e a evidência declara a procedência.
        var official = bill.Lookup?.DueDate ?? bill.PixLookup?.DueDate;
        var dueDate = official ?? bill.Reading?.DueDate;
        var fromReading = official is null && dueDate is not null;

        if (dueDate is null)
            return CheckResult.Inconclusive(
                CheckType.DueDateSanity,
                CheckReasons.DUE_DATE_NOT_AVAILABLE,
                "Nem a consulta oficial nem a leitura do documento trouxeram vencimento.");

        if (dueDate < context.Today)
            return CheckResult.Failed(
                CheckType.DueDateSanity,
                CheckReasons.OVERDUE,
                DueDateEvidence(bill, "O documento está vencido"));

        if (bill.Lookup?.MinimumScheduleDate is { } minimum && minimum > dueDate)
            return CheckResult.Failed(
                CheckType.DueDateSanity,
                CheckReasons.CANNOT_SCHEDULE_BEFORE_DUE,
                $"O provedor só agenda a partir de {minimum:yyyy-MM-dd}, depois do vencimento em {dueDate:yyyy-MM-dd}.");

        if (dueDate == context.Today && context.TimeOfDay.Hour >= PROVIDER_CUTOFF_HOUR)
            return CheckResult.Failed(
                CheckType.DueDateSanity,
                CheckReasons.SAME_DAY_AFTER_CUTOFF,
                $"Vence hoje e já passou das {PROVIDER_CUTOFF_HOUR}h — o provedor processaria no dia útil seguinte.");

        return CheckResult.Passed(
            CheckType.DueDateSanity,
            evidence: fromReading
                ? $"Vence em {dueDate:yyyy-MM-dd} (data lida do documento pela IA; sem fonte oficial); "
                    + $"há {dueDate.Value.DayNumber - context.Today.DayNumber} dia(s)."
                : $"Vence em {dueDate:yyyy-MM-dd}; há {dueDate.Value.DayNumber - context.Today.DayNumber} dia(s).");
    }

    // 13. O documento impresso × a consulta oficial, com a leitura por IA como testemunha. É o
    // par que faltava: LookupConsistency compara o parse offline, PixBarcodeConsistency compara
    // os dois trilhos oficiais — este compara o que o EMISSOR imprimiu com o que o REGISTRO diz.
    // A assimetria é a mesma do PayerMatch: contradição de identidade escala para Blocking (é o
    // vetor de instrumento trocado sobre documento legítimo); valor e vencimento divergentes são
    // aviso — boleto vencido acumula encargos legitimamente; e ausência nunca pesa.
    private static CheckResult EvaluateDocumentConsistency(BillValidationContext context)
    {
        var bill = context.Bill;
        var reading = bill.Reading;

        if (reading is null || !reading.HasContent)
            return CheckResult.Skipped(
                CheckType.DocumentConsistency,
                CheckReasons.READING_NOT_AVAILABLE,
                "Sem leitura por IA para comparar — extração desligada ou sem conteúdo.");

        var official = bill.Beneficiary;

        // Identidade primeiro: documento fiscal lido (já provado pelo DV) contra o oficial.
        if (reading.PayeeTaxId is { } readTaxId
            && official?.TaxId is { } officialTaxId
            && !readTaxId.Equals(officialTaxId))
        {
            return CheckResult.Failed(
                CheckType.DocumentConsistency,
                CheckReasons.DOCUMENT_PAYEE_MISMATCH,
                $"O documento imprime o beneficiário {readTaxId.Formatted()}, mas a consulta "
                    + $"oficial diz {officialTaxId.Formatted()} — cara de instrumento trocado "
                    + "sobre documento legítimo.",
                severity: CheckSeverity.Blocking);
        }

        var warnings = new List<string>();

        var officialAmount = bill.Lookup?.OriginalAmount?.Amount ?? bill.PixLookup?.Amount?.Amount;
        if (reading.Amount is { } readAmount && officialAmount is { } faceAmount && readAmount != faceAmount)
        {
            warnings.Add(
                $"valor impresso R$ {readAmount:N2} × valor registrado R$ {faceAmount:N2}");
        }

        var officialDue = bill.Lookup?.DueDate ?? bill.PixLookup?.DueDate;
        if (reading.DueDate is { } readDue && officialDue is { } officialDueDate
            && Math.Abs(readDue.DayNumber - officialDueDate.DayNumber) > DUE_DATE_TOLERANCE_DAYS)
        {
            warnings.Add(
                $"vencimento impresso {readDue:yyyy-MM-dd} × registrado {officialDueDate:yyyy-MM-dd}");
        }

        if (warnings.Count > 0)
        {
            var reason = warnings[0].StartsWith("valor", StringComparison.Ordinal)
                ? CheckReasons.DOCUMENT_AMOUNT_DIVERGENCE
                : CheckReasons.DOCUMENT_DUE_DATE_DIVERGENCE;

            return CheckResult.Warning(
                CheckType.DocumentConsistency,
                reason,
                $"Divergência entre o impresso e o oficial: {string.Join("; ", warnings)}.");
        }

        var comparedIdentity = reading.PayeeTaxId is not null && official?.TaxId is not null;
        var comparedAmount = reading.Amount is not null && officialAmount is not null;
        var comparedDueDate = reading.DueDate is not null && officialDue is not null;

        if (comparedIdentity || comparedAmount || comparedDueDate)
        {
            var compared = new List<string>();
            if (comparedIdentity)
                compared.Add("beneficiário");
            if (comparedAmount)
                compared.Add("valor");
            if (comparedDueDate)
                compared.Add("vencimento");

            return CheckResult.Passed(
                CheckType.DocumentConsistency,
                evidence: $"O impresso confere com o oficial ({string.Join(", ", compared)}).");
        }

        // Há leitura, mas nada em comum com o oficial para confrontar — arrecadação sem CNPJ na
        // consulta, ou consulta indisponível. Ausência não pesa (ADR-004).
        return CheckResult.Inconclusive(
            CheckType.DocumentConsistency,
            official is null ? CheckReasons.OFFICIAL_IDENTITY_NOT_AVAILABLE : CheckReasons.NOTHING_COMPARABLE,
            "A leitura existe, mas não há campo oficial correspondente para confrontar.");
    }

    /// <summary>
    /// Informa, não decide: aprovar um boleto que chegou por inferência é decisão diferente de
    /// aprovar um que chegou por constatação, e a tela precisa deixar isso visível.
    /// </summary>
    private static CheckResult EvaluateTenantRouting(BillValidationContext context)
    {
        if (context.Bill.Origin.SourceKind == BillSourceKind.ManualUpload)
            return CheckResult.Skipped(
                CheckType.TenantRouting,
                CheckReasons.ROUTING_MANUAL_IMPORT,
                "O próprio usuário trouxe o documento; não passou pela escada de roteamento.");

        var routing = context.Bill.Routing;
        if (routing is null)
            return CheckResult.Inconclusive(
                CheckType.TenantRouting,
                CheckReasons.ROUTING_NOT_RECORDED,
                "A captura não registrou por qual degrau este documento foi atribuído à conta.");

        return routing.IsConclusive
            ? CheckResult.Passed(CheckType.TenantRouting, evidence: $"Atribuído por {routing.Name}.")
            : CheckResult.Inconclusive(
                CheckType.TenantRouting,
                CheckReasons.ROUTING_INFERRED,
                $"Atribuído por {routing.Name} — a conta foi inferida, não constatada.");
    }

    /// <summary>
    /// A defesa mais barata do catálogo: duas consultas que o sistema já faz, comparadas entre
    /// si. Pega o vetor mais direto em circulação — QR Pix adulterado colado sobre boleto
    /// verdadeiro. <strong>Nunca escolhe um trilho e segue.</strong>
    /// </summary>
    private static CheckResult EvaluatePixBarcodeConsistency(BillValidationContext context)
    {
        var bill = context.Bill;

        if (Barcode(bill) is null || PixInstrument(bill) is null)
            return CheckResult.Skipped(
                CheckType.PixBarcodeConsistency,
                CheckReasons.SINGLE_RAIL_DOCUMENT,
                "O documento traz um trilho só; não há duas histórias para comparar.");

        if (bill.PixLookup is { CanBePaid: false } refused)
            return CheckResult.Failed(
                CheckType.PixBarcodeConsistency,
                CheckReasons.PIX_QR_NOT_PAYABLE,
                $"O provedor recusa este QR: {refused.CannotBePaidReason ?? "sem motivo informado"}.");

        var barcodeLookup = bill.Lookup;
        var pixLookup = bill.PixLookup;

        if (barcodeLookup is null || pixLookup is null)
            return CheckResult.Skipped(
                CheckType.PixBarcodeConsistency,
                CheckReasons.LOOKUP_UNAVAILABLE,
                "Falta o retrato de um dos trilhos; a comparação exige os dois.");

        var payeeDivergence = TaxIdsDiverge(barcodeLookup.Beneficiary.TaxId, pixLookup.Receiver.TaxId);
        if (payeeDivergence)
            return CheckResult.Failed(
                CheckType.PixBarcodeConsistency,
                CheckReasons.PIX_BARCODE_PAYEE_MISMATCH,
                $"O código de barras aponta para {barcodeLookup.Beneficiary.TaxId!.Formatted()} "
                + $"e o QR Pix para {pixLookup.Receiver.TaxId!.Formatted()}.");

        if (pixLookup.PayableAmount is null)
            return CheckResult.Warning(
                CheckType.PixBarcodeConsistency,
                CheckReasons.STATIC_QR_WITHOUT_AMOUNT,
                "O QR não carrega valor; só foi possível comparar o beneficiário.");

        if (barcodeLookup.Amount is { } expected && expected.Amount != pixLookup.PayableAmount.Amount)
            return CheckResult.Failed(
                CheckType.PixBarcodeConsistency,
                CheckReasons.PIX_BARCODE_AMOUNT_MISMATCH,
                Invariant($"Código de barras cobra {expected.Amount:0.00} e o QR Pix {pixLookup.PayableAmount.Amount:0.00}."));

        if (barcodeLookup.DueDate is { } barcodeDue && pixLookup.DueDate is { } pixDue
            && !WithinDueDateTolerance(barcodeDue, pixDue))
        {
            return CheckResult.Warning(
                CheckType.PixBarcodeConsistency,
                CheckReasons.PIX_BARCODE_DUE_DATE_MISMATCH,
                $"Vencimento {barcodeDue:yyyy-MM-dd} no código de barras e {pixDue:yyyy-MM-dd} no QR Pix.");
        }

        return CheckResult.Passed(CheckType.PixBarcodeConsistency);
    }

    private static IEnumerable<(string Rail, LookupResult? Result)> RailResults(BillValidationContext context)
    {
        if (Barcode(context.Bill) is not null)
            yield return (PaymentRail.Boleto.Name, context.BankSlipLookup);
        if (PixInstrument(context.Bill) is not null)
            yield return (PaymentRail.Pix.Name, context.PixLookup);
    }

    private static PaymentInstrument? Barcode(Bill bill)
        => bill.Instruments.FirstOrDefault(i => i.Kind == PaymentInstrumentKind.Barcode);

    private static PaymentInstrument? PixInstrument(Bill bill)
        => bill.Instruments.FirstOrDefault(i => i.Kind == PaymentInstrumentKind.PixQr);

    /// <summary>O beneficiário do trilho que vai pagar, com o outro como reserva.</summary>
    private static LookupParty? Beneficiary(Bill bill)
        => bill.Rail == PaymentRail.Pix
            ? bill.PixLookup?.Receiver ?? bill.Lookup?.Beneficiary
            : bill.Lookup?.Beneficiary ?? bill.PixLookup?.Receiver;

    private static Money? PayableAmount(Bill bill)
        => bill.Rail == PaymentRail.Pix
            ? bill.PixLookup?.PayableAmount ?? bill.Lookup?.Amount
            : bill.Lookup?.Amount ?? bill.PixLookup?.PayableAmount;

    private static bool IsOpenAmount(Bill bill)
        => bill.Rail == PaymentRail.Pix
            ? bill.PixLookup?.CanBePaidWithDifferentValue == true
            : bill.Lookup?.AllowChangeValue == true;

    private static BankCode? BankFromPix(BillValidationContext context)
        => context.Bill.PixLookup?.ReceiverIspb is { } ispb
            ? context.BankDirectory.FromIspb(ispb)
            : null;

    /// <summary>
    /// O documento aparece <strong>dentro</strong> de algum código de barras do boleto?
    /// </summary>
    /// <remarks>
    /// Compara contra as 44 posições já validadas do instrumento, e não contra o texto do PDF.
    /// A distinção é o que separa "trecho de um código" de "campo colado ao vizinho": IPTU, DARF
    /// e DAS imprimem o CNPJ do contribuinte encostado no código de arrecadação, e olhar o texto
    /// bruto reprovaria 90 guias legítimas medidas no acervo.
    /// </remarks>
    private static bool IsOnlyInsideBarcode(Bill bill, TaxId taxId)
        => bill.Instruments
            .Where(i => i.Kind == PaymentInstrumentKind.Barcode)
            .Any(i => i.DigitableLine.Barcode.Contains(taxId.Value, StringComparison.Ordinal));

    private static IEnumerable<TaxId> AllTenantTaxIds(BillValidationContext context)
    {
        var profile = context.PayerProfile!;

        yield return profile.PrimaryTaxId;
        foreach (var additional in profile.AdditionalTaxIds)
            yield return additional;
    }

    private static bool TaxIdsDiverge(TaxId? left, TaxId? right)
        => left is not null && right is not null && !left.Equals(right);

    private static bool WithinDueDateTolerance(DateOnly left, DateOnly right)
        => Math.Abs(left.DayNumber - right.DayNumber) <= DUE_DATE_TOLERANCE_DAYS;

    private static string ConsistencyReason(List<string> divergences)
    {
        var first = divergences[0];
        if (first.StartsWith("banco", StringComparison.Ordinal))
            return CheckReasons.LOOKUP_BANK_MISMATCH;

        return first.StartsWith("valor", StringComparison.Ordinal)
            ? CheckReasons.LOOKUP_AMOUNT_MISMATCH
            : CheckReasons.LOOKUP_DUE_DATE_MISMATCH;
    }

    /// <summary>
    /// A evidência distingue "cobraram a mais" de "está vencido e acumulou encargos" — sem
    /// isso o aprovador não tem como decidir sobre uma diferença de valor.
    /// </summary>
    private static string AmountEvidence(Bill bill, Money payable)
    {
        var original = bill.Lookup?.OriginalAmount ?? bill.PixLookup?.Amount;
        var interest = bill.Lookup?.Interest ?? bill.PixLookup?.Interest;
        var fine = bill.Lookup?.Fine ?? bill.PixLookup?.Fine;

        var parts = new List<string> { Invariant($"a pagar {payable.Amount:0.00}") };

        if (original is not null && original.Amount != payable.Amount)
            parts.Add(Invariant($"original {original.Amount:0.00}"));
        if (interest is { IsZero: false })
            parts.Add(Invariant($"juros {interest.Amount:0.00}"));
        if (fine is { IsZero: false })
            parts.Add(Invariant($"multa {fine.Amount:0.00}"));

        return string.Join(", ", parts) + ".";
    }

    private static string DueDateEvidence(Bill bill, string prefix)
    {
        var dueDate = bill.Lookup?.DueDate ?? bill.PixLookup?.DueDate;
        var payable = bill.Lookup?.Amount ?? bill.PixLookup?.PayableAmount;

        var suffix = payable is null ? string.Empty : Invariant($" Valor atualizado: {payable.Amount:0.00}.");
        return dueDate is null ? $"{prefix}.{suffix}" : $"{prefix} desde {dueDate:yyyy-MM-dd}.{suffix}";
    }

    private static string Invariant(FormattableString text) => text.ToString(CultureInfo.InvariantCulture);
}
