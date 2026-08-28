namespace BillPayment.Domain.Ports;

using BillPayment.Domain.Extraction;
using BillPayment.Domain.SharedKernel;

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
    /// <param name="knownTaxIds">
    /// Os documentos fiscais do tenant, como estão no cadastro. A varredura os procura
    /// <strong>diretamente</strong> no texto, em vez de descobrir números que se pareçam com
    /// documento e conferir depois.
    /// <para>
    /// Medido sobre 915 boletos reais em 2026-08-26: a busca dirigida acha o documento do tenant
    /// em 523 deles contra 469 da varredura por tamanho exato — <strong>+54, e nenhuma perda</strong>.
    /// O ganho é todo em documento impresso colado a outro número, que a regra de tamanho exato
    /// descartava. Falso positivo medido: <strong>zero</strong> — em 915 documentos o documento do
    /// tenant nunca apareceu por acaso dentro de outro número.
    /// </para>
    /// <para>
    /// Lista vazia é estado válido: o tenant sem cadastro fiscal cai na varredura genérica, que
    /// continua existindo para a pergunta oposta ("há aqui documento de OUTRA pessoa?").
    /// </para>
    /// </param>
    Task<ExtractionResult> ParseAsync(
        ReadOnlyMemory<byte> content,
        string? contentType,
        IReadOnlyList<PasswordCandidate> passwordCandidates,
        IReadOnlyList<TaxId> knownTaxIds,
        DateOnly today,
        CancellationToken cancellationToken);

    /// <summary>
    /// Devolve uma cópia do documento que abre <strong>sem senha</strong>, quando o original é
    /// cifrado e alguma candidata o abre. <c>null</c> significa "siga com o original".
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Existe porque decifrar em memória não basta: o extrator de visão recebe BYTES.</strong>
    /// O <c>ParseAsync</c> abre o PDF cifrado internamente e devolve o que leu, mas os bytes
    /// continuam sendo os cifrados — e eram esses que seguiam para o modelo, que respondia
    /// <c>400</c> sobre um arquivo que não consegue abrir. Medido em 2026-08-28: os três únicos
    /// boletos do acervo sem retrato de IA eram exatamente os três abertos por senha derivada.
    /// </para>
    /// <para>
    /// <strong>Ausência tem dois sentidos, e os dois pedem a mesma reação do chamador.</strong>
    /// Documento que já abre sem senha não precisa de cópia; documento que nenhuma candidata
    /// abriu não tem cópia a oferecer. Nos dois casos o chamador segue com o original — e quem
    /// distingue "não precisava" de "não consegui" é <c>ExtractionResult.UnlockedBy</c>, que o
    /// chamador já tem em mãos.
    /// </para>
    /// <para>
    /// <strong>Não devolve texto nem senha</strong> — só o documento, e nunca é logado (ADR-009).
    /// </para>
    /// </remarks>
    Task<ReadOnlyMemory<byte>?> UnlockAsync(
        ReadOnlyMemory<byte> content,
        string? contentType,
        IReadOnlyList<PasswordCandidate> passwordCandidates,
        CancellationToken cancellationToken);
}
