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

    // Recapturar reescreve o registro em cima do que existe: o id é o mesmo, todo anexo volta a
    // Pending, o desfecho anterior some, e a chave do corpo guardado é devolvida para quem chamou
    // apagar o blob depois do commit.
    [Fact]
    public void Recapture_ShouldResetEveryArtifactAndReturnThePreviousBodyKey()
    {
        var message = CapturedMessageMother.Register();
        message.RecordOutcome(CapturedMessageMother.BoletoKey, ArtifactOutcome.Promoted, null, SomeItem, SomeBill, CapturedMessageMother.DefaultOccurredAt);
        message.RecordBody("tenants/x/body", "text/html", CapturedMessageMother.DefaultOccurredAt);
        var id = message.Id;
        var later = CapturedMessageMother.DefaultOccurredAt.AddHours(1);

        var previousBody = message.Recapture(
            CapturedMessageMother.DefaultMessageId, "Faturas@ENEL.com.br", "Segunda via", CapturedMessageMother.DefaultReceivedAt.AddMinutes(5),
            [(CapturedMessageMother.BoletoKey, "boleto-v2.pdf", "application/pdf")], later);

        Assert.Equal(id, message.Id);
        Assert.Equal("tenants/x/body", previousBody);
        Assert.False(message.HasStoredBody);
        Assert.Null(message.ProcessedAt);
        Assert.Equal("faturas@enel.com.br", message.Sender);
        Assert.Equal("Segunda via", message.Subject);
        Assert.Equal(later, message.UpdatedAt);

        var artifact = Assert.Single(message.Artifacts);
        Assert.Equal(ArtifactOutcome.Pending, artifact.Outcome);
        Assert.Equal("boleto-v2.pdf", artifact.FileName);
        Assert.Null(artifact.CaptureItemId);
        Assert.Null(artifact.BillId);
        Assert.Null(artifact.DecidedAt);
    }

    // Os anexos são sincronizados com o que o provedor devolve AGORA: o que continua existindo é
    // mantido (mesma entidade), o que sumiu sai, o que é novo entra.
    [Fact]
    public void Recapture_ShouldSyncTheArtifactsWithWhatTheProviderReturnsNow()
    {
        var message = CapturedMessageMother.WithTwoArtifacts();
        var boletoId = message.Artifacts.Single(a => a.ArtifactKey == CapturedMessageMother.BoletoKey).Id;

        message.Recapture(
            CapturedMessageMother.DefaultMessageId, CapturedMessageMother.DefaultSender, null, CapturedMessageMother.DefaultReceivedAt,
            [(CapturedMessageMother.BoletoKey, "boleto.pdf", "application/pdf"), ("anexo-novo", "novo.pdf", "application/pdf")],
            CapturedMessageMother.DefaultOccurredAt);

        Assert.Equal(2, message.ArtifactCount);
        Assert.Contains(message.Artifacts, a => a.ArtifactKey == CapturedMessageMother.BoletoKey && a.Id == boletoId);
        Assert.Contains(message.Artifacts, a => a.ArtifactKey == "anexo-novo");
        Assert.DoesNotContain(message.Artifacts, a => a.ArtifactKey == CapturedMessageMother.ReciboKey);
    }

    // Sem o identificador do cabeçalho não há como reencontrar o e-mail no provedor — BLP.CMS08.
    [Fact]
    public void Recapture_WithoutAnInternetMessageId_Throws_BLP_CMS08()
    {
        var message = CapturedMessageMother.Register(internetMessageId: null);

        var exception = Assert.Throws<DomainException>(() => message.Recapture(
            CapturedMessageMother.DefaultMessageId, CapturedMessageMother.DefaultSender, null, CapturedMessageMother.DefaultReceivedAt,
            [(CapturedMessageMother.BoletoKey, "boleto.pdf", "application/pdf")], CapturedMessageMother.DefaultOccurredAt));

        Assert.Equal("BLP.CMS08", exception.Id);
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
