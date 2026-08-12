namespace BillPayment.Domain.Services;

using BillPayment.Domain.Extraction;

/// <summary>
/// Decide se o corpo de uma mensagem vira artefato capturado.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Existe porque nem toda mensagem tem anexo, e algumas contas nunca terão.</strong> A
/// Perfil Líder informa por escrito que <em>não envia boleto por e-mail</em> — só link. A SABESP
/// no formato novo manda o BR Code inteiro no texto do corpo. Antes desta sprint, a varredura
/// exigia <c>hasAttachments</c> e essas contas eram invisíveis.
/// </para>
/// <para>
/// <strong>Sem portão, toda mensagem viraria item.</strong> A caixa é de uso misto e a maioria do
/// que chega é conversa, pedido e propaganda; um <c>CaptureItem</c> por mensagem encheria a fila
/// de quarentena e ninguém olharia uma fila assim — é a mesma lição que fixou o descarte por
/// desfecho na 2.3.
/// </para>
/// <para>
/// <strong>Os três sinais são determinísticos, e nenhum é palavra-chave.</strong> Dois deles se
/// validam sozinhos adiante (o DV da linha e o CRC do BR Code); o terceiro exige que o link aponte
/// para um host que o sistema <em>sabe</em> resolver. Um portão por assunto — "boleto", "fatura" —
/// apagaria em silêncio a cobrança cujo assunto é "Sua fatura chegou", que é justamente a que foi
/// medida na caixa real.
/// </para>
/// </remarks>
public static class BodyCaptureGateService
{
    /// <summary>
    /// Menor linha digitável que existe (cobrança bancária). Abaixo disso não há o que conferir.
    /// </summary>
    private const int MIN_DIGIT_RUN = 47;

    /// <summary>Todo BR Code começa com o payload format indicator do EMV.</summary>
    private const string EMV_PREFIX = "000201";

    /// <summary>
    /// Se vale ingerir o corpo desta mensagem como artefato.
    /// </summary>
    /// <param name="plainText">O corpo já reduzido a texto, sem marcação.</param>
    /// <param name="links">Os links do corpo, já desembrulhados de rastreador.</param>
    /// <param name="resolvableHosts">
    /// Hosts para os quais existe receita de resolução configurada. Um link para host desconhecido
    /// <strong>não</strong> é sinal: o sistema não teria como buscar o documento, e o item nasceria
    /// só para morrer na quarentena.
    /// </param>
    public static bool ShouldCapture(
        string? plainText,
        IEnumerable<DocumentLink>? links,
        IReadOnlyCollection<string>? resolvableHosts)
    {
        if (CarriesInstrumentInText(plainText))
            return true;

        if (links is null || resolvableHosts is null || resolvableHosts.Count == 0)
            return false;

        return links.Any(link => resolvableHosts.Contains(link.Host, StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Se o texto carrega, à primeira vista, algo pagável.
    /// </summary>
    /// <remarks>
    /// <strong>Aproximação de propósito.</strong> É portão, não parser: quem confere DV e CRC é a
    /// cascata, depois. Aqui um falso positivo custa um item que será descartado; um falso negativo
    /// custa um boleto que nunca foi visto — e por isso o critério é frouxo para o lado de capturar.
    /// </remarks>
    private static bool CarriesInstrumentInText(string? plainText)
    {
        if (string.IsNullOrWhiteSpace(plainText))
            return false;

        if (plainText.Contains(EMV_PREFIX, StringComparison.Ordinal))
            return true;

        var run = 0;

        foreach (var character in plainText)
        {
            if (char.IsAsciiDigit(character))
            {
                if (++run >= MIN_DIGIT_RUN)
                    return true;

                continue;
            }

            // Mesma regra do varredor de candidatos: ponto, espaço e hífen são formatação que os
            // emissores usam para deixar a linha legível; qualquer outra coisa — inclusive quebra
            // de linha — encerra a sequência, senão números de linhas diferentes se emendariam.
            if (character is not ('.' or ' ' or '-' or '\t'))
                run = 0;
        }

        return false;
    }
}
