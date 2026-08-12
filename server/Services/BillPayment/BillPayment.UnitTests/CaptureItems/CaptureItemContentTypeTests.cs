namespace BillPayment.UnitTests.CaptureItems;

using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.Extraction;
using BillPayment.UnitTests.CaptureItems.Mothers;

/// <summary>
/// O tipo de mídia declarado na ingestão.
/// </summary>
/// <remarks>
/// <strong>Teste de regressão.</strong> Bug de 2026-08-11: o processamento deduzia o tipo pela
/// extensão do <c>ArtifactKey</c>, mas no Microsoft Graph essa chave é um identificador opaco
/// — <c>AAMkADk0NWIxNGMy...</c>, sem extensão nenhuma. Todo anexo virava
/// <c>application/pdf</c>, o extrator de visão recebia imagem rotulada como PDF, o provedor
/// recusava, e os 54 anexos <c>not_a_pdf</c> seguiam inalcançáveis mesmo depois de a visão
/// existir e de eu ter afirmado que estavam cobertos.
/// </remarks>
public class CaptureItemContentTypeTests
{
    /// <summary>Uma chave de anexo do Graph, na forma real: opaca e sem extensão.</summary>
    private const string OpaqueGraphKey =
        "AAMkADk0NWIxNGMyLTA4MzgtNDcxZi1iNmY2LTE3Y2MyNDY5YmFhZABGAAAAAABO7anvy880TJLf6c1r1zPpBwAO";

    // O tipo declarado pelo provedor é guardado como veio — é o único lugar de onde o
    // processamento pode sabê-lo depois.
    [Theory]
    [InlineData("application/pdf")]
    [InlineData("image/png")]
    [InlineData("image/jpeg")]
    public void Ingest_ShouldKeepTheDeclaredContentType(string contentType)
    {
        var item = CaptureItemMother.Ingest(artifactKey: OpaqueGraphKey, contentType: contentType);

        Assert.Equal(contentType, item.ContentType);
    }

    // Sem tipo declarado, fica nulo — e nulo NÃO é suportado pelo extrator, então o item vai
    // para a quarentena em vez de ser mandado como PDF por chute.
    [Fact]
    public void Ingest_WithoutContentType_ShouldLeaveItNullAndUnsupported()
    {
        var item = CaptureItemMother.Ingest(artifactKey: OpaqueGraphKey, contentType: null);

        Assert.Null(item.ContentType);
        Assert.False(DocumentPayload.IsSupported(item.ContentType));
    }

    // A chave opaca do Graph não carrega extensão, então nada pode ser deduzido dela. Este é o
    // teste que teria pego o bug: a chave termina em letras, não em ".pdf".
    [Fact]
    public void ArtifactKey_FromGraph_ShouldNotCarryAnyExtension()
    {
        var item = CaptureItemMother.Ingest(artifactKey: OpaqueGraphKey, contentType: "image/png");

        Assert.DoesNotContain(".", item.ArtifactKey, StringComparison.Ordinal);
        Assert.Equal("image/png", item.ContentType);
    }

    // O nome do arquivo é guardado à parte, e é ele que o portão de gasto examina — a chave
    // opaca nunca casaria com palavra nenhuma.
    [Fact]
    public void Ingest_ShouldKeepTheFileNameSeparately()
    {
        var item = CaptureItemMother.Ingest(
            artifactKey: OpaqueGraphKey, contentType: "application/pdf", fileName: "boleto-agosto.pdf");

        Assert.Equal("boleto-agosto.pdf", item.FileName);
    }

    // Tipo comprido é truncado, não recusado: ele vem do provedor e não pode impedir a ingestão
    // de um boleto.
    [Fact]
    public void Ingest_WithAnOverlongContentType_ShouldTruncate()
    {
        var item = CaptureItemMother.Ingest(
            artifactKey: OpaqueGraphKey,
            contentType: new string('x', CaptureItem.CONTENT_TYPE_MAX_LENGTH + 40));

        Assert.Equal(CaptureItem.CONTENT_TYPE_MAX_LENGTH, item.ContentType!.Length);
    }
}
