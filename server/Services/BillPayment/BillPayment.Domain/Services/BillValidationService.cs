namespace BillPayment.Domain.Services;

using System.Globalization;
using BillPayment.Domain.Bills;
using BillPayment.Domain.Bills.Checks;
using BillPayment.Domain.Expectations;
using BillPayment.Domain.Instruments;
using BillPayment.Domain.Lookups;
using BillPayment.Domain.Payees;
using BillPayment.Domain.SharedKernel;

/// <summary>
/// Apura as catorze verificações do catálogo (<c>03-bill-validation.md</c>) cruzando o boleto com
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
/// Sempre devolve <strong>as catorze</strong>. Verificação que não se aplica sai <c>Skipped</c>
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
            EvaluateExpectationMatch(context),
        ];
    }

    /// <summary>
    /// 14. O inverso do alerta do ADR-014: o sistema já avisa quando a conta esperada não chega;
    /// aqui ele diz que chegou uma conta que ninguém esperava.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Todo desfecho tem teto de Atenção (<see cref="CheckSeverity.Notice"/> no
    /// <see cref="CheckType.ExpectationMatch"/>): não haver expectativa não desmente nada — a
    /// maior parte dos boletos legítimos nunca teve uma.
    /// </para>
    /// <para>
    /// Quem apura é o <c>ExpectationMatchingService</c>, o <strong>mesmo</strong> serviço que o
    /// cumprimento usa. Uma segunda implementação da regra faria a tela dizer "conta esperada"
    /// sobre um ciclo que segue alertando sozinho.
    /// </para>
    /// </remarks>
    private static CheckResult EvaluateExpectationMatch(BillValidationContext context)
    {
        var bill = context.Bill;

        if (bill.PayeeId is null)
            return CheckResult.Inconclusive(
                CheckType.ExpectationMatch,
                CheckReasons.EXPECTATION_PAYEE_UNRESOLVED,
                "Sem beneficiário resolvido não há expectativa contra a qual perguntar.");

        if (context.Expectations.Count == 0)
            return CheckResult.Inconclusive(
                CheckType.ExpectationMatch,
                CheckReasons.EXPECTATION_NOT_REGISTERED,
                "Nenhuma conta esperada está cadastrada para este beneficiário.");

        if (bill.DueDate is not { } dueDate)
            return CheckResult.Inconclusive(
                CheckType.ExpectationMatch,
                CheckReasons.EXPECTATION_DUE_DATE_UNAVAILABLE,
                "Sem vencimento legível não há competência para casar com a conta esperada.");

        // O próprio boleto pode já ter cumprido o ciclo numa passagem anterior — revalidar é
        // rotina, e sem isto o segundo passe encontraria "nenhuma expectativa" sobre ele.
        var match = ExpectationMatchingService.Match(
            context.Expectations, dueDate, context.Today, alreadyFulfilledBy: bill.Id);

        if (match is not null)
            return CheckResult.Passed(
                CheckType.ExpectationMatch,
                evidence: $"Casou com a conta esperada, competência {new CompetencePeriod(dueDate.Year, dueDate.Month)}.");

        if (!context.Expectations.Any(e => e.IsWatchingOn(context.Today)))
            return CheckResult.Inconclusive(
                CheckType.ExpectationMatch,
                CheckReasons.EXPECTATION_PAUSED,
                "A conta esperada deste beneficiário está pausada ou desativada — ninguém a vigia hoje.");

        // Sem ciclo para a competência, mas com uma única expectativa vigiando: o cumprimento
        // abre o ciclo na chegada. É a mesma rede de segurança que o handler usa, e por isso a
        // pergunta é feita ao mesmo serviço.
        var competence = new CompetencePeriod(dueDate.Year, dueDate.Month);
        if (ExpectationMatchingService.SoleWatchingWithoutCycleFor(
                context.Expectations, competence, context.Today) is not null)
        {
            return CheckResult.Passed(
                CheckType.ExpectationMatch,
                CheckReasons.EXPECTATION_CYCLE_OPENS_ON_ARRIVAL,
                $"A conta era esperada; o ciclo de {competence} nasce nesta chegada.");
        }

        return CheckResult.Inconclusive(
            CheckType.ExpectationMatch,
            CheckReasons.EXPECTATION_AMBIGUOUS,
            "Mais de uma conta deste beneficiário poderia ser esta — não é possível dizer qual.");
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

    // 3. A consulta é obrigatória e nunca cai para "aprova sem consulta". Falhar aqui leva o
    // boleto a EXTREMO PERIGO, e não a Perigo: sem resposta ninguém confirmou quem recebe, quanto
    // e quando, e um atacante capaz de derrubar ou saturar a consulta ganharia justamente a janela
    // em que o boleto não pode ser conferido.
    //
    // O motivo distingue três situações que pedem AÇÕES diferentes de quem aprova, e é por isso
    // que não colapsam num código só: esperar, desconfiar, ou ir cadastrar a conta.
    private static CheckResult EvaluateLookupAvailability(BillValidationContext context)
    {
        var unresolved = RailResults(context)
            .Where(r => r.Result is { IsResolved: false })
            .ToList();

        if (unresolved.Count == 0)
            return CheckResult.Passed(CheckType.LookupAvailability);

        var evidence = string.Join("; ", unresolved.Select(r => $"{r.Rail}: {r.Result!.ReasonCode}"));

        return CheckResult.Failed(
            CheckType.LookupAvailability,
            AvailabilityReason(unresolved),
            $"A consulta oficial não devolveu o documento — {evidence}.");
    }

    /// <summary>
    /// Qual das três ausências. <strong>"Sem chave" vence</strong>: quando é ela, o tenant não
    /// consultou coisa nenhuma, e mandar "revalide mais tarde" seria prometer o que o tempo não
    /// cumpre.
    /// </summary>
    private static string AvailabilityReason(List<(string Rail, LookupResult? Result)> unresolved)
    {
        if (unresolved.TrueForAll(r => string.Equals(
                r.Result!.ReasonCode, LookupReasons.TENANT_KEY_NOT_CONFIGURED, StringComparison.Ordinal)))
        {
            return CheckReasons.LOOKUP_NOT_CONFIGURED;
        }

        return unresolved.Exists(r => r.Result!.IsRetryable)
            ? CheckReasons.LOOKUP_UNAVAILABLE
            : CheckReasons.LOOKUP_UNRESOLVED;
    }

    private static CheckResult EvaluateLookupConsistency(BillValidationContext context)
    {
        var barcode = Barcode(context.Bill);
        var snapshot = context.Bill.Lookup;

        // Sem código de barras, ou com ele e SEM o retrato do boleto, a comparação possível é a do
        // Pix. O ramo do Pix era inalcançável sempre que existisse um código de barras — e é
        // justamente em arrecadação, onde a consulta do boleto mais falha, que o decode responde
        // (doc 12). Cair para cá é o mesmo conserto do banco recebedor: não descartar a fonte que
        // respondeu porque a outra não respondeu.
        if (barcode is null || snapshot is null)
            return EvaluatePixLookupConsistency(context);

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
                "Nenhum dos trilhos tem retrato oficial para comparar com o que o documento declara.");

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

        // Outra filial do mesmo inscrito. A raiz do CNPJ é atribuída pela Receita a uma pessoa
        // jurídica só, então isto é identidade — e não o sósia do ramo acima. Órgão público cobra
        // por unidade da federação (a Receita emite DAS por uma filial do Ministério da Fazenda e
        // o cadastro guarda outra) e rede com CNPJ por loja faz o mesmo. Sai com teto de Atenção:
        // é o cadastro que está incompleto, não o boleto que está errado — e anunciar "Possível
        // golpe" numa conta de rotina é como o alerta que importa deixa de ser lido.
        if (resolution.Kind == PayeeMatchKind.SameCnpjRoot)
            return CheckResult.Warning(
                CheckType.PayeeMatch,
                CheckReasons.PAYEE_SAME_CNPJ_ROOT,
                $"A cobrança vem de {beneficiary.TaxId!.Formatted()} e o cadastro de "
                + $"\"{payee.LegalName}\" guarda {payee.TaxId.Formatted()} — mesma raiz de CNPJ, "
                + "outra filial.",
                CheckSeverity.Notice);

        // Casou só por nome: sem documento fiscal não há como GARANTIR o beneficiário, então
        // não é Verde — é Atenção (decisão do usuário, 2026-08-31). A conta de concessionária
        // híbrida escapa disto pelo trilho Pix, cujo decode devolve o CNPJ que o código de
        // barras de arrecadação não carrega. A severidade Notice é o que MANTÉM isto em
        // Atenção depois do endurecimento de 2026-09-08 (ADR-020): 100% da arrecadação chega
        // por aqui, e mandá-la para Perigo faria a conta de luz exigir "assumo o risco" todo
        // mês — pelo nome ter batido, que é o desfecho bom deste ramo.
        if (resolution.Kind == PayeeMatchKind.ByName)
            return CheckResult.Inconclusive(
                CheckType.PayeeMatch,
                CheckReasons.MATCHED_BY_NAME_ONLY,
                $"Casou por nome com \"{payee.LegalName}\"; a consulta não devolveu documento fiscal. "
                + "Verificação parcial.",
                CheckSeverity.Notice);

        // Casou por documento. Nome diferente do cadastro é rotina — razão social muda, CNPJ
        // não —, mas em arrecadação é a única evidência que existe e não pode ser silenciada.
        // Compara os DOIS nomes que a consulta devolve, pelo mesmo critério da resolução: até
        // 2026-09-08 só o DisplayName era conferido, e quem cadastrava o beneficiário pelo nome
        // fantasia via divergência em todo boleto, mesmo com o TradingName batendo exatamente.
        if (!PayeeResolutionService.NameMatches(payee, beneficiary))
            return CheckResult.Warning(
                CheckType.PayeeMatch,
                CheckReasons.PAYEE_NAME_DIVERGENCE,
                $"Documento fiscal confere, mas a consulta devolveu \"{beneficiary.DisplayName}\" "
                + $"e o cadastro diz \"{payee.LegalName}\".",
                CheckSeverity.Notice);

        return CheckResult.Passed(CheckType.PayeeMatch);
    }

    private static CheckResult EvaluateReceivingBankMatch(BillValidationContext context)
    {
        var bill = context.Bill;

        var barcode = Barcode(bill);
        var fromBarcode = bill.Kind == BillKind.Utility ? null : barcode?.DigitableLine.BankCode;
        var fromPix = BankFromPix(context);

        // Arrecadação não tem campo de banco no código de barras — não é escolha de desenho, é
        // ausência estrutural de dado, e trocar de provedor não muda isso. MAS o decode do QR Pix
        // devolve o ISPB do recebedor, e num documento híbrido é exatamente o dado que falta
        // (doc 12, achado 3: o Pix cobre o buraco da arrecadação). Pular o check com o banco na
        // mão descartava a única fonte que a guia tem — e a tela dizia "não se aplica" enquanto a
        // consulta oficial, logo abaixo, exibia o banco recebedor.
        if (bill.Kind == BillKind.Utility && fromPix is null)
            return CheckResult.Skipped(
                CheckType.ReceivingBankMatch,
                CheckReasons.BANK_NOT_AVAILABLE_FOR_UTILITY,
                "O código de barras de arrecadação não carrega banco recebedor, e não há QR Pix "
                + "de onde tirá-lo.");

        // Duas fontes autoritativas discordando sobre o destino do dinheiro não é evento
        // legítimo — e é exatamente o campo que a fraude clássica precisa trocar.
        if (fromBarcode is not null && bill.Lookup?.BankCode is { } fromLookup && !fromBarcode.Equals(fromLookup))
            return CheckResult.Failed(
                CheckType.ReceivingBankMatch,
                CheckReasons.BANK_SOURCE_CONFLICT,
                $"O código de barras aponta o banco {fromBarcode.Value} e a consulta oficial o {fromLookup.Value}.",
                CheckSeverity.Blocking);

        // O banco confrontado com o cadastro é o do trilho QUE VAI PAGAR — mesma precedência de
        // PayableAmount e Beneficiary. Ler o COMPE do boleto num documento que liquida por Pix
        // descreveria um pagamento que não vai acontecer: o dinheiro sai pelo PSP do recebedor.
        // A comparação barcode × consulta acima é outra coisa e continua como está — ali o
        // objetivo é confrontar duas fontes, não escolher uma.
        var bank = bill.Rail == PaymentRail.Pix
            ? fromPix ?? fromBarcode
            : fromBarcode ?? fromPix;

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

        // Teto de Atenção: o tenant nunca declarou bancos aceitos, e ausência de EXPECTATIVA não
        // desmente nada (decisão do usuário, 2026-09-10). Continuam pesando como Perigo o banco
        // fora da lista e o banco que o Bacen não conhece — aqueles são evidência, não ausência.
        if (accepted is null)
            return CheckResult.Inconclusive(
                CheckType.ReceivingBankMatch,
                CheckReasons.BANK_EXPECTATION_NOT_SET,
                $"O boleto liquida no banco {bank.Value}; o beneficiário ainda não tem bancos aceitos cadastrados.",
                CheckSeverity.Notice);

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
        // Teto de Atenção pelo mesmo motivo do banco: é o cadastro que está incompleto, não o
        // boleto que está errado. Valor FORA de uma política declarada continua sendo Perigo.
        if (!payee.AmountPolicy.IsConclusive)
            return CheckResult.Inconclusive(
                CheckType.AmountMatch,
                CheckReasons.AMOUNT_POLICY_UNBOUNDED,
                Invariant($"O beneficiário não tem expectativa de valor. Cobrado: {payable.Amount:0.00}."),
                CheckSeverity.Notice);

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

        // O pagador que o decode do Pix devolve é FONTE OFICIAL, e vem antes do que se inferiu do
        // PDF (doc 12, achado 1: em cobrança registrada ele volta completo, não mascarado). Estava
        // atrás da inferência, e por isso nunca era consultado num documento que trazia o CNPJ
        // impresso. A assimetria do ADR-004 não muda: contradição bloqueia, compatibilidade não
        // confirma — o que muda é a ordem de quem se consulta primeiro.
        if (context.Bill.PixLookup?.Payer is { } masked && masked.VisibleDigitCount > 0
            && !masked.IsCompatibleWithAny(AllTenantTaxIds(context)))
        {
            return CheckResult.Failed(
                CheckType.PayerMatch,
                CheckReasons.PAYER_MISMATCH,
                "O pagador que a consulta oficial do Pix devolveu não pode ser nenhum dos "
                + "documentos fiscais desta conta.",
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

        // O vencimento é o CONSOLIDADO pelo agregado, não um recálculo local: o check refazia a
        // precedência à mão, sempre pelo boleto primeiro e pulando a linha digitável, e a
        // verificação 14 lia outra data no mesmo boleto. A procedência vem junto, para a evidência
        // continuar dizendo em quem se está confiando.
        var dueDate = bill.DueDate;
        var source = bill.DueDateOrigin;

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

        var days = dueDate.Value.DayNumber - context.Today.DayNumber;
        var provenance = source switch
        {
            _ when source == DueDateSource.Barcode =>
                " (data embutida no código de barras, protegida por DV; a consulta oficial não trouxe vencimento)",
            _ when source == DueDateSource.Reading =>
                " (data lida do documento pela IA; nem a consulta oficial nem o código de barras trouxeram vencimento)",
            _ => string.Empty,
        };

        return CheckResult.Passed(
            CheckType.DueDateSanity,
            evidence: $"Vence em {dueDate:yyyy-MM-dd}{provenance}; há {days} dia(s).");
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

        // A leitura apontou o PRÓPRIO pagador como beneficiário: descarte, jamais contradição.
        //
        // Guia de tributo — DAS, DARF, GPS — imprime UM par CNPJ/Razão Social, o do contribuinte,
        // e não imprime beneficiário nenhum: o órgão arrecadador só existe no código de barras.
        // Diante de um campo de beneficiário a preencher e de uma única parte no papel, o
        // extrator atribui essa parte ao beneficiário, e o boleto de imposto nascia bloqueado
        // como "instrumento trocado sobre documento legítimo".
        //
        // <strong>O descarte não custa capacidade de detecção.</strong> A fraude que este check
        // existe para pegar imprime SEMPRE um terceiro — o CNPJ para onde o dinheiro deve ir.
        // Um "beneficiário" igual ao pagador não descreve pagamento nenhum: ninguém emite
        // cobrança contra si mesmo, e é a mesma impossibilidade que o check 8 usa para bloquear
        // pelo lado oposto.
        var misreadAsPayer = reading.PayeeTaxId is { } read && IsOwnedByTenant(context, read);
        var readPayeeTaxId = misreadAsPayer ? null : reading.PayeeTaxId;

        // Identidade primeiro: documento fiscal lido (já provado pelo DV) contra o oficial.
        //
        // O PESO depende do trilho (decisão do usuário, 2026-09-10). No BOLETO a consulta oficial
        // devolve menos — em arrecadação, nada de documento —, então o impresso é parte do que
        // sustenta a verificação, e contradizê-lo é indício forte de adulteração: bloqueia. No
        // PIX o decode devolve o CNPJ do recebedor, e a identidade já está verificada sem ajuda do
        // papel; ali a divergência é mais provável ser erro da leitura por IA, que ninguém
        // certifica e cujo insumo inclui o corpo do e-mail. Vira aviso com teto de Atenção, com
        // texto próprio — o alerta continua existindo, deixa de decidir sozinho.
        //
        // O que segura o boleto adulterado quando este ramo não bloqueia: o beneficiário do
        // fraudador não está cadastrado, e o check 5 o reprova como `payee_not_registered`, que
        // continua pesando Perigo DE PROPÓSITO. Rebaixar aquele motivo abriria esta porta.
        if (readPayeeTaxId is { } readTaxId
            && official?.TaxId is { } officialTaxId
            && !readTaxId.Equals(officialTaxId))
        {
            var printed = $"O documento imprime o beneficiário {readTaxId.Formatted()}, "
                + $"mas a consulta oficial diz {officialTaxId.Formatted()}";

            // Procedência antes de peso: número que só existe no corpo do e-mail foi escrito por
            // quem mandou a mensagem. Bloquear com base nele entregaria a essa pessoa o poder de
            // travar os pagamentos de quem recebe — e, casando de propósito com o oficial, o de
            // calar a verificação. Nenhum dos dois pode depender de texto de terceiro.
            if (reading.PayeeTaxIdSource == ReadingFieldSource.EmailBody)
            {
                return CheckResult.Warning(
                    CheckType.DocumentConsistency,
                    CheckReasons.DOCUMENT_PAYEE_FROM_EMAIL_BODY,
                    $"{printed} — mas esse documento aparece no corpo do e-mail, não no papel. "
                    + "Confira o documento em vez de confiar na mensagem.",
                    CheckSeverity.Notice);
            }

            return bill.Rail == PaymentRail.Pix
                ? CheckResult.Warning(
                    CheckType.DocumentConsistency,
                    CheckReasons.DOCUMENT_PAYEE_SUSPICION,
                    $"{printed} — o Pix vai pagar quem a consulta devolveu. Pode ser erro de "
                    + "leitura ou documento adulterado.",
                    CheckSeverity.Notice)
                : CheckResult.Failed(
                    CheckType.DocumentConsistency,
                    CheckReasons.DOCUMENT_PAYEE_MISMATCH,
                    $"{printed} — cara de instrumento trocado sobre documento legítimo.",
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

        // O descarte acima é dito em voz alta: quem aprova precisa saber que a leitura errou o
        // papel do documento, mesmo — sobretudo — quando isso deixou de bloquear.
        var misreadNote = misreadAsPayer
            ? " A leitura atribuiu ao beneficiário o documento do próprio pagador e foi descartada."
            : string.Empty;

        if (warnings.Count > 0)
        {
            var reason = warnings[0].StartsWith("valor", StringComparison.Ordinal)
                ? CheckReasons.DOCUMENT_AMOUNT_DIVERGENCE
                : CheckReasons.DOCUMENT_DUE_DATE_DIVERGENCE;

            return CheckResult.Warning(
                CheckType.DocumentConsistency,
                reason,
                $"Divergência entre o impresso e o oficial: {string.Join("; ", warnings)}.{misreadNote}");
        }

        var comparedIdentity = readPayeeTaxId is not null && official?.TaxId is not null;
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
                evidence: $"O impresso confere com o oficial ({string.Join(", ", compared)}).{misreadNote}");
        }

        // Só havia a identidade a confrontar, e ela era o documento do próprio pagador. Dizer
        // "nada comparável" aqui esconderia por quê.
        if (misreadAsPayer)
            return CheckResult.Inconclusive(
                CheckType.DocumentConsistency,
                CheckReasons.DOCUMENT_PAYEE_IS_THE_PAYER,
                "O único beneficiário lido no documento é o documento fiscal do próprio pagador — "
                + "leitura descartada, e não sobrou campo para confrontar.",
                CheckSeverity.Notice);

        // Há leitura, mas nada em comum com o oficial para confrontar — arrecadação sem CNPJ na
        // consulta, ou consulta indisponível. Ausência não pesa (ADR-004).
        // Teto de Atenção: não havia o que confrontar. Contradição entre o impresso e o oficial
        // continua acima disto — ver o desfecho de identidade lá em cima.
        return CheckResult.Inconclusive(
            CheckType.DocumentConsistency,
            official is null ? CheckReasons.OFFICIAL_IDENTITY_NOT_AVAILABLE : CheckReasons.NOTHING_COMPARABLE,
            "A leitura existe, mas não há campo oficial correspondente para confrontar.",
            CheckSeverity.Notice);
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
            // Teto de Atenção: informa, não desmente — é o que o próprio XML doc deste check diz.
            : CheckResult.Inconclusive(
                CheckType.TenantRouting,
                CheckReasons.ROUTING_INFERRED,
                $"Atribuído por {routing.Name} — a conta foi inferida, não constatada.",
                CheckSeverity.Notice);
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

    /// <summary>
    /// O documento é do próprio tenant? Sem cadastro fiscal não há contra o que comparar, e a
    /// resposta segura é "não sei" — que aqui significa não descartar leitura nenhuma.
    /// </summary>
    private static bool IsOwnedByTenant(BillValidationContext context, TaxId taxId)
        => context.PayerProfile is not null && AllTenantTaxIds(context).Any(own => own.Equals(taxId));

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
