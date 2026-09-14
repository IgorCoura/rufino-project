namespace BillPayment.IntegrationTests.Bills;

using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using BillPayment.Domain.Bills;
using BillPayment.Domain.CapturedMessages;
using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.Instruments;
using BillPayment.Domain.PayerProfiles;
using BillPayment.Domain.PaymentOrders;
using BillPayment.Domain.SharedKernel;
using BillPayment.IntegrationTests.Contracts;
using BillPayment.IntegrationTests.Documents;
using BillPayment.IntegrationTests.Extraction;
using BillPayment.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;
using UglyToad.PdfPig;

/// <summary>
/// Baixar os documentos de vários boletos de uma vez — <c>POST /bills/documents/export</c>.
/// </summary>
/// <remarks>
/// Decisões do usuário em 2026-09-14 que estes testes fixam: "somente a primeira página" vale só
/// para o boleto (o comprovante sai inteiro); um PDF por boleto sai num .zip; boleto sem documento
/// vira página de aviso com a procedência e o código de pagamento; a ordem é a da seleção.
/// </remarks>
[Collection(nameof(IntegrationTestCollection))]
public sealed class BillDocumentExportTests : BaseIntegrationTest
{
    private static readonly TenantId TenantA = TenantId.From(TestTenants.Primary);
    private static readonly TenantId TenantB = TenantId.From(TestTenants.Secondary);
    private static readonly DateTime OccurredAt = new(2026, 8, 19, 12, 30, 0, DateTimeKind.Utc);
    private static readonly Guid SourceId = new("0195a1f0-0000-7000-8000-0000000000c1");

    // Instrumentos sintéticos com DVs corretos. O Pix estático não deduplica, e por isso serve
    // para semear quantos boletos o teste precisar.
    private const string BankSlipLine = "34191234546789012345767890123457314880000061507";
    private const string UtilityLine = "826600000010224812345672890123456786901234567898";
    private const string StaticPix =
        "00020126560014br.gov.bcb.pix0114112223330001810216conta de energia52040000530398654071500.005802BR5912SABESP TESTE6009SAO PAULO62120508TXID000163046665";

    private readonly WebApplicationFactory<Program> _host;
    private readonly InMemoryAttachmentStorage _storage;

    public BillDocumentExportTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
        _host = factory.WithCaptureChain();
        _storage = _host.Services.GetRequiredService<InMemoryAttachmentStorage>();
    }

    // PDF único: todas as páginas de todos os boletos, na ordem em que foram marcados — e não na
    // ordem de criação.
    [Fact]
    public async Task Export_AsSinglePdf_ShouldKeepEveryPageInSelectionOrder()
    {
        var first = await SeedPixBillAsync(TenantA, PdfComposerTests.PdfWithPages("A", 2));
        var second = await SeedPixBillAsync(TenantA, PdfComposerTests.PdfWithPages("B", 3));

        var response = await ExportAsync([second, first], "All", includeReceipts: false, "SinglePdf");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        using var document = PdfDocument.Open(await response.Content.ReadAsByteArrayAsync());
        Assert.Equal(5, document.NumberOfPages);
        Assert.Contains("B-1", document.GetPage(1).Text, StringComparison.Ordinal);
        Assert.Contains("A-1", document.GetPage(4).Text, StringComparison.Ordinal);
    }

    // "Somente a primeira página" corta o documento do boleto.
    [Fact]
    public async Task Export_WithFirstPageOnly_ShouldKeepOnlyTheFirstPageOfEachBill()
    {
        var first = await SeedPixBillAsync(TenantA, PdfComposerTests.PdfWithPages("A", 2));
        var second = await SeedPixBillAsync(TenantA, PdfComposerTests.PdfWithPages("B", 3));

        var document = await ExportPdfAsync([first, second], "FirstPage", includeReceipts: false);

        Assert.Equal(2, document.NumberOfPages);
        Assert.Contains("A-1", document.GetPage(1).Text, StringComparison.Ordinal);
        Assert.Contains("B-1", document.GetPage(2).Text, StringComparison.Ordinal);
    }

    // O comprovante entra logo depois do documento do boleto e sai INTEIRO, mesmo com
    // "somente a primeira página" — a opção vale só para o boleto.
    [Fact]
    public async Task Export_WithReceipts_ShouldAppendTheWholeReceiptAfterTheBill()
    {
        var bill = await SeedPixBillAsync(TenantA, PdfComposerTests.PdfWithPages("BOLETO", 2));
        await SeedPaidOrderAsync(bill, PdfComposerTests.PdfWithPages("COMPROVANTE", 2));

        var document = await ExportPdfAsync([bill], "FirstPage", includeReceipts: true);

        Assert.Equal(3, document.NumberOfPages);
        Assert.Contains("BOLETO-1", document.GetPage(1).Text, StringComparison.Ordinal);
        Assert.Contains("COMPROVANTE-1", document.GetPage(2).Text, StringComparison.Ordinal);
        Assert.Contains("COMPROVANTE-2", document.GetPage(3).Text, StringComparison.Ordinal);
    }

    // Sem pedir comprovante, ele não entra — mesmo existindo.
    [Fact]
    public async Task Export_WithoutReceipts_ShouldNotAppendTheReceipt()
    {
        var bill = await SeedPixBillAsync(TenantA, PdfComposerTests.PdfWithPages("BOLETO", 1));
        await SeedPaidOrderAsync(bill, PdfComposerTests.PdfWithPages("COMPROVANTE", 1));

        var document = await ExportPdfAsync([bill], "All", includeReceipts: false);

        Assert.Equal(1, document.NumberOfPages);
    }

    // Boleto que nunca foi pago não tem comprovante, e isso não merece aviso.
    [Fact]
    public async Task Export_WithReceiptsForAnUnpaidBill_ShouldAddNothing()
    {
        var bill = await SeedPixBillAsync(TenantA, PdfComposerTests.PdfWithPages("BOLETO", 1));

        var document = await ExportPdfAsync([bill], "All", includeReceipts: true);

        Assert.Equal(1, document.NumberOfPages);
    }

    // Pago sem comprovante guardado: a página de aviso diz que ele não chegou, em vez de o PDF
    // parecer completo.
    [Fact]
    public async Task Export_WithReceiptsForAPaidBillWithoutReceipt_ShouldAddAReceiptNotice()
    {
        var bill = await SeedPixBillAsync(TenantA, PdfComposerTests.PdfWithPages("BOLETO", 1));
        await SeedPaidOrderAsync(bill, receipt: null);

        var document = await ExportPdfAsync([bill], "All", includeReceipts: true);

        Assert.Equal(2, document.NumberOfPages);
        Assert.Contains("Comprovante indisponível", document.GetPage(2).Text, StringComparison.Ordinal);
    }

    // Regra do usuário: boleto importado à mão, sem documento, vira página de aviso dizendo que o
    // código foi inserido manualmente — e trazendo o código, formatado como é impresso.
    [Fact]
    public async Task Export_WhenTheBillHasNoDocument_ShouldAddANoticeWithOriginAndPaymentCode()
    {
        var bill = await SeedManualBillAsync(BankSlipLine);

        var document = await ExportPdfAsync([bill], "All", includeReceipts: false);
        var text = document.GetPage(1).Text;

        Assert.Equal(1, document.NumberOfPages);
        Assert.Contains("Boleto sem documento", text, StringComparison.Ordinal);
        Assert.Contains("Inserido manualmente", text, StringComparison.Ordinal);
        Assert.Contains("34191.23454 67890.123457 67890.123457 3 14880000061507", text, StringComparison.Ordinal);
    }

    // Linha de arrecadação sai nos quatro blocos com o dígito de cada um.
    [Fact]
    public async Task Export_WhenAUtilityBillHasNoDocument_ShouldFormatTheLineInFourBlocks()
    {
        var bill = await SeedManualBillAsync(UtilityLine);

        var document = await ExportPdfAsync([bill], "All", includeReceipts: false);

        Assert.Contains(
            "82660000001-0 22481234567-2 89012345678-6 90123456789-8",
            document.GetPage(1).Text,
            StringComparison.Ordinal);
    }

    // Documento guardado que não abre: o aviso diz que ele veio por e-mail, de quem, com qual
    // assunto, e traz o Pix copia e cola.
    [Fact]
    public async Task Export_WhenTheStoredDocumentIsUnreadable_ShouldAddANoticeWithTheEmailOrigin()
    {
        var bill = await SeedPixBillAsync(TenantA, "%PDF-1.4 corrompido"u8.ToArray(), messageId: "msg-1");
        await SeedCapturedMessageAsync("msg-1", "Sua fatura de agosto chegou");

        var document = await ExportPdfAsync([bill], "All", includeReceipts: false);
        var text = string.Concat(document.GetPages().Select(p => p.Text));

        Assert.Contains("Documento do boleto indisponível", text, StringComparison.Ordinal);
        Assert.Contains("E-mail", text, StringComparison.Ordinal);
        Assert.Contains("faturas@sabesp.com.br", text, StringComparison.Ordinal);
        Assert.Contains("Sua fatura de agosto chegou", text, StringComparison.Ordinal);
        Assert.Contains(StaticPix.Replace(" ", string.Empty, StringComparison.Ordinal), text.Replace(" ", string.Empty, StringComparison.Ordinal), StringComparison.Ordinal);
    }

    // Documento em imagem vira uma página do PDF.
    [Fact]
    public async Task Export_WhenTheDocumentIsAnImage_ShouldBecomeAPage()
    {
        var bill = await SeedPixBillAsync(TenantA, PdfComposerTests.ImageBytes(SKEncodedImageFormat.Png), "image/png");

        var document = await ExportPdfAsync([bill], "All", includeReceipts: false);

        Assert.Equal(1, document.NumberOfPages);
        Assert.Single(document.GetPage(1).GetImages());
    }

    // PDF cifrado entra destravado — a mesma cópia legível do visualizador, sem a senha sair.
    [Fact]
    public async Task Export_WhenTheDocumentIsEncrypted_ShouldIncludeTheUnlockedCopy()
    {
        await SeedPayerProfileAsync();
        var bill = await SeedPixBillAsync(TenantA, EncryptedPdfFixture.Bytes());

        var document = await ExportPdfAsync([bill], "All", includeReceipts: false);

        Assert.Equal(1, document.NumberOfPages);
        Assert.DoesNotContain("indisponível", document.GetPage(1).Text, StringComparison.Ordinal);
    }

    // Um PDF por boleto: um .zip com uma entrada por boleto, na ordem da seleção.
    [Fact]
    public async Task Export_AsPdfPerBill_ShouldZipOnePdfPerBill()
    {
        var first = await SeedPixBillAsync(TenantA, PdfComposerTests.PdfWithPages("A", 2));
        var second = await SeedManualBillAsync(BankSlipLine);

        var response = await ExportAsync([first, second], "All", includeReceipts: false, "PdfPerBill");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/zip", response.Content.Headers.ContentType?.MediaType);

        using var archive = new ZipArchive(new MemoryStream(await response.Content.ReadAsByteArrayAsync()));
        Assert.Equal(2, archive.Entries.Count);
        Assert.All(archive.Entries, e => Assert.EndsWith(".pdf", e.FullName, StringComparison.Ordinal));
        Assert.Equal(2, PageCountOf(archive.Entries[0]));
        Assert.Equal(1, PageCountOf(archive.Entries[1]));
    }

    // Um PDF por boleto com um boleto só não embrulha num .zip.
    [Fact]
    public async Task Export_AsPdfPerBillWithASingleBill_ShouldReturnThePdfDirectly()
    {
        var bill = await SeedPixBillAsync(TenantA, PdfComposerTests.PdfWithPages("A", 1));

        var response = await ExportAsync([bill], "All", includeReceipts: false, "PdfPerBill");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
    }

    // Um boleto de outro tenant no meio da seleção derruba o pedido inteiro com 404 — a mesma
    // resposta de um id inexistente.
    [Fact]
    public async Task Export_WhenABillBelongsToAnotherTenant_ShouldReturnNotFound()
    {
        var mine = await SeedPixBillAsync(TenantA, PdfComposerTests.PdfWithPages("A", 1));
        var foreign = await SeedPixBillAsync(TenantB, PdfComposerTests.PdfWithPages("B", 1));

        var response = await ExportAsync([mine, foreign], "All", includeReceipts: false, "SinglePdf");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // Seleção vazia é recusada com BLP.BIL42.
    [Fact]
    public async Task Export_WithoutBills_ShouldReturnBadRequest_BIL42()
    {
        var response = await ExportAsync([], "All", includeReceipts: false, "SinglePdf");

        await AssertErrorAsync(response, "BLP.BIL42");
    }

    // Acima do teto de boletos por vez é recusado com BLP.BIL43, antes de ler qualquer coisa.
    [Fact]
    public async Task Export_AboveTheLimit_ShouldReturnBadRequest_BIL43()
    {
        var ids = Enumerable.Range(0, 51).Select(_ => BillId.From(Guid.CreateVersion7())).ToList();

        var response = await ExportAsync(ids, "All", includeReceipts: false, "SinglePdf");

        await AssertErrorAsync(response, "BLP.BIL43");
    }

    // Opção desconhecida é erro de entrada, não 500.
    [Fact]
    public async Task Export_WithAnUnknownOption_ShouldReturnBadRequest()
    {
        var bill = await SeedPixBillAsync(TenantA, PdfComposerTests.PdfWithPages("A", 1));

        var response = await ExportAsync([bill], "Everything", includeReceipts: false, "SinglePdf");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<PdfDocument> ExportPdfAsync(List<BillId> ids, string pages, bool includeReceipts)
    {
        var response = await ExportAsync(ids, pages, includeReceipts, "SinglePdf");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return PdfDocument.Open(await response.Content.ReadAsByteArrayAsync());
    }

    private Task<HttpResponseMessage> ExportAsync(List<BillId> ids, string pages, bool includeReceipts, string packaging)
        => _host.CreateClient().Authenticated().PostAsJsonAsync(
            new Uri($"/api/v1/{TestTenants.Primary}/bills/documents/export", UriKind.Relative),
            new
            {
                billIds = ids.ConvertAll(id => id.Value),
                pages,
                includeReceipts,
                packaging,
            });

    private static async Task AssertErrorAsync(HttpResponseMessage response, string errorId)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Equal(errorId, error?.Id);
    }

    private static int PageCountOf(ZipArchiveEntry entry)
    {
        using var stream = entry.Open();
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        using var document = PdfDocument.Open(buffer.ToArray());
        return document.NumberOfPages;
    }

    private async Task<BillId> SeedPixBillAsync(
        TenantId tenantId,
        byte[] document,
        string contentType = "application/pdf",
        string? messageId = null)
    {
        var key = await _storage.StoreAsync(tenantId, "boleto", contentType, document, default);

        return await ExecuteDbContextAsync(async db =>
        {
            var origin = BillOrigin.Create(
                BillSourceKind.Mailbox,
                OccurredAt,
                sourceId: SourceId,
                senderAddress: "faturas@sabesp.com.br",
                externalMessageId: messageId,
                storageKey: key);

            var bill = Bill.Capture(tenantId, [PaymentInstrument.FromPixQr(PixPayload.Parse(StaticPix))], origin, OccurredAt);

            await db.Bills.AddAsync(bill);
            await db.SaveEntitiesAsync();
            return bill.Id;
        });
    }

    private Task<BillId> SeedManualBillAsync(string line)
        => ExecuteDbContextAsync(async db =>
        {
            var bill = Bill.Capture(
                TenantA,
                [PaymentInstrument.FromBarcode(DigitableLine.Parse(line, OccurredAt))],
                BillOrigin.Create(BillSourceKind.ManualUpload, OccurredAt),
                OccurredAt);

            await db.Bills.AddAsync(bill);
            await db.SaveEntitiesAsync();
            return bill.Id;
        });

    private async Task SeedPaidOrderAsync(BillId billId, byte[]? receipt)
    {
        var key = receipt is null
            ? null
            : await _storage.StoreAsync(TenantA, "comprovante.pdf", "application/pdf", receipt, default);

        await ExecuteDbContextAsync(async db =>
        {
            var order = PaymentOrder.Draft(
                TenantA, billId, PaymentRail.Pix, new DateOnly(2026, 8, 20), new Money(1500m, Currency.BRL), OccurredAt);
            order.MarkSubmitted("pay_export_0001", new DateOnly(2026, 8, 20), null, null, OccurredAt);
            order.ApplyProviderStatus(
                PaymentOrderStatus.Paid, new DateOnly(2026, 8, 20), feeAmount: null, null, OccurredAt, OccurredAt);

            if (key is not null)
                order.AttachReceipt(key, OccurredAt);

            order.PullDomainEvents();
            await db.PaymentOrders.AddAsync(order);
            await db.SaveEntitiesAsync();
        });
    }

    private Task SeedCapturedMessageAsync(string messageId, string subject)
        => ExecuteDbContextAsync(async db =>
        {
            await db.CapturedMessages.AddAsync(CapturedMessage.Register(
                TenantA,
                CaptureSourceId.From(SourceId),
                messageId,
                "faturas@sabesp.com.br",
                subject,
                OccurredAt,
                OccurredAt,
                [("att-1", "fatura.pdf", "application/pdf")]));

            await db.SaveEntitiesAsync();
        });

    private Task SeedPayerProfileAsync()
        => ExecuteDbContextAsync(async db =>
        {
            await db.PayerProfiles.AddAsync(PayerProfile.Register(
                TenantA,
                PayerKind.Company,
                "RUFINO EMPREITEIRA LTDA",
                TaxId.Parse(EncryptedPdfFixture.TenantCnpj),
                OccurredAt));

            await db.SaveEntitiesAsync();
        });
}
