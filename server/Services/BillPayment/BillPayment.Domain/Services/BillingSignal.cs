namespace BillPayment.Domain.Services;

using BillPayment.Domain.TrustedOrigins;

/// <summary>
/// Se um e-mail se parece com cobrança — a evidência fraca, usada só para decidir esforço.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Este juízo NUNCA decide descartar.</strong> É a regra mais importante daqui, e ela
/// vem medida: existe cobrança de verdade com assunto "Sua fatura chegou", sem nenhuma das
/// palavras óbvias. Um portão por palavra-chave que apagasse o que não casa perderia boleto em
/// silêncio — o modo de falha que o ADR-014 existe para impedir. Aqui a heurística só decide
/// <em>gastar</em>: olhar o corpo, chamar o extrator, guardar para uma pessoa conferir. Quem
/// decide o que é boleto continua sendo o DV da linha e o CRC do BR Code.
/// </para>
/// <para>
/// <strong>Nasceu privado dentro do <see cref="VisionGateService"/></strong> e saiu de lá quando
/// o portão do corpo e a triagem passaram a precisar do mesmo juízo. Três cópias da mesma lista
/// divergiriam, e a divergência apareceria como "esse e-mail some e aquele não" sem que ninguém
/// achasse a causa.
/// </para>
/// </remarks>
public static class BillingSignal
{
    /// <summary>
    /// Palavras que aparecem em cobrança de verdade — e em muita coisa que não é.
    /// </summary>
    /// <remarks>
    /// A lista é deliberadamente ampla: o custo de um falso positivo é um item que a cascata
    /// descarta; o de um falso negativo é uma conta que ninguém viu.
    /// </remarks>
    private static readonly string[] Signals =
    [
        "boleto", "fatura", "conta", "cobranca", "cobrança", "pagamento", "vencimento", "vence",
        "2a via", "2ª via", "segunda via", "duplicata", "mensalidade", "parcela",
        "fgts", "darf", "gps", "dae", "das", "guia", "tributo", "imposto", "contribuicao", "contribuição",
        "condominio", "condomínio", "aluguel", "sindicato", "seguro", "energia", "agua", "água",
    ];

    /// <summary>
    /// Evidência forte o bastante para justificar esforço: remetente cadastrado, ou sinal no texto.
    /// </summary>
    /// <param name="origin">
    /// A origem que casou com o remetente, ou <c>null</c>. <strong>Origem banida não conta</strong>:
    /// o tenant já disse que não quer nada dali, e insistir contrariaria a decisão dele.
    /// </param>
    /// <param name="texts">
    /// Assunto e nome do anexo — <strong>nunca o endereço do remetente</strong>. A comparação é
    /// por substring, e endereço de e-mail a arruína: <c>contato@</c> e <c>contabilidade@</c>
    /// contêm "conta", e o segundo é o endereço do contador, que o corpus mediu como origem de
    /// 72 dos 95 itens de quarentena. Casar contra o remetente inundaria a fila com holerite e
    /// nota fiscal — e uma fila que ninguém olha é pior que fila nenhuma. Nulos são ignorados.
    /// </param>
    public static bool IsStrong(TrustedOrigin? origin, params string?[] texts)
    {
        if (origin is not null && origin.Decision != TrustDecision.Blocked)
            return true;

        return texts is not null && Array.Exists(texts, IsPresentIn);
    }

    /// <summary>Se o texto traz alguma das palavras de cobrança.</summary>
    public static bool IsPresentIn(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var normalized = text.ToLowerInvariant();

        return Array.Exists(
            Signals,
            signal => normalized.Contains(signal, StringComparison.Ordinal));
    }
}
