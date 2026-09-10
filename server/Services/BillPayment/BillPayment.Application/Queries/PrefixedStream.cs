namespace BillPayment.Application.Queries;

/// <summary>
/// Serve primeiro os bytes já espiados, depois o resto do fluxo original.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Existe porque o fluxo do S3 não volta atrás.</strong> Descobrir o que um documento é
/// exige ler os primeiros bytes dele, e um fluxo de rede não é <c>Seekable</c> — lido o começo,
/// ele está perdido. Este tipo guarda o começo e o recoloca na frente, o que deixa
/// <see cref="ArtifactDownload.OpenAsync"/> conferir a assinatura sem carregar o arquivo inteiro
/// na memória do servidor.
/// </para>
/// <para>
/// Só de leitura e só para frente, que é tudo o que um download precisa. Descartá-lo descarta o
/// fluxo original — quem o consome já fecha o <see cref="ArtifactDownload"/> e nada mais muda.
/// </para>
/// </remarks>
internal sealed class PrefixedStream(ReadOnlyMemory<byte> prefix, Stream inner) : Stream
{
    private int _consumed;

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override int Read(Span<byte> buffer)
    {
        var fromPrefix = ReadPrefix(buffer);

        // O prefixo é servido sozinho: misturá-lo com uma leitura do fluxo obrigaria a bloquear
        // aqui, e quem chama já sabe lidar com leitura curta.
        return fromPrefix > 0 ? fromPrefix : inner.Read(buffer);
    }

    public override int Read(byte[] buffer, int offset, int count)
        => Read(buffer.AsSpan(offset, count));

    public override async ValueTask<int> ReadAsync(
        Memory<byte> buffer,
        CancellationToken cancellationToken = default)
    {
        var fromPrefix = ReadPrefix(buffer.Span);

        return fromPrefix > 0 ? fromPrefix : await inner.ReadAsync(buffer, cancellationToken);
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    public override void Flush()
    {
    }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    private int ReadPrefix(Span<byte> buffer)
    {
        var remaining = prefix.Length - _consumed;
        if (remaining <= 0 || buffer.IsEmpty)
            return 0;

        var take = Math.Min(remaining, buffer.Length);
        prefix.Span.Slice(_consumed, take).CopyTo(buffer);
        _consumed += take;

        return take;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            inner.Dispose();

        base.Dispose(disposing);
    }
}
