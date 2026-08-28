namespace BillPayment.UnitTests.CaptureItems;

using BillPayment.Domain.Bills;
using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.SeedWork;
using BillPayment.UnitTests.CaptureItems.Mothers;

/// <summary>
/// As duas saídas humanas da quarentena: reprovar e anexar o documento à mão.
/// </summary>
/// <remarks>
/// <para>
/// Reprovar é a única operação da quarentena que <strong>tira trabalho da vista sem que ninguém
/// tenha verificado o documento</strong>. Por isso registra autor, é reversível, e a máquina de
/// estados recusa aplicá-la ao que já virou boleto ou já foi atribuído a outro pagador.
/// </para>
/// <para>
/// Anexar fecha o caminho que a escada de link não alcança: emissor com página atrás de login ou
/// sem receita cadastrada. O documento entra e o item volta à fila — sem um segundo caminho de
/// processamento, que seria um segundo lugar para as regras envelhecerem.
/// </para>
/// </remarks>
public class CaptureItemReviewTests
{
    private static readonly DateTime Now = CaptureItemMother.DefaultOccurredAt;

    private const string Hash = "sha256:abcdef";
    private const string Key = "tenants/0195a1f0/capture/boleto-manual.pdf";
    private const string SourceUrl = "https://www.asaas.com/i/55p08vsad5vci3g7";

    private static CaptureItem Quarantined()
    {
        var item = CaptureItemMother.Ingest();
        item.RecordResolvedLink(SourceUrl, Now);
        item.StoreArtifact(Hash, CaptureItem.PENDING_REVIEW, Now);
        item.MarkUnrecognized("no_instrument_in_document", Now);

        return item;
    }

    // TESTE ÂNCORA: reprovar tira o item da fila e registra QUEM decidiu — sem autor a
    // quarentena deixaria de ser auditável.
    [Fact]
    public void Dismiss_ShouldRecordTheDeciderAndLeaveTheQueue()
    {
        var item = Quarantined();

        item.Dismiss(CaptureItemMother.DefaultUser, note: null, Now);

        Assert.Equal(CaptureItemStatus.Dismissed, item.Status);
        Assert.Equal(CaptureItemMother.DefaultUser, item.DismissedBy);
        Assert.Equal(Now, item.DismissedAt);
        Assert.Equal(CaptureItem.REASON_DISMISSED, item.Reason);
    }

    // A observação é opcional; informada, ela substitui o motivo padrão.
    [Fact]
    public void Dismiss_WithANote_ShouldKeepIt()
    {
        var item = Quarantined();

        item.Dismiss(CaptureItemMother.DefaultUser, "nao e nosso, e do vizinho", Now);

        Assert.Equal("nao e nosso, e do vizinho", item.Reason);
    }

    // Sem autor não há trilha: reprovar é decisão sobre dinheiro e precisa saber de quem foi.
    [Fact]
    public void Dismiss_WithoutADecider_ShouldThrow()
    {
        var item = Quarantined();

        var error = Assert.Throws<DomainException>(
            () => item.Dismiss(Domain.SharedKernel.UserId.Empty, null, Now));

        Assert.Equal("BLP.CPI16", error.Id);
    }

    // TESTE ÂNCORA DA REVERSIBILIDADE: reprovar por engano uma conta real é a falha silenciosa
    // que o ADR-014 combate, e decidir só por remetente e assunto erra com facilidade.
    [Fact]
    public void Dismiss_ShouldBeUndoable()
    {
        var item = Quarantined();
        item.Dismiss(CaptureItemMother.DefaultUser, null, Now);

        item.Reopen(Now.AddMinutes(1));

        Assert.Equal(CaptureItemStatus.Received, item.Status);
        Assert.Null(item.DismissedBy);
        Assert.Null(item.DismissedAt);
    }

    // Boleto já promovido não se reprova: o dinheiro já está em jogo, e a saída é cancelar o
    // boleto, não sumir com o item que o originou.
    [Fact]
    public void Dismiss_OnAPromotedItem_ShouldThrow()
    {
        var item = CaptureItemMother.Ingest();
        item.StoreArtifact(Hash, Key, Now);
        item.MarkParsed(ExtractionMethod.EmbeddedText, null, Now);
        item.Promote(CaptureItemMother.DefaultBill, RoutingConfidence.Strong, Now);

        var error = Assert.Throws<DomainException>(
            () => item.Dismiss(CaptureItemMother.DefaultUser, null, Now));

        Assert.Equal("BLP.CPI03", error.Id);
    }

    // Item de outro pagador também não: a decisão não é deste tenant, e o estado já diz isso.
    [Fact]
    public void Dismiss_OnAForeignPayerItem_ShouldThrow()
    {
        var item = CaptureItemMother.Ingest();
        item.StoreArtifact(Hash, Key, Now);
        item.MarkParsed(ExtractionMethod.EmbeddedText, null, Now);
        item.MarkForeign("payer_is_someone_else", Now);

        Assert.Throws<DomainException>(() => item.Dismiss(CaptureItemMother.DefaultUser, null, Now));
    }

    // TESTE ÂNCORA DO ANEXO: o documento entra, o item volta à fila, e o tipo passa a ser o do
    // arquivo enviado — não mais o `text/html` do corpo do e-mail que não trazia o boleto.
    [Fact]
    public void AttachManualArtifact_ShouldReturnTheItemToTheQueueWithTheNewDocument()
    {
        var item = Quarantined();

        item.AttachManualArtifact(Hash, Key, "application/pdf", "boleto.pdf", Now);

        Assert.Equal(CaptureItemStatus.Received, item.Status);
        Assert.True(item.ManuallySupplied);
        Assert.True(item.HasStoredArtifact);
        Assert.Equal("application/pdf", item.ContentType);

        // O método de extração é limpo, não marcado como `Manual`: ele diz COMO o instrumento
        // foi lido, e quem o preenche é a cascata logo adiante. Quem diz que uma PESSOA trouxe
        // o arquivo é `ManuallySupplied` — colapsar os dois faria o item mentir sobre o degrau
        // que o resolveu.
        Assert.Null(item.Extraction);
    }

    // TESTE ÂNCORA DA PROCEDÊNCIA: o anexo manual PRESERVA a URL, ao contrário do Reopen.
    // Ela é de onde a pessoa tirou o documento — apagá-la descreveria uma busca que não houve.
    [Fact]
    public void AttachManualArtifact_ShouldPreserveTheSourceUrl()
    {
        var item = Quarantined();

        item.AttachManualArtifact(Hash, Key, "application/pdf", "boleto.pdf", Now);

        Assert.Equal(SourceUrl, item.SourceUrl);
    }

    // A contraprova: o reprocessamento COMUM apaga a URL, porque a escada vai ser percorrida de
    // novo e pode trazer o documento de outro endereço. Sem este par, a diferença entre os dois
    // caminhos pode ser apagada sem quebrar nada.
    [Fact]
    public void Reopen_ShouldClearTheSourceUrl()
    {
        var item = Quarantined();

        item.Reopen(Now);

        Assert.Null(item.SourceUrl);
    }

    // Anexar também desfaz a reprovação: quem sobe o documento está dizendo que reconhece.
    [Fact]
    public void AttachManualArtifact_OnADismissedItem_ShouldClearTheDismissal()
    {
        var item = Quarantined();
        item.Dismiss(CaptureItemMother.DefaultUser, null, Now);

        item.AttachManualArtifact(Hash, Key, "application/pdf", "boleto.pdf", Now);

        Assert.Equal(CaptureItemStatus.Received, item.Status);
        Assert.Null(item.DismissedBy);
    }

    // A URL aparece nos estados que esperam decisão humana — é o que permite ir buscar o
    // documento à mão quando a escada não alcançou o emissor.
    [Theory]
    [InlineData(3)]  // Locked
    [InlineData(5)]  // LinkFailed
    [InlineData(9)]  // Unrecognized
    [InlineData(12)] // Failed
    [InlineData(6)]  // Promoted — já expunha, por carregar o financeiro
    [InlineData(8)]  // Unrouted — idem
    public void ExposesSourceUrl_ShouldBeTrueWhereAHumanStillDecides(int statusId)
        => Assert.True(Enumeration.FromValue<CaptureItemStatus>(statusId).ExposesSourceUrl);

    // E CONTINUA FECHADA onde o sistema já concluiu. `ForeignPayer` é o caso que sustenta o
    // ADR-008: mostrar o link do boleto de outro pagador seria vazamento gratuito.
    [Theory]
    [InlineData(7)]  // ForeignPayer
    [InlineData(13)] // Dismissed — a decisão já foi tomada
    [InlineData(1)]  // Received
    [InlineData(11)] // VisionPending
    public void ExposesSourceUrl_ShouldBeFalseWhereTheSystemAlreadyConcluded(int statusId)
        => Assert.False(Enumeration.FromValue<CaptureItemStatus>(statusId).ExposesSourceUrl);

    // Reprovado não expõe detalhe financeiro: o portão que se abriu para decidir fecha de novo.
    [Fact]
    public void Dismissed_ShouldNotExposeFinancialDetail()
        => Assert.False(CaptureItemStatus.Dismissed.ExposesFinancialDetail);
}
