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

    public Task<ExtractionAttempt> ExtractAsync(
        DocumentPayload payload,
        ExtractionHints hints,
        CancellationToken cancellationToken)
        // `Empty`, e NÃO `Unavailable`: ausência de provedor é decisão de configuração, não
        // instabilidade. Marcá-la como retentável faria a fila insistir para sempre num degrau
        // que ninguém ligou.
        => Task.FromResult(ExtractionAttempt.Answered(ExtractedDocument.Empty));
}
