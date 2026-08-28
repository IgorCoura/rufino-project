namespace BillPayment.UnitTests.Services.Mothers;

using BillPayment.Domain.Bills;
using BillPayment.Domain.Lookups;
using BillPayment.Domain.Payees;
using BillPayment.Domain.PayerProfiles;
using BillPayment.Domain.Ports;
using BillPayment.Domain.Services;
using BillPayment.Domain.SharedKernel;
using BillPayment.Domain.TrustedOrigins;
using BillPayment.UnitTests.Bills.Mothers;
using BillPayment.UnitTests.Instruments;
using BillPayment.UnitTests.Lookups.Mothers;

/// <summary>
/// Monta o contexto de validação. Os cenários têm nome de negócio: o caminho limpo é o de um
/// boleto que confere com o cadastro, e cada teste desvia dele em um ponto só — é o que faz o
/// desvio ser o que o teste está provando.
/// </summary>
internal static class ValidationMother
{
    /// <summary>O banco do boleto sintético do corpus (Itaú), para o cadastro casar por default.</summary>
    public const string BarcodeBankCode = "341";

    /// <summary>
    /// "Hoje" do cenário fica <strong>antes</strong> do vencimento que o código de barras
    /// sintético carrega (2026-06-25). Um "hoje" posterior deixaria o caminho limpo vencido, e
    /// todos os testes dos outros checks rodariam sobre um documento em situação de exceção.
    /// </summary>
    public static readonly DateOnly Today = new(2026, 6, 20);

    public static readonly TimeOnly Morning = new(9, 0);
    public static readonly TimeOnly AfterCutoff = new(15, 30);
    public static readonly DateTime OccurredAt = new(2026, 6, 20, 9, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Instante da consulta, alinhado ao "hoje" do cenário. Precisa ser o mesmo dia: a guarda
    /// de validade do retrato mede a distância entre os dois, e um retrato datado no futuro
    /// nunca ficaria velho — a guarda passaria despercebida em teste.
    /// </summary>
    public static readonly DateTimeOffset ConsultedAt = new(2026, 6, 20, 9, 0, 0, TimeSpan.Zero);

    public static Payee RegisteredPayee(
        string? taxId = null,
        string? legalName = null,
        bool active = true,
        string? acceptedBank = BarcodeBankCode)
    {
        var payee = Payee.Register(
            BillMother.DefaultTenant,
            legalName ?? LookupMother.BENEFICIARY_NAME,
            taxId ?? LookupMother.BENEFICIARY_CNPJ,
            AmountPolicyKind.Unbounded,
            null, null, null, null,
            BillMother.DefaultOccurredAt);

        if (acceptedBank is not null)
            payee.AllowBank(acceptedBank, BillMother.DefaultOccurredAt);

        if (!active)
            payee.SetActivation(false, BillMother.DefaultOccurredAt);

        return payee;
    }

    public static Payee PayeeExpecting(decimal amount, decimal tolerancePercent = 0m)
    {
        var payee = Payee.Register(
            BillMother.DefaultTenant,
            LookupMother.BENEFICIARY_NAME,
            LookupMother.BENEFICIARY_CNPJ,
            AmountPolicyKind.Fixed,
            amount, tolerancePercent, null, null,
            BillMother.DefaultOccurredAt);

        payee.AllowBank(BarcodeBankCode, BillMother.DefaultOccurredAt);
        return payee;
    }

    public static PayerProfile TenantProfile(string? taxId = null)
        => PayerProfile.Register(
            BillMother.DefaultTenant,
            PayerKind.Company,
            "RUFINO EMPREITEIRA LTDA",
            taxId ?? "11222333000181",
            BillMother.DefaultOccurredAt);

    /// <summary>Perfil de pessoa física — o CPF exige <c>PayerKind.Individual</c>.</summary>
    public static PayerProfile IndividualProfile(string taxId)
        => PayerProfile.Register(
            BillMother.DefaultTenant,
            PayerKind.Individual,
            "IGOR DE BRITO COURA",
            taxId,
            BillMother.DefaultOccurredAt);

    public static TrustedOrigin TrustedSender(TrustDecision? decision = null)
        => TrustedOrigin.Register(
            BillMother.DefaultTenant,
            OriginKind.EmailAddress,
            BillMother.DefaultSender,
            decision ?? TrustDecision.Trusted,
            UserId.From(new Guid("0195a1f0-0000-7000-8000-00000000000a")),
            note: null,
            BillMother.DefaultOccurredAt);

    /// <summary>Valor e vencimento que o código de barras sintético do corpus realmente carrega.</summary>
    public static Money BarcodeAmount => InstrumentSamples.Barcode().DigitableLine.Amount;

    public static DateTime? BarcodeDueDate => InstrumentSamples.Barcode().DigitableLine.DueDate;

    /// <summary>
    /// Boleto de cobrança já consultado, com beneficiário conferindo — o caminho limpo.
    /// </summary>
    /// <remarks>
    /// O retrato default é montado <strong>a partir do próprio código de barras</strong>. Um
    /// snapshot com valor ou vencimento inventado faria o check de consistência reprovar o
    /// caminho limpo — e, pior, faria os testes dos outros checks rodarem sobre um documento
    /// que o sistema considera incoerente.
    /// </remarks>
    public static Bill BankSlipWithLookup(LookupSnapshot? snapshot = null, Bill? bill = null)
    {
        var target = bill ?? BillMother.Capture();

        target.AttachLookups(
            BillLookupResult.Resolved(snapshot ?? ConsistentWithBarcode(), ConsultedAt),
            null,
            OccurredAt);

        return target;
    }

    /// <summary>Retrato que confere com o código de barras sintético em banco, valor e vencimento.</summary>
    public static LookupSnapshot ConsistentWithBarcode(
        DateOnly? dueDate = null,
        DateOnly? minimumScheduleDate = null,
        bool isOverdue = false)
        => LookupSnapshot.Create(
            LookupParty.From(LookupMother.BENEFICIARY_NAME, null, LookupMother.BENEFICIARY_CNPJ),
            ConsultedAt,
            bankCode: new BankCode(BarcodeBankCode),
            amount: BarcodeAmount,
            originalAmount: BarcodeAmount,
            dueDate: dueDate ?? (BarcodeDueDate is { } d ? DateOnly.FromDateTime(d) : null),
            isOverdue: isOverdue,
            fee: LookupMother.Brl(1.99m),
            minimumScheduleDate: minimumScheduleDate);

    public static BillValidationContext Context(
        Bill bill,
        Payee? payee = null,
        PayeeMatchKind? matchKind = null,
        TrustedOrigin? origin = null,
        PayerProfile? payerProfile = null,
        DuplicateFinding? duplicate = null,
        BillId? duplicateOf = null,
        DateOnly? today = null,
        TimeOnly? timeOfDay = null,
        IBankDirectory? bankDirectory = null)
        => new()
        {
            Bill = bill,
            PayeeResolution = Resolution(payee, matchKind),
            Origin = origin,
            PayerProfile = payerProfile,
            BankDirectory = bankDirectory ?? new FakeBankDirectory(),
            Duplicate = duplicate ?? DuplicateFinding.None,
            DuplicateOf = duplicateOf,
            Today = today ?? Today,
            TimeOfDay = timeOfDay ?? Morning,
        };

    private static PayeeResolution Resolution(Payee? payee, PayeeMatchKind? kind)
    {
        if (payee is null)
            return PayeeResolutionService.Resolve(null, []);

        // Passa pelo serviço real em vez de fabricar a resolução: o teste do check não deve
        // poder afirmar um casamento que a resolução verdadeira não produziria.
        var beneficiary = (kind ?? PayeeMatchKind.ByTaxId) == PayeeMatchKind.ByName
            ? LookupParty.From(payee.LegalName, null, null)
            : LookupParty.From(payee.LegalName, null, payee.TaxId.Value);

        return PayeeResolutionService.Resolve(beneficiary, [payee]);
    }
}

/// <summary>
/// Diretório de bancos de teste: conhece os do corpus real e responde "desconhecido" para o
/// resto, que é o que o check de banco fabricado precisa exercitar.
/// </summary>
internal sealed class FakeBankDirectory : IBankDirectory
{
    private static readonly Dictionary<string, (string Name, bool Compe)> Known = new(StringComparer.Ordinal)
    {
        ["341"] = ("ITAÚ UNIBANCO S.A.", true),
        ["237"] = ("BCO BRADESCO S.A.", true),
        ["033"] = ("BCO SANTANDER (BRASIL) S.A.", true),
        ["007"] = ("BNDES", false),
    };

    private static readonly Dictionary<string, string> ByIspb = new(StringComparer.Ordinal)
    {
        ["60701190"] = "341",
        ["60746948"] = "237",
    };

    public bool IsKnown(BankCode bankCode) => bankCode is not null && Known.ContainsKey(bankCode.Value);

    public bool ParticipatesInCompe(BankCode bankCode)
        => bankCode is not null && Known.TryGetValue(bankCode.Value, out var entry) && entry.Compe;

    public string? NameOf(BankCode bankCode)
        => bankCode is not null && Known.TryGetValue(bankCode.Value, out var entry) ? entry.Name : null;

    public BankCode? FromIspb(string ispb)
        => ByIspb.TryGetValue(ispb, out var compe) ? new BankCode(compe) : null;
}
