namespace BillPayment.Domain.Services;

using BillPayment.Domain.TrustedOrigins;

/// <summary>
/// Decide se vale pagar o extrator de visão por este artefato.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Palavra-chave aqui decide GASTAR, nunca descartar.</strong> A distinção é a lição mais
/// cara desta fase: filtrar por "conta"/"boleto" antes da cascata apagaria boleto de verdade em
/// silêncio — foi medido na caixa real que existe cobrança com assunto <em>"Sua fatura chegou"</em>,
/// sem nenhuma das duas palavras, e o mesmo vale para FGTS, DARF, GPS e "2ª via". Aqui errar
/// custa centavos: um documento que não é boleto passa pelo modelo e volta vazio.
/// </para>
/// <para>
/// <strong>Sem este portão, o gasto seria desproporcional.</strong> Na medição de 2026-08-11, dos
/// 404 anexos varridos apenas 95 chegariam ao extrator com o cadastro carregado — os outros 250
/// vinham de remetente desconhecido e sem sinal nenhum de cobrança. Mandar todos custaria quatro
/// vezes mais para achar os mesmos boletos.
/// </para>
/// <para>
/// Estático e puro, como os outros Domain Services do BC: sem estado, sem I/O, sem relógio.
/// </para>
/// </remarks>
public static class VisionGateService
{
    /// <summary>
    /// Sinais de cobrança no assunto ou no nome do arquivo.
    /// </summary>
    /// <remarks>
    /// Deliberadamente largo: inclui o vocabulário das guias (FGTS, DARF, GPS, DAE) e o das
    /// concessionárias ("fatura", "conta de"), porque um falso positivo custa uma chamada barata
    /// e um falso negativo custa um boleto não pago. Comparação sem acento e sem caixa — o
    /// assunto real vem em MAIÚSCULAS, com e sem acento, dos dois jeitos.
    /// </remarks>
    public static bool ShouldAttempt(TrustedOrigin? origin, string? subject, string? artifactKey)
        // A lista de palavras saiu daqui para `BillingSignal` quando o portão do corpo e a
        // triagem passaram a precisar do mesmo juízo. Três cópias divergiriam, e a divergência
        // apareceria como "esse e-mail some e aquele não" sem que ninguém achasse a causa.
        => BillingSignal.IsStrong(origin, subject, artifactKey);
}
