namespace BillPayment.UnitTests.Bills;

using BillPayment.Domain.Bills;
using BillPayment.Domain.SeedWork;
using BillPayment.UnitTests.Bills.Mothers;

/// <summary>
/// A evidência de procedência: quanto rastro externo cada tipo de origem precisa trazer.
/// </summary>
public class BillOriginTests
{
    // TESTE DE REGRESSÃO — o bug: importar um boleto pela tela devolvia BLP.BIL12 SEMPRE.
    // A importação manual nasce só com os dígitos (sem fonte, remetente, mensagem, arquivo ou
    // hash), e a guarda de identificador, escrita para a captura automática, recusava todas.
    [Fact]
    public void Create_WithManualUploadAndNoIdentifier_ShouldBeAccepted()
    {
        var origin = BillOrigin.Create(BillSourceKind.ManualUpload, BillMother.DefaultOccurredAt);

        Assert.Same(BillSourceKind.ManualUpload, origin.SourceKind);
        Assert.Null(origin.SourceId);
        Assert.Null(origin.SenderAddress);
        Assert.Null(origin.ExternalMessageId);
        Assert.Null(origin.StorageKey);
        Assert.Null(origin.ContentHash);
    }

    // CONTRAPROVA da regressão acima: quem entrou sozinho continua tendo de dizer por onde. Para
    // Mailbox e Portal quem cumpre isso é a própria fonte de captura, obrigatória neles — e é por
    // isso que a isenção do upload manual não afrouxa nada do caminho automático.
    [Fact]
    public void Create_WithMailboxAndOnlyTheSource_ShouldBeAcceptedAndKeepIt()
    {
        var origin = BillOrigin.Create(
            BillSourceKind.Mailbox,
            BillMother.DefaultOccurredAt,
            sourceId: BillMother.DefaultSourceId);

        Assert.Equal(BillMother.DefaultSourceId, origin.SourceId);
    }

    // Origem automática SEM fonte de captura é recusada — e é a guarda de fonte (BLP.BIL11) que
    // dispara, antes da de identificador. Com o catálogo de hoje, esse é o desfecho de toda
    // origem automática sem rastro: a BLP.BIL12 só voltaria a ser alcançável no dia em que
    // existir um tipo que dispense a fonte e ainda assim exija rastro.
    [Theory]
    [InlineData("Mailbox")]
    [InlineData("Portal")]
    public void Create_WithAnAutomaticKindAndNoSource_ShouldThrow_BLP_BIL11(string kindName)
    {
        var kind = Enumeration.FromDisplayName<BillSourceKind>(kindName);

        var error = Assert.Throws<DomainException>(
            () => BillOrigin.Create(kind, BillMother.DefaultOccurredAt));

        Assert.Equal("BLP.BIL11", error.Id);
    }

    // A importação manual continua guardando o que ela TEM: quem anexa arquivo grava a chave.
    [Fact]
    public void Create_WithManualUploadAndAStorageKey_ShouldKeepIt()
    {
        var origin = BillOrigin.Create(
            BillSourceKind.ManualUpload,
            BillMother.DefaultOccurredAt,
            storageKey: "tenants/abc/captures/boleto.pdf",
            contentHash: "sha256:" + new string('a', 64));

        Assert.Equal("tenants/abc/captures/boleto.pdf", origin.StorageKey);
        Assert.Equal("sha256:" + new string('a', 64), origin.ContentHash);
    }

    // Quem exige rastro externo é o tipo de origem, e a capacidade é lida do Smart Enum — nunca
    // reescrita como lista de `if` em quem chama. Percorre o catálogo inteiro, então um tipo novo
    // acrescentado sem decidir isto derruba o teste em vez de herdar um default em silêncio.
    [Fact]
    public void RequiresOriginIdentifier_ShouldBeFalseOnlyForManualUpload()
    {
        var exempt = Enumeration.GetAll<BillSourceKind>()
            .Where(kind => !kind.RequiresOriginIdentifier)
            .ToList();

        Assert.Same(BillSourceKind.ManualUpload, Assert.Single(exempt));
    }

}
