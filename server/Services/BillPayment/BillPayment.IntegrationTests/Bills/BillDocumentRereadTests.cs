namespace BillPayment.IntegrationTests.Bills;

using System.Net.Http.Headers;
using System.Net.Http.Json;
using BillPayment.Domain.Bills;
using BillPayment.Domain.Bills.Checks;
using BillPayment.Domain.PayerProfiles;
using BillPayment.Domain.SharedKernel;
using BillPayment.IntegrationTests.Contracts;
using BillPayment.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

/// <summary>
/// A releitura do documento guardado a cada validação — a verificação 8 respondendo sobre o
/// arquivo, e não sobre o que a captura conseguiu ler no dia.
/// </summary>
/// <remarks>
/// <para>
/// <strong>É o que torna "Revalidar" um conserto de verdade.</strong> Enquanto o pagador vinha
/// congelado da captura, boleto lido errado — ou lido por um caminho que esquecia de preencher o
/// campo, que foi o defeito do RUF101 — continuava errado para sempre, e a única saída era
/// reimportar o documento.
/// </para>
/// <para>
/// O host é o da cadeia de pagamento porque ele traz as duas coisas de que esta classe precisa: a
/// consulta oficial determinística e o armazenamento em memória. A fábrica compartilhada mantém o
/// balde não configurado, que falha em toda leitura — e é justamente ele que prova a falha fechada.
/// </para>
/// </remarks>
[Collection(nameof(IntegrationTestCollection))]
public sealed class BillDocumentRereadTests : BaseIntegrationTest, IDisposable
{
    private static readonly Guid TenantId = TestTenants.Primary;
    private static readonly DateTime ReceivedAt = new(2026, 9, 15, 9, 0, 0, DateTimeKind.Utc);

    private const string BankSlipLine = "34191234546789012345767890123457314880000061507";

    /// <summary>Documento do tenant — diferente do beneficiário impresso, senão a verificação 8
    /// bloquearia por "o beneficiário é o próprio pagador" antes de chegar ao pagador.</summary>
    private const string TenantCnpj = "45678901000175";

    private readonly WebApplicationFactory<Program> _host;
    private readonly FakeLookupServices _lookups;

    public BillDocumentRereadTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
        _host = factory.WithPaymentChain();
        _lookups = _host.Services.GetRequiredService<FakeLookupServices>();
    }

    public void Dispose()
    {
        _lookups.Reset();
        _host.Dispose();
    }

    // TESTE ÂNCORA (2026-09-15): boleto gravado SEM pagador — o acervo anterior ao conserto do
    // import — passa a ser identificado na revalidação, porque ela relê o documento guardado em vez
    // de acreditar no que a captura anotou.
    [Fact]
    public async Task Revalidate_OnABillStoredWithoutAPayer_ShouldReadItFromTheDocumentAgain()
    {
        await SeedPayerProfileAsync();
        var billId = await ImportWithDocumentAsync(PdfNamingThePayer());
        await ForgetTheExtractedPayerAsync(billId);

        await RevalidateAsync(billId);

        var bill = await LoadAsync(billId);
        Assert.Equal(TenantCnpj, bill.ExtractedPayer?.TaxId?.Value);

        var check = bill.Checks.Single(c => c.Type == CheckType.PayerMatch);
        Assert.Equal(CheckOutcome.Passed, check.Outcome);
        Assert.DoesNotContain(bill.Checks, c => c.ReasonCode == CheckReasons.PAYER_NOT_EXTRACTABLE);
    }

    // O cadastro fiscal entra na releitura como está HOJE: documento cadastrado depois da captura
    // passa a ser reconhecido sem que ninguém reimporte o boleto.
    [Fact]
    public async Task Revalidate_AfterTheTenantRegistersItsTaxId_ShouldRecognizeThePayer()
    {
        var billId = await ImportWithDocumentAsync(PdfNamingThePayer());

        await SeedPayerProfileAsync();
        await RevalidateAsync(billId);

        var bill = await LoadAsync(billId);
        Assert.Equal(TenantCnpj, bill.ExtractedPayer?.TaxId?.Value);
    }

    // CONTRAPROVA, e é a que impede a releitura de apagar evidência: boleto importado só com os
    // dígitos não tem documento para reler, e o pagador que a captura gravou continua valendo.
    [Fact]
    public async Task Revalidate_OnABillWithoutAStoredDocument_ShouldKeepThePayerItAlreadyHad()
    {
        await SeedPayerProfileAsync();

        var response = await ClientFor().PostAsJsonAsync(
            ImportRoute(), new ImportBillRequest(BankSlipLine, null, "ManualUpload", ReceivedAt));

        response.EnsureSuccessStatusCode();
        var billId = (await response.Content.ReadFromJsonAsync<ImportBillResponseContract>())!.Id;

        await RememberAPayerAsync(billId);
        await RevalidateAsync(billId);

        var bill = await LoadAsync(billId);
        Assert.Equal(TenantCnpj, bill.ExtractedPayer?.TaxId?.Value);
    }

    // A releitura que ACONTECEU e não achou pagador nenhum afirma isso: o documento não identifica
    // ninguém, e o retrato guardado não sobrevive a essa afirmação.
    [Fact]
    public async Task Revalidate_WhenTheDocumentNamesNobody_ShouldClearThePayerItHad()
    {
        await SeedPayerProfileAsync();
        var billId = await ImportWithDocumentAsync(PdfWith("Banco Itau", BankSlipLine, "Sem bloco de pagador"));

        await RememberAPayerAsync(billId);
        await RevalidateAsync(billId);

        var bill = await LoadAsync(billId);
        Assert.Null(bill.ExtractedPayer);

        var check = bill.Checks.Single(c => c.Type == CheckType.PayerMatch);
        Assert.Equal(CheckReasons.PAYER_NOT_EXTRACTABLE, check.ReasonCode);
    }

    /// <summary>O boleto como o emissor o imprime: a linha em cima, o bloco do pagador embaixo.</summary>
    private static byte[] PdfNamingThePayer()
        => PdfWith(
            "Banco Itau",
            BankSlipLine,
            "Beneficiario JUND DIAMOND C F LTDA CNPJ/CPF: 11.222.333/0001-81",
            "Pagador: RUFINO EMP.ELET.HIDR.LTDA CNPJ/CPF: " + TenantCnpj);

    private async Task<Guid> ImportWithDocumentAsync(byte[] pdf)
    {
        var file = new ByteArrayContent(pdf);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

        var form = new MultipartFormDataContent { { file, "file", "boleto.pdf" } };
        form.Add(new StringContent("ManualUpload"), "sourceKind");
        form.Add(new StringContent(ReceivedAt.ToString("O")), "receivedAt");

        var response = await ClientFor().PostAsync(ImportRoute(), form);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<ImportBillResponseContract>();
        return body!.Id;
    }

    /// <summary>
    /// Apaga o pagador direto no banco — o retrato de um boleto capturado antes de o import
    /// preenchê-lo. Pelo agregado seria impossível: `RefreshExtractedPayer` é justamente o método
    /// que esta classe exercita.
    /// </summary>
    private async Task ForgetTheExtractedPayerAsync(Guid billId)
        => await ExecuteDbContextAsync(db => db.Database.ExecuteSqlRawAsync(
            "UPDATE bill_payment.bills SET extracted_payer_tax_id = NULL, extracted_payer_name = NULL WHERE id = {0}",
            billId));

    /// <summary>O caminho inverso: grava um pagador que a releitura terá de confirmar ou desmentir.</summary>
    private async Task RememberAPayerAsync(Guid billId)
        => await ExecuteDbContextAsync(db => db.Database.ExecuteSqlRawAsync(
            "UPDATE bill_payment.bills SET extracted_payer_tax_id = {1} WHERE id = {0}",
            billId,
            TenantCnpj));

    private Task SeedPayerProfileAsync()
        => ExecuteDbContextAsync(async db =>
        {
            await db.PayerProfiles.AddAsync(PayerProfile.Register(
                Domain.SharedKernel.TenantId.From(TenantId),
                PayerKind.Company,
                "RUFINO EMPREITEIRA LTDA",
                TaxId.Parse(TenantCnpj),
                ReceivedAt));

            await db.SaveEntitiesAsync();
        });

    /// <summary>Roda a validação pelo mesmo comando do outbox — revalidar é rodar de novo.</summary>
    private async Task RevalidateAsync(Guid billId)
    {
        using var scope = _host.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<Application.Mediator.IMediator>();

        await mediator.Send(
            new Application.Bills.Commands.ValidateBillCommand(TenantId, billId), CancellationToken.None);
    }

    private async Task<Bill> LoadAsync(Guid billId)
        => (await ExecuteDbContextAsync(db => db.Bills
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == BillId.From(billId))))!;

    private static Uri ImportRoute() => new($"/api/v1/{TenantId}/bills/import", UriKind.Relative);

    private HttpClient ClientFor()
    {
        var client = _host.CreateClient().Authenticated();
        client.DefaultRequestHeaders.Add("x-requestid", Guid.CreateVersion7().ToString());
        return client;
    }

    private static byte[] PdfWith(params string[] lines)
    {
        var builder = new PdfDocumentBuilder();
        var page = builder.AddPage(595, 842);
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);

        var y = 800;
        foreach (var line in lines)
        {
            page.AddText(line, 10, new UglyToad.PdfPig.Core.PdfPoint(30, y), font);
            y -= 20;
        }

        return builder.Build();
    }
}
