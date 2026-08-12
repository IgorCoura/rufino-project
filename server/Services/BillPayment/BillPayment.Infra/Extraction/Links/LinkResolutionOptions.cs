namespace BillPayment.Infra.Extraction.Links;

using BillPayment.Domain.Extraction;

/// <summary>
/// Quais endereços o sistema pode buscar, e com que teto.
/// </summary>
/// <remarks>
/// <strong>É allowlist, não bloqueio.</strong> Sem receita nenhuma o resolvedor não busca nada —
/// a degradação segura é não sair para a rede. O caminho oposto (buscar tudo menos o que estiver
/// numa lista de proibidos) transformaria cada e-mail recebido num pedido de requisição arbitrária
/// partindo de dentro da rede, que é a definição de SSRF.
/// </remarks>
public sealed class LinkResolutionOptions
{
    public const string SectionName = "LinkResolution";

    /// <summary>
    /// Desliga a escada inteira. <strong>Ligado por padrão</strong> — ao contrário do extrator de
    /// visão, aqui não há custo por documento nem cota a queimar, e sem receita configurada nada
    /// acontece de qualquer forma.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Teto por requisição. Curto porque o worker é serial: uma busca pendurada não atrasa um
    /// documento, atrasa a fila inteira.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 20;

    /// <summary>
    /// Teto de bytes por documento buscado. Mesmo número do extrator de visão, e pelo mesmo
    /// motivo: documento de cobrança maior que isto é outra coisa.
    /// </summary>
    public int MaxBytes { get; set; } = DocumentPayload.MAX_BYTES;

    /// <summary>
    /// Quantas requisições uma única mensagem pode provocar, somando os dois saltos.
    /// </summary>
    /// <remarks>
    /// É o que impede um e-mail construído de propósito — dezenas de links para o mesmo host
    /// autorizado — de virar um amplificador de tráfego saindo da nossa rede.
    /// </remarks>
    public int MaxFetchesPerMessage { get; set; } = 4;

    /// <summary>
    /// As receitas. Vazio significa escada desligada; os padrões medidos são aplicados no registro
    /// da DI quando a configuração não traz nenhuma.
    /// </summary>
    public IList<LinkRecipe> Recipes { get; set; } = [];
}

/// <summary>
/// Como buscar o documento de um emissor específico.
/// </summary>
/// <remarks>
/// <para>
/// <strong>A receita é por host, e o host não é o do remetente.</strong> Medido em 2026-08-11: a
/// SABESP publica o PDF em <c>7az.com.br</c> e a EDP em <c>montreal.com.br</c> — terceirizadas sem
/// relação nenhuma com o domínio do e-mail. Derivar autorização do remetente recusaria os dois
/// casos reais e ainda autorizaria qualquer coisa hospedada no domínio de quem mandou.
/// </para>
/// <para>
/// <strong>A porta faz parte da receita.</strong> O PDF da SABESP vive em <c>:7446</c>; uma regra
/// que assumisse 443 perderia o único documento hoje alcançável por download direto.
/// </para>
/// </remarks>
public sealed class LinkRecipe
{
    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 443;

    /// <summary>
    /// Prefixo de caminho que identifica o documento, em minúsculas. Nulo aceita qualquer caminho
    /// do host.
    /// </summary>
    /// <remarks>
    /// É o que separa o link do boleto dos outros links do mesmo emissor — no e-mail do condomínio,
    /// <c>/bill/</c> distingue o botão do boleto do <c>/emailadvertisingclick/</c> logo abaixo dele.
    /// </remarks>
    public string? PathPrefix { get; set; }

    /// <summary>
    /// Se o endereço já responde com o documento, sem página intermediária.
    /// </summary>
    public bool DirectDocument { get; set; }

    /// <summary>
    /// Hosts autorizados para o segundo salto, quando a primeira resposta é uma página.
    /// </summary>
    /// <remarks>
    /// <strong>Um salto, e só para host declarado aqui.</strong> Seguir link encontrado dentro de
    /// página buscada é a parte perigosa da escada: sem esta lista, o conteúdo de uma página de
    /// terceiro passaria a decidir o que a nossa rede requisita. Vazio permite apenas o próprio
    /// host da receita.
    /// </remarks>
    public IList<string> FollowHosts { get; set; } = [];
}
