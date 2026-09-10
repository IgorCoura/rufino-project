namespace BillPayment.IntegrationTests.Bills;

using BillPayment.Application.Queries.Bills;
using BillPayment.Domain.Bills;
using BillPayment.Domain.Bills.Checks;
using BillPayment.Domain.Instruments;
using BillPayment.Domain.Lookups;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using BillPayment.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// A fila que reconsulta boletos deixados sem consulta oficial.
/// </summary>
/// <remarks>
/// Nasceu junto com a decisão de 2026-09-10, que passou a classificar consulta sem resposta como
/// Extremo Perigo: sem esta varredura, o aviso "revalide mais tarde" seria trabalho manual e o
/// boleto ficaria exigindo a alçada máxima por um incidente de provedor que já terminou.
/// </remarks>
[Collection(nameof(IntegrationTestCollection))]
public sealed class BillRevalidationQueueTests : BaseIntegrationTest
{
    private static readonly TenantId TenantA = TenantId.From(new Guid("0195a1f0-0000-7000-8000-000000000001"));
    private static readonly TenantId TenantB = TenantId.From(new Guid("0195a1f0-0000-7000-8000-000000000002"));
    private static readonly Guid Source = new("0195a1f0-0000-7000-8000-0000000000b1");
    private static readonly DateTime OccurredAt = new(2026, 9, 10, 9, 0, 0, DateTimeKind.Utc);
    private static readonly TimeSpan BaseDelay = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan MaxDelay = TimeSpan.FromHours(1);

    // Uma linha por boleto: a chave de dedup é única GLOBAL, então reusar uma reprovaria no banco
    // e esconderia o que o teste mede.
    private static readonly string[] BankSlipLines =
    [
        "34191234546789012345767890123457314880000061507",
        "03399876534321098765743210987657414930000140980",
        "826600000010224812345672890123456786901234567898",
    ];

    public BillRevalidationQueueTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    // TESTE-ÂNCORA: o boleto que ficou sem consulta por indisponibilidade é reivindicado, e é
    // isso que faz a classificação de Extremo Perigo ser temporária em vez de definitiva.
    [Fact]
    public async Task ClaimStaleWithoutLookup_ShouldReturnTheBillLeftWithoutALookup()
    {
        var billId = await SeedAsync(CheckReasons.LOOKUP_UNAVAILABLE);

        var pending = await ClaimAsync(10);

        Assert.Single(pending);
        Assert.Equal(billId.Value, pending[0].BillId);
        Assert.Equal(TenantA.Value, pending[0].TenantId);
    }

    // CONTRAPROVA: quem o provedor respondeu que não conhece, e quem está sem chave vinculada,
    // NÃO entra. Reconsultar o primeiro devolve o mesmo e martelaria o provedor à toa; o segundo
    // não depende do tempo, e sim de um cadastro.
    [Theory]
    [InlineData(CheckReasons.LOOKUP_UNRESOLVED)]
    [InlineData(CheckReasons.LOOKUP_NOT_CONFIGURED)]
    public async Task ClaimStaleWithoutLookup_ShouldIgnoreReasonsThatTimeDoesNotFix(string reason)
    {
        await SeedAsync(reason);

        Assert.Empty(await ClaimAsync(10));
    }

    // TESTE-ÂNCORA: boleto já APROVADO não é reivindicado. Revalidar ali derruba a aprovação
    // incondicionalmente, e um worker fazendo isso desfaria decisão humana em silêncio.
    [Fact]
    public async Task ClaimStaleWithoutLookup_ShouldNeverTouchABillSomeoneAlreadyDecided()
    {
        await SeedAsync(CheckReasons.LOOKUP_UNAVAILABLE, approve: true);

        Assert.Empty(await ClaimAsync(10));
    }

    // Reivindicar tira o boleto da fila para os outros workers, no mesmo comando que o escolheu —
    // e o adiamento é o backoff: o segundo ciclo não o vê de novo.
    [Fact]
    public async Task ClaimStaleWithoutLookup_ShouldNotHandTheSameBillTwice()
    {
        await SeedAsync(CheckReasons.LOOKUP_UNAVAILABLE);

        Assert.Single(await ClaimAsync(10));
        Assert.Empty(await ClaimAsync(10));
    }

    // A tentativa é contada na SAÍDA da fila: quem derruba o worker antes de escrever qualquer
    // coisa não pode voltar sem espera nenhuma.
    [Fact]
    public async Task ClaimStaleWithoutLookup_ShouldCountTheAttemptOnTheWayOut()
    {
        var billId = await SeedAsync(CheckReasons.LOOKUP_UNAVAILABLE);

        var pending = await ClaimAsync(10);

        Assert.Equal(1, pending[0].Attempts);

        var bill = await LoadAsync(billId);
        Assert.Equal(1, bill.RevalidationAttempts);
        Assert.NotNull(bill.RevalidationNextAttemptAt);
    }

    // A fila atravessa tenants — o worker roda fora de requisição — e cada linha carrega o tenant
    // que reconstitui o escopo do comando seguinte.
    [Fact]
    public async Task ClaimStaleWithoutLookup_ShouldSpanTenantsAndCarryTheirScope()
    {
        await SeedAsync(CheckReasons.LOOKUP_UNAVAILABLE, tenantId: TenantA);
        await SeedAsync(CheckReasons.LOOKUP_UNAVAILABLE, tenantId: TenantB, line: 1);

        var pending = await ClaimAsync(10);

        Assert.Equal(2, pending.Count);
        Assert.Contains(pending, p => p.TenantId == TenantA.Value);
        Assert.Contains(pending, p => p.TenantId == TenantB.Value);
    }

    // O teto de lote é respeitado: um ciclo não puxa a fila inteira contra um provedor que pode
    // estar fora.
    [Fact]
    public async Task ClaimStaleWithoutLookup_ShouldRespectTheLimit()
    {
        for (var i = 0; i < BankSlipLines.Length; i++)
            await SeedAsync(CheckReasons.LOOKUP_UNAVAILABLE, line: i, createdAt: OccurredAt.AddMinutes(i));

        Assert.Equal(2, (await ClaimAsync(2)).Count);
    }

    // Vincular a chave do provedor devolve à fila, AGORA, os boletos daquele tenant que ficaram
    // sem consulta por falta dela — e só os daquele tenant.
    [Fact]
    public async Task ReleaseNotConfigured_ShouldRequeueOnlyTheTenantThatLinkedTheAccount()
    {
        var mine = await SeedAsync(CheckReasons.LOOKUP_NOT_CONFIGURED, tenantId: TenantA);
        await SeedAsync(CheckReasons.LOOKUP_NOT_CONFIGURED, tenantId: TenantB, line: 1);

        var released = await ReleaseAsync(TenantA.Value);

        Assert.Equal(1, released);
        Assert.Null((await LoadAsync(mine)).RevalidationNextAttemptAt);
    }

    private async Task<IReadOnlyList<PendingBillRevalidation>> ClaimAsync(int limit)
    {
        using var scope = Factory.Services.CreateScope();
        var queries = scope.ServiceProvider.GetRequiredService<IBillRevalidationWorkQueries>();

        return await queries.ClaimStaleWithoutLookupAsync(limit, BaseDelay, MaxDelay);
    }

    private async Task<int> ReleaseAsync(Guid tenantId)
    {
        using var scope = Factory.Services.CreateScope();
        var queries = scope.ServiceProvider.GetRequiredService<IBillRevalidationWorkQueries>();

        return await queries.ReleaseNotConfiguredAsync(tenantId);
    }

    private Task<Bill> LoadAsync(BillId billId)
        => ExecuteDbContextAsync(async db => (await db.Bills.FindAsync(billId))!);

    /// <summary>
    /// Boleto verificado com a consulta falhando pelo motivo pedido — o estado real em que a fila
    /// o encontra.
    /// </summary>
    private Task<BillId> SeedAsync(
        string reason,
        TenantId? tenantId = null,
        int line = 0,
        DateTime? createdAt = null,
        bool approve = false)
        => ExecuteDbContextAsync(async db =>
        {
            var occurredAt = createdAt ?? OccurredAt;

            var bill = Bill.Capture(
                tenantId ?? TenantA,
                [PaymentInstrument.FromBarcode(DigitableLine.Parse(BankSlipLines[line], occurredAt))],
                BillOrigin.Create(
                    BillSourceKind.Mailbox,
                    occurredAt,
                    sourceId: Source,
                    senderAddress: "faturas@fornecedor.com.br",
                    storageKey: $"tenants/captures/{Guid.NewGuid()}"),
                occurredAt);

            bill.AttachLookups(
                BillLookupResult.Unavailable("timeout", null, occurredAt),
                pix: null,
                occurredAt);

            bill.RecordChecks(CatalogWith(reason), occurredAt);

            if (approve)
            {
                bill.Approve(
                    UserId.From(new Guid("0195a1f0-0000-7000-8000-00000000000a")),
                    note: "risco assumido",
                    ApprovalPolicy.Default(null),
                    RiskLevel.ExtremeDanger,
                    occurredAt,
                    acknowledgeRisk: true);
            }

            await db.Bills.AddAsync(bill);
            await db.SaveEntitiesAsync();
            return bill.Id;
        });

    /// <summary>
    /// O catálogo inteiro — <c>RecordChecks</c> recusa conjunto parcial —, com a verificação 3
    /// reprovando pelo motivo do cenário e as demais passando.
    /// </summary>
    private static List<CheckResult> CatalogWith(string reason)
        =>
        [
            .. Enumeration.GetAll<CheckType>()
                .Select(type => type == CheckType.LookupAvailability
                    ? CheckResult.Failed(type, reason, "A consulta oficial não devolveu o documento.")
                    : CheckResult.Passed(type)),
        ];
}
