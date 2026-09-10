namespace BillPayment.Infra.Extraction.Links;

using BillPayment.Domain.Extraction;

/// <summary>
/// Quais endereços o sistema pode buscar, com que teto, e sob qual regime.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Há dois regimes, e a diferença entre eles é quem escolhe o destino.</strong> Em
/// <see cref="LinkResolutionMode.Allowlist"/> o destino sai de uma receita escrita por nós; em
/// <see cref="LinkResolutionMode.Open"/> ele sai do e-mail — isto é, de quem mandou a mensagem.
/// O segundo regime é o que permite descobrir emissor novo sem cadastro manual, e é também a
/// definição de SSRF: por isso ele só é defensável junto das travas que não dependem da receita
/// (faixa de IP conferida no <em>connect</em>, porta, profundidade, orçamento e teto por
/// remetente), e <strong>nunca é o padrão</strong>.
/// </para>
/// <para>
/// <strong>A allowlist não morre quando o regime abre.</strong> Ela deixa de ser a fronteira de
/// segurança e vira atalho: link que casa receita é buscado direto, sem descer a escada. O que
/// muda é o que acontece com o que <em>não</em> casa — recusa no regime fechado, escada no aberto.
/// </para>
/// </remarks>
public sealed class LinkResolutionOptions
{
    public const string SectionName = "LinkResolution";

    /// <summary>
    /// Desliga a escada inteira. <strong>Ligado por padrão</strong> — ao contrário do extrator de
    /// visão, aqui não há custo por documento nem cota a queimar.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// O regime. <strong>Fechado por padrão, de propósito</strong>: uma instalação nova nunca
    /// deve nascer buscando endereço escolhido por quem manda e-mail para a caixa.
    /// </summary>
    public LinkResolutionMode Mode { get; set; } = LinkResolutionMode.Allowlist;

    /// <summary>
    /// Teto até a resposta começar a chegar. Curto porque o worker é serial: uma busca pendurada
    /// não atrasa um documento, atrasa a fila inteira.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 20;

    /// <summary>
    /// Teto para <em>ler o corpo</em> da resposta, contado à parte.
    /// </summary>
    /// <remarks>
    /// <strong>Não é redundante com <see cref="TimeoutSeconds"/>.</strong> Com
    /// <c>HttpCompletionOption.ResponseHeadersRead</c>, o <c>HttpClient.Timeout</c> cobre até os
    /// cabeçalhos e <em>solta</em> a leitura do stream. Um servidor que responde 200 e depois
    /// entrega um byte por segundo segura o worker para sempre — slowloris, e do lado de fora não
    /// se distingue de um host lento honesto.
    /// </remarks>
    public int ReadTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Teto de tempo da <strong>escada inteira</strong>, somando todos os saltos.
    /// </summary>
    /// <remarks>
    /// Existe porque profundidade e orçamento limitam <em>quantas</em> requisições saem, não
    /// quanto tempo elas levam: doze buscas de vinte segundos são quatro minutos de worker parado
    /// numa mensagem só.
    /// </remarks>
    public int TotalTimeoutSeconds { get; set; } = 90;

    /// <summary>Teto de bytes por documento buscado.</summary>
    public int MaxBytes { get; set; } = DocumentPayload.MAX_BYTES;

    /// <summary>
    /// Quantas requisições uma única mensagem pode provocar, somando <strong>todos</strong> os
    /// níveis da escada.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>É este teto que impede a explosão combinatória, não a profundidade.</strong>
    /// Profundidade limita a forma da árvore; volume é o produto dos níveis. Com sessenta links
    /// por página e cinco níveis, uma árvore sem orçamento são 60^5 ≈ 777 milhões de requisições
    /// saindo da nossa rede por causa de um e-mail — que é um ataque de negação de serviço contra
    /// terceiros, com o nosso IP na origem.
    /// </para>
    /// <para>
    /// Doze é generoso para o caso real medido (Acessórias resolve em dois) e barato o bastante
    /// para não fazer diferença numa mensagem hostil.
    /// </para>
    /// </remarks>
    public int MaxFetchesPerMessage { get; set; } = 12;

    /// <summary>
    /// Quantas requisições podem cair no <strong>mesmo host</strong> dentro de uma mensagem.
    /// </summary>
    /// <remarks>
    /// Sem ele, o orçamento total inteiro pode ser gasto martelando um alvo só — que é a forma
    /// que a amplificação assume quando o atacante quer atingir um terceiro específico.
    /// </remarks>
    public int MaxFetchesPerHost { get; set; } = 3;

    /// <summary>
    /// Quantos níveis a escada desce: nível 1 é o link do e-mail, nível 2 o link achado na página
    /// que ele abriu, e assim por diante.
    /// </summary>
    public int MaxDepth { get; set; } = 5;

    /// <summary>
    /// Portas que a escada pode alcançar <strong>no regime aberto</strong>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// No regime fechado a porta já vem da receita, que é autorização explícita — é assim que o
    /// PDF da SABESP em <c>:7446</c> continua alcançável sem afrouxar nada aqui.
    /// </para>
    /// <para>
    /// <strong>Contra IP público, porta estranha é quase inofensiva</strong> — o estrago mora em
    /// alcançar porta interna, e disso cuida a conferência de faixa. Esta lista é defesa em
    /// profundidade: se um dia a conferência de faixa falhar, ela ainda barra <c>:6379</c>,
    /// <c>:5432</c> e <c>:11211</c>, que é o que um atacante quer.
    /// </para>
    /// </remarks>
    public IList<int> AllowedPorts { get; set; } = [80, 443];

    /// <summary>
    /// Faixas <strong>proibidas</strong> além das reservadas, em CIDR (<c>203.0.113.0/24</c>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>É aqui que entram os SEUS próprios endereços públicos.</strong> A conferência
    /// embutida recusa faixa reservada — privada, loopback, link-local, CGNAT —, mas não tem como
    /// saber que <c>203.0.113.10</c> é o IP público da sua própria VPS. E é justamente esse
    /// endereço que um atacante quer alcançar a partir de dentro: sua API, seu Keycloak e seu
    /// balde vivem lá, e frequentemente confiam em quem chega pelo IP de saída da própria
    /// instalação.
    /// </para>
    /// <para>
    /// Aceita IPv4 e IPv6. Entrada malformada é recusada no arranque, não ignorada em silêncio:
    /// uma faixa que ninguém percebeu que não vale é pior que faixa nenhuma.
    /// </para>
    /// </remarks>
    public IList<string> BlockedCidrs { get; set; } = [];

    /// <summary>
    /// Faixas liberadas <strong>por exceção</strong>, em CIDR — vencem qualquer recusa.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Existe para o emissor que hospeda o documento numa faixa que a regra geral recusa, e para
    /// ambiente de teste que precisa alcançar um host de laboratório. <strong>Use com parcimônia:
    /// cada entrada aqui é um pedaço da rede interna que uma mensagem de e-mail passa a poder
    /// endereçar.</strong>
    /// </para>
    /// <para>
    /// A ordem é deliberada — exceção vence proibição — porque o contrário tornaria a lista
    /// inútil: toda faixa que alguém precisa liberar está proibida por algum motivo, senão não
    /// precisaria ser liberada.
    /// </para>
    /// </remarks>
    public IList<string> AllowedCidrs { get; set; } = [];

    /// <summary>
    /// Hosts que a escada nunca busca, mesmo no regime aberto — casamento por sufixo de domínio.
    /// </summary>
    /// <remarks>
    /// Serve ao encurtador e ao rastreador que o desembrulho não desfaz: são endereços que só
    /// existem para levar a outro lugar, e buscá-los gasta orçamento entregando ao remetente a
    /// confirmação de que a mensagem foi processada.
    /// </remarks>
    public IList<string> BlockedHosts { get; set; } = [];

    /// <summary>
    /// Proxy de saída obrigatório para a escada (<c>http://bp-proxy:3128</c>). Vazio = sem proxy.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Preencher isto DESLIGA o IP pinado, e a troca é deliberada.</strong> Com proxy, a
    /// conexão TCP que o processo abre é para o <em>proxy</em> — não para o destino. Conferir o
    /// endereço ali validaria o IP do próprio proxy, que é interno e seria recusado, derrubando
    /// toda busca. Quem passa a decidir o que é alcançável é o proxy, e é por isso que ele precisa
    /// ser <em>default-deny</em> de verdade.
    /// </para>
    /// <para>
    /// <strong>Sem esta opção o proxy seria decoração.</strong> O handler da escada é construído à
    /// mão e nasceu com <c>UseProxy = false</c>, então ele ignorava <c>HTTP_PROXY</c> do ambiente —
    /// enquanto os demais clientes do BC (Graph, Asaas, Gemini) o respeitam por serem handlers
    /// padrão. Um operador que fechasse o egresso confiando na variável de ambiente teria fechado
    /// tudo <em>menos</em> o único cliente que busca endereço escolhido por terceiro.
    /// </para>
    /// </remarks>
    public string? Proxy { get; set; }

    /// <summary>
    /// Teto diário de buscas provocadas pelo <strong>mesmo remetente</strong>, no regime aberto.
    /// </summary>
    /// <remarks>
    /// O orçamento por mensagem limita um e-mail; este limita mil. Sem ele, quem consegue mandar
    /// e-mail para a caixa consegue gastar a rede, a fila e a cota de IA de todos os tenants —
    /// que é o achado A9 (filas sem justiça) visto pelo lado da entrada.
    /// </remarks>
    public int MaxFetchesPerSenderPerDay { get; set; } = 200;

    /// <summary>
    /// As receitas. No regime fechado, vazio significa escada desligada; os padrões medidos são
    /// aplicados no registro da DI quando a configuração não traz nenhuma.
    /// </summary>
    public IList<LinkRecipe> Recipes { get; set; } = [];
}

/// <summary>
/// Quem escolhe o endereço que a escada busca.
/// </summary>
public enum LinkResolutionMode
{
    /// <summary>
    /// Só endereço com receita nossa. Degradação segura, e o padrão.
    /// </summary>
    Allowlist = 0,

    /// <summary>
    /// Qualquer endereço que sobreviva às travas de rede, profundidade e orçamento.
    /// </summary>
    /// <remarks>
    /// <strong>Ligar isto move a fronteira de segurança para fora do código.</strong> A partir
    /// daqui, quem escolhe o destino da requisição é quem manda o e-mail, e o que impede o
    /// estrago é a conferência de faixa no <em>connect</em> — mais o egresso da rede, que é a
    /// única barreira que continua de pé se o código estiver errado.
    /// </remarks>
    Open = 1,
}

/// <summary>
/// Como buscar o documento de um emissor específico.
/// </summary>
/// <remarks>
/// <para>
/// <strong>A receita é por host, e o host não é o do remetente.</strong> Medido em 2026-08-11: a
/// SABESP publica o PDF em <c>7az.com.br</c> e a EDP em <c>montreal.com.br</c> — terceirizadas sem
/// relação nenhuma com o domínio do e-mail.
/// </para>
/// <para>
/// <strong>A porta faz parte da receita</strong>, e por isso ela dispensa
/// <see cref="LinkResolutionOptions.AllowedPorts"/>: o PDF da SABESP vive em <c>:7446</c>, e a
/// receita é a autorização explícita que a lista de portas genérica não teria como dar.
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
    public string? PathPrefix { get; set; }

    /// <summary>
    /// Se o endereço já responde com o documento, sem página intermediária.
    /// </summary>
    public bool DirectDocument { get; set; }

    /// <summary>
    /// Hosts autorizados para os saltos seguintes, quando a primeira resposta é uma página.
    /// </summary>
    /// <remarks>
    /// <strong>Só vale no regime fechado.</strong> No aberto não há lista a respeitar — a escada
    /// vai onde a página apontar, e quem segura é a faixa de IP, a porta, a profundidade e o
    /// orçamento. Vazio permite apenas o próprio host da receita.
    /// </remarks>
    public IList<string> FollowHosts { get; set; } = [];
}
