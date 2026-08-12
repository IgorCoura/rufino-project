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
}
