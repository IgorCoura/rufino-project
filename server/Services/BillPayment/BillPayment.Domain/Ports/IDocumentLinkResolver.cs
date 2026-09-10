namespace BillPayment.Domain.Ports;

using BillPayment.Domain.Extraction;

/// <summary>
/// A escada de resolução de link: tira do corpo de uma mensagem o documento que ele aponta.
/// </summary>
/// <remarks>
/// <para>
/// <strong>É a maior superfície de ataque do BC</strong> — o único ponto em que o sistema busca,
/// por conta própria, um endereço que veio de fora. A implementação é fechada por construção: o
/// endereço IP é conferido no <em>connect</em> (e não no nome, que o DNS pode trocar entre a
/// conferência e a conexão), a porta é restrita, só <c>GET</c>, sem seguir redirecionamento, sem
/// alcançar endereço de rede interna, com teto de bytes, de tempo, de profundidade e de
/// requisições. Nada aqui envia formulário nem preenche credencial: portal com login é a fase 5, e
/// sem evasão de anti-bot (ADR-012).
/// </para>
/// <para>
/// <strong>Há dois regimes.</strong> No fechado, só endereço com receita nossa é buscado — a
/// degradação segura, e o padrão. No aberto, o destino sai do e-mail, o que permite descobrir
/// emissor novo sem cadastro manual e é também a definição de SSRF: ali o que impede o estrago são
/// as travas acima, mais o egresso da rede.
/// </para>
/// <para>
/// <strong>Não lança por documento inalcançável.</strong> Link expirado, host fora do ar ou
/// resposta que não é documento devolvem um <see cref="LinkResolution"/> que <em>diz por quê</em> —
/// e a mensagem segue para a quarentena, como qualquer outro artefato que a cascata não resolveu.
/// </para>
/// </remarks>
public interface IDocumentLinkResolver
{
    /// <summary>Se a escada busca alguma coisa nesta instalação.</summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Os hosts para os quais existe receita.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Exposto porque o portão de ingestão o usa como sinal barato: link para host com receita é
    /// evidência suficiente de que a mensagem carrega documento, sem precisar de mais nada.
    /// </para>
    /// <para>
    /// <strong>No regime aberto ele continua sendo só as receitas, e isso é deliberado.</strong>
    /// Devolver "todos os hosts" faria o portão capturar toda mensagem com qualquer link, e a fila
    /// de quarentena — que é onde uma pessoa trabalha — deixaria de ser utilizável. Host sem
    /// receita continua entrando pelo quarto sinal do portão, que exige evidência de cobrança.
    /// </para>
    /// </remarks>
    IReadOnlyCollection<string> ResolvableHosts { get; }

    /// <param name="body">O corpo da mensagem como veio — HTML ou texto.</param>
    /// <param name="contentType">Tipo declarado do corpo.</param>
    /// <param name="sender">
    /// Quem mandou a mensagem, para o teto diário por remetente. Nulo dispensa o teto — é o caso
    /// do artefato que não veio de caixa de e-mail.
    /// </param>
    Task<LinkResolution> ResolveAsync(
        ReadOnlyMemory<byte> body,
        string? contentType,
        string? sender,
        CancellationToken cancellationToken);
}
