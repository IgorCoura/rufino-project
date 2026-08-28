namespace BillPayment.IntegrationTests.CaptureItems;

using BillPayment.Application.CaptureItems.Commands;
using BillPayment.Application.Mediator;
using BillPayment.Application.Queries.CaptureItems;
using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.Extraction;
using BillPayment.Domain.Payees;
using BillPayment.Domain.PayerProfiles;
using BillPayment.Domain.Secrets;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using BillPayment.Domain.TrustedOrigins;
using BillPayment.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

/// <summary>
/// O degrau 3 da cascata: o extrator de visão propõe, o funil determinístico dispõe.
/// </summary>
/// <remarks>
/// <strong>O teste que mais importa aqui não é o que prova que a visão resolve um boleto</strong>
/// — é o que prova que uma linha digitável alucinada é barrada e o item NÃO vira boleto. Se
/// alguém um dia ligar a saída do modelo direto na <c>Bill</c>, é ele que quebra (ADR-011).
/// </remarks>
[Collection(nameof(IntegrationTestCollection))]
public sealed class VisionExtractionTests : BaseIntegrationTest
{
    private static readonly TenantId Tenant = TenantId.From(new Guid("0195a1f0-0000-7000-8000-000000000001"));
    private static readonly UserId Decider = UserId.From(new Guid("0195a1f0-0000-7000-8000-0000000000a1"));
    private static readonly DateTime OccurredAt = new(2026, 8, 11, 9, 0, 0, DateTimeKind.Utc);

    private const string ValidBankSlip = "34191234546789012345767890123457314880000061507";

    /// <summary>Linha de arrecadação válida — natureza DIFERENTE da de cobrança acima.</summary>
    private const string ValidUtility = "826600000010224812345672890123456786901234567898";
    private const string KnownSender = "faturas@fornecedor.com.br";
    private const string UnknownSender = "marketing@desconhecido.com";

    private readonly IServiceProvider _services;
    private readonly FakeDocumentIntelligence _vision;

    public VisionExtractionTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
        _services = factory.WithCaptureChain().Services;
        _vision = _services.GetRequiredService<FakeDocumentIntelligence>();
    }

    // A visão resolve o que o determinístico não resolveu, e o degrau fica registrado — é o que
    // permite medir quanto da cascata está custando dinheiro.
    [Fact]
    public async Task Process_WhenVisionFindsAValidLine_ShouldParseAndRecordTheVisionStep()
    {
        await SeedTrustedOriginAsync();
        _vision.Result = ExtractedDocument.From(digitableLineCandidates: [ValidBankSlip]);

        var itemId = await SeedAsync(KnownSender, "Boleto de agosto", PdfWith("documento sem linha nenhuma"));
        var result = await ProcessAsync(itemId);

        Assert.Equal("Parse", result.Decision);

        var stored = await LoadAsync(itemId);
        // `Parsed` virou estado de PASSAGEM na 2.6: a escada de roteamento roda logo depois da
        // cascata, e sem `PayerProfile` cadastrado ela não tem contra o quê comparar — o item
        // segue para a fila de reivindicação. O que este teste mede é o degrau da cascata, e
        // esse não mudou.
        Assert.Same(CaptureItemStatus.Unrouted, stored!.Status);
        Assert.Same(ExtractionMethod.Vision, stored.Extraction);
    }

    // TESTE ÂNCORA DO ADR-011. O modelo devolve uma linha com um dígito trocado — o erro típico
    // de leitura — e o dígito verificador a barra. O item continua na quarentena, e nenhum
    // boleto nasce de um número inventado.
    [Fact]
    public async Task Process_WhenVisionHallucinatesALine_ShouldNotProduceABill()
    {
        await SeedTrustedOriginAsync();
        _vision.Result = ExtractedDocument.From(digitableLineCandidates: [Tamper(ValidBankSlip)]);

        var itemId = await SeedAsync(KnownSender, "Boleto de agosto", PdfWith("documento sem linha nenhuma"));
        var result = await ProcessAsync(itemId);

        Assert.Equal("Quarantine", result.Decision);

        var stored = await LoadAsync(itemId);
        Assert.Same(CaptureItemStatus.Unrecognized, stored!.Status);
        Assert.Null(stored.Extraction);
    }

    // Sem remetente conhecido e sem sinal de cobrança, o extrator NÃO é chamado — é o portão que
    // mantém o gasto proporcional: dos 404 anexos medidos, 250 não tinham sinal algum.
    [Fact]
    public async Task Process_WithoutAnyBillingSignal_ShouldNotSpendTheExtractor()
    {
        var itemId = await SeedAsync(UnknownSender, "APRESENTAÇÃO DE CABOS ELETRICOS", PdfWith("catalogo"));

        await ProcessAsync(itemId);

        Assert.Equal(0, _vision.CallCount);
    }

    // Sinal de cobrança no assunto vale a chamada mesmo de remetente desconhecido — inclusive
    // quando não contém "boleto" nem "conta", que é o caso real de "Sua fatura chegou".
    [Fact]
    public async Task Process_WithABillingSignal_ShouldSpendEvenForAnUnknownSender()
    {
        var itemId = await SeedAsync(UnknownSender, "Sua fatura chegou", PdfWith("documento"));

        await ProcessAsync(itemId);

        Assert.Equal(1, _vision.CallCount);
    }

    // Anexo em IMAGEM chega ao extrator como imagem. A cascata determinística só abre PDF, e a
    // medição de 2026-08-11 recusou 12 anexos com not_a_pdf — baixados e nunca lidos.
    [Fact]
    public async Task Process_WithAnImageAttachment_ShouldStillReachTheExtractor()
    {
        await SeedTrustedOriginAsync();
        _vision.Result = ExtractedDocument.From(digitableLineCandidates: [ValidBankSlip]);

        var itemId = await SeedAsync(KnownSender, "Boleto", [1, 2, 3, 4], contentType: "image/png");
        var result = await ProcessAsync(itemId);

        Assert.Equal("Parse", result.Decision);
        Assert.Contains("image/png", _vision.ReceivedMediaTypes, StringComparer.Ordinal);
    }

    // As dicas levam o que o PRÓPRIO tenant cadastrou — documentos do pagador e nomes de
    // beneficiários. É escolha consciente do doc 10, e nada de outro tenant entra aqui.
    [Fact]
    public async Task Process_ShouldSendOnlyOwnTenantDataAsHints()
    {
        await SeedTrustedOriginAsync();
        await SeedPayerProfileAsync();
        await SeedPayeeAsync();

        var itemId = await SeedAsync(KnownSender, "Boleto", PdfWith("documento"));
        await ProcessAsync(itemId);

        var hints = _vision.LastHints;
        Assert.NotNull(hints);
        Assert.Contains("11222333000181", hints.PayerTaxIds, StringComparer.Ordinal);
        Assert.Contains("FORNECEDOR CONHECIDO SA", hints.KnownPayeeNames, StringComparer.Ordinal);
        Assert.Equal(KnownSender, hints.SenderAddress);
    }

    // Extrator desligado não muda nada: a cascata termina no determinístico, como antes da 2.4.
    [Fact]
    public async Task Process_WhenTheExtractorIsDisabled_ShouldFallBackToTheDeterministicOutcome()
    {
        await SeedTrustedOriginAsync();
        _vision.IsEnabled = false;

        var itemId = await SeedAsync(KnownSender, "Boleto", PdfWith("documento sem linha"));
        var result = await ProcessAsync(itemId);

        Assert.Equal("Quarantine", result.Decision);
        Assert.Equal(0, _vision.CallCount);
    }

    // TESTE DE REGRESSÃO. O tipo vem do que o provedor DECLAROU, nunca da chave do artefato —
    // que no Graph é opaca. Sem tipo declarado o extrator não é chamado, em vez de receber o
    // arquivo rotulado como PDF por chute e o provedor recusar.
    [Fact]
    public async Task Process_WithoutADeclaredContentType_ShouldNotCallTheExtractor()
    {
        await SeedTrustedOriginAsync();

        var itemId = await SeedAsync(KnownSender, "Boleto", PdfWith("documento"), contentType: null);
        await ProcessAsync(itemId);

        Assert.Equal(0, _vision.CallCount);
    }

    // O portão de gasto examina o NOME do arquivo, não a chave opaca — senão o sinal de cobrança
    // no anexo nunca casaria com nada.
    [Fact]
    public async Task Process_WithABillingSignalInTheFileName_ShouldSpend()
    {
        var itemId = await SeedAsync(
            UnknownSender, "ENC:", PdfWith("documento"), fileName: "boleto-agosto.pdf");

        await ProcessAsync(itemId);

        Assert.Equal(1, _vision.CallCount);
    }

    /// <summary>Troca um dígito do meio — a alucinação típica, que "parece" a linha certa.</summary>
    private static string Tamper(string line)
    {
        var digits = line.ToCharArray();
        var middle = digits.Length / 2;
        digits[middle] = digits[middle] == '9' ? '8' : (char)(digits[middle] + 1);

        return new string(digits);
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

    /// <summary>
    /// As DUAS passagens, como os dois workers fazem em produção.
    /// </summary>
    /// <remarks>
    /// <summary>O CNPJ do tenant, para a escada promover e o boleto chegar a ser criado.</summary>
    private const string TenantCnpj = "11222333000181";

    // TESTE ÂNCORA DE REGRESSÃO (2026-08-26): um PDF com dois boletos de naturezas diferentes
    // faz o domínio recusar (BLP.BIL15) e a recusa é DEFINITIVA — o item para em Failed em vez
    // de voltar à fila. Em produção esse caso rodou 1.709 vezes em quatro artefatos, cada um
    // ocupando permanentemente uma das dez vagas do lote.
    [Fact]
    public async Task Process_WhenTheDomainRefusesTheDocument_ShouldStopInsteadOfRetryingForever()
    {
        var itemId = await SeedMixedKindsDocumentAsync();

        // A faixa rápida cede a vez (todo candidato passa pela visão desde 2026-08-27); a
        // recusa do domínio acontece na passagem da faixa de visão, onde a promoção roda.
        await SendAsync(itemId, visionLane: false);
        var failure = await Assert.ThrowsAsync<DomainException>(() => SendAsync(itemId, visionLane: true));
        Assert.Equal("BLP.BIL15", failure.Id);

        await RecordFailureAsync(itemId, permanent: true);

        var item = await LoadAsync(itemId);
        Assert.Equal(CaptureItemStatus.Failed, item!.Status);
        Assert.Equal(CaptureItem.REASON_PROCESSING_REJECTED, item.Reason);
        Assert.Contains("BLP.BIL15", item.LastError, StringComparison.Ordinal);
    }

    // E, tendo parado, ele sai da fila: é isso que devolve as vagas do lote aos outros artefatos.
    [Fact]
    public async Task Process_WhenTheItemGaveUp_ShouldLeaveTheQueue()
    {
        var itemId = await SeedMixedKindsDocumentAsync();

        await SendAsync(itemId, visionLane: false);
        await Assert.ThrowsAsync<DomainException>(() => SendAsync(itemId, visionLane: true));
        await RecordFailureAsync(itemId, permanent: true);

        using var scope = _services.CreateScope();
        var queries = scope.ServiceProvider.GetRequiredService<ICaptureItemWorkQueries>();

        var fila = await queries.ClaimPendingAsync(10, DateTimeOffset.UtcNow.AddMinutes(5));
        Assert.DoesNotContain(fila, p => p.CaptureItemId == itemId.Value);
    }

    // TESTE-ANCORA da correcao de 2026-08-27: provedor fora do ar NAO manda o documento para a
    // quarentena. Antes disso um 503 chegava ao chamador como "nao achei boleto" — indistinguivel
    // de um documento que de fato nao e boleto — e o artefato era descartado sem NENHUMA
    // retentativa, embora a fila ja soubesse retentar. Medido: 24 documentos por 503 num dia.
    [Fact]
    public async Task Process_WhenTheExtractorIsUnavailable_ShouldNotQuarantineTheDocument()
    {
        await SeedTrustedOriginAsync();
        var itemId = await SeedAsync(KnownSender, "Boleto", PdfWith("documento sem linha nenhuma"));

        _vision.Outcome = ExtractionStatus.Unavailable;

        var failure = await Assert.ThrowsAsync<DomainException>(() => ProcessAsync(itemId));

        // BLP.EXT08 e o sinal que devolve o item a fila — e a fila e quem retenta.
        Assert.Equal("BLP.EXT08", failure.Id);

        var item = await LoadAsync(itemId);
        Assert.NotEqual(CaptureItemStatus.Unrecognized, item!.Status);
    }

    // A CONTRAPROVA, e ela e metade do valor do teste acima: o modelo que respondeu e nao achou
    // nada continua mandando o artefato para a quarentena. Sem ela, "nunca descartar" passaria.
    [Fact]
    public async Task Process_WhenTheExtractorAnswersWithNothing_ShouldStillQuarantine()
    {
        await SeedTrustedOriginAsync();
        var itemId = await SeedAsync(KnownSender, "Boleto", PdfWith("documento sem linha nenhuma"));

        _vision.Outcome = ExtractionStatus.Resolved;
        _vision.Result = ExtractedDocument.Empty;

        await ProcessAsync(itemId);

        var item = await LoadAsync(itemId);
        Assert.Equal(CaptureItemStatus.Unrecognized, item!.Status);
    }

    // Recusa do provedor (400) e fato sobre o ARTEFATO, nao sobre a rede: repetir devolveria a
    // mesma recusa, entao ela segue o caminho da quarentena e nao gasta o teto de tentativas.
    [Fact]
    public async Task Process_WhenTheExtractorRejectsTheArtifact_ShouldQuarantineWithoutRetrying()
    {
        await SeedTrustedOriginAsync();
        var itemId = await SeedAsync(KnownSender, "Boleto", PdfWith("documento sem linha nenhuma"));

        _vision.Outcome = ExtractionStatus.Rejected;

        await ProcessAsync(itemId);

        var item = await LoadAsync(itemId);
        Assert.Equal(CaptureItemStatus.Unrecognized, item!.Status);
        Assert.Equal(0, item.ProcessingAttempts);
    }

    // Falha passageira NÃO desiste na primeira: retentar é o que resolve rede instável. O item
    // fica na fila, com a tentativa contada e a próxima adiada — e é o adiamento que impede o
    // laço apertado contra um provedor que já está com problema.
    [Fact]
    public async Task Process_WhenTheFailureIsTransient_ShouldKeepTheItemForAnotherAttempt()
    {
        await SeedTrustedOriginAsync();
        var itemId = await SeedAsync(KnownSender, "Boleto", PdfWith(ValidBankSlip));

        await ClaimAsync();
        await RecordFailureAsync(itemId, permanent: false);

        var item = await LoadAsync(itemId);
        Assert.Equal(CaptureItemStatus.Received, item!.Status);
        Assert.Equal(1, item.ProcessingAttempts);
        Assert.True(item.LeaseExpiresAt > DateTime.UtcNow);
    }

    // Esgotado o teto de tentativas, até a falha passageira desiste — senão um item que nunca
    // fecha ocuparia a vaga indefinidamente, que é a outra metade do bloqueio de fila.
    [Fact]
    public async Task Process_WhenTransientFailuresExhaustTheBudget_ShouldGiveUp()
    {
        await SeedTrustedOriginAsync();
        var itemId = await SeedAsync(KnownSender, "Boleto", PdfWith(ValidBankSlip));

        // O orçamento é gasto pelas reivindicações, e cada uma só ocorre depois de a espera da
        // anterior vencer. Adiantar o contador equivale a essas esperas terem passado.
        await ExhaustAttemptsAsync(itemId);
        await RecordFailureAsync(itemId, permanent: false);

        var item = await LoadAsync(itemId);
        Assert.Equal(CaptureItemStatus.Failed, item!.Status);
        Assert.Equal(CaptureItem.REASON_ATTEMPTS_EXHAUSTED, item.Reason);
    }

    /// <summary>Um artefato que a escada promove e cujo conteúdo o domínio recusa.</summary>
    private async Task<CaptureItemId> SeedMixedKindsDocumentAsync()
    {
        await SeedTrustedOriginAsync();
        await SeedPayerProfileAsync();

        // As duas linhas são válidas e de naturezas OPOSTAS — cobrança e arrecadação —, que é o
        // documento real que travou a fila: dois boletos digitalizados num PDF só.
        return await SeedAsync(
            KnownSender,
            "Dois boletos num PDF só",
            PdfWith($"Pagador {TenantCnpj}", ValidBankSlip, ValidUtility));
    }

    private async Task ClaimAsync()
    {
        using var scope = _services.CreateScope();
        var queries = scope.ServiceProvider.GetRequiredService<ICaptureItemWorkQueries>();

        await queries.ClaimPendingAsync(10, DateTimeOffset.UtcNow.AddMinutes(5));
    }

    private Task ExhaustAttemptsAsync(CaptureItemId itemId)
        => ExecuteDbContextAsync(async db =>
        {
            var item = await db.CaptureItems.FirstAsync(i => i.Id == itemId);
            for (var i = 0; i < 3; i++)
                item.Lease(DateTime.UtcNow.AddMinutes(5), DateTime.UtcNow);

            await db.SaveEntitiesAsync();
        });

    private async Task RecordFailureAsync(CaptureItemId itemId, bool permanent)
    {
        using var scope = _services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        await mediator.Send(new RecordCaptureItemFailureCommand(
            Tenant.Value,
            itemId.Value,
            permanent
                ? "[BLP.BIL15] O documento traz códigos de barras de naturezas diferentes."
                : "TimeoutException: rede",
            permanent));
    }

    /// <summary>Roda o artefato pela cascata, atravessando as duas faixas quando preciso.</summary>
    /// <remarks>
    /// A faixa rápida não gasta cota de IA: ao chegar no degrau 3 ela põe o item em
    /// <c>VisionPending</c> e devolve o worker para os outros. Quem chama a IA é a faixa lenta.
    /// Um teste que só rodasse a primeira passagem descreveria metade do fluxo.
    /// </remarks>
    private async Task<ProcessCaptureItemResponse> ProcessAsync(CaptureItemId itemId)
    {
        var first = await SendAsync(itemId, visionLane: false);

        return first.Decision == "VisionPending"
            ? await SendAsync(itemId, visionLane: true)
            : first;
    }

    private async Task<ProcessCaptureItemResponse> SendAsync(CaptureItemId itemId, bool visionLane)
    {
        using var scope = _services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        return await mediator.Send(
            new ProcessCaptureItemCommand(Tenant.Value, itemId.Value, VisionLane: visionLane));
    }

    private Task<CaptureItem?> LoadAsync(CaptureItemId itemId)
        => ExecuteDbContextAsync(db => db.CaptureItems.AsNoTracking().FirstOrDefaultAsync(i => i.Id == itemId));

    private Task SeedTrustedOriginAsync()
        => ExecuteDbContextAsync(async db =>
        {
            var origin = TrustedOrigin.Register(
                Tenant, OriginKind.EmailAddress, KnownSender, TrustDecision.Trusted, Decider, note: null, OccurredAt);

            await db.TrustedOrigins.AddAsync(origin);
            await db.SaveEntitiesAsync();
        });

    private Task SeedPayerProfileAsync()
        => ExecuteDbContextAsync(async db =>
        {
            var profile = PayerProfile.Register(
                Tenant, PayerKind.Company, "EMPRESA DE TESTE LTDA", "11222333000181", OccurredAt);

            await db.PayerProfiles.AddAsync(profile);
            await db.SaveEntitiesAsync();
        });

    private Task SeedPayeeAsync()
        => ExecuteDbContextAsync(async db =>
        {
            var payee = Payee.Register(
                Tenant, "FORNECEDOR CONHECIDO SA", "11444777000161", AmountPolicyKind.Unbounded,
                expectedAmount: null, tolerancePercent: null, minAmount: null, maxAmount: null, OccurredAt);

            await db.Payees.AddAsync(payee);
            await db.SaveEntitiesAsync();
        });

    private async Task<CaptureItemId> SeedAsync(
        string sender,
        string subject,
        byte[] content,
        string? contentType = "application/pdf",
        string? fileName = null)
    {
        var sourceId = await ExecuteDbContextAsync(async db =>
        {
            var source = CaptureSource.Connect(
                Tenant,
                CaptureSourceKind.MicrosoftGraphMailbox,
                "Caixa",
                "contas@empresa.com.br",
                CredentialRef.ForLocalVault(Guid.CreateVersion7()),
                OccurredAt);

            await db.CaptureSources.AddAsync(source);
            await db.SaveEntitiesAsync();
            return source.Id;
        });

        // Chave OPACA, como a do Graph: sem extensão, para nenhum teste passar por acidente
        // deduzindo o tipo dela.
        var artifactKey = $"AAMkAGI2{Guid.CreateVersion7():N}";
        _services.GetRequiredService<FakeMailboxReader>().Artifacts[artifactKey] = content;

        return await ExecuteDbContextAsync(async db =>
        {
            var item = CaptureItem.Ingest(
                Tenant, sourceId, "AAMkAGI2THVSAAA=", artifactKey, sender, subject, OccurredAt, OccurredAt,
                contentType, fileName);

            await db.CaptureItems.AddAsync(item);
            await db.SaveEntitiesAsync();
            return item.Id;
        });
    }

    // A faixa rápida NÃO gasta cota de IA: ao precisar do extrator, ela cede a vez e devolve o
    // worker para os outros itens. É o que impede um artefato de 5 s de segurar um lote cujo item
    // mediano leva 150 ms — medido em 2026-08-26: 27% dos itens consumindo 86% do tempo.
    [Fact]
    public async Task Process_OnTheFastLane_WhenVisionIsNeeded_ShouldDeferInsteadOfCalling()
    {
        await SeedTrustedOriginAsync();
        _vision.Result = ExtractedDocument.From(digitableLineCandidates: [ValidBankSlip]);

        var itemId = await SeedAsync(KnownSender, "Boleto de agosto", PdfWith("documento sem linha nenhuma"));

        var result = await SendAsync(itemId, visionLane: false);

        Assert.Equal("VisionPending", result.Decision);
        Assert.Same(CaptureItemStatus.VisionPending, (await LoadAsync(itemId))!.Status);

        // CONTRAPROVA: o extrator não foi chamado. Sem ela o teste passaria mesmo se a faixa
        // rápida tivesse gastado a cota e só depois marcado o item.
        Assert.Equal(0, _vision.CallCount);
    }

    // A faixa lenta retoma de onde a rápida parou e conclui o processamento.
    [Fact]
    public async Task Process_OnTheVisionLane_ShouldResumeAndFinish()
    {
        await SeedTrustedOriginAsync();
        _vision.Result = ExtractedDocument.From(digitableLineCandidates: [ValidBankSlip]);

        var itemId = await SeedAsync(KnownSender, "Boleto de agosto", PdfWith("documento sem linha nenhuma"));
        await SendAsync(itemId, visionLane: false);

        var result = await SendAsync(itemId, visionLane: true);

        Assert.Equal("Parse", result.Decision);
        Assert.Equal(1, _vision.CallCount);
        Assert.Same(ExtractionMethod.Vision, (await LoadAsync(itemId))!.Extraction);
    }

    // Desde 2026-08-27 TODO candidato a boleto passa pela faixa de visão: o que o determinístico
    // resolveu cede a vez na faixa rápida e ganha o retrato de enriquecimento na de visão — e a
    // captura nunca é refém da IA: modelo devolvendo vazio, o boleto nasce mesmo assim, sem retrato.
    [Fact]
    public async Task Process_WhenTheDeterministicCascadeResolves_ShouldYieldToVisionForEnrichment()
    {
        await SeedTrustedOriginAsync();

        var itemId = await SeedAsync(KnownSender, "Boleto de agosto", PdfWith(ValidBankSlip));

        var first = await SendAsync(itemId, visionLane: false);
        Assert.Equal("VisionPending", first.Decision);
        Assert.Equal(0, _vision.CallCount);

        var second = await SendAsync(itemId, visionLane: true);

        Assert.Equal("Parse", second.Decision);
        Assert.Equal(1, _vision.CallCount);
    }

    // A fila de visão devolve só o que está esperando por ela; o item comum não entra nela.
    [Fact]
    public async Task ListPendingVision_ShouldReturnOnlyTheDeferredItems()
    {
        await SeedTrustedOriginAsync();
        _vision.Result = ExtractedDocument.From(digitableLineCandidates: [ValidBankSlip]);

        var deferred = await SeedAsync(KnownSender, "Precisa de IA", PdfWith("sem linha nenhuma"));
        await SendAsync(deferred, visionLane: false);

        using var scope = _services.CreateScope();
        var queries = scope.ServiceProvider.GetRequiredService<ICaptureItemWorkQueries>();

        var fila = await queries.ClaimPendingVisionAsync(10, DateTimeOffset.UtcNow.AddMinutes(15));

        Assert.Equal(deferred.Value, Assert.Single(fila).CaptureItemId);
        Assert.DoesNotContain(
            await queries.ClaimPendingAsync(10, DateTimeOffset.UtcNow.AddMinutes(5)),
            p => p.CaptureItemId == deferred.Value);
    }
}
