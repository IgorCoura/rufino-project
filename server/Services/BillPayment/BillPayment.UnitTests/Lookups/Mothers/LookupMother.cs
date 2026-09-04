namespace BillPayment.UnitTests.Lookups.Mothers;

using BillPayment.Domain.Lookups;
using BillPayment.Domain.SharedKernel;

/// <summary>
/// Retratos de consulta para os testes. Os cenários têm nome de negócio de propósito: o que
/// distingue um do outro é a <em>cobertura</em> medida na sprint 1.0, não os valores.
/// </summary>
internal static class LookupMother
{
    /// <summary>
    /// O CNPJ do beneficiário. <strong>Tem de ser diferente do documento do tenant</strong>
    /// (<c>ValidationMother.TenantProfile</c>): eram o mesmo número até 2026-08-26, o que modelava
    /// um boleto em que o credor e o devedor são a mesma pessoa — impossível na vida real, e agora
    /// bloqueado pelo check <c>PayerMatch</c> com o motivo <c>payee_is_the_payer</c>.
    /// </summary>
    public const string BENEFICIARY_CNPJ = "45678901000175";
    public const string BENEFICIARY_NAME = "PADARIA SAO JOSE LTDA";
    public const string UTILITY_COMPANY_NAME = "SABESP";

    public static readonly DateTimeOffset ConsultedAt = new(2026, 8, 5, 12, 0, 0, TimeSpan.Zero);
    public static readonly DateOnly DueDate = new(2026, 8, 20);

    public static Money Brl(decimal amount) => new(amount, Currency.BRL);

    /// <summary>Cobrança bancária como ela deve voltar em produção: com documento e banco.</summary>
    public static LookupSnapshot BankSlip(
        Money? amount = null,
        BankCode? bankCode = null,
        bool allowChangeValue = false,
        DateOnly? dueDate = null)
        => LookupSnapshot.Create(
            LookupParty.From(BENEFICIARY_NAME, tradingName: null, BENEFICIARY_CNPJ),
            ConsultedAt,
            bankCode: bankCode ?? new BankCode("341"),
            amount: amount ?? Brl(150.00m),
            originalAmount: Brl(150.00m),
            allowChangeValue: allowChangeValue,
            dueDate: dueDate ?? DueDate,
            fee: Brl(1.99m),
            minimumScheduleDate: new DateOnly(2026, 8, 6));

    /// <summary>
    /// Arrecadação como ela realmente volta: nome comercial e valor, sem documento, sem banco
    /// e — em 70% dos casos medidos — sem vencimento.
    /// </summary>
    public static LookupSnapshot Utility()
        => LookupSnapshot.Create(
            LookupParty.From(name: null, UTILITY_COMPANY_NAME, taxId: null),
            ConsultedAt,
            amount: Brl(89.34m),
            originalAmount: Brl(89.34m));

    /// <summary>QR Pix dinâmico com recebedor identificado por documento.</summary>
    public static PixLookupSnapshot PixDynamic(
        Money? totalAmount = null,
        bool canBePaid = true,
        MaskedParty? payer = null)
        => PixLookupSnapshot.Create(
            LookupParty.From(BENEFICIARY_NAME, tradingName: null, BENEFICIARY_CNPJ),
            ConsultedAt,
            canBePaid: canBePaid,
            isDynamic: true,
            receiverIspb: "60701190",
            receiverIspbName: "ITAÚ UNIBANCO S.A.",
            receiverKind: TaxIdKind.CNPJ,
            amount: Brl(150.00m),
            totalAmount: totalAmount ?? Brl(153.20m),
            interest: Brl(3.20m),
            dueDate: DueDate,
            payer: payer);

    /// <summary>QR estático: reutilizável, sem valor e sem vencimento.</summary>
    public static PixLookupSnapshot PixStatic()
        => PixLookupSnapshot.Create(
            LookupParty.From(BENEFICIARY_NAME, tradingName: null, BENEFICIARY_CNPJ),
            ConsultedAt,
            isDynamic: false,
            receiverIspb: "60701190");
}
