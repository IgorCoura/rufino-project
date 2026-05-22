namespace AccountsPayable.UnitTests.Payables.Mothers;

using AccountsPayable.Domain.ChartOfAccounts;
using AccountsPayable.Domain.ChartOfAccounts.Entities;
using AccountsPayable.Domain.CostCenters;
using AccountsPayable.Domain.ExpenseClassificationRules;
using AccountsPayable.Domain.InstallmentPlans;
using AccountsPayable.Domain.Payables;
using AccountsPayable.Domain.Payables.Enumerations;
using AccountsPayable.Domain.Payables.Events;
using AccountsPayable.Domain.Payables.ValueObjects;
using AccountsPayable.Domain.SeedWork;
using AccountsPayable.Domain.Suppliers;
using AccountsPayable.Domain.Suppliers.Enumerations;
using AccountsPayable.Domain.Suppliers.ValueObjects;

public static class PayableMother
{
    public static readonly DateTime DEFAULT_OCCURRED_AT = new(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc);
    public static readonly DateOnly DEFAULT_DUE_DATE = new(2024, 2, 15);
    public static readonly DateOnly DEFAULT_SCHEDULED_FOR = new(2024, 2, 14);
    public static readonly TenantId DEFAULT_TENANT = TenantId.From(new Guid("11111111-1111-1111-1111-111111111111"));
    public static readonly SupplierId DEFAULT_SUPPLIER = SupplierId.From(new Guid("22222222-2222-2222-2222-222222222222"));
    public static readonly ChartOfAccountsId DEFAULT_CHART_OF_ACCOUNTS = ChartOfAccountsId.From(new Guid("aaaaaaaa-3333-3333-3333-aaaaaaaaaaaa"));
    public static readonly AccountId DEFAULT_ACCOUNT = AccountId.From(new Guid("33333333-3333-3333-3333-333333333333"));
    public static readonly AccountRef DEFAULT_ACCOUNT_REF = new(DEFAULT_CHART_OF_ACCOUNTS, DEFAULT_ACCOUNT);
    public static readonly CostCenterId DEFAULT_COST_CENTER = CostCenterId.From(new Guid("44444444-4444-4444-4444-444444444444"));
    public static readonly UserId DEFAULT_USER = UserId.From(new Guid("55555555-5555-5555-5555-555555555555"));
    public static readonly PaymentOrderId DEFAULT_PAYMENT_ORDER =
        PaymentOrderId.From(new Guid("99999999-9999-9999-9999-999999999999"));
    public static readonly CapturedBillId DEFAULT_CAPTURED_BILL =
        CapturedBillId.From(new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"));
    public static readonly InstallmentPlanId DEFAULT_INSTALLMENT_PLAN =
        InstallmentPlanId.From(new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"));
    public static readonly ExpenseClassificationRuleId DEFAULT_RULE =
        ExpenseClassificationRuleId.From(new Guid("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"));

    public static readonly PaymentProof DEFAULT_PROOF =
        new("https://docs.acme.com.br/payable/proof.pdf", PaymentProofType.Receipt);

    // ---- Defaults de PaymentInstrument (Sprint 12.B; refinado em 12.G — PaymentMethod é derivado) ----
    public static readonly LegalName DEFAULT_SUPPLIER_LEGAL_NAME = new("Acme Brasil LTDA");
    public static readonly TaxId DEFAULT_SUPPLIER_TAX_ID = new("59.199.597/0001-98");
    public static readonly PaymentInstrument DEFAULT_PAYMENT_INSTRUMENT = new SupplierBankTransferInstrument(
        DEFAULT_SUPPLIER_LEGAL_NAME, DEFAULT_SUPPLIER_TAX_ID,
        "001", "0001", "123456-7", BankAccountType.Checking);

    /// <summary>
    /// Template para construir <see cref="PayableCreated"/> em testes de reidratação A+ES.
    /// Use <c>with { ... }</c> para customizar campos sem precisar passar os 13 do instrumento.
    /// </summary>
    public static readonly PayableCreated TEMPLATE_PAYABLE_CREATED = new(
        EventId: Guid.Empty,
        OccurredAt: DEFAULT_OCCURRED_AT,
        PayableId: PayableId.New(),
        TenantId: DEFAULT_TENANT,
        SupplierId: DEFAULT_SUPPLIER,
        AmountValue: 1_500m,
        AmountCurrency: "BRL",
        DueDate: DEFAULT_DUE_DATE,
        Description: "default",
        InstrumentKind: "SUPPLIER_BANK",
        SupplierLegalName: "Acme Brasil LTDA",
        SupplierTaxIdValue: "59199597000198",
        SupplierTaxIdType: "CNPJ",
        PixKeyValue: null,
        PixKeyType: null,
        BankCode: "001",
        Branch: "0001",
        AccountNumber: "123456-7",
        AccountType: "CHECKING",
        EmvPayload: null,
        BarcodeDigits: null);

    /// <summary>Template equivalente para <see cref="PayableCreatedFromCapture"/>.</summary>
    public static readonly PayableCreatedFromCapture TEMPLATE_PAYABLE_CREATED_FROM_CAPTURE = new(
        EventId: Guid.Empty,
        OccurredAt: DEFAULT_OCCURRED_AT,
        PayableId: PayableId.New(),
        TenantId: DEFAULT_TENANT,
        CapturedBillId: DEFAULT_CAPTURED_BILL,
        SupplierId: DEFAULT_SUPPLIER,
        AmountValue: 1_500m,
        AmountCurrency: "BRL",
        DueDate: DEFAULT_DUE_DATE,
        Description: "default",
        InstrumentKind: "SUPPLIER_BANK",
        SupplierLegalName: "Acme Brasil LTDA",
        SupplierTaxIdValue: "59199597000198",
        SupplierTaxIdType: "CNPJ",
        PixKeyValue: null,
        PixKeyType: null,
        BankCode: "001",
        Branch: "0001",
        AccountNumber: "123456-7",
        AccountType: "CHECKING",
        EmvPayload: null,
        BarcodeDigits: null);

    public static Payable Draft(
        PayableId? id = null,
        TenantId? tenantId = null,
        SupplierId? supplierId = null,
        decimal amount = 1_500m,
        Currency? currency = null,
        DateOnly? dueDate = null,
        string description = "Aluguel sede março",
        PaymentInstrument? paymentInstrument = null,
        DateTime? occurredAt = null)
    {
        return Payable.Initialize(
            id: id ?? PayableId.New(),
            tenantId: tenantId ?? DEFAULT_TENANT,
            supplierId: supplierId ?? DEFAULT_SUPPLIER,
            amount: new Money(amount, currency ?? Currency.Brl),
            dueDate: new DueDate(dueDate ?? DEFAULT_DUE_DATE),
            description: new Description(description),
            paymentInstrument: paymentInstrument ?? DEFAULT_PAYMENT_INSTRUMENT,
            occurredAt: occurredAt ?? DEFAULT_OCCURRED_AT);
    }

    /// <summary>Draft as a single installment of an InstallmentPlan (Sprint 8) — Status = Draft, InstallmentPlanId+Number populated.</summary>
    public static Payable DraftAsInstallment(
        PayableId? id = null,
        TenantId? tenantId = null,
        InstallmentPlanId? installmentPlanId = null,
        int installmentNumber = 1,
        SupplierId? supplierId = null,
        decimal amount = 1_000m,
        Currency? currency = null,
        DateOnly? dueDate = null,
        string description = "Aluguel anual (1/12)",
        PaymentInstrument? paymentInstrument = null,
        DateTime? occurredAt = null)
    {
        return Payable.InitializeAsInstallment(
            id: id ?? PayableId.New(),
            tenantId: tenantId ?? DEFAULT_TENANT,
            installmentPlanId: installmentPlanId ?? DEFAULT_INSTALLMENT_PLAN,
            installmentNumber: installmentNumber,
            supplierId: supplierId ?? DEFAULT_SUPPLIER,
            amount: new Money(amount, currency ?? Currency.Brl),
            dueDate: new DueDate(dueDate ?? DEFAULT_DUE_DATE),
            description: new Description(description),
            paymentInstrument: paymentInstrument ?? DEFAULT_PAYMENT_INSTRUMENT,
            occurredAt: occurredAt ?? DEFAULT_OCCURRED_AT);
    }

    /// <summary>Draft created from a CapturedBill (Sprint 7) — Status = Draft, CapturedBillId populated, unclassified.</summary>
    public static Payable DraftFromCapture(
        PayableId? id = null,
        TenantId? tenantId = null,
        CapturedBillId? capturedBillId = null,
        SupplierId? supplierId = null,
        decimal amount = 1_500m,
        Currency? currency = null,
        DateOnly? dueDate = null,
        string description = "Boleto Sabesp março",
        PaymentInstrument? paymentInstrument = null,
        DateTime? occurredAt = null)
    {
        return Payable.InitializeFromCapture(
            id: id ?? PayableId.New(),
            tenantId: tenantId ?? DEFAULT_TENANT,
            capturedBillId: capturedBillId ?? DEFAULT_CAPTURED_BILL,
            supplierId: supplierId ?? DEFAULT_SUPPLIER,
            amount: new Money(amount, currency ?? Currency.Brl),
            dueDate: new DueDate(dueDate ?? DEFAULT_DUE_DATE),
            description: new Description(description),
            paymentInstrument: paymentInstrument ?? DEFAULT_PAYMENT_INSTRUMENT,
            occurredAt: occurredAt ?? DEFAULT_OCCURRED_AT);
    }

    /// <summary>Draft already classified — ready to Schedule under the default strict invariant.</summary>
    public static Payable Classified(
        AccountRef? accountRef = null,
        CostCenterId? costCenterId = null,
        UserId? classifiedBy = null,
        decimal amount = 1_500m)
    {
        var payable = Draft(amount: amount);
        payable.Classify(
            accountRef ?? DEFAULT_ACCOUNT_REF,
            costCenterId ?? DEFAULT_COST_CENTER,
            classifiedBy ?? DEFAULT_USER,
            DEFAULT_OCCURRED_AT.AddMinutes(2));
        return payable;
    }

    /// <summary>Draft classified by the auto-classifier (Sprint 9) — Status = Draft, LastClassificationRuleId set, ClassifiedBy null.</summary>
    public static Payable AutoClassified(
        AccountRef? accountRef = null,
        CostCenterId? costCenterId = null,
        ExpenseClassificationRuleId? ruleId = null,
        decimal amount = 1_500m)
    {
        var payable = Draft(amount: amount);
        payable.ClassifyAutomatically(
            accountRef ?? DEFAULT_ACCOUNT_REF,
            costCenterId ?? DEFAULT_COST_CENTER,
            ruleId ?? DEFAULT_RULE,
            DEFAULT_OCCURRED_AT.AddMinutes(2));
        return payable;
    }

    public static Payable Scheduled(DateOnly? scheduledFor = null)
    {
        var payable = Classified();
        payable.Schedule(
            scheduledFor: scheduledFor ?? DEFAULT_SCHEDULED_FOR,
            occurredAt: DEFAULT_OCCURRED_AT.AddMinutes(5));
        return payable;
    }

    public static Payable Paid()
    {
        var payable = Scheduled();
        payable.MarkAsPaidManually(
            proof: DEFAULT_PROOF,
            paidAt: DEFAULT_OCCURRED_AT.AddDays(30),
            occurredAt: DEFAULT_OCCURRED_AT.AddDays(30).AddMinutes(1));
        return payable;
    }

    public static Payable Cancelled(string reason = "Boleto duplicado")
    {
        var payable = Draft();
        payable.Cancel(reason, DEFAULT_OCCURRED_AT.AddMinutes(5));
        return payable;
    }

    /// <summary>Classified Draft that already requested approval — Status = AwaitingApproval.</summary>
    public static Payable AwaitingApproval()
    {
        var payable = Classified();
        payable.RequestApproval(DEFAULT_OCCURRED_AT.AddMinutes(3));
        return payable;
    }

    public static Payable Approved(UserId? approver = null)
    {
        var payable = AwaitingApproval();
        payable.Approve(approver ?? DEFAULT_USER, DEFAULT_OCCURRED_AT.AddMinutes(4));
        return payable;
    }

    public static Payable Rejected(UserId? rejector = null, string reason = "Valor inconsistente")
    {
        var payable = AwaitingApproval();
        payable.Reject(rejector ?? DEFAULT_USER, reason, DEFAULT_OCCURRED_AT.AddMinutes(4));
        return payable;
    }

    /// <summary>Scheduled payable that already requested payment — Status = PaymentRequested (Sprint 6).
    /// O instrumento de pagamento vem do estado (decidido na criação — Sprint 12.B); para customizar
    /// use <see cref="Draft"/> ou similares.</summary>
    public static Payable PaymentRequested()
    {
        var payable = Scheduled();
        payable.RequestPayment(DEFAULT_OCCURRED_AT.AddMinutes(10));
        return payable;
    }

    /// <summary>Payable whose previous payment attempt failed — Status = PaymentFailed (Sprint 6).</summary>
    public static Payable PaymentFailed(
        PaymentOrderId? paymentOrderId = null,
        string reason = "Conta destino inválida")
    {
        var payable = PaymentRequested();
        payable.MarkPaymentFailed(
            paymentOrderId ?? DEFAULT_PAYMENT_ORDER,
            reason,
            DEFAULT_OCCURRED_AT.AddMinutes(15));
        return payable;
    }
}
