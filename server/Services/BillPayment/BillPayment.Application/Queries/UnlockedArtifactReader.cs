namespace BillPayment.Application.Queries;

using BillPayment.Domain.Ports;
using BillPayment.Domain.Services;
using BillPayment.Domain.SharedKernel;
using BillPayment.Infra.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Abre o documento guardado e o entrega <strong>já sem senha</strong> quando ele veio cifrado.
/// </summary>
/// <remarks>
/// <para>
/// <strong>O sistema já sabia a senha e mesmo assim mandava o arquivo trancado.</strong> A
/// derivação existe desde a 2.2 e abre o PDF na ingestão; a cópia legível, porém, nascia só
/// dentro do processamento e morria ali. Quem aprovava recebia os bytes originais e via o
/// pedido de senha do leitor — pedindo justamente o dado que o cadastro do tenant já tinha.
/// </para>
/// <para>
/// <strong>Destravar é do servidor porque a senha não pode sair daqui.</strong> Mandá-la junto
/// para o app abrir o documento seria a solução curta, e é exatamente a que o ADR-009 proíbe:
/// senha derivada não é logada nem devolvida por API. O que atravessa é o documento.
/// </para>
/// <para>
/// <strong>O original continua sendo o que está guardado.</strong> A cópia é produzida a cada
/// leitura e não volta para o balde — o arquivo como chegou é o comprovante do que o sistema viu
/// quando decidiu pagar, e sobrescrevê-lo por uma versão reescrita trocaria a prova pela
/// conveniência.
/// </para>
/// </remarks>
internal sealed class UnlockedArtifactReader(
    BillPaymentDbContext context,
    IAttachmentStorage storage,
    IBoletoDocumentParser parser)
{
    /// <summary>O único tipo que tem cifra a remover. Imagem e HTML passam direto.</summary>
    private const string PDF_CONTENT_TYPE = "application/pdf";

    /// <summary>
    /// Teto acima do qual o documento é servido como veio, ainda que cifrado.
    /// </summary>
    /// <remarks>
    /// Destravar exige o arquivo inteiro em memória, e o download normal é <em>streaming</em>
    /// justamente para não pagar isso por requisição. Boleto vive na casa dos kilobytes; o que
    /// passa daqui não é boleto, e trancado ele já estava.
    /// </remarks>
    private const long MAX_UNLOCK_BYTES = 20L * 1024 * 1024;

    /// <summary>
    /// Serve o documento de <paramref name="storageKey"/>. <c>null</c> quando a chave é órfã —
    /// o mesmo 404 de sempre para quem chama.
    /// </summary>
    /// <param name="declaredContentType">
    /// O tipo que o provedor declarou na ingestão, quando quem chama tem esse dado guardado. A
    /// <c>Bill</c> não tem, e por isso o parâmetro aceita nulo.
    /// </param>
    public async Task<ArtifactDownload?> OpenAsync(
        TenantId tenantId,
        string storageKey,
        string? declaredContentType,
        string fallbackFileName,
        CancellationToken cancellationToken)
    {
        var artifact = await storage.OpenAsync(tenantId, storageKey, cancellationToken);
        if (artifact is null)
            return null;

        var download = ArtifactDownload.From(artifact, declaredContentType, fallbackFileName);

        if (!IsWorthUnlocking(download.ContentType, artifact.Length))
            return download;

        byte[] original;

        // O fluxo do balde é lido até o fim e liberado aqui: daqui para a frente quem responde
        // pelos bytes é o buffer, e deixar o original aberto seguraria a conexão do S3 à toa.
        using (download)
        {
            using var buffer = new MemoryStream(capacity: (int)artifact.Length!.Value);
            await download.Content.CopyToAsync(buffer, cancellationToken);
            original = buffer.ToArray();
        }

        var clear = await UnlockAsync(tenantId, original, download.ContentType, cancellationToken);
        var served = clear ?? original;

        return download with
        {
            Content = new MemoryStream(served, writable: false),
            Length = served.LongLength,
            Unlocked = clear is not null,
        };
    }

    /// <summary>
    /// A cópia legível, ou <c>null</c> quando não há o que destravar.
    /// </summary>
    /// <remarks>
    /// <strong>Nulo cobre dois casos que pedem a mesma reação</strong>: o documento já abria sem
    /// senha, ou nenhuma candidata o abriu. No segundo o leitor do app ainda vai pedir a senha, e
    /// isso é o comportamento certo — ali é a pessoa que sabe algo que o sistema não sabe.
    /// </remarks>
    private async Task<byte[]?> UnlockAsync(
        TenantId tenantId,
        byte[] original,
        string contentType,
        CancellationToken cancellationToken)
    {
        // Tenant sem cadastro fiscal não tem de onde derivar senha nenhuma. Ler o balde e não
        // ter candidata é estado normal — o cadastro pode vir depois do primeiro documento.
        var profile = await context.PayerProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.TenantId == tenantId, cancellationToken);

        var candidates = PasswordDerivationService.Derive(profile);
        if (candidates.Count == 0)
            return null;

        var clear = await parser.UnlockAsync(original, contentType, candidates, cancellationToken);

        return clear?.ToArray();
    }

    /// <summary>
    /// Tamanho desconhecido conta como grande demais: é justamente quando não dá para prometer
    /// que o arquivo cabe na memória do processo.
    /// </summary>
    private static bool IsWorthUnlocking(string contentType, long? length)
        => contentType == PDF_CONTENT_TYPE && length is > 0 and <= MAX_UNLOCK_BYTES;
}
