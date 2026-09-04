namespace BillPayment.Domain.Ports;

using BillPayment.Domain.Extraction;

/// <summary>
/// A escada de resolução de link: tira do corpo de uma mensagem o documento que ele aponta.
/// </summary>
/// <remarks>
/// <para>
/// <strong>É a maior superfície de ataque do BC</strong> — o único ponto em que o sistema busca,
/// por conta própria, um endereço que veio de fora. Por isso a implementação é fechada por
/// construção: só host explicitamente configurado, só <c>GET</c>, sem seguir redirecionamento, sem
/// alcançar endereço de rede interna, com teto de bytes e de tempo. Nada aqui envia formulário nem
/// preenche credencial: portal com login é a fase 5, e sem evasão de anti-bot (ADR-012).
/// </para>
/// <para>
/// <strong>A allowlist é por receita, nunca derivada do remetente.</strong> Medido em 2026-08-11: a
/// SABESP publica o PDF em <c>7az.com.br</c> e a EDP em <c>montreal.com.br</c> — terceirizadas cujo
/// domínio não tem relação nenhuma com o do e-mail. Derivar a autorização do domínio do remetente
/// recusaria os dois casos reais e ainda autorizaria qualquer coisa hospedada no domínio de quem
/// mandou.
/// </para>
/// <para>
/// <strong>Não lança por documento inalcançável.</strong> Link expirado, host fora do ar ou
/// resposta que não é documento devolvem <c>null</c> — a mensagem segue para a quarentena, como
/// qualquer outro artefato que a cascata não resolveu.
/// </para>
/// </remarks>
public interface IDocumentLinkResolver
{
    /// <summary>Se há alguma receita configurada. Sem receita, a escada inteira é pulada.</summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Os hosts que este resolvedor sabe buscar.
    /// </summary>
    /// <remarks>
    /// Exposto porque o portão de ingestão precisa dele: um link para host desconhecido não é sinal
    /// de boleto, já que o sistema não teria como buscar o documento — o item nasceria só para
    /// morrer na quarentena.
    /// </remarks>
    IReadOnlyCollection<string> ResolvableHosts { get; }

    /// <param name="body">O corpo da mensagem como veio — HTML ou texto.</param>
    /// <param name="contentType">Tipo declarado do corpo.</param>
    Task<ResolvedDocument?> ResolveAsync(
        ReadOnlyMemory<byte> body,
        string? contentType,
        CancellationToken cancellationToken);

    /// <summary>
    /// Os links do corpo já desembrulhados de rastreador — <strong>sem tocar a rede</strong>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Serve ao caso em que a escada não resolveu: saber <em>para onde</em> ela teria ido é o que
    /// transforma "não consegui" numa fila de emissores a cadastrar. Sem isto, o item cai na
    /// quarentena sem dizer de onde veio, e a informação que faltava para escrever a receita se
    /// perde justamente no caso em que ela é necessária.
    /// </para>
    /// <para>
    /// Devolve <strong>todos</strong> os links, inclusive os de host sem receita — é para eles
    /// que existe. O desembrulho não faz requisição: seguir o redirecionamento entregaria ao
    /// remetente a confirmação de leitura, e decodificar é mais barato e mais seguro.
    /// </para>
    /// </remarks>
    IReadOnlyCollection<DocumentLink> HarvestLinks(ReadOnlyMemory<byte> body, string? contentType);
}
