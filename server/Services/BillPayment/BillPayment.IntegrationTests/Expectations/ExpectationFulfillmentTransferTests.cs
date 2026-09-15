namespace BillPayment.IntegrationTests.Expectations;

using BillPayment.Application.Expectations.Commands;
using BillPayment.Application.Mediator;
using BillPayment.Domain.Bills;
using BillPayment.Domain.Bills.Checks;
using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.Expectations;
using BillPayment.Domain.Instruments;
using BillPayment.Domain.Payees;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using BillPayment.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// O ciclo da conta esperada acompanha o compromisso, e não o boleto que primeiro o representou.
/// </summary>
/// <remarks>
/// Relatado em 2026-09-15: a fatura da Vivo foi recapturada, a recaptura cancelou o boleto antigo e
/// criou outro com o mesmo código de barras, e o ciclo continuou cumprido pelo cancelado — o novo
/// nunca cumpria nada e a verificação 14 saía inconclusiva.
/// </remarks>
[Collection(nameof(IntegrationTestCollection))]
public sealed class ExpectationFulfillmentTransferTests : BaseIntegrationTest
{
    private static readonly TenantId TenantA = TenantId.From(new Guid("0195a1f0-0000-7000-8000-000000000001"));
    private static readonly TenantId TenantB = TenantId.From(new Guid("0195a1f0-0000-7000-8000-000000000002"));
    private static readonly PayeeId Vivo = PayeeId.From(new Guid("0195a1f0-0000-7000-8000-0000000000e8"));
    private static readonly UserId Operator = UserId.From(new Guid("0195a1f0-0000-7000-8000-0000000000a1"));
    private static readonly Guid Source = new("0195a1f0-0000-7000-8000-0000000000b1");
    private static readonly DateTime OccurredAt = new(2026, 8, 11, 9, 0, 0, DateTimeKind.Utc);

    private const string BankSlipLine = "34191234546789012345767890123457314880000061507";
    private const string OtherBankSlipLine = "03399876534321098765743210987657414930000140980";

    public ExpectationFulfillmentTransferTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    // TESTE DE REGRESSÃO. O boleto recapturado (mesmo código de barras do cancelado) passa a cumprir
    // o ciclo que o antigo cumpria — e a expectativa não conta a conta duas vezes.
    [Fact]
    public async Task Fulfill_WhenTheCycleBelongsToTheCancelledBillThisOneReplaces_ShouldTransferIt()
    {
        var oldBill = await SeedBillAsync(BankSlipLine, cancel: true);
        var (expectationId, cycleId) = await SeedFulfilledCycleAsync(oldBill);
        var observations = (await LoadExpectationAsync(expectationId)).ObservationCount;
        var newBill = await SeedBillAsync(BankSlipLine, cancel: false);

        await FulfillAsync(newBill);

        var expectation = await LoadExpectationAsync(expectationId);
        var cycle = expectation.Cycles.Single(c => c.Id == cycleId);
        Assert.Same(CycleStatus.Fulfilled, cycle.Status);
        Assert.Equal(newBill, cycle.FulfilledByBillId);
        Assert.Equal(observations, expectation.ObservationCount);
    }

    // A CONTRAPROVA: cancelado com OUTRO código de barras não é o mesmo compromisso — o ciclo fica
    // com quem o cumpriu, e o boleto novo continua sem conta esperada.
    [Fact]
    public async Task Fulfill_WhenTheCancelledBillHadAnotherBarcode_ShouldNotTransfer()
    {
        var oldBill = await SeedBillAsync(OtherBankSlipLine, cancel: true);
        var (expectationId, cycleId) = await SeedFulfilledCycleAsync(oldBill, dueLine: BankSlipLine);
        var newBill = await SeedBillAsync(BankSlipLine, cancel: false);

        await FulfillAsync(newBill);

        var cycle = (await LoadExpectationAsync(expectationId)).Cycles.Single(c => c.Id == cycleId);
        Assert.Equal(oldBill, cycle.FulfilledByBillId);
    }

    // A consulta dos substituídos é do tenant: o boleto cancelado de outra conta com o mesmo código
    // de barras não entra.
    [Fact]
    public async Task ListReplaced_ShouldIgnoreBillsOfAnotherTenant()
    {
        await SeedBillAsync(BankSlipLine, cancel: true, tenant: TenantB);
        var newBill = await SeedBillAsync(BankSlipLine, cancel: false);

        using var scope = Factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IBillRepository>();
        var fromRepository = await repository.ListReplacedAsync(TenantA, await DedupKeyOf(newBill), newBill);

        Assert.Empty(fromRepository);
    }

    private async Task FulfillAsync(BillId billId)
    {
        using var scope = Factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IMediator>()
            .Send(new FulfillExpectationForBillCommand(TenantA.Value, billId.Value));
    }

    private Task<string?> DedupKeyOf(BillId billId)
        => ExecuteDbContextAsync(async db => (await db.Bills.AsNoTracking().FirstAsync(b => b.Id == billId)).DedupKey);

    private Task<BillExpectation> LoadExpectationAsync(BillExpectationId id)
        => ExecuteDbContextAsync(db => db.BillExpectations.AsNoTracking().Include(e => e.Cycles).FirstAsync(e => e.Id == id));

    private Task<BillId> SeedBillAsync(string line, bool cancel, TenantId? tenant = null)
        => ExecuteDbContextAsync(async db =>
        {
            var bill = Bill.Capture(
                tenant ?? TenantA,
                [PaymentInstrument.FromBarcode(DigitableLine.Parse(line, OccurredAt))],
                BillOrigin.Create(BillSourceKind.Mailbox, OccurredAt, sourceId: Source, senderAddress: "contadigital@vivo.com.br"),
                OccurredAt);

            bill.ResolvePayee(Vivo, OccurredAt);

            if (cancel)
            {
                bill.RecordChecks([.. Enumeration.GetAll<CheckType>().Select(t => CheckResult.Passed(t))], OccurredAt);
                bill.Cancel(Operator, "recapturado", OccurredAt);
            }

            await db.Bills.AddAsync(bill);
            await db.SaveEntitiesAsync();
            return bill.Id;
        });

    /// <summary>
    /// A expectativa com o ciclo da competência do vencimento de <paramref name="dueLine"/> já
    /// cumprido por <paramref name="fulfilledBy"/>.
    /// </summary>
    private Task<(BillExpectationId, ExpectationCycleId)> SeedFulfilledCycleAsync(BillId fulfilledBy, string dueLine = BankSlipLine)
        => ExecuteDbContextAsync(async db =>
        {
            var dueDate = DateOnly.FromDateTime(DigitableLine.Parse(dueLine, OccurredAt).DueDate!.Value);

            var expectation = BillExpectation.Register(
                TenantA, Vivo, "1123004411", "Vivo", Recurrence.Monthly,
                expectedDueDay: dueDate.Day, observedLeadDays: 4, alertLeadDays: null,
                anchorDueDate: dueDate, hintSourceId: CaptureSourceId.From(Source), OccurredAt);

            var cycle = expectation.OpenCycle(new CompetencePeriod(dueDate.Year, dueDate.Month), OccurredAt);
            expectation.Fulfill(cycle.Id, fulfilledBy, dueDate, DateOnly.FromDateTime(OccurredAt), null, OccurredAt);

            await db.BillExpectations.AddAsync(expectation);
            await db.SaveEntitiesAsync();
            return (expectation.Id, cycle.Id);
        });
}
