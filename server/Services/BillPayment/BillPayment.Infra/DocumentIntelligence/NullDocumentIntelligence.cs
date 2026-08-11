namespace BillPayment.Infra.DocumentIntelligence;

using BillPayment.Domain.Extraction;
using BillPayment.Domain.Ports;

/// <summary>
/// Substituto usado quando não há extrator de IA configurado.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Devolve vazio, não falha</strong> — ao contrário do cofre e do armazenamento. A
/// diferença é o que a ausência significa: guardar em lugar nenhum perde um comprovante que
/// ninguém recupera, enquanto não ter IA apenas deixa a cascata terminar onde ela já terminava
/// antes da 2.4. O sistema segue capturando, e o que não resolve vai para a quarentena.
/// </para>
/// <para>
/// <see cref="IsEnabled"/> é <c>false</c> para que quem chama nem monte o payload — serializar
/// megabytes em base64 para descartar em seguida seria desperdício silencioso.
/// </para>
/// </remarks>
internal sealed class NullDocumentIntelligence : IDocumentIntelligence
{
    public bool IsEnabled => false;

    public Task<ExtractedDocument> ExtractAsync(
        DocumentPayload payload,
        ExtractionHints hints,
        CancellationToken cancellationToken)
        => Task.FromResult(ExtractedDocument.Empty);
}
