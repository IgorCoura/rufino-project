namespace BillPayment.IntegrationTests.Bills;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BillPayment.Domain.Bills;
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
/// A importação manual <strong>como a tela a faz</strong>: só os dígitos, só o arquivo, ou os
/// dois.
/// </summary>
/// <remarks>
/// <para>
/// Os testes de importação que já existiam sempre mandavam <c>StorageKey</c> junto com
/// <c>ManualUpload</c> — inclusive o que se chama
/// <c>PostImport_FromManualUpload_ShouldBeAcceptedWithoutASource</c>, que afirma sobre a ausência
/// da <em>fonte</em>. Nenhum deles montava o corpo que o aplicativo monta, e é por isso que a
/// recusa de toda importação feita pela tela sobreviveu com a suíte verde.
/// </para>
/// <para>
/// O host da cadeia de captura é o que traz o armazenamento em memória: o caminho com arquivo
/// grava no balde, e a fábrica compartilhada mantém o substituto que falha em toda escrita.
/// </para>
/// </remarks>
[Collection(nameof(IntegrationTestCollection))]
public sealed class ImportBillManualTests : BaseIntegrationTest
{
    private static readonly Guid TenantId = TestTenants.Primary;
    private static readonly DateTime ReceivedAt = new(2026, 8, 28, 9, 0, 0, DateTimeKind.Utc);

    // Instrumentos sintéticos com DVs e CRC corretos — instrumento real não entra no repositório.
    private const string BankSlipLine = "34191234546789012345767890123457314880000061507";
    private const string OtherBankSlipLine = "03399876534321098765743210987657414930000140980";

    /// <summary>
    /// Documento fiscal do tenant — diferente do beneficiário impresso, senão a verificação 8
    /// bloquearia por "o beneficiário é o próprio pagador" antes de chegar ao pagador.
    /// </summary>
    private const string TenantCnpj = "45678901000175";

    private const string DynamicPix =
        "00020101021226760014br.gov.bcb.pix2554pix.example.com/qr/v2/9d36b84fc70b478fb95c12729b90ca255204000053039865802BR5912EDP TESTE SA6007TAUBATE62120508TXID00026304E47A";

    private readonly WebApplicationFactory<Program> _host;

    public ImportBillManualTests(IntegrationTestWebAppFactory factory) : base(factory)
        => _host = factory.WithCaptureChain();

    // TESTE DE REGRESSÃO — o bug relatado: colar o código Pix na tela e clicar em Importar
    // devolvia "A origem precisa de ao menos um identificador" (BLP.BIL12). O aplicativo manda
    // só digitableLine/pixPayload/sourceKind/receivedAt, e a guarda de identificador, escrita
    // para a captura automática, recusava todas as importações manuais.
    [Fact]
    public async Task PostImport_WithOnlyThePixPayload_ShouldBeAccepted()
    {
        var response = await PostJsonAsync(ManualRequest(pixPayload: DynamicPix));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ImportBillResponseContract>();
        Assert.Equal("Pix", body!.Rail);

        var bill = await LoadAsync(body.Id);
        Assert.Same(BillSourceKind.ManualUpload, bill!.Origin.SourceKind);
        Assert.Null(bill.Origin.StorageKey);
    }

    // A outra metade do mesmo bug: quem cola a linha digitável também não tem rastro a oferecer.
    [Fact]
    public async Task PostImport_WithOnlyTheDigitableLine_ShouldBeAccepted()
    {
        var response = await PostJsonAsync(ManualRequest(digitableLine: BankSlipLine));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ImportBillResponseContract>();
        Assert.Equal("Boleto", body!.Rail);
    }

    // TESTE ÂNCORA do anexo: só o arquivo, sem digitar nada. A cascata determinística tira a
    // linha do PDF e o documento fica guardado como evidência.
    [Fact]
    public async Task PostImport_WithOnlyTheDocument_ShouldReadItAndKeepTheFile()
    {
        var response = await PostMultipartAsync(PdfWith("Banco Itau", BankSlipLine));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ImportBillResponseContract>();
        var bill = await LoadAsync(body!.Id);

        Assert.Same(BillSourceKind.ManualUpload, bill!.Origin.SourceKind);
        Assert.NotNull(bill.Origin.StorageKey);
        Assert.StartsWith("sha256:", bill.Origin.ContentHash, StringComparison.Ordinal);
        Assert.Single(bill.Instruments);
    }

    // O arquivo guardado põe o boleto na fila da leitura por IA — quem tem documento tem o que
    // ler, e é a mesma máquina que já serve o boleto vindo de caixa.
    [Fact]
    public async Task PostImport_WithADocument_ShouldQueueTheAiReading()
    {
        var response = await PostMultipartAsync(PdfWith("Banco Itau", BankSlipLine));
        var body = await response.Content.ReadFromJsonAsync<ImportBillResponseContract>();

        var bill = await LoadAsync(body!.Id);

        Assert.Same(ReadingStatus.Queued, bill!.ReadingState);
    }

    // Sem arquivo não há o que ler, e o boleto nasce dizendo isso em vez de anunciar uma fila.
    [Fact]
    public async Task PostImport_WithoutADocument_ShouldNotQueueTheAiReading()
    {
        var response = await PostJsonAsync(ManualRequest(digitableLine: BankSlipLine));
        var body = await response.Content.ReadFromJsonAsync<ImportBillResponseContract>();

        var bill = await LoadAsync(body!.Id);

        Assert.Same(ReadingStatus.NotApplicable, bill!.ReadingState);
    }

    // O caso normal de quem faz as duas coisas: colar a linha E anexar o PDF que a contém. Os
    // dois caminhos produzem a MESMA chave natural, e Bill.Capture recusa instrumento repetido —
    // sem a união por chave, a importação mais natural que existe estouraria com BLP.BIL09.
    [Fact]
    public async Task PostImport_WithTheSameLineTypedAndInTheFile_ShouldKeepASingleInstrument()
    {
        var response = await PostMultipartAsync(
            PdfWith("Banco Itau", BankSlipLine),
            digitableLine: BankSlipLine);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ImportBillResponseContract>();
        var bill = await LoadAsync(body!.Id);

        Assert.Single(bill!.Instruments);
    }

    // Instrumento digitado e instrumento do arquivo, diferentes, convivem: o documento pode
    // trazer o QR que a pessoa não colou, e é dele que sai o trilho preferencial.
    [Fact]
    public async Task PostImport_WithDifferentInstrumentsTypedAndInTheFile_ShouldKeepBoth()
    {
        var response = await PostMultipartAsync(
            PdfWith("Banco Itau", BankSlipLine),
            pixPayload: DynamicPix);

        var body = await response.Content.ReadFromJsonAsync<ImportBillResponseContract>();
        var bill = await LoadAsync(body!.Id);

        Assert.Equal(2, bill!.Instruments.Count);
        Assert.Equal("Pix", body.Rail);
    }

    // Arquivo ilegível SEM dígitos é recusado: o agregado exige instrumento, e a mensagem fala do
    // arquivo — quem anexou um papel precisa saber que ele não foi lido.
    [Fact]
    public async Task PostImport_WithAnUnreadableDocumentAndNoDigits_ShouldRefuse_BLP_BIL01()
    {
        var response = await PostMultipartAsync(PdfWith("Comunicado interno", "Sem boleto aqui"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Equal("BLP.BIL01", error!.Id);
    }

    // Arquivo ilegível COM dígitos é aceito: os dígitos dão o instrumento e o arquivo entra como
    // evidência. Recusar aqui jogaria fora o papel que o aprovador vai querer conferir.
    [Fact]
    public async Task PostImport_WithAnUnreadableDocumentAndTypedDigits_ShouldKeepTheFileAsEvidence()
    {
        var response = await PostMultipartAsync(
            PdfWith("Comprovante digitalizado"),
            digitableLine: BankSlipLine);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ImportBillResponseContract>();
        var bill = await LoadAsync(body!.Id);

        Assert.NotNull(bill!.Origin.StorageKey);
        Assert.Single(bill.Instruments);
    }

    // Tipo que a leitura não sabe abrir é recusado ANTES de gravar — o balde não acumula arquivo
    // que nunca poderá ser lido.
    [Fact]
    public async Task PostImport_WithAnUnsupportedDocumentType_ShouldRefuse_BLP_BIL29()
    {
        var response = await PostMultipartAsync(
            [1, 2, 3],
            fileName: "planilha.xlsx",
            contentType: "application/vnd.ms-excel");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Equal("BLP.BIL29", error!.Id);

        Assert.Equal(0, await ExecuteDbContextAsync(db => db.Bills.CountAsync()));
    }

    // Importação sem linha, sem Pix e sem arquivo é recusada pelo agregado — a tela já exige um
    // dos três, e esta é a mesma regra para quem chama a API direto.
    [Fact]
    public async Task PostImport_WithNothingAtAll_ShouldRefuse_BLP_BIL08()
    {
        var response = await PostJsonAsync(ManualRequest());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Equal("BLP.BIL08", error!.Id);
    }

    // Reimportar o mesmo boleto recusa com BLP.BIL02 — e o arquivo que já tinha sido gravado é
    // apagado. O balde está fora da transação do EF, então sem a limpeza cada tentativa repetida
    // deixaria um órfão; e reenviar o mesmo documento é o engano mais comum que existe.
    [Fact]
    public async Task PostImport_WhenTheBillAlreadyExists_ShouldRefuseAndNotLeaveTheFileBehind()
    {
        var storage = _host.Services.GetRequiredService<InMemoryAttachmentStorage>();

        var first = await PostMultipartAsync(PdfWith("Banco Itau", BankSlipLine));
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var kept = storage.Count;

        var second = await PostMultipartAsync(PdfWith("Banco Itau", BankSlipLine));

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);

        var error = await second.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Equal("BLP.BIL02", error!.Id);

        Assert.Equal(kept, storage.Count);
    }

    // A rota é a MESMA para os dois formatos, e quem escolhe a action é o Content-Type. Sem esta
    // afirmação, uma das duas poderia deixar de casar sem nenhum outro teste perceber.
    [Fact]
    public async Task PostImport_ShouldAnswerBothJsonAndMultipartOnTheSameRoute()
    {
        var json = await PostJsonAsync(ManualRequest(digitableLine: BankSlipLine));
        var multipart = await PostMultipartAsync(PdfWith("Banco Itau", OtherBankSlipLine));

        Assert.Equal(HttpStatusCode.OK, json.StatusCode);
        Assert.Equal(HttpStatusCode.OK, multipart.StatusCode);
        Assert.Equal(2, await ExecuteDbContextAsync(db => db.Bills.CountAsync()));
    }

    // O formulário sem receivedAt é recusado pelo binder: um DateTime ausente viraria o ano 1
    // gravado como data de recebimento, que é o oposto de evidência confiável.
    [Fact]
    public async Task PostImport_WithoutReceivedAtInTheForm_ShouldRefuse()
    {
        var file = new ByteArrayContent(PdfWith("Banco Itau", BankSlipLine));
        file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

        var form = new MultipartFormDataContent { { file, "file", "boleto.pdf" } };
        form.Add(new StringContent("ManualUpload"), "sourceKind");

        var response = await ClientFor().PostAsync(ImportRoute(), form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // SONDA: o package:http do Flutter escreve os CAMPOS antes do ARQUIVO; o teste acima monta na
    // ordem inversa. Se a ordem importar, e so ela difere entre o teste e o cliente real.
    [Fact]
    public async Task PostImport_WithFieldsBeforeTheFile_ShouldStillBindTheDocument()
    {
        var file = new ByteArrayContent(PdfWith("Banco Itau", BankSlipLine));
        file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

        var form = new MultipartFormDataContent();
        form.Add(new StringContent("ManualUpload"), "sourceKind");
        form.Add(new StringContent(ReceivedAt.ToString("O")), "receivedAt");
        form.Add(file, "file", "boleto.pdf");

        var response = await ClientFor().PostAsync(ImportRoute(), form);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ImportBillResponseContract>();
        var bill = await LoadAsync(body!.Id);

        Assert.NotNull(bill!.Origin.StorageKey);
    }

    // TESTE ÂNCORA (RUF101 - JUNDDIAMONDS, 2026-09-15). O defeito relatado: boleto importado à
    // mão sempre caía na verificação 8 com "não foi possível determinar de quem é este documento"
    // — sobre um PDF que imprime, na parte de baixo, "Pagador: RUFINO ... CNPJ/CPF: ...". A
    // cascata já lia o documento fiscal e o handler o descartava.
    [Fact]
    public async Task PostImport_WithThePayerPrintedInTheDocument_ShouldCarryItToTheBill()
    {
        await SeedPayerProfileAsync(TenantCnpj);

        var response = await PostMultipartAsync(BankSlipPdfNaming(TenantCnpj));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ImportBillResponseContract>();
        var bill = await LoadAsync(body!.Id);

        Assert.Equal(TenantCnpj, bill!.ExtractedPayer?.TaxId?.Value);
    }

    // CONTRAPROVA, e é ela que protege o boleto legítimo: o CNPJ que está no papel SEM rótulo de
    // pagador e fora do cadastro não vira pagador nenhum. Num boleto o documento do beneficiário
    // está impresso ao lado do do pagador — tomá-lo por pagador faria a verificação 8 enxergar
    // contradição com o cadastro e BLOQUEAR uma conta boa.
    [Fact]
    public async Task PostImport_WithOnlyThePayeeTaxIdPrinted_ShouldLeaveThePayerEmpty()
    {
        await SeedPayerProfileAsync(TenantCnpj);

        var response = await PostMultipartAsync(
            PdfWith("Banco Itau", BankSlipLine, "Beneficiario PADARIA SAO JOSE LTDA", "CNPJ 11.222.333/0001-81"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ImportBillResponseContract>();
        var bill = await LoadAsync(body!.Id);

        Assert.Null(bill!.ExtractedPayer);
    }

    // Sem cadastro fiscal ainda não há com o que casar, e é o rótulo que sustenta a leitura — o
    // mesmo critério da captura automática, que é por isso que ele mora no Domain Service.
    [Fact]
    public async Task PostImport_WithoutAPayerProfile_ShouldStillReadTheLabelledPayer()
    {
        var response = await PostMultipartAsync(BankSlipPdfNaming(TenantCnpj));

        var body = await response.Content.ReadFromJsonAsync<ImportBillResponseContract>();
        var bill = await LoadAsync(body!.Id);

        Assert.Equal(TenantCnpj, bill!.ExtractedPayer?.TaxId?.Value);
    }

    /// <summary>O boleto como o emissor o imprime: a linha em cima, o bloco do pagador embaixo.</summary>
    private static byte[] BankSlipPdfNaming(string payerTaxId)
        => PdfWith(
            "Banco Itau",
            BankSlipLine,
            "Beneficiario JUND DIAMOND C F LTDA CNPJ/CPF: 11.222.333/0001-81",
            "Pagador: RUFINO EMP.ELET.HIDR.LTDA CNPJ/CPF: " + payerTaxId);

    private Task SeedPayerProfileAsync(string taxId)
        => ExecuteDbContextAsync(async db =>
        {
            await db.PayerProfiles.AddAsync(PayerProfile.Register(
                Domain.SharedKernel.TenantId.From(TenantId),
                PayerKind.Company,
                "RUFINO EMPREITEIRA LTDA",
                TaxId.Parse(taxId),
                ReceivedAt));

            await db.SaveEntitiesAsync();
        });

    private static ImportBillRequest ManualRequest(
        string? digitableLine = null,
        string? pixPayload = null)
        => new(digitableLine, pixPayload, "ManualUpload", ReceivedAt);

    private static Uri ImportRoute() => new($"/api/v1/{TenantId}/bills/import", UriKind.Relative);

    private HttpClient ClientFor()
    {
        var client = _host.CreateClient().Authenticated();
        client.DefaultRequestHeaders.Add("x-requestid", Guid.CreateVersion7().ToString());
        return client;
    }

    private Task<HttpResponseMessage> PostJsonAsync(ImportBillRequest request)
        => ClientFor().PostAsJsonAsync(ImportRoute(), request);

    private Task<HttpResponseMessage> PostMultipartAsync(
        byte[] bytes,
        string? digitableLine = null,
        string? pixPayload = null,
        string fileName = "boleto.pdf",
        string contentType = "application/pdf")
    {
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        // O nome da parte tem de ser "file": é assim que o parâmetro da action se chama, e o
        // binding do IFormFile casa por nome.
        var form = new MultipartFormDataContent { { file, "file", fileName } };
        form.Add(new StringContent("ManualUpload"), "sourceKind");
        form.Add(new StringContent(ReceivedAt.ToString("O")), "receivedAt");

        if (digitableLine is not null)
            form.Add(new StringContent(digitableLine), "digitableLine");

        if (pixPayload is not null)
            form.Add(new StringContent(pixPayload), "pixPayload");

        return ClientFor().PostAsync(ImportRoute(), form);
    }

    private Task<Bill?> LoadAsync(Guid id)
        => ExecuteDbContextAsync(db => db.Bills
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == BillId.From(id)));

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
