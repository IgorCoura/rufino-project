namespace BillPayment.UnitTests.CapturedMessages;

using BillPayment.Domain.Bills;
using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.CapturedMessages;
using BillPayment.Domain.SeedWork;
using BillPayment.UnitTests.CapturedMessages.Mothers;

/// <summary>
/// O livro-caixa da captura: o registro que sobrevive ao descarte.
/// </summary>
public class CapturedMessageTests
{
    private static readonly BillId SomeBill = BillId.From(new Guid("0195a1f0-0000-7000-8000-0000000000d1"));
    private static readonly CaptureItemId SomeItem =
        CaptureItemId.From(new Guid("0195a1f0-0000-7000-8000-0000000000c1"));

    // O registro nasce com um anexo por artefato e todos pendentes — nada foi decidido ainda.
    [Fact]
    public void Register_ShouldStartWithEveryArtifactPending()
    {
        var message = CapturedMessageMother.WithTwoArtifacts();

        Assert.Equal(2, message.ArtifactCount);
        Assert.All(message.Artifacts, a => Assert.Equal(ArtifactOutcome.Pending, a.Outcome));
        Assert.Null(message.ProcessedAt);
    }

    // Chave repetida faria o desfecho de um anexo sobrescrever o do irmão, e o histórico passaria
    // a mentir sobre um dos dois.
    [Fact]
    public void Register_WithRepeatedArtifactKey_Throws_BLP_CMS06()
    {
        var exception = Assert.Throws<DomainException>(() => CapturedMessageMother.Register(artifacts:
        [
            (CapturedMessageMother.BoletoKey, "a.pdf", "application/pdf"),
            (CapturedMessageMother.BoletoKey, "b.pdf", "application/pdf"),
        ]));

        Assert.Equal("BLP.CMS06", exception.Id);
    }

    // O caso que justifica o agregado: o item some no descarte, e é este registro que continua
    // sabendo que o e-mail existiu.
    [Fact]
    public void RecordOutcome_WhenDiscarded_ShouldKeepTheRecordWithoutAnItem()
    {
        var message = CapturedMessageMother.Register();

        message.RecordOutcome(
            CapturedMessageMother.BoletoKey,
            ArtifactOutcome.Discarded,
            "no_instrument_in_document",
            captureItemId: null,
            billId: null,
            CapturedMessageMother.DefaultOccurredAt);

        var artifact = Assert.Single(message.Artifacts);
        Assert.Equal(ArtifactOutcome.Discarded, artifact.Outcome);
        Assert.Equal("no_instrument_in_document", artifact.Reason);
        Assert.Null(artifact.CaptureItemId);
    }

    // Anexo que virou boleto guarda para onde navegar, e é o que torna o registro inpurgável.
    [Fact]
    public void RecordOutcome_WhenPromoted_ShouldLinkTheBillAndBlockPurge()
    {
        var message = CapturedMessageMother.Register();

        message.RecordOutcome(
            CapturedMessageMother.BoletoKey,
            ArtifactOutcome.Promoted,
            reason: null,
            SomeItem,
            SomeBill,
            CapturedMessageMother.DefaultOccurredAt);

        var artifact = Assert.Single(message.Artifacts);
        Assert.Equal(SomeBill, artifact.BillId);
        Assert.Equal(SomeItem, artifact.CaptureItemId);
        Assert.True(message.ProducedBill);
    }

    // Um e-mail com boleto e recibo tem desfechos diferentes na mesma mensagem — e o que produziu
    // dinheiro é o que manda na purga.
    [Fact]
    public void ProducedBill_WithMixedOutcomes_ShouldBeTrueWhenAnyArtifactBecameABill()
    {
        var message = CapturedMessageMother.WithTwoArtifacts();

        message.RecordOutcome(
            CapturedMessageMother.BoletoKey, ArtifactOutcome.Promoted, null, SomeItem, SomeBill,
            CapturedMessageMother.DefaultOccurredAt);
        message.RecordOutcome(
            CapturedMessageMother.ReciboKey, ArtifactOutcome.Discarded, "no_instrument_in_document",
            null, null, CapturedMessageMother.DefaultOccurredAt);

        Assert.True(message.ProducedBill);
    }

    // ProcessedAt anda a cada decisão: um e-mail com três anexos é processado em três passagens do
    // worker, e esperar a última deixaria a tela dizendo "não processado" sobre e-mail resolvido.
    [Fact]
    public void RecordOutcome_ShouldStampProcessedAtOnTheFirstDecision()
    {
        var message = CapturedMessageMother.WithTwoArtifacts();

        message.RecordOutcome(
            CapturedMessageMother.BoletoKey, ArtifactOutcome.Promoted, null, SomeItem, SomeBill,
            CapturedMessageMother.DefaultOccurredAt);

        Assert.Equal(CapturedMessageMother.DefaultOccurredAt, message.ProcessedAt);
    }

    // Desfecho para anexo que o registro não conhece significa que ingestão e processamento
    // discordam sobre o que a mensagem tem — falhar alto é melhor que histórico incompleto.
    [Fact]
    public void RecordOutcome_ForUnknownArtifact_Throws_BLP_CMS07()
    {
        var message = CapturedMessageMother.Register();

        var exception = Assert.Throws<DomainException>(() => message.RecordOutcome(
            "anexo-que-nao-existe", ArtifactOutcome.Discarded, null, null, null,
            CapturedMessageMother.DefaultOccurredAt));

        Assert.Equal("BLP.CMS07", exception.Id);
    }

    // Sem o Message-ID do cabeçalho não há chave permanente, e a recaptura não teria como
    // reencontrar a mensagem — que é justamente o problema que ela existe para resolver.
    [Fact]
    public void EnsureCanBeRecaptured_WithoutInternetMessageId_Throws_BLP_CMS08()
    {
        var message = CapturedMessageMother.Register(internetMessageId: null);

        var exception = Assert.Throws<DomainException>(message.EnsureCanBeRecaptured);

        Assert.Equal("BLP.CMS08", exception.Id);
        Assert.False(message.CanBeRecaptured);
    }

    [Fact]
    public void EnsureCanBeRecaptured_WithInternetMessageId_ShouldPass()
    {
        var message = CapturedMessageMother.Register();

        message.EnsureCanBeRecaptured();

        Assert.True(message.CanBeRecaptured);
    }

    // O remetente é normalizado pela mesma regra do TrustedOrigin — normalizar em dois lugares
    // diferentes é como a resolução de origem passa a divergir do que foi cadastrado.
    [Fact]
    public void Register_ShouldNormalizeTheSender()
    {
        var message = CapturedMessageMother.Register(sender: "  Faturas@ENEL.com.BR ");

        Assert.Equal("faturas@enel.com.br", message.Sender);
    }

    // Só Promoted produz boleto — o resto dos desfechos não trava a purga.
    [Theory]
    [InlineData(nameof(ArtifactOutcome.Discarded))]
    [InlineData(nameof(ArtifactOutcome.Quarantined))]
    [InlineData(nameof(ArtifactOutcome.Unrouted))]
    [InlineData(nameof(ArtifactOutcome.ForeignPayer))]
    [InlineData(nameof(ArtifactOutcome.DownloadFailed))]
    [InlineData(nameof(ArtifactOutcome.Locked))]
    public void ProducedBill_ForOutcomesWithoutABill_ShouldBeFalse(string outcomeName)
    {
        var message = CapturedMessageMother.Register();
        var outcome = Enumeration.FromDisplayName<ArtifactOutcome>(outcomeName);

        message.RecordOutcome(
            CapturedMessageMother.BoletoKey, outcome, null, SomeItem, null,
            CapturedMessageMother.DefaultOccurredAt);

        Assert.False(message.ProducedBill);
        Assert.False(outcome.ProducesBill);
    }
}
