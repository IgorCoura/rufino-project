namespace BillPayment.IntegrationTests.CaptureItems;

using System.Text;
using BillPayment.Application.Bills.Commands;
using BillPayment.Application.CaptureItems.Commands;
using BillPayment.Application.Mediator;
using BillPayment.Domain.Bills;
using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.Lookups;
using BillPayment.Domain.PayerProfiles;
using BillPayment.Domain.Ports;
using BillPayment.Domain.Secrets;
using BillPayment.Domain.SharedKernel;
using BillPayment.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>
/// Degrau 1 da escada de roteamento: o pagador que a consulta oficial do Pix dinâmico devolve.
/// </summary>
/// <remarks>
/// Conta de concessionária quase nunca imprime o documento do pagador — a Vivo imprime só o nome
/// do titular —, e sem o degrau 1 ela caía sempre na fila de reivindicação. A cobrança registrada
/// é emitida CONTRA um documento, e quem o afirma é o PSP do trilho que paga (ADR-025).
/// </remarks>
[Collection(nameof(IntegrationTestCollection))]
public sealed class OfficialPixPayerRoutingTests : BaseIntegrationTest
{
    private static readonly TenantId Tenant = TenantId.From(new Guid("0195a1f0-0000-7000-8000-000000000001"));
    private static readonly DateTime OccurredAt = new(2026, 9, 13, 3, 20, 0, DateTimeKind.Utc);

    private const string TenantCnpj = "11222333000181";
    private const string SomeoneElsesCpf = "08244181826";

    /// <summary>BR Code dinâmico, escrito no corpo do e-mail como a concessionária escreve.</summary>
    private const string DynamicPixPayload =
        "00020101021226770014BR.GOV.BCB.PIX2555api.itau/pix/qr/v2/fe80130d-c5ef-407b-94a5-f6b2005020095"
        + "204000053039865802BR5906SABESP6009SAO PAULO62070503***6304A76E";

    private readonly IServiceProvider _services;
    private readonly FakeLookupServices _lookups;

    public OfficialPixPayerRoutingTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
        _services = factory.WithCaptureChain()
            .WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<FakeLookupServices>();
                services.RemoveAll<IBillLookupService>();
                services.RemoveAll<IPixLookupService>();
                services.AddSingleton<IBillLookupService>(sp => sp.GetRequiredService<FakeLookupServices>());
                services.AddSingleton<IPixLookupService>(sp => sp.GetRequiredService<FakeLookupServices>());
            }))
            .Services;

        _lookups = _services.GetRequiredService<FakeLookupServices>();
        _lookups.Reset();
    }

    // TESTE ÂNCORA. Nenhum documento impresso, e a consulta oficial diz que a cobrança é contra o
    // CNPJ do tenant: o boleto é dele, com força de degrau forte — em vez de ir para a reivindicação.
    [Fact]
    public async Task Process_WhenTheOfficialPixPayerIsTheTenant_ShouldPromote()
    {
        await SeedProfileAsync(linkAsaasAccount: true);
        _lookups.PixResult = ResolvedPix(payerTaxId: TenantCnpj);
        var itemId = await SeedBodyAsync();

        var result = await ProcessAsync(itemId);

        Assert.Equal("Promote", result.Routing);
        var item = await LoadItemAsync(itemId);
        Assert.Same(CaptureItemStatus.Promoted, item!.Status);
        Assert.Same(RoutingConfidence.Strong, item.Routing);
    }

    // Decisão do usuário (2026-09-14): a consulta oficial diz que o pagador é OUTRA pessoa, e o
    // boleto é descartado — sem item e sem arquivo, como o boleto de outro pagador sob rótulo.
    [Fact]
    public async Task Process_WhenTheOfficialPixPayerIsSomeoneElse_ShouldDiscard()
    {
        await SeedProfileAsync(linkAsaasAccount: true);
        _lookups.PixResult = ResolvedPix(payerTaxId: SomeoneElsesCpf);
        var itemId = await SeedBodyAsync();

        var result = await ProcessAsync(itemId);

        Assert.Equal("Drop", result.Decision);
        Assert.Null(await LoadItemAsync(itemId));
    }

    // Sem conta Asaas vinculada não há com o que consultar: a escada segue como sempre seguiu, e o
    // provedor nem é chamado.
    [Fact]
    public async Task Process_WhenTheTenantHasNoLinkedAccount_ShouldNotConsultAndStayUnrouted()
    {
        await SeedProfileAsync(linkAsaasAccount: false);
        _lookups.PixResult = ResolvedPix(payerTaxId: TenantCnpj);
        var itemId = await SeedBodyAsync();

        await ProcessAsync(itemId);

        Assert.Equal(0, _lookups.PixCallCount);
        Assert.Same(CaptureItemStatus.Unrouted, (await LoadItemAsync(itemId))!.Status);
    }

    // Consulta que não responde não decide nada: nem promove nem descarta.
    [Fact]
    public async Task Process_WhenTheOfficialLookupIsUnavailable_ShouldStayUnrouted()
    {
        await SeedProfileAsync(linkAsaasAccount: true);
        var itemId = await SeedBodyAsync();

        await ProcessAsync(itemId);

        Assert.Same(CaptureItemStatus.Unrouted, (await LoadItemAsync(itemId))!.Status);
    }

    // A primeira validação reaproveita a leitura de QR que o roteamento acabou de fazer: o
    // provedor limita leituras de QR por conta, e a pergunta é a mesma segundos depois.
    [Fact]
    public async Task Validate_RightAfterTheOfficialPayerPromoted_ShouldReuseTheLookup()
    {
        await SeedProfileAsync(linkAsaasAccount: true);
        _lookups.PixResult = ResolvedPix(payerTaxId: TenantCnpj);
        var itemId = await SeedBodyAsync();

        var result = await ProcessAsync(itemId);
        await ValidateAsync(result.BillId!.Value);

        Assert.Equal(1, _lookups.PixCallCount);
        var bill = await ExecuteDbContextAsync(db => db.Bills.AsNoTracking().FirstAsync(b => b.Id == BillId.From(result.BillId!.Value)));
        Assert.Single(bill.LookupHistory);
        Assert.NotNull(bill.PixLookup);
    }

    /// <remarks>
    /// O instante da consulta é o do relógio real, e não fixo: a janela de reaproveitamento é
    /// medida contra o <c>TimeProvider</c> do host, que é o do sistema.
    /// </remarks>
    private static PixLookupResult ResolvedPix(string payerTaxId)
    {
        var consultedAt = DateTimeOffset.UtcNow;

        return PixLookupResult.Resolved(
            PixLookupSnapshot.Create(
                LookupParty.From("SABESP", null, "43776517000180"),
                consultedAt,
                isDynamic: true,
                payer: MaskedParty.Of("TITULAR", payerTaxId)),
            consultedAt);
    }

    private async Task<ProcessCaptureItemResponse> ProcessAsync(CaptureItemId itemId)
    {
        using var scope = _services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var first = await mediator.Send(new ProcessCaptureItemCommand(Tenant.Value, itemId.Value, VisionLane: false));

        return first.Decision != "VisionPending"
            ? first
            : await mediator.Send(new ProcessCaptureItemCommand(Tenant.Value, itemId.Value, VisionLane: true));
    }

    private async Task ValidateAsync(Guid billId)
    {
        using var scope = _services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        await mediator.Send(new ValidateBillCommand(Tenant.Value, billId));
    }

    private Task<CaptureItem?> LoadItemAsync(CaptureItemId itemId)
        => ExecuteDbContextAsync(db => db.CaptureItems.AsNoTracking().FirstOrDefaultAsync(i => i.Id == itemId));

    private Task SeedProfileAsync(bool linkAsaasAccount)
        => ExecuteDbContextAsync(async db =>
        {
            var profile = PayerProfile.Register(
                Tenant, PayerKind.Company, "EMPRESA DE TESTE LTDA", TenantCnpj, OccurredAt);

            if (linkAsaasAccount)
                profile.LinkAsaasAccount(CredentialRef.ForLocalVault(new Guid("0195a1f0-0000-7000-8000-0000000000c1")), OccurredAt);

            await db.PayerProfiles.AddAsync(profile);
            await db.SaveEntitiesAsync();
        });

    private async Task<CaptureItemId> SeedBodyAsync()
    {
        var sourceId = await ExecuteDbContextAsync(async db =>
        {
            var source = CaptureSource.Connect(
                Tenant,
                CaptureSourceKind.MicrosoftGraphMailbox,
                "Caixa",
                "contas@empresa.com.br",
                CredentialRef.ForLocalVault(new Guid("0195a1f0-0000-7000-8000-0000000000c2")),
                OccurredAt);

            await db.CaptureSources.AddAsync(source);
            await db.SaveEntitiesAsync();
            return source.Id;
        });

        _services.GetRequiredService<FakeMailboxReader>().Artifacts[IMailboxReader.BODY_ARTIFACT_KEY] =
            Encoding.UTF8.GetBytes($"<html><body><p>Pague com Pix</p><p>{DynamicPixPayload}</p></body></html>");

        return await ExecuteDbContextAsync(async db =>
        {
            var item = CaptureItem.Ingest(
                Tenant, sourceId, "AAMkAGI2PIXAAA=", IMailboxReader.BODY_ARTIFACT_KEY,
                "contas@concessionaria.com.br", "Sua fatura chegou", OccurredAt, OccurredAt,
                "text/html", fileName: null);

            await db.CaptureItems.AddAsync(item);
            await db.SaveEntitiesAsync();
            return item.Id;
        });
    }
}
