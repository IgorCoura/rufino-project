namespace BillPayment.Domain.Ports;

using BillPayment.Domain.Extraction;

/// <summary>
/// A cascata de extração: tira de um documento os instrumentos de pagamento que ele carrega.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Ordenada por custo</strong> (doc 09). Texto embutido resolve o caso comum de graça;
/// a leitura de QR entra logo depois porque o BR Code, medido, existe só como imagem; o
/// extrator de visão é o último e o caro. Um degrau só roda se o anterior falhou.
/// </para>
/// <para>
/// <strong>A saída é candidato, nunca verdade.</strong> Mas é candidato <em>já validado</em>:
/// os tipos que a porta devolve não têm instância inválida — <c>DigitableLine</c> exige DV e
/// <c>PixPayload</c> exige CRC. O que decide se o título existe continua sendo a consulta
/// oficial (ADR-011).
/// </para>
/// <para>
/// <strong>Não lança por documento ilegível.</strong> "Não é boleto" é o desfecho mais comum
/// numa caixa de uso misto e vem como <c>ExtractionResult.NotFound</c>.
/// </para>
/// </remarks>
public interface IBoletoDocumentParser
{
    /// <param name="content">Os bytes do documento como recebido, decifrado só em memória.</param>
    /// <param name="contentType">Tipo declarado pelo provedor; a cascata confere os bytes mágicos.</param>
    /// <param name="passwordCandidates">
    /// Senhas derivadas do cadastro do tenant, na ordem do degrau 0 do doc 09. A porta para no
    /// primeiro acerto e devolve <strong>qual campo</strong> abriu — jamais a senha.
    /// </param>
    /// <param name="today">
    /// A data corrente entra por parâmetro porque o fator de vencimento da linha digitável é
    /// ambíguo entre duas épocas (rollover de 2025-02-22) — e o domínio não lê relógio.
    /// </param>
    Task<ExtractionResult> ParseAsync(
        ReadOnlyMemory<byte> content,
        string? contentType,
        IReadOnlyList<PasswordCandidate> passwordCandidates,
        DateOnly today,
        CancellationToken cancellationToken);
}
