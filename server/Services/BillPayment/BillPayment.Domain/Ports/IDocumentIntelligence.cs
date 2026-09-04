namespace BillPayment.Domain.Ports;

using BillPayment.Domain.Extraction;

/// <summary>
/// Lê um documento que a cascata determinística não resolveu, e <strong>propõe</strong> candidatos.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Nenhum termo de IA cruza esta porta</strong> — sem <c>model</c>, <c>prompt</c>,
/// <c>token</c>, <c>schema</c> ou <c>temperature</c>. Endpoint, autenticação, montagem do
/// request, structured output, retentativa, Batch e contagem de custo vivem no adapter, e o
/// prompt é detalhe de implementação: provedores diferentes pedem prompts diferentes (ADR-013).
/// Trocar de IA é um adapter novo mais uma linha de configuração.
/// </para>
/// <para>
/// <strong>O que volta é candidato, não resposta.</strong> Toda string proposta atravessa DV,
/// CRC, filtro de plausibilidade e consulta oficial antes de virar qualquer coisa (ADR-011). O
/// teste mais valioso da suíte é o que prova que uma linha digitável alucinada é barrada.
/// </para>
/// <para>
/// <strong>Falha do provedor não trava a ingestão.</strong> Timeout, limite de taxa ou erro
/// devolvem uma <see cref="ExtractionAttempt"/> cujo status diz SE vale tentar de novo; quem chama trata como "não resolvi" e o
/// artefato vai para a quarentena com motivo próprio. Nunca "aprova sem extrair" — e por isso
/// esta porta <strong>não lança</strong> por indisponibilidade, do mesmo modo que a consulta
/// oficial modela a falha em vez de lançá-la.
/// </para>
/// <para>
/// A triagem de mensagem por modelo (doc 10) <strong>não está aqui de propósito</strong>: quem
/// decide se vale gastar é um filtro determinístico e gratuito antes desta chamada. Uma chamada
/// de triagem custaria, em boa parte dos casos, mais do que a extração que ela evitaria.
/// </para>
/// </remarks>
public interface IDocumentIntelligence
{
    /// <summary>Se há extrator configurado. <c>false</c> quando a cascata termina no determinístico.</summary>
    bool IsEnabled { get; }

    Task<ExtractionAttempt> ExtractAsync(
        DocumentPayload payload,
        ExtractionHints hints,
        CancellationToken cancellationToken);
}
