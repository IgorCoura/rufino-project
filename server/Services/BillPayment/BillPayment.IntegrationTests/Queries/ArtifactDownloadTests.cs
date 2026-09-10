namespace BillPayment.IntegrationTests.Queries;

using System.Text;
using BillPayment.Application.Queries;
using BillPayment.Domain.Ports;

/// <summary>
/// O tipo de mídia com que o documento guardado é SERVIDO.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Teste de regressão.</strong> Bug de 2026-09-10: o boleto da Notredame Intermédica sai
/// do emissor com <c>Content-Type: pdf</c>, rótulo inválido que o Exchange normaliza para
/// <c>application/octet-stream</c>. Esse rótulo era gravado no balde e devolvido tal e qual pela
/// API — e o app, que só abre <c>application/pdf</c> e <c>image/*</c>, mostrava "Este documento
/// chegou em um formato que o app não exibe" sobre um PDF perfeitamente legível.
/// </para>
/// <para>
/// Corrigir <em>ao servir</em>, e não só na ingestão, é o que dispensa migração de balde: os
/// objetos já gravados com o rótulo errado passam a ser servidos pelo que são.
/// </para>
/// </remarks>
public sealed class ArtifactDownloadTests
{
    private static readonly byte[] Pdf =
        Encoding.ASCII.GetBytes("%PDF-1.4\n1 0 obj\n<< /Type /Catalog >>\nendobj\n%%EOF");

    /// <summary>Um fluxo que só anda para frente e devolve pouco por leitura, como o do S3.</summary>
    private static ChunkedStream Chunked(byte[] content) => new(content, chunkSize: 3);

    // O CASO QUE MOTIVOU TUDO.
    [Fact]
    public async Task OpenAsync_WithAPdfStoredAsOctetStream_ShouldServeItAsPdf()
    {
        using var artifact = new StoredArtifact(Chunked(Pdf), "application/octet-stream", Pdf.Length);

        using var download = await ArtifactDownload.OpenAsync(artifact, null, "boleto", default);

        Assert.Equal("application/pdf", download.ContentType);
        Assert.Equal("boleto.pdf", download.FileName);
    }

    // Consertar o rótulo não pode custar um byte do arquivo: o prefixo espiado tem que voltar
    // para a frente do fluxo, senão o PDF servido chega truncado no cabeçalho e não abre.
    [Fact]
    public async Task OpenAsync_ShouldStillServeEveryByteOfTheDocument()
    {
        using var artifact = new StoredArtifact(Chunked(Pdf), "application/octet-stream", Pdf.Length);

        using var download = await ArtifactDownload.OpenAsync(artifact, null, "boleto", default);

        using var buffer = new MemoryStream();
        await download.Content.CopyToAsync(buffer);

        Assert.Equal(Pdf, buffer.ToArray());
    }

    // O tipo declarado na ingestão continua entrando quando os bytes não dizem nada — é o caso do
    // corpo de e-mail, que não tem assinatura.
    [Fact]
    public async Task OpenAsync_WithoutAKnownSignature_ShouldKeepTheStoredLabel()
    {
        var html = "<html><body>fatura</body></html>"u8.ToArray();
        using var artifact = new StoredArtifact(Chunked(html), "text/html", html.Length);

        using var download = await ArtifactDownload.OpenAsync(artifact, null, "mensagem", default);

        Assert.Equal("text/html", download.ContentType);
        Assert.Equal("mensagem.html", download.FileName);
    }

    // Nem balde nem ingestão souberam dizer, e os bytes também não: continua sendo o octet-stream
    // que faz o navegador baixar em vez de adivinhar.
    [Fact]
    public async Task OpenAsync_WithNothingToGoOn_ShouldFallBack()
    {
        using var artifact = new StoredArtifact(Chunked([1, 2, 3]), null, 3);

        using var download = await ArtifactDownload.OpenAsync(artifact, null, "documento", default);

        Assert.Equal(ArtifactDownload.FALLBACK_CONTENT_TYPE, download.ContentType);
    }

    // Arquivo menor que o prefixo espiado não pode travar nem perder bytes.
    [Fact]
    public async Task OpenAsync_WithADocumentShorterThanThePrefix_ShouldServeItWhole()
    {
        byte[] tiny = [0x41, 0x42];
        using var artifact = new StoredArtifact(Chunked(tiny), "application/octet-stream", tiny.Length);

        using var download = await ArtifactDownload.OpenAsync(artifact, null, "documento", default);

        using var buffer = new MemoryStream();
        await download.Content.CopyToAsync(buffer);

        Assert.Equal(tiny, buffer.ToArray());
    }

    /// <summary>
    /// Devolve poucos bytes por leitura e não volta atrás — o que o fluxo do S3 faz, e o que um
    /// <c>MemoryStream</c> esconderia.
    /// </summary>
    private sealed class ChunkedStream(byte[] content, int chunkSize) : Stream
    {
        private int _position;

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
            => Read(buffer.AsSpan(offset, count));

        public override int Read(Span<byte> buffer)
        {
            var take = Math.Min(Math.Min(chunkSize, buffer.Length), content.Length - _position);
            if (take <= 0)
                return 0;

            content.AsSpan(_position, take).CopyTo(buffer);
            _position += take;

            return take;
        }

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
