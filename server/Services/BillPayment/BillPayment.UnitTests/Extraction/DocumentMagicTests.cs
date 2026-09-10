namespace BillPayment.UnitTests.Extraction;

using BillPayment.Domain.Extraction;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;

/// <summary>
/// O tipo de mídia decidido pelos bytes.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Teste de regressão.</strong> Bug de 2026-09-10: o boleto da Notredame Intermédica
/// chega com <c>Content-Type: pdf</c> — rótulo inválido, sem o <c>application/</c>. O Exchange
/// normaliza para <c>application/octet-stream</c>, esse rótulo atravessava ingestão, balde e API,
/// e o app dizia "formato que o app não exibe" sobre um PDF que abre em qualquer leitor.
/// </para>
/// <para>
/// É a terceira encarnação do mesmo erro: em 2026-08-11 o tipo saía da extensão de uma chave
/// opaca. A lição que faltava é que <strong>guardar a declaração do provedor não a torna
/// verdadeira</strong>.
/// </para>
/// </remarks>
public class DocumentMagicTests
{
    private static readonly TenantId Tenant = TenantId.From(Guid.CreateVersion7());

    private static byte[] Pdf => "%PDF-1.4 conteudo do boleto"u8.ToArray();

    private static byte[] Png => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x01];

    private static byte[] Jpeg => [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10];

    private static byte[] Webp => [.. "RIFF"u8, 0x24, 0x00, 0x00, 0x00, .. "WEBP"u8];

    [Fact]
    public void MediaTypeOf_WithAPdf_ShouldSayPdf()
        => Assert.Equal("application/pdf", DocumentMagic.MediaTypeOf(Pdf));

    [Fact]
    public void MediaTypeOf_WithAPng_ShouldSayPng()
        => Assert.Equal("image/png", DocumentMagic.MediaTypeOf(Png));

    [Fact]
    public void MediaTypeOf_WithAJpeg_ShouldSayJpeg()
        => Assert.Equal("image/jpeg", DocumentMagic.MediaTypeOf(Jpeg));

    [Fact]
    public void MediaTypeOf_WithAWebp_ShouldSayWebp()
        => Assert.Equal("image/webp", DocumentMagic.MediaTypeOf(Webp));

    // RIFF sem "WEBP" nos bytes 8-11 é outro contêiner (WAV, AVI). Não é imagem, e dizer que é
    // mandaria áudio para o decodificador do leitor de QR.
    [Fact]
    public void MediaTypeOf_WithARiffThatIsNotWebp_ShouldSayNothing()
    {
        byte[] wav = [.. "RIFF"u8, 0x24, 0x00, 0x00, 0x00, .. "WAVE"u8];

        Assert.Null(DocumentMagic.MediaTypeOf(wav));
    }

    // Nulo é resposta legítima: corpo de e-mail em HTML não tem assinatura, e quem chama sabe
    // cair no rótulo declarado.
    [Fact]
    public void MediaTypeOf_WithHtml_ShouldSayNothing()
        => Assert.Null(DocumentMagic.MediaTypeOf("<html><body>fatura</body></html>"u8));

    [Fact]
    public void MediaTypeOf_WithFewerBytesThanAnySignature_ShouldSayNothing()
        => Assert.Null(DocumentMagic.MediaTypeOf([0x25, 0x50]));

    // O CASO QUE MOTIVOU TUDO: o rótulo que o Exchange carimbou não descreve o arquivo, e são os
    // bytes que decidem.
    [Fact]
    public void From_WithAPdfLabelledOctetStream_ShouldCarryItAsPdf()
    {
        var payload = DocumentPayload.From(Tenant, Pdf, "application/octet-stream");

        Assert.Equal(DocumentPayload.PDF, payload.MediaType);
    }

    // O outro lado da mesma moeda, e o defeito medido em 2026-08-11: octet-stream virava PDF por
    // convenção, então uma IMAGEM assim rotulada ia para o extrator de visão como PDF e o
    // provedor recusava o artefato inteiro.
    [Fact]
    public void From_WithAnImageLabelledOctetStream_ShouldCarryItAsImage()
    {
        var payload = DocumentPayload.From(Tenant, Png, "application/octet-stream");

        Assert.Equal("image/png", payload.MediaType);
    }

    // Os bytes ganham até de um rótulo declarado, suportado e específico — ele também é
    // declaração de terceiro.
    [Fact]
    public void From_WithBytesThatContradictASupportedLabel_ShouldFollowTheBytes()
    {
        var payload = DocumentPayload.From(Tenant, Png, DocumentPayload.PDF);

        Assert.Equal("image/png", payload.MediaType);
    }

    // Sem assinatura conhecida o rótulo volta a valer: PDF com lixo antes do "%PDF-" existe, e
    // recusá-lo perderia um boleto legítimo.
    [Fact]
    public void From_WithoutAKnownSignature_ShouldFallBackToTheLabel()
    {
        var payload = DocumentPayload.From(Tenant, new byte[] { 1, 2, 3 }, DocumentPayload.PDF);

        Assert.Equal(DocumentPayload.PDF, payload.MediaType);
    }

    // Sem assinatura E sem rótulo utilizável continua sendo recusa — nada mudou para o anexo que
    // ninguém sabe abrir.
    [Fact]
    public void From_WithoutASignatureAndWithAnUnsupportedLabel_ShouldStillRefuse()
        => Assert.Throws<DomainException>(
            () => DocumentPayload.From(Tenant, new byte[] { 1, 2, 3 }, "application/zip"));
}
