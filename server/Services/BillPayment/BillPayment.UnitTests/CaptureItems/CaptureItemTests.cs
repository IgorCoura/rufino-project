namespace BillPayment.UnitTests.CaptureItems;

using BillPayment.Domain.Bills;
using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using BillPayment.UnitTests.CaptureItems.Mothers;

public class CaptureItemTests
{
    private static readonly DateTime Later = CaptureItemMother.DefaultOccurredAt.AddMinutes(10);

    // Ingerir um artefato guarda a procedência e o deixa em Received, antes de qualquer leitura.
    [Fact]
    public void Ingest_ShouldStoreProvenanceAndStartInReceived()
    {
        var item = CaptureItemMother.Ingest();

        Assert.Same(CaptureItemStatus.Received, item.Status);
        Assert.Equal(CaptureItemMother.DefaultMessageId, item.ExternalMessageId);
        Assert.Equal(CaptureItemMother.DefaultArtifactKey, item.ArtifactKey);
        Assert.Equal(CaptureItemMother.DefaultSender, item.Sender);
        Assert.Equal(CaptureItemMother.DefaultReceivedAt, item.ReceivedAt);
        Assert.Equal(CaptureItemMother.DefaultSource, item.SourceId);
        Assert.Null(item.StorageKey);
        Assert.Null(item.BillId);
        Assert.Null(item.Routing);
    }

    // O remetente é normalizado, para casar com o TrustedOrigin que foi cadastrado.
    [Fact]
    public void Ingest_WithUnnormalizedSender_ShouldStoreNormalized()
    {
        var item = CaptureItemMother.Ingest(sender: "  Faturas@ENEL.com.BR ");

        Assert.Equal(CaptureItemMother.DefaultSender, item.Sender);
    }

    // Dois anexos da mesma mensagem são dois itens distintos — a chave inclui o artefato.
    [Fact]
    public void Ingest_SameMessageDifferentArtifacts_ShouldProduceDistinctItems()
    {
        var primeiro = CaptureItemMother.Ingest(artifactKey: "boleto-1.pdf");
        var segundo = CaptureItemMother.Ingest(artifactKey: "boleto-2.pdf");

        Assert.NotEqual(primeiro.Id, segundo.Id);
        Assert.Equal(primeiro.ExternalMessageId, segundo.ExternalMessageId);
        Assert.NotEqual(primeiro.ArtifactKey, segundo.ArtifactKey);
    }

    // Sem id de mensagem no provedor não há idempotência de ingestão — BLP.CPI05.
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Ingest_WithBlankExternalMessageId_ShouldThrowBLP_CPI05(string messageId)
    {
        var exception = Assert.Throws<DomainException>(() => CaptureItemMother.Ingest(externalMessageId: messageId));

        Assert.Equal("BLP.CPI05", exception.Id);
    }

    // Sem chave de artefato os irmãos da mesma mensagem colidiriam — BLP.CPI06.
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Ingest_WithBlankArtifactKey_ShouldThrowBLP_CPI06(string artifactKey)
    {
        var exception = Assert.Throws<DomainException>(() => CaptureItemMother.Ingest(artifactKey: artifactKey));

        Assert.Equal("BLP.CPI06", exception.Id);
    }

    // Item capturado sempre pertence a uma fonte — BLP.CPI07.
    [Fact]
    public void Ingest_WithoutSource_ShouldThrowBLP_CPI07()
    {
        var exception = Assert.Throws<DomainException>(() =>
            CaptureItemMother.Ingest(sourceId: CaptureSourceId.Empty));

        Assert.Equal("BLP.CPI07", exception.Id);
    }

    // Assunto longo demais é truncado, não recusado — assunto não decide nada e perder o item seria pior.
    [Fact]
    public void Ingest_WithOverlongSubject_ShouldTruncateInsteadOfThrowing()
    {
        var item = CaptureItemMother.Ingest(subject: new string('s', CaptureItem.SUBJECT_MAX_LENGTH + 100));

        Assert.Equal(CaptureItem.SUBJECT_MAX_LENGTH, item.Subject!.Length);
    }

    // Guardar o artefato registra hash e local sem mudar o status — armazenar não é desfecho.
    [Fact]
    public void StoreArtifact_ShouldRecordHashAndKeyWithoutChangingStatus()
    {
        var item = CaptureItemMother.Stored();

        Assert.Same(CaptureItemStatus.Received, item.Status);
        Assert.Equal(CaptureItemMother.DefaultContentHash, item.ContentHash);
        Assert.Equal(CaptureItemMother.DefaultStorageKey, item.StorageKey);
    }

    // Processar antes de armazenar o artefato é recusado com BLP.CPI09.
    [Fact]
    public void MarkParsed_WithoutStoredArtifact_ShouldThrowBLP_CPI09()
    {
        var item = CaptureItemMother.Ingest();

        var exception = Assert.Throws<DomainException>(() =>
            item.MarkParsed(ExtractionMethod.EmbeddedText, unlockedBy: null, Later));

        Assert.Equal("BLP.CPI09", exception.Id);
    }

    // Extração registra por qual degrau da cascata o artefato foi resolvido.
    [Fact]
    public void MarkParsed_ShouldRecordExtractionMethod()
    {
        var item = CaptureItemMother.Stored();

        item.MarkParsed(ExtractionMethod.Vision, unlockedBy: null, Later);

        Assert.Same(CaptureItemStatus.Parsed, item.Status);
        Assert.Same(ExtractionMethod.Vision, item.Extraction);
        Assert.Equal(Later, item.UpdatedAt);
    }

    // UnlockedBy guarda QUAL campo derivou a senha do PDF — jamais a senha.
    [Fact]
    public void MarkParsed_WithUnlockedBy_ShouldRecordTheFieldNotThePassword()
    {
        var item = CaptureItemMother.Stored();

        item.MarkParsed(ExtractionMethod.EmbeddedText, "cnpj_first_5", Later);

        Assert.Equal("cnpj_first_5", item.UnlockedBy);
    }

    // PDF que nenhum candidato abriu fica em Locked com motivo estável.
    [Fact]
    public void MarkLocked_ShouldRecordPdfLockedReason()
    {
        var item = CaptureItemMother.Stored();

        item.MarkLocked(Later);

        Assert.Same(CaptureItemStatus.Locked, item.Status);
        Assert.Equal("pdf_locked", item.Reason);
    }

    // Item que aguarda download precisa registrar a URL de origem — BLP.CPI14.
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void MarkLinkPending_WithBlankUrl_ShouldThrowBLP_CPI14(string url)
    {
        var item = CaptureItemMother.Ingest();

        var exception = Assert.Throws<DomainException>(() => item.MarkLinkPending(url, Later));

        Assert.Equal("BLP.CPI14", exception.Id);
    }

    // Promover registra o boleto gerado e por qual degrau da escada o item foi roteado.
    [Fact]
    public void Promote_ShouldLinkBillAndRecordRouting()
    {
        var item = CaptureItemMother.Parsed();

        item.Promote(CaptureItemMother.DefaultBill, RoutingConfidence.Strong, Later);

        Assert.Same(CaptureItemStatus.Promoted, item.Status);
        Assert.Equal(CaptureItemMother.DefaultBill, item.BillId);
        Assert.Same(RoutingConfidence.Strong, item.Routing);
    }

    // Promover sem o boleto que o item gerou é recusado com BLP.CPI10.
    [Fact]
    public void Promote_WithoutBill_ShouldThrowBLP_CPI10()
    {
        var item = CaptureItemMother.Parsed();

        var exception = Assert.Throws<DomainException>(() =>
            item.Promote(BillId.Empty, RoutingConfidence.Strong, Later));

        Assert.Equal("BLP.CPI10", exception.Id);
    }

    // Promover sem registrar o degrau de roteamento é recusado com BLP.CPI11.
    [Fact]
    public void Promote_WithoutRoutingConfidence_ShouldThrowBLP_CPI11()
    {
        var item = CaptureItemMother.Parsed();

        var exception = Assert.Throws<DomainException>(() =>
            item.Promote(CaptureItemMother.DefaultBill, confidence: null!, Later));

        Assert.Equal("BLP.CPI11", exception.Id);
    }

    // Promover um item recém-ingerido, sem passar pela extração, é transição inválida — BLP.CPI03.
    [Fact]
    public void Promote_FromReceived_ShouldThrowBLP_CPI03()
    {
        var item = CaptureItemMother.Ingest();

        var exception = Assert.Throws<DomainException>(() =>
            item.Promote(CaptureItemMother.DefaultBill, RoutingConfidence.Strong, Later));

        Assert.Equal("BLP.CPI03", exception.Id);
    }

    // Reivindicar promove o item com confiança Claimed e grava quem assumiu a decisão.
    [Fact]
    public void Claim_FromUnrouted_ShouldPromoteWithClaimedConfidenceAndAudit()
    {
        var item = CaptureItemMother.Unrouted();

        item.Claim(CaptureItemMother.DefaultUser, CaptureItemMother.DefaultBill, Later);

        Assert.Same(CaptureItemStatus.Promoted, item.Status);
        Assert.Same(RoutingConfidence.Claimed, item.Routing);
        Assert.Equal(CaptureItemMother.DefaultUser, item.ClaimedBy);
        Assert.Equal(Later, item.ClaimedAt);
    }

    // Reivindicar item cujo pagador identificado é outro é recusado com BLP.CPI04 — a defesa
    // contra o usuário pagar a conta de terceiro, e ela vence a transição genérica.
    [Fact]
    public void Claim_WhenPayerBelongsToAnotherTenant_ShouldThrowBLP_CPI04()
    {
        var item = CaptureItemMother.Foreign();

        var exception = Assert.Throws<DomainException>(() =>
            item.Claim(CaptureItemMother.DefaultUser, CaptureItemMother.DefaultBill, Later));

        Assert.Equal("BLP.CPI04", exception.Id);
    }

    // Reivindicação é ato humano registrado: sem usuário, não acontece — BLP.CPI13.
    [Fact]
    public void Claim_WithoutUser_ShouldThrowBLP_CPI13()
    {
        var item = CaptureItemMother.Unrouted();

        var exception = Assert.Throws<DomainException>(() =>
            item.Claim(UserId.Empty, CaptureItemMother.DefaultBill, Later));

        Assert.Equal("BLP.CPI13", exception.Id);
    }

    // Quarentena por pagador de terceiro guarda o motivo em código estável.
    [Fact]
    public void MarkForeign_ShouldRecordReason()
    {
        var item = CaptureItemMother.Foreign();

        Assert.Same(CaptureItemStatus.ForeignPayer, item.Status);
        Assert.Equal("payer_belongs_to_other_tenant", item.Reason);
    }

    // Motivo de quarentena em branco é recusado com BLP.CPI12.
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void MarkUnrouted_WithBlankReason_ShouldThrowBLP_CPI12(string reason)
    {
        var item = CaptureItemMother.Parsed();

        var exception = Assert.Throws<DomainException>(() => item.MarkUnrouted(reason, Later));

        Assert.Equal("BLP.CPI12", exception.Id);
    }

    // Descartar duplicata guarda o ponteiro para o item original.
    [Fact]
    public void Discard_ShouldPointToOriginalItem()
    {
        var original = CaptureItemMother.Ingest(artifactKey: "original.pdf");
        var duplicata = CaptureItemMother.Ingest(artifactKey: "reenvio.pdf");

        duplicata.Discard(original.Id, Later);

        Assert.Same(CaptureItemStatus.Discarded, duplicata.Status);
        Assert.Equal(original.Id, duplicata.DiscardedOf);
        Assert.Equal("duplicate_content", duplicata.Reason);
    }

    // Estado terminal não aceita mais nenhuma transição — BLP.CPI03.
    [Fact]
    public void Transition_FromTerminalStatus_ShouldThrowBLP_CPI03()
    {
        var item = CaptureItemMother.Parsed();
        item.Promote(CaptureItemMother.DefaultBill, RoutingConfidence.Strong, Later);

        var exception = Assert.Throws<DomainException>(() => item.MarkUnrouted("tarde_demais", Later.AddHours(1)));

        Assert.Equal("BLP.CPI03", exception.Id);
    }
}
