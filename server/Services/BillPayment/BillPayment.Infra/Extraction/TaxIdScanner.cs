namespace BillPayment.Infra.Extraction;

using System.Text.RegularExpressions;
using BillPayment.Domain.Extraction;
using BillPayment.Domain.SharedKernel;

/// <summary>
/// Varre texto solto atrás dos documentos fiscais impressos no artefato — o insumo do degrau 1
/// da escada de roteamento.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Sequência exata, nunca janela deslizante</strong> — e esta é a diferença entre a
/// varredura de documento fiscal e a de linha digitável, que faz o oposto. O CNPJ tem só dois
/// dígitos verificadores, então uma janela de 14 dígitos tem cerca de 1% de chance de passar por
/// acaso; um código de barras de 44 posições oferece trinta e uma janelas. Medido em 2026-08-12
/// sobre 714 documentos: a regra deslizante <strong>fabricaria um CNPJ aparentemente válido
/// dentro do código de barras em 46,9% deles</strong>. Como um documento fabricado pode cair ao
/// lado de um rótulo de pagador, ele mandaria para a quarentena cega uma conta legítima.
/// </para>
/// <para>
/// Exigir que a sequência inteira tenha 11 ou 14 dígitos elimina isso por construção — um bloco
/// de 44 não é nenhum dos dois. <strong>Mas custa cobertura</strong>, ao contrário do que a
/// medição de 2026-08-12 sugeria: aquele número (93,3%) saiu de texto lido por
/// <c>pdftotext -layout</c>, que separa os campos. O sistema lê por PdfPig, que entrega tudo
/// emendado — e aí documento fiscal colado a outro número deixa de ter 11 ou 14 dígitos e é
/// descartado. Medido em 2026-08-26 sobre 915 boletos reais, lidos <em>pelo caminho do código</em>:
/// tamanho exato acha 469; com a busca dirigida, 523.
/// </para>
/// <para>
/// <strong>Por isso há dois degraus.</strong> A busca dirigida responde "este boleto é do
/// tenant?" e é imune ao emendamento. A varredura por tamanho exato continua respondendo a
/// pergunta oposta — "há aqui documento de OUTRA pessoa?" —, que a dirigida é incapaz de
/// responder, e é ela que sustenta o <c>ForeignPayer</c> e o isolamento do ADR-008.
/// </para>
/// <para>
/// <strong>O DV continua sendo quem decide.</strong> A varredura propõe a sequência e
/// <c>PartyCandidate.TryCreate</c> reprova o que não fecha (ADR-011).
/// </para>
/// <para>
/// Nada aqui é logado: documento fiscal de pagador é dado pessoal.
/// </para>
/// </remarks>
internal static partial class TaxIdScanner
{
    private const int CPF_LENGTH = 11;
    private const int CNPJ_LENGTH = 14;

    /// <summary>
    /// Quanto texto antes da ocorrência é examinado atrás do rótulo. 260 caracteres é o que
    /// cobre o bloco "Pagador / nome / endereço / CNPJ" dos layouts medidos sem alcançar o bloco
    /// anterior do documento.
    /// </summary>
    private const int LABEL_LOOKBACK = 260;

    /// <summary>
    /// Teto de documentos fiscais por artefato. Uma folha de rosto de contabilidade lista
    /// dezenas; passar disso não melhora o roteamento e só alimenta laço.
    /// </summary>
    private const int MAX_PARTIES = 40;

    /// <summary>
    /// Os cinco rótulos observados no corpus, por frequência: pagador (103), tomador (92),
    /// sacado (4), cliente (4) — mais contribuinte, que é o de guia de tributo.
    /// </summary>
    /// <remarks>
    /// <strong>Sem borda de palavra, e isso é deliberado.</strong> <c>sacado</c> casa dentro de
    /// <c>Sacador</c> — rótulo do outro lado, presente em quase todo boleto — e parece defeito.
    /// Não é: os dois detectores casam no <em>mesmo índice</em>, e o desempate é <c>&gt;</c>
    /// estrito, então o empate já resolve para "não é rótulo de pagador", que é a resposta certa.
    /// Acrescentar <c>\b</c> quebra o caso real, medido em 2026-08-26: o PdfPig entrega o texto
    /// emendado (<c>PagadorRUFINO EMPREITEIRA</c>) e a borda de palavra nunca fecha.
    /// </remarks>
    [GeneratedRegex(@"pagador|sacado|tomador|cliente|contribuinte|devedor",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex PayerLabel();

    /// <summary>
    /// O outro lado. Existe para desempatar: num boleto os dois blocos são vizinhos, e sem isto
    /// o rótulo do credor seria confundido com o do devedor por simples proximidade.
    /// </summary>
    [GeneratedRegex(@"benefici[áa]rio|cedente|favorecido|credor|sacador",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex PayeeLabel();

    /// <param name="knownTaxIds">
    /// Documentos do cadastro do tenant, procurados <strong>diretamente</strong> no texto antes
    /// da varredura genérica. Vazio desliga o degrau dirigido.
    /// </param>
    public static IReadOnlyList<PartyCandidate> Scan(string? text, IReadOnlyList<TaxId>? knownTaxIds = null)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        var found = new List<PartyCandidate>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        // Degrau dirigido: procura o que o tenant declarou ter. Encontra o documento impresso
        // colado a outro número — o caso que a regra de tamanho exato descarta e que vale +54
        // documentos em 915 medidos. Vem antes para que a ocorrência do PRÓPRIO tenant entre na
        // deduplicação primeiro.
        AddDirectMatches(text, knownTaxIds, found, seen);

        foreach (var (digits, start) in DigitRuns(text))
        {
            if (found.Count >= MAX_PARTIES)
                break;

            var candidate = PartyCandidate.TryCreate(digits, IsUnderPayerLabel(text, start));

            // Deduplica por documento E por rótulo: o mesmo CNPJ costuma aparecer no bloco do
            // pagador e no rodapé sem rótulo nenhum, e é a ocorrência rotulada que importa.
            if (candidate is not null && seen.Add($"{candidate.TaxId.Value}|{candidate.UnderPayerLabel}"))
                found.Add(candidate);
        }

        return found;
    }

    /// <summary>
    /// Procura cada documento cadastrado dentro das sequências de dígitos do texto.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Aqui não há dígito verificador a conferir</strong>, e não é omissão: o documento
    /// veio do cadastro, onde <c>TaxId.Parse</c> já o provou na gravação. O que esta busca decide
    /// é presença, não validade.
    /// </para>
    /// <para>
    /// <strong>Por que é seguro procurar o documento dentro de um número maior.</strong> A chance
    /// de um documento específico de 14 dígitos aparecer por acaso num código de barras de 44 é
    /// da ordem de 3 em 10 trilhões. Medido em 915 boletos reais: das 92 ocorrências dentro de
    /// sequências longas, 52 estavam no início e 38 no fim — campo colado ao vizinho, como o
    /// IPTU e o DARF fazem com o código de arrecadação — e as 2 do meio eram extrato bancário
    /// com o CNPJ entre dois campos. Nenhuma coincidência. O que sobra de risco teórico é coberto
    /// pelo check <c>PayerMatch</c>, que bloqueia se o documento estiver dentro do código de
    /// barras validado.
    /// </para>
    /// </remarks>
    private static void AddDirectMatches(
        string text,
        IReadOnlyList<TaxId>? knownTaxIds,
        List<PartyCandidate> found,
        HashSet<string> seen)
    {
        if (knownTaxIds is null || knownTaxIds.Count == 0)
            return;

        foreach (var (digits, start) in AllDigitRuns(text))
        {
            foreach (var value in knownTaxIds
                .Select(known => known.Value)
                .Where(value => digits.Contains(value, StringComparison.Ordinal)))
            {
                // O rótulo é aferido no começo da sequência que contém o documento. Num campo
                // colado ao vizinho os dois pontos diferem por poucos caracteres, e a janela de
                // 260 que procura o rótulo cobre a diferença.
                var candidate = PartyCandidate.TryCreate(value, IsUnderPayerLabel(text, start));

                if (candidate is not null && seen.Add($"{candidate.TaxId.Value}|{candidate.UnderPayerLabel}"))
                    found.Add(candidate);
            }
        }
    }

    /// <summary>
    /// Toda sequência de dígitos do texto, sem exigência de tamanho — o insumo da busca dirigida.
    /// </summary>
    private static IEnumerable<(string Digits, int Start)> AllDigitRuns(string text)
    {
        var length = 0;
        var start = -1;

        for (var i = 0; i <= text.Length; i++)
        {
            var character = i < text.Length ? text[i] : '\n';

            if (char.IsAsciiDigit(character))
            {
                if (length == 0)
                    start = i;

                length++;
                continue;
            }

            if (character is '.' or '-' or '/' or ' ' or '\t')
                continue;

            if (length > 0)
                yield return (Digits(text, start, i), start);

            length = 0;
        }
    }

    /// <summary>
    /// Sequências de dígitos de tamanho exato, com a posição onde começam no texto original.
    /// </summary>
    /// <remarks>
    /// A barra entra na formatação ignorada — sem ela <c>02.624.917/0001-92</c> viraria duas
    /// sequências curtas e nenhum CNPJ seria lido. Quebra de linha encerra a sequência, pelo
    /// mesmo motivo do <c>CandidateScanner</c>: emendar linhas diferentes produz números que não
    /// existem no documento.
    /// </remarks>
    private static IEnumerable<(string Digits, int Start)> DigitRuns(string text)
    {
        var length = 0;
        var start = -1;

        for (var i = 0; i <= text.Length; i++)
        {
            var character = i < text.Length ? text[i] : '\n';

            if (char.IsAsciiDigit(character))
            {
                if (length == 0)
                    start = i;

                length++;
                continue;
            }

            if (character is '.' or '-' or '/' or ' ' or '\t')
                continue;

            if (length is CPF_LENGTH or CNPJ_LENGTH)
                yield return (Digits(text, start, i), start);

            length = 0;
        }
    }

    private static string Digits(string text, int start, int end)
        => string.Concat(text[start..end].Where(char.IsAsciiDigit));

    /// <summary>
    /// O rótulo mais próximo antes da ocorrência é de pagador, e não de beneficiário.
    /// </summary>
    /// <remarks>
    /// <strong>Só serve para negar, nunca para afirmar.</strong> A atribuição do boleto vem de
    /// casar com o cadastro do próprio tenant, que é seguro sem rótulo nenhum; o rótulo é o que
    /// autoriza a afirmação contrária — "este é de outra pessoa" —, e só 66,8% das ocorrências o
    /// têm. Ver <c>BillRoutingService</c>.
    /// </remarks>
    private static bool IsUnderPayerLabel(string text, int start)
    {
        var window = text[Math.Max(0, start - LABEL_LOOKBACK)..start];

        var payer = LastIndexOf(PayerLabel(), window);
        return payer >= 0 && payer > LastIndexOf(PayeeLabel(), window);
    }

    private static int LastIndexOf(Regex pattern, string window)
    {
        var index = -1;

        try
        {
            foreach (var match in pattern.Matches(window).Cast<Match>())
                index = match.Index;
        }
        catch (RegexMatchTimeoutException)
        {
            // Janela patológica: sem rótulo é o lado seguro de errar — nunca vira ForeignPayer
            // por um regex que não terminou.
            return -1;
        }

        return index;
    }
}
