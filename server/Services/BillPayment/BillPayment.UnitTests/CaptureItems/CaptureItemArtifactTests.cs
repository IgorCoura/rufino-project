namespace BillPayment.UnitTests.CaptureItems;

using BillPayment.Domain.CaptureItems;
using BillPayment.UnitTests.CaptureItems.Mothers;

/// <summary>
/// Ter chave de armazenamento não é ter arquivo.
/// </summary>
/// <remarks>
/// A retenção é por desfecho: só o caminho que reconheceu boleto grava os bytes. Os desfechos que
/// mantêm o item para uma pessoa resolver carimbam uma sentinela no lugar da chave, para
/// preservar o histórico sem prometer conteúdo. Quem for buscar o documento pergunta ao
/// comportamento, não ao campo — a alternativa é cada consumidor comparar as strings por conta
/// própria, e um deles esquecer.
/// </remarks>
public class CaptureItemArtifactTests
{
    private const string RealKey = "tenants/0195a1f0/captures/2026/08/019-boleto.pdf";

    // Item recém-ingerido não tem chave nenhuma — o download nem começou.
    [Fact]
    public void HasStoredArtifact_WhenNothingWasStoredYet_ShouldBeFalse()
    {
        var item = CaptureItemMother.Ingest();

        Assert.False(item.HasStoredArtifact);
    }

    // O caminho que reconheceu boleto guarda os bytes, e é o único que promete documento.
    [Fact]
    public void HasStoredArtifact_WhenTheArtifactWasStored_ShouldBeTrue()
    {
        var item = CaptureItemMother.Ingest();
        item.StoreArtifact(CaptureItemMother.DefaultContentHash, RealKey, CaptureItemMother.DefaultOccurredAt);

        Assert.True(item.HasStoredArtifact);
    }

    // As duas sentinelas existem para o item sobreviver sem arquivo. Tratá-las como chave faria
    // a leitura do documento procurar "pending-unlock" no balde, e devolver 500 no lugar de 404.
    [Theory]
    [InlineData(CaptureItem.PENDING_UNLOCK)]
    [InlineData(CaptureItem.PENDING_REVIEW)]
    public void HasStoredArtifact_WhenTheKeyIsASentinel_ShouldBeFalse(string sentinel)
    {
        var item = CaptureItemMother.Ingest();
        item.StoreArtifact(CaptureItemMother.DefaultContentHash, sentinel, CaptureItemMother.DefaultOccurredAt);

        Assert.False(item.HasStoredArtifact);
    }
}
