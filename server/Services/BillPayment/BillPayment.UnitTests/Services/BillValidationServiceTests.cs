namespace BillPayment.UnitTests.Services;

using BillPayment.Domain.Bills;
using BillPayment.Domain.Bills.Checks;
using BillPayment.Domain.Lookups;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.Services;
using BillPayment.Domain.SharedKernel;
using BillPayment.Domain.TrustedOrigins;
using BillPayment.UnitTests.Bills.Mothers;
using BillPayment.UnitTests.Instruments;
using BillPayment.UnitTests.Lookups.Mothers;
using BillPayment.UnitTests.Services.Mothers;

public class BillValidationServiceTests
{
    // O catálogo inteiro sai em toda validação: o que não se aplica vira Skipped, nunca some.
    // É o que sustenta a exigência de cobertura completa em RecordChecks.
    [Fact]
    public void Evaluate_ShouldAlwaysProduceTheWholeCatalogExactlyOnce()
    {
        var catalog = Enumeration.GetAll<CheckType>().Count();

        var results = Evaluate(ValidationMother.Context(ValidationMother.BankSlipWithLookup()));

        Assert.Equal(catalog, results.Count);
        Assert.Equal(catalog, results.Select(r => r.Type).Distinct().Count());
        Assert.All(Enumeration.GetAll<CheckType>(), type => Assert.Contains(results, r => r.Type == type));
    }

    // Um boleto que confere com o cadastro em tudo não produz nenhuma falha bloqueante.
    [Fact]
    public void Evaluate_WithEverythingMatchingTheRegistration_ShouldHaveNoBlockingFailure()
    {
        var context = ValidationMother.Context(
            ValidationMother.BankSlipWithLookup(),
            payee: ValidationMother.RegisteredPayee(),
            origin: ValidationMother.TrustedSender(),
            payerProfile: ValidationMother.TenantProfile());

        Assert.DoesNotContain(Evaluate(context), r => r.IsBlockingFailure);
    }

    // A integridade estrutural é provada pela construção do VO — o check existe para a
    // auditoria ficar completa, não para reprovar aqui.
    [Fact]
    public void Evaluate_BarcodeIntegrity_ShouldAlwaysPassBecauseTheInstrumentAlreadyParsed()
    {
        var result = Check(ValidationMother.Context(ValidationMother.BankSlipWithLookup()), CheckType.BarcodeIntegrity);

        Assert.Equal(CheckOutcome.Passed, result.Outcome);
    }

    // Duplicata do mesmo tenant reprova e a evidência traz o id do boleto original.
    [Fact]
    public void Evaluate_Duplicate_WhenTheOriginalBelongsToTheTenant_ShouldFailWithTheOriginalId()
    {
        var original = BillId.From(new Guid("0195a1f0-0000-7000-8000-0000000000ff"));

        var result = Check(
            ValidationMother.Context(
                ValidationMother.BankSlipWithLookup(),
                duplicate: DuplicateFinding.SameTenant,
                duplicateOf: original),
            CheckType.Duplicate);

        Assert.Equal(CheckOutcome.Failed, result.Outcome);
        Assert.Equal(CheckReasons.DUPLICATE_SAME_TENANT, result.ReasonCode);
        Assert.Contains(original.Value.ToString(), result.Evidence, StringComparison.Ordinal);
    }

    // Duplicata de outro tenant reprova com aviso GENÉRICO — dizer de quem é vazaria conta
    // alheia (ADR-008). A evidência não pode conter identificador nenhum.
    [Fact]
    public void Evaluate_Duplicate_WhenTheOriginalBelongsToAnotherTenant_ShouldNotRevealAnything()
    {
        var result = Check(
            ValidationMother.Context(ValidationMother.BankSlipWithLookup(), duplicate: DuplicateFinding.OtherTenant),
            CheckType.Duplicate);

        Assert.Equal(CheckOutcome.Failed, result.Outcome);
        Assert.Equal(CheckReasons.DUPLICATE_OTHER_TENANT, result.ReasonCode);
        Assert.DoesNotContain(BillMother.DefaultTenant.Value.ToString(), result.Evidence, StringComparison.Ordinal);
        Assert.Contains("outra conta", result.Evidence, StringComparison.Ordinal);
    }

    // Documento só com QR estático não tem chave de uso único: a lacuna aparece como
    // inconclusiva em vez de passar por verificação bem-sucedida.
    [Fact]
    public void Evaluate_Duplicate_WithStaticPixOnly_ShouldBeInconclusive()
    {
        var result = Check(ValidationMother.Context(BillMother.StaticPixOnly()), CheckType.Duplicate);

        Assert.Equal(CheckOutcome.Inconclusive, result.Outcome);
        Assert.Equal(CheckReasons.DUPLICATE_KEY_UNAVAILABLE, result.ReasonCode);
    }

    // A consulta é obrigatória: sem resposta, reprova bloqueando. Nunca cai para "aprova sem
    // consulta". Indisponibilidade é retentável e o motivo distingue isso.
    [Fact]
    public void Evaluate_LookupAvailability_WhenTheProviderDidNotAnswer_ShouldFailAsUnavailable()
    {
        var bill = BillMother.Capture();
        bill.AttachLookups(
            BillLookupResult.Unavailable("timeout", null, LookupMother.ConsultedAt), null, ValidationMother.OccurredAt);

        var result = Check(ContextWithLookups(bill, BillLookupResult.Unavailable("timeout", null, LookupMother.ConsultedAt)), CheckType.LookupAvailability);

        Assert.Equal(CheckOutcome.Failed, result.Outcome);
        Assert.Equal(CheckReasons.LOOKUP_UNAVAILABLE, result.ReasonCode);
        Assert.True(result.IsBlockingFailure);
    }

    // "Título não registrado" é fato sobre o documento, não sobre a rede — motivo diferente,
    // porque retentar não muda a resposta.
    [Fact]
    public void Evaluate_LookupAvailability_WhenTheTitleIsNotRegistered_ShouldFailAsUnresolved()
    {
        var bill = BillMother.Capture();
        var unresolved = BillLookupResult.Unresolved("unregistered_bank_slip", null, LookupMother.ConsultedAt);
        bill.AttachLookups(unresolved, null, ValidationMother.OccurredAt);

        var result = Check(ContextWithLookups(bill, unresolved), CheckType.LookupAvailability);

        Assert.Equal(CheckReasons.LOOKUP_UNRESOLVED, result.ReasonCode);
    }

    // Banco divergente entre o código de barras e a consulta é divergência estrutural grave.
    [Fact]
    public void Evaluate_LookupConsistency_WithDivergentBank_ShouldFail()
    {
        var bill = ValidationMother.BankSlipWithLookup(
            LookupMother.BankSlip(bankCode: new BankCode("237"), amount: BarcodeAmount()));

        var result = Check(ValidationMother.Context(bill), CheckType.LookupConsistency);

        Assert.Equal(CheckOutcome.Failed, result.Outcome);
        Assert.Equal(CheckReasons.LOOKUP_BANK_MISMATCH, result.ReasonCode);
    }

    // Valor em aberto (típico de arrecadação) pula a comparação de valor em vez de reprovar:
    // não há valor registrado contra o qual comparar o embutido no código de barras.
    [Fact]
    public void Evaluate_LookupConsistency_WhenTheIssuerAllowsChangingTheValue_ShouldNotFailOnAmount()
    {
        var snapshot = LookupSnapshot.Create(
            LookupParty.From(LookupMother.BENEFICIARY_NAME, null, LookupMother.BENEFICIARY_CNPJ),
            LookupMother.ConsultedAt,
            bankCode: new BankCode(ValidationMother.BarcodeBankCode),
            amount: LookupMother.Brl(999_999m),
            originalAmount: LookupMother.Brl(999_999m),
            allowChangeValue: true);

        var result = Check(
            ValidationMother.Context(ValidationMother.BankSlipWithLookup(snapshot)),
            CheckType.LookupConsistency);

        Assert.NotEqual(CheckOutcome.Failed, result.Outcome);
    }

    // Casou por documento e o nome do cadastro bate: verificação forte, sem ressalva.
    [Fact]
    public void Evaluate_PayeeMatch_WhenTaxIdAndNameMatch_ShouldPassClean()
    {
        var result = Check(
            ValidationMother.Context(ValidationMother.BankSlipWithLookup(), payee: ValidationMother.RegisteredPayee()),
            CheckType.PayeeMatch);

        Assert.Equal(CheckOutcome.Passed, result.Outcome);
        Assert.Null(result.ReasonCode);
    }

    // Beneficiário novo é rotina, não falha — a tela oferece cadastrar e o próximo boleto passa.
    [Fact]
    public void Evaluate_PayeeMatch_WithoutAnyRegisteredPayee_ShouldBeInconclusive()
    {
        var result = Check(ValidationMother.Context(ValidationMother.BankSlipWithLookup()), CheckType.PayeeMatch);

        Assert.Equal(CheckOutcome.Inconclusive, result.Outcome);
        Assert.Equal(CheckReasons.PAYEE_NOT_REGISTERED, result.ReasonCode);
    }

    // Beneficiário desativado que volta a emitir boleto reprova por payee_inactive — e não
    // passa por "não cadastrado", que é o que aconteceria se a carga omitisse os inativos.
    [Fact]
    public void Evaluate_PayeeMatch_WithAnInactivePayee_ShouldFail()
    {
        var result = Check(
            ValidationMother.Context(
                ValidationMother.BankSlipWithLookup(),
                payee: ValidationMother.RegisteredPayee(active: false)),
            CheckType.PayeeMatch);

        Assert.Equal(CheckOutcome.Failed, result.Outcome);
        Assert.Equal(CheckReasons.PAYEE_INACTIVE, result.ReasonCode);
    }

    // Documento confere e o nome não: razão social muda, CNPJ não. Vira aviso, nunca bloqueio.
    [Fact]
    public void Evaluate_PayeeMatch_WhenOnlyTheNameDiverges_ShouldWarnWithoutBlocking()
    {
        var payee = ValidationMother.RegisteredPayee(legalName: "PADARIA SAO JOSE COMERCIO DE ALIMENTOS LTDA");

        var result = Check(
            ValidationMother.Context(ValidationMother.BankSlipWithLookup(), payee: payee),
            CheckType.PayeeMatch);

        Assert.Equal(CheckOutcome.Warning, result.Outcome);
        Assert.Equal(CheckReasons.PAYEE_NAME_DIVERGENCE, result.ReasonCode);
        Assert.False(result.IsBlockingFailure);
    }

    // Arrecadação não devolve documento: casar por nome é verificação PARCIAL, e a ressalva
    // fica registrada para a tela não mostrar o mesmo "verificado" da cobrança bancária.
    [Fact]
    public void Evaluate_PayeeMatch_WhenMatchedByNameOnly_ShouldPassWithAPartialVerificationReason()
    {
        var bill = ValidationMother.BankSlipWithLookup(LookupMother.Utility(), BillMother.Capture([InstrumentSamples.UtilityBarcode()]));
        var payee = ValidationMother.RegisteredPayee(legalName: LookupMother.UTILITY_COMPANY_NAME, acceptedBank: null);

        var result = Check(
            ValidationMother.Context(bill, payee: payee, matchKind: PayeeMatchKind.ByName),
            CheckType.PayeeMatch);

        Assert.Equal(CheckOutcome.Passed, result.Outcome);
        Assert.Equal(CheckReasons.MATCHED_BY_NAME_ONLY, result.ReasonCode);
    }

    // Arrecadação não tem campo de banco em posição nenhuma — ausência estrutural, não omissão.
    [Fact]
    public void Evaluate_ReceivingBankMatch_ForUtilityBills_ShouldBeSkipped()
    {
        var bill = ValidationMother.BankSlipWithLookup(
            LookupMother.Utility(), BillMother.Capture([InstrumentSamples.UtilityBarcode()]));

        var result = Check(ValidationMother.Context(bill), CheckType.ReceivingBankMatch);

        Assert.Equal(CheckOutcome.Skipped, result.Outcome);
        Assert.Equal(CheckReasons.BANK_NOT_AVAILABLE_FOR_UTILITY, result.ReasonCode);
    }

    // Duas fontes autoritativas discordando sobre o destino do dinheiro não é evento legítimo:
    // vira BLOQUEANTE, mesmo o check sendo Advisory por natureza.
    [Fact]
    public void Evaluate_ReceivingBankMatch_WhenBarcodeAndLookupDisagree_ShouldBlock()
    {
        var bill = ValidationMother.BankSlipWithLookup(LookupMother.BankSlip(bankCode: new BankCode("237")));

        var result = Check(
            ValidationMother.Context(bill, payee: ValidationMother.RegisteredPayee()),
            CheckType.ReceivingBankMatch);

        Assert.Equal(CheckOutcome.Failed, result.Outcome);
        Assert.Equal(CheckReasons.BANK_SOURCE_CONFLICT, result.ReasonCode);
        Assert.Equal(CheckSeverity.Blocking, result.Severity);
    }

    // Banco fora da lista aceita é advisory: troca de banco por fornecedor é evento legítimo
    // e frequente, mas merece o olho do aprovador.
    [Fact]
    public void Evaluate_ReceivingBankMatch_WhenTheBankIsNotAccepted_ShouldFailAsAdvisory()
    {
        var payee = ValidationMother.RegisteredPayee(acceptedBank: "033");

        var result = Check(
            ValidationMother.Context(ValidationMother.BankSlipWithLookup(), payee: payee),
            CheckType.ReceivingBankMatch);

        Assert.Equal(CheckOutcome.Failed, result.Outcome);
        Assert.Equal(CheckReasons.BANK_NOT_ACCEPTED, result.ReasonCode);
        Assert.Equal(CheckSeverity.Advisory, result.Severity);
        Assert.False(result.IsBlockingFailure);
    }

    // Sem bancos cadastrados não há expectativa — inconclusivo, com oferta de aprender o banco.
    [Fact]
    public void Evaluate_ReceivingBankMatch_WithoutAnyAcceptedBank_ShouldBeInconclusive()
    {
        var payee = ValidationMother.RegisteredPayee(acceptedBank: null);

        var result = Check(
            ValidationMother.Context(ValidationMother.BankSlipWithLookup(), payee: payee),
            CheckType.ReceivingBankMatch);

        Assert.Equal(CheckOutcome.Inconclusive, result.Outcome);
        Assert.Equal(CheckReasons.BANK_EXPECTATION_NOT_SET, result.ReasonCode);
    }

    // Valor dentro da política passa.
    [Fact]
    public void Evaluate_AmountMatch_WithinThePolicy_ShouldPass()
    {
        var result = Check(
            ValidationMother.Context(
                ValidationMother.BankSlipWithLookup(),
                payee: ValidationMother.PayeeExpecting(ValidationMother.BarcodeAmount.Amount)),
            CheckType.AmountMatch);

        Assert.Equal(CheckOutcome.Passed, result.Outcome);
    }

    // Valor fora da política reprova, e a evidência carrega o valor cobrado.
    [Fact]
    public void Evaluate_AmountMatch_OutsideThePolicy_ShouldFailWithTheChargedAmount()
    {
        var result = Check(
            ValidationMother.Context(
                ValidationMother.BankSlipWithLookup(),
                payee: ValidationMother.PayeeExpecting(10.00m)),
            CheckType.AmountMatch);

        Assert.Equal(CheckOutcome.Failed, result.Outcome);
        Assert.Equal(CheckReasons.AMOUNT_OUTSIDE_POLICY, result.ReasonCode);
        Assert.Contains(
            ValidationMother.BarcodeAmount.Amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
            result.Evidence,
            StringComparison.Ordinal);
    }

    // Política Unbounded passa em tudo, então o resultado é inconclusivo: nada foi provado.
    [Fact]
    public void Evaluate_AmountMatch_WithAnUnboundedPolicy_ShouldBeInconclusiveNotPassed()
    {
        var result = Check(
            ValidationMother.Context(ValidationMother.BankSlipWithLookup(), payee: ValidationMother.RegisteredPayee()),
            CheckType.AmountMatch);

        Assert.Equal(CheckOutcome.Inconclusive, result.Outcome);
        Assert.Equal(CheckReasons.AMOUNT_POLICY_UNBOUNDED, result.ReasonCode);
    }

    // A assimetria do ADR-004: pagador extraído que CONTRADIZ o cadastro bloqueia, mesmo o
    // check sendo Advisory por natureza. É o que garante que um usuário não pague a conta de outro.
    [Fact]
    public void Evaluate_PayerMatch_WhenTheExtractedPayerContradictsTheTenant_ShouldBlock()
    {
        var bill = BillMother.CaptureVerbatim(
            [InstrumentSamples.Barcode()],
            BillMother.MailboxOrigin(),
            extractedPayer: PartyInfo.FromExtraction("OUTRA EMPRESA LTDA", "52998224725"));

        var result = Check(
            ValidationMother.Context(ValidationMother.BankSlipWithLookup(bill: bill), payerProfile: ValidationMother.TenantProfile()),
            CheckType.PayerMatch);

        Assert.Equal(CheckOutcome.Failed, result.Outcome);
        Assert.Equal(CheckReasons.PAYER_MISMATCH, result.ReasonCode);
        Assert.Equal(CheckSeverity.Blocking, result.Severity);
    }

    // O outro lado da assimetria: ausência de confirmação NÃO bloqueia. O CNPJ do pagador
    // aparece em só 38% dos boletos reais — inconclusivo é o caso majoritário por medição.
    [Fact]
    public void Evaluate_PayerMatch_WhenThePayerIsNotExtractable_ShouldBeInconclusiveWithoutBlocking()
    {
        var result = Check(
            ValidationMother.Context(ValidationMother.BankSlipWithLookup(), payerProfile: ValidationMother.TenantProfile()),
            CheckType.PayerMatch);

        Assert.Equal(CheckOutcome.Inconclusive, result.Outcome);
        Assert.Equal(CheckReasons.PAYER_NOT_EXTRACTABLE, result.ReasonCode);
        Assert.False(result.IsBlockingFailure);
    }

    // Pagador extraído que casa com o cadastro passa — mas isso não prova propriedade, só que
    // nada contradisse.
    [Fact]
    public void Evaluate_PayerMatch_WhenTheExtractedPayerBelongsToTheTenant_ShouldPass()
    {
        var bill = BillMother.CaptureVerbatim(
            [InstrumentSamples.Barcode()],
            BillMother.MailboxOrigin(),
            extractedPayer: PartyInfo.FromExtraction("RUFINO EMPREITEIRA LTDA", "11222333000181"));

        var result = Check(
            ValidationMother.Context(ValidationMother.BankSlipWithLookup(bill: bill), payerProfile: ValidationMother.TenantProfile()),
            CheckType.PayerMatch);

        Assert.Equal(CheckOutcome.Passed, result.Outcome);
    }

    // BLOQUEIO NOVO: ninguém emite boleto contra si mesmo. Beneficiário igual ao pagador é
    // consulta descrevendo outro título ou documento adulterado — nos dois casos, não se paga.
    [Fact]
    public void Evaluate_PayerMatch_WhenTheBeneficiaryIsTheTenantItself_ShouldBlock()
    {
        // O perfil do tenant passa a ser o MESMO documento que a consulta devolveu como beneficiário.
        var context = ValidationMother.Context(
            ValidationMother.BankSlipWithLookup(),
            payerProfile: ValidationMother.TenantProfile(LookupMother.BENEFICIARY_CNPJ));

        var result = Check(context, CheckType.PayerMatch);

        Assert.Equal(CheckOutcome.Failed, result.Outcome);
        Assert.Equal(CheckReasons.PAYEE_IS_THE_PAYER, result.ReasonCode);
        Assert.Equal(CheckSeverity.Blocking, result.Severity);
    }

    // BLOQUEIO NOVO: a busca dirigida procura o documento do cadastro dentro do texto, e um
    // código de barras é uma sequência longa que pode, em tese, conter um deles por coincidência.
    // Se o documento só existe LÁ DENTRO, ele não identifica ninguém — e a atribuição do boleto
    // se apoiou em nada.
    [Fact]
    public void Evaluate_PayerMatch_WhenTheTaxIdOnlyExistsInsideTheBarcode_ShouldBlock()
    {
        var barcode = InstrumentSamples.Barcode();

        // Este CPF existe DE VERDADE dentro do código de barras do fixture — dígitos verificadores
        // fechando, por coincidência aritmética. É exatamente o cenário que a regra defende.
        const string DentroDoCodigo = "01234567890";
        Assert.Contains(DentroDoCodigo, barcode.DigitableLine.Barcode, StringComparison.Ordinal);

        var bill = BillMother.CaptureVerbatim(
            [barcode],
            BillMother.MailboxOrigin(),
            extractedPayer: PartyInfo.FromExtraction(null, DentroDoCodigo));

        var result = Check(
            ValidationMother.Context(
                ValidationMother.BankSlipWithLookup(bill: bill),
                payerProfile: ValidationMother.IndividualProfile(DentroDoCodigo)),
            CheckType.PayerMatch);

        Assert.Equal(CheckOutcome.Failed, result.Outcome);
        Assert.Equal(CheckReasons.PAYER_ONLY_INSIDE_BARCODE, result.ReasonCode);
        Assert.Equal(CheckSeverity.Blocking, result.Severity);
    }

    // CONTRAPROVA: documento impresso como campo continua passando, mesmo o boleto tendo código
    // de barras. Sem esta, o bloqueio acima poderia estar reprovando todo mundo.
    [Fact]
    public void Evaluate_PayerMatch_WhenTheTaxIdIsPrintedAsAField_ShouldStillPass()
    {
        var bill = BillMother.CaptureVerbatim(
            [InstrumentSamples.Barcode()],
            BillMother.MailboxOrigin(),
            extractedPayer: PartyInfo.FromExtraction("RUFINO EMPREITEIRA LTDA", "11222333000181"));

        var result = Check(
            ValidationMother.Context(
                ValidationMother.BankSlipWithLookup(bill: bill),
                payerProfile: ValidationMother.TenantProfile()),
            CheckType.PayerMatch);

        Assert.Equal(CheckOutcome.Passed, result.Outcome);
    }

    // Sem cadastro fiscal do tenant não há contra o que comparar — Skipped, não falha.
    [Fact]
    public void Evaluate_PayerMatch_WithoutATenantProfile_ShouldBeSkipped()
    {
        var result = Check(ValidationMother.Context(ValidationMother.BankSlipWithLookup()), CheckType.PayerMatch);

        Assert.Equal(CheckOutcome.Skipped, result.Outcome);
        Assert.Equal(CheckReasons.PAYER_PROFILE_MISSING, result.ReasonCode);
    }

    // Origem explicitamente banida não passa, e aí o Advisory vira bloqueante.
    [Fact]
    public void Evaluate_OriginTrust_WhenTheSenderIsBlocked_ShouldBlock()
    {
        var result = Check(
            ValidationMother.Context(
                ValidationMother.BankSlipWithLookup(),
                origin: ValidationMother.TrustedSender(TrustDecision.Blocked)),
            CheckType.OriginTrust);

        Assert.Equal(CheckOutcome.Failed, result.Outcome);
        Assert.Equal(CheckReasons.ORIGIN_BLOCKED, result.ReasonCode);
        Assert.Equal(CheckSeverity.Blocking, result.Severity);
    }

    // Remetente nunca visto é inconclusivo, com a ação de "confiar nesta origem" na aprovação.
    [Fact]
    public void Evaluate_OriginTrust_WithAnUnknownSender_ShouldBeInconclusive()
    {
        var result = Check(ValidationMother.Context(ValidationMother.BankSlipWithLookup()), CheckType.OriginTrust);

        Assert.Equal(CheckOutcome.Inconclusive, result.Outcome);
        Assert.Equal(CheckReasons.ORIGIN_UNKNOWN, result.ReasonCode);
    }

    // Importação manual por usuário autenticado passa — mas não dispensa o check de pagador.
    [Fact]
    public void Evaluate_OriginTrust_ForManualUpload_ShouldPass()
    {
        var bill = ValidationMother.BankSlipWithLookup(bill: BillMother.Capture(origin: BillMother.ManualOrigin()));

        var result = Check(ValidationMother.Context(bill), CheckType.OriginTrust);

        Assert.Equal(CheckOutcome.Passed, result.Outcome);
        Assert.Equal(CheckReasons.ORIGIN_MANUAL_UPLOAD, result.ReasonCode);
    }

    // Boleto vencido reprova com o valor atualizado destacado — o provedor processa vencido
    // imediatamente, sem agendamento.
    [Fact]
    public void Evaluate_DueDateSanity_WhenOverdue_ShouldFail()
    {
        var snapshot = LookupSnapshot.Create(
            LookupParty.From(LookupMother.BENEFICIARY_NAME, null, LookupMother.BENEFICIARY_CNPJ),
            LookupMother.ConsultedAt,
            bankCode: new BankCode(ValidationMother.BarcodeBankCode),
            amount: LookupMother.Brl(153.20m),
            dueDate: new DateOnly(2026, 7, 30),
            isOverdue: true);

        var result = Check(
            ValidationMother.Context(ValidationMother.BankSlipWithLookup(snapshot)),
            CheckType.DueDateSanity);

        Assert.Equal(CheckOutcome.Failed, result.Outcome);
        Assert.Equal(CheckReasons.OVERDUE, result.ReasonCode);
    }

    // Vence hoje depois do corte do provedor: seria processado no dia útil seguinte.
    [Fact]
    public void Evaluate_DueDateSanity_WhenDueTodayAfterTheCutoff_ShouldFail()
    {
        var result = Check(
            ValidationMother.Context(
                ValidationMother.BankSlipWithLookup(
                    ValidationMother.ConsistentWithBarcode(dueDate: ValidationMother.Today)),
                timeOfDay: ValidationMother.AfterCutoff),
            CheckType.DueDateSanity);

        Assert.Equal(CheckOutcome.Failed, result.Outcome);
        Assert.Equal(CheckReasons.SAME_DAY_AFTER_CUTOFF, result.ReasonCode);
    }

    // Com folga até o vencimento, passa.
    [Fact]
    public void Evaluate_DueDateSanity_WithRoomBeforeTheDueDate_ShouldPass()
    {
        var result = Check(ValidationMother.Context(ValidationMother.BankSlipWithLookup()), CheckType.DueDateSanity);

        Assert.Equal(CheckOutcome.Passed, result.Outcome);
    }

    // Importação manual não passa pela escada de roteamento.
    [Fact]
    public void Evaluate_TenantRouting_ForManualImport_ShouldBeSkipped()
    {
        var bill = ValidationMother.BankSlipWithLookup(bill: BillMother.Capture(origin: BillMother.ManualOrigin()));

        var result = Check(ValidationMother.Context(bill), CheckType.TenantRouting);

        Assert.Equal(CheckOutcome.Skipped, result.Outcome);
        Assert.Equal(CheckReasons.ROUTING_MANUAL_IMPORT, result.ReasonCode);
    }

    // Atribuição constatada passa; atribuição inferida é inconclusiva — informar quanto foi
    // inferido é justamente o propósito do check.
    [Theory]
    [InlineData("Strong", "Passed")]
    [InlineData("Learned", "Passed")]
    [InlineData("Weak", "Inconclusive")]
    [InlineData("Claimed", "Inconclusive")]
    public void Evaluate_TenantRouting_ShouldReflectHowMuchTheAssignmentWasInferred(string confidence, string expected)
    {
        var bill = BillMother.CaptureVerbatim(
            [InstrumentSamples.Barcode()],
            BillMother.MailboxOrigin(),
            routing: Enumeration.FromDisplayName<RoutingConfidence>(confidence));

        var result = Check(ValidationMother.Context(ValidationMother.BankSlipWithLookup(bill: bill)), CheckType.TenantRouting);

        Assert.Equal(expected, result.Outcome.Name);
    }

    // Documento de um trilho só não tem duas histórias para comparar.
    [Fact]
    public void Evaluate_PixBarcodeConsistency_WithASingleRail_ShouldBeSkipped()
    {
        var result = Check(
            ValidationMother.Context(ValidationMother.BankSlipWithLookup()),
            CheckType.PixBarcodeConsistency);

        Assert.Equal(CheckOutcome.Skipped, result.Outcome);
        Assert.Equal(CheckReasons.SINGLE_RAIL_DOCUMENT, result.ReasonCode);
    }

    // O vetor de fraude mais direto em circulação: QR Pix adulterado colado sobre boleto
    // verdadeiro. Beneficiário divergente entre os trilhos BLOQUEIA — nunca "escolhe um e segue".
    [Fact]
    public void Evaluate_PixBarcodeConsistency_WhenTheQrPointsToAnotherPayee_ShouldBlock()
    {
        var bill = HybridBill(
            barcodeTaxId: LookupMother.BENEFICIARY_CNPJ,
            pixTaxId: "52998224725");

        var result = Check(ValidationMother.Context(bill), CheckType.PixBarcodeConsistency);

        Assert.Equal(CheckOutcome.Failed, result.Outcome);
        Assert.Equal(CheckReasons.PIX_BARCODE_PAYEE_MISMATCH, result.ReasonCode);
        Assert.True(result.IsBlockingFailure);
    }

    // Mesmo beneficiário nos dois trilhos e mesmo valor: as duas histórias conferem.
    [Fact]
    public void Evaluate_PixBarcodeConsistency_WhenBothRailsAgree_ShouldPass()
    {
        var bill = HybridBill(LookupMother.BENEFICIARY_CNPJ, LookupMother.BENEFICIARY_CNPJ);

        var result = Check(ValidationMother.Context(bill), CheckType.PixBarcodeConsistency);

        Assert.Equal(CheckOutcome.Passed, result.Outcome);
    }

    // QR que o provedor já sabe que não paga é porteira: reprova antes de consumir aprovação.
    [Fact]
    public void Evaluate_PixBarcodeConsistency_WhenTheProviderRefusesTheQr_ShouldFail()
    {
        var bill = HybridBill(
            LookupMother.BENEFICIARY_CNPJ,
            LookupMother.BENEFICIARY_CNPJ,
            canBePaid: false);

        var result = Check(ValidationMother.Context(bill), CheckType.PixBarcodeConsistency);

        Assert.Equal(CheckOutcome.Failed, result.Outcome);
        Assert.Equal(CheckReasons.PIX_QR_NOT_PAYABLE, result.ReasonCode);
    }

    private static Bill HybridBill(string barcodeTaxId, string pixTaxId, bool canBePaid = true)
    {
        var bill = BillMother.WithBothRails();

        var barcodeSnapshot = LookupSnapshot.Create(
            LookupParty.From(LookupMother.BENEFICIARY_NAME, null, barcodeTaxId),
            LookupMother.ConsultedAt,
            bankCode: new BankCode(ValidationMother.BarcodeBankCode),
            amount: LookupMother.Brl(153.20m),
            originalAmount: BarcodeAmount(),
            dueDate: LookupMother.DueDate);

        var pixSnapshot = PixLookupSnapshot.Create(
            LookupParty.From(LookupMother.BENEFICIARY_NAME, null, pixTaxId),
            LookupMother.ConsultedAt,
            canBePaid: canBePaid,
            cannotBePaidReason: canBePaid ? null : "QR_CODE_EXPIRED",
            isDynamic: true,
            totalAmount: LookupMother.Brl(153.20m),
            dueDate: LookupMother.DueDate);

        bill.AttachLookups(
            BillLookupResult.Resolved(barcodeSnapshot, LookupMother.ConsultedAt),
            PixLookupResult.Resolved(pixSnapshot, LookupMother.ConsultedAt),
            ValidationMother.OccurredAt);

        return bill;
    }

    /// <summary>O valor que o código de barras sintético do corpus carrega.</summary>
    private static Money BarcodeAmount() => InstrumentSamples.Barcode().DigitableLine.Amount;

    private static BillValidationContext ContextWithLookups(Bill bill, BillLookupResult bankSlip)
        => new()
        {
            Bill = bill,
            BankSlipLookup = bankSlip,
            PayeeResolution = PayeeResolutionService.Resolve(null, []),
            BankDirectory = new FakeBankDirectory(),
            Today = ValidationMother.Today,
            TimeOfDay = ValidationMother.Morning,
        };

    private static CheckResult Check(BillValidationContext context, CheckType type)
        => Evaluate(context).Single(r => r.Type == type);

    private static IReadOnlyCollection<CheckResult> Evaluate(BillValidationContext context)
        => BillValidationService.Evaluate(context);
}
