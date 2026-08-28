# gotchas.md — BillPayment

Registro de correções e lições aprendidas neste BC (ver regra em CLAUDE.md → Self-Correction).

## Object Mother com `??` engole a invariante que o teste queria provar

**Quando:** 2026-07-31, sprint 1.1 (`TrustedOrigin`).

**O que aconteceu:** o Mother expunha `Register(OriginKind? kind = null, ...)` e resolvia com `kind ?? OriginKind.EmailAddress`. Os testes que provavam "tipo nulo é recusado" chamavam `Register(kind: null!)` — e o `??` substituía o nulo pelo default, então o agregado era criado com sucesso e o `Assert.Throws` falhava.

**Por que é traiçoeiro:** se o agregado *não* tivesse a validação, esses dois testes falhariam exatamente do mesmo jeito. Eles nunca teriam pegado a ausência da regra — só apareceram porque falharam por sorte.

**Regra:** todo Mother tem dois caminhos.

- `Register(...)` com parâmetros opcionais e defaults — caminho feliz, onde o setup não é o assunto do teste.
- `RegisterVerbatim(...)` com parâmetros **obrigatórios, repassados sem coalescer** — o único caminho usado por teste de invariante que rejeita argumento nulo ou inválido.

`Register` delega para `RegisterVerbatim`, então a construção real fica num lugar só. Aplicado em `TrustedOriginMother`, `PayeeMother` e `PayerProfileMother`.

**Como pegar de novo:** ao escrever um teste `*_ShouldThrow_*` que passa argumento nulo ou inválido, confira se ele atravessa o Mother sem passar por `??`. Se passar, o teste está mentindo.

## Índice do EF não alcança propriedade de owned type

**Quando:** 2026-07-31, sprint 1.1 (`Payee`).

**O que aconteceu:** `TaxId` foi mapeado como owned type (`OwnsOne`) achatado em duas colunas, e o índice único de unicidade por tenant foi declarado como `builder.HasIndex(e => new { e.TenantId, e.TaxId!.Value })`. **Compila sem reclamar.** Em runtime, na construção do modelo, o EF lança `ArgumentException: The expression ... is not a valid member access expression`.

**Por que é traiçoeiro:** a falha derruba o `OnModelCreating` inteiro, então **todos** os testes de integração quebram de uma vez — inclusive o `HealthCheckTests`, que não tem nada a ver com `Payee`. A mensagem aponta o mapping certo, mas o sintoma sugere que a infra de teste quebrou.

**Regra:** o construtor de índice só enxerga propriedades do **próprio** tipo. Se um valor precisa entrar num índice composto com a raiz, ele não pode ser owned. Quando o resto do VO é dedutível do valor guardado — `TaxId` é: 11 dígitos é CPF, 14 é CNPJ — mapeie como **uma coluna escalar com `HasConversion` + `ValueComparer`** e reidrate pela factory do domínio (`TaxId.Parse`). Ganha-se o índice, a comparação direta em LINQ (`p.TaxId == taxId`) e a revalidação do DV na leitura.

**Como pegar de novo:** ao escrever `HasIndex` com mais de uma propriedade, confira se alguma vem de `OwnsOne`. Se vier, o índice não existe — decida entre coluna escalar convertida ou abrir mão do índice.

## Valor com barra em segmento de rota morre em 404 antes do controller

**Quando:** 2026-07-31, sprint 1.1 (`Payee`, `PayerProfile`).

**O que aconteceu:** `GET api/v1/{tenantId}/payees/by-tax-id/{taxId}` funcionava com `11222333000181` e devolvia 404 com `11.222.333/0001-81`. A barra do CNPJ formatado vira separador de segmento e nenhuma rota casa — o request nem chega ao controller, então não há erro de domínio para diagnosticar.

**Regra:** valor livre (documento formatado, apelido, razão social) vai em **query string**; só fica no path o que é sabidamente seguro — `{id:guid}`, código de banco de três dígitos. Vale também para `DELETE`, onde a tentação de pôr a identidade do item no path é maior.

**Como pegar de novo:** todo endpoint com `{algumaCoisa}` que não seja `:guid` ou `:int` precisa de um teste com o valor na forma que o usuário realmente digita — formatada, com espaço, com acento.

## `EnsureCreatedAsync` não cria a tabela que faltou — ele não faz nada

**Quando:** 2026-08-11, ensaio de ponta a ponta da fase 2.

**O que aconteceu:** a API local subia, mas todo endpoint de captura devolvia **500** com `42P01: relation "bill_payment.capture_sources" does not exist`. O banco de desenvolvimento tinha sido criado semanas antes, na época do walking skeleton, com **4 tabelas de plataforma** e nenhum agregado. `EnsureCreatedAsync` decide por "o banco tem schema?" — não por "o schema bate com o modelo?". Achando qualquer tabela, ele retorna sem fazer nada, e todo Aggregate criado depois (`Bill`, `Payee`, `TrustedOrigin`, `PayerProfile`, `CaptureSource`, `CaptureItem`) nunca ganhou tabela.

**Por que é traiçoeiro:** a aplicação **sobe com êxito**. Não há erro de startup, o log diz `Now listening on`, e o Swagger abre. A falha só aparece na primeira consulta, como erro de banco — que parece problema de conexão ou de permissão, não de schema desatualizado. E é silencioso por tempo indefinido: a API em contêiner respondia 500 havia **4 dias** sem ninguém notar, porque ninguém a tinha chamado. A suíte de integração não pega: o Testcontainers sobe um Postgres **vazio** a cada execução, que é exatamente o caso em que `EnsureCreatedAsync` funciona.

**Regra:** `EnsureCreatedAsync` só serve para banco descartável e vazio. Depois de acrescentar Aggregate, **recrie o schema de desenvolvimento** (`DROP SCHEMA bill_payment CASCADE` + reiniciar a API) em vez de esperar que ele se atualize. A correção definitiva é o item já listado no "Checklist pré-produção": trocar por `MigrateAsync` e gerar as migrações — este incidente é a demonstração de que o item não é burocracia.

**Como pegar de novo:** ao ver `42P01 relation ... does not exist` num banco que "já funcionava", conte as tabelas antes de investigar conexão:
`SELECT count(*) FROM information_schema.tables WHERE table_schema='bill_payment';` — hoje o modelo tem 12.

## Serviço compatível com S3 recusa a assinatura quando falta a região

**Quando:** 2026-08-11, ao configurar o balde de anexos pela primeira vez.

**O que aconteceu:** `StorageOptions` tinha `ServiceUrl`, `AccessKey`, `SecretKey` e `ForcePathStyle`, mas **não tinha região**. O BC irmão `PeopleManagement`, que fala com o mesmo servidor Garage, configura `AuthenticationRegion = "garage"` — e o Garage recusa SigV4 assinado com outra região.

**Por que é traiçoeiro:** não existe erro de configuração. O `AmazonS3Client` é construído sem reclamar, a aplicação sobe, e a falha só aparece na primeira gravação de anexo, como `SignatureDoesNotMatch` — que lê como credencial errada e manda investigar a chave, não a região. Pior: no BillPayment ela cairia **dentro do worker de processamento**, virando "todo item falha ao processar" sem apontar para o storage.

**Regra:** `AuthenticationRegion` entra em `IsConfigured` e **não tem default**. Qualquer default estaria errado para metade dos alvos (Garage usa `garage`, MinIO e a maioria usam `us-east-1`), então a ausência desliga o armazenamento — que falha alto e explicitamente — em vez de adivinhar e falhar tarde.

**Como pegar de novo:** ao apontar o BC para um serviço compatível com S3 novo, confirme a região que **aquele servidor** espera antes do primeiro deploy. Não copie a do ambiente anterior.

## Cursor de keyset por chave não única esconde tudo além da primeira página

**Quando:** 2026-08-11, ensaio de ponta a ponta da fase 2.

**O que aconteceu:** o `CursorCodec` codificava só o `CreatedAt` da última linha, e a página seguinte filtrava `CreatedAt > T`. Só que **`CreatedAt` não é único, e o empate é o caso normal, não a exceção**: a varredura de caixa carimba um instante e o repassa a todos os itens que ingere. Medido em produção: **404 itens, `count(DISTINCT created_at) = 1`**. A página 1 devolvia 100 itens e um cursor; a página 2, com aquele cursor, devolvia **zero**. Todo o resto ficava inalcançável.

O `ThenBy(i => i.Id)` do `ORDER BY` dava a impressão de que o desempate existia — mas o desempate só serve se estiver **no cursor** também. E o `BillQueries` piorava: ordenava `CreatedAt` descendente e desempatava `Id` **ascendente**, direções cruzadas que fazem `ORDER BY` e `WHERE` discordarem sobre quem já foi visto.

**Por que é traiçoeiro:** não há erro, não há log, e a resposta é *sintaticamente* perfeita — `items: []`, `nextCursor: null`. Para quem consome, a lista simplesmente acabou. Foi assim que o primeiro ensaio da caixa produziu uma medição errada (relatou 304 descartes contando por subtração sobre uma lista truncada) sem que nada parecesse quebrado. É exatamente o modo de falha que o comentário do próprio `CursorCodec` dizia querer evitar quando a implementação foi unificada.

**Regra:** cursor de keyset carrega a **chave inteira**, e a chave precisa ser uma ordem total. Aqui é `(CreatedAt, Id)`, com o `Id` no cursor, no `ORDER BY` **e** no `WHERE` — e com a direção do desempate igual à da chave primária. Cursor no formato antigo não é honrado pela metade: `TryDecode` recusa e a lista reinicia.

Como o `Id` é value-converted, comparar `>` exige que o record struct declare os operadores; os cinco Ids paginados (`Bill`, `Payee`, `TrustedOrigin`, `CaptureSource`, `CaptureItem`) implementam `IComparable<T>` por isso. **A ordem que vale é a do `uuid` no Postgres** — `Guid.CompareTo` do .NET compara por campos e não coincide, o que é inofensivo porque `ORDER BY` e `WHERE` rodam ambos no banco, e passa a ser armadilha no dia em que alguém ordenar em memória esperando a mesma sequência.

**Como pegar de novo:** ao paginar por keyset, pergunte "esta chave pode repetir?". Se puder — e `CreatedAt` de lote sempre pode —, o teste que importa é semear **mais registros com a chave idêntica do que cabe numa página** e caminhar até o fim. Um teste que semeia com datas distintas passa e não prova nada.

## FK sombra de owned type precisa ter o tipo da chave da raiz, não `Guid`

**Quando:** 2026-08-11, ao mover o cursor de sincronização para uma Entity interna (`MonitoredFolder`).

**O que aconteceu:** a coleção owned foi mapeada com `folders.Property<Guid>("capture_source_id")` e `WithOwner().HasForeignKey("capture_source_id")`. **Compila sem reclamar.** Em runtime o EF recusa o modelo:

> The relationship from 'MonitoredFolder' to 'CaptureSource.Folders' with foreign key properties {'capture_source_id' : Guid} cannot target the primary key {'Id' : CaptureSourceId} because it is not compatible.

**Por que é traiçoeiro:** é o mesmo sintoma do índice sobre owned type já registrado aqui — a falha derruba a validação do modelo, então **todos** os testes de integração quebram de uma vez, inclusive os de `TrustedOrigin`, que nada têm a ver com pastas. Quem lê o resultado conclui que a infraestrutura de teste quebrou, não que um mapeamento novo está errado.

**Regra:** com Id strongly-typed, a FK sombra é `Property<CaptureSourceId>(...)` **com a mesma `HasConversion` da raiz**. O tipo da FK tem que casar com o da chave principal — `Guid` é o tipo da *coluna*, não o da *propriedade*.

**Como pegar de novo:** ao ver a suíte de integração inteira vermelha depois de mexer em mapeamento, rode um teste isolado e leia a exceção: se for `InvalidOperationException` vinda de `ModelValidator`, o problema é o modelo, não o teste.

## Índice único não impede duas linhas com `NULL` no Postgres

**Quando:** 2026-08-11, ao permitir várias pastas por fonte.

**O que aconteceu:** `path` nulo significa "caixa de entrada", e a unicidade de `(capture_source_id, path)` deveria impedir a mesma pasta duas vezes. Só que no Postgres, em índice único comum, **`NULL` não é igual a `NULL`** — duas linhas de caixa de entrada passariam pelo banco sem violar nada, e a fonte varreria a mesma pasta duas vezes, gastando o dobro de chamadas e ingerindo tudo em duplicidade na primeira vez.

**Regra:** quando o `NULL` da coluna é um **valor de negócio** (aqui: "a caixa de entrada"), o índice precisa de `NULLS NOT DISTINCT` — no EF, `.AreNullsDistinct(false)`. O agregado já recusa (`BLP.CPS16`), mas o banco é quem garante sob concorrência, e a garantia sem essa cláusula simplesmente não existe.

**Como pegar de novo:** ao escrever `HasIndex(...).IsUnique()` sobre coluna anulável, pergunte se o nulo é "ausência" ou "um caso concreto". Se for um caso concreto, `AreNullsDistinct(false)`.

## Trocar `EnsureCreated` por `Migrate` tem dois efeitos colaterais na suíte, e os dois derrubam tudo

**Quando:** 2026-08-11, ao criar a migração inicial.

**O que aconteceu:** com `MigrateAsync` no lugar de `EnsureCreatedAsync`, **192 dos 257 testes de integração** passaram a falhar com `42P07: relation "bills" already exists`. Duas causas independentes, ambas invisíveis enquanto o schema era criado por `EnsureCreated`:

1. **O contexto da fábrica não herdava o `MigrationsHistoryTable`.** Ele é construído à mão (`new DbContextOptionsBuilder<...>().UseNpgsql(cs)`), fora do DI, então não passava pelo `AddInfraDependencies` que configura `MigrationsHistoryTable("__ef_migrations_history", DEFAULT_SCHEMA)`. A fábrica migrava e gravava o histórico no lugar padrão do EF; o host subia, procurava no lugar configurado, não achava registro nenhum, e tentava criar tudo de novo.
2. **O Respawn apagava a tabela de histórico.** Ela vive dentro do schema `bill_payment`, que está em `SchemasToInclude`, e histórico de migração **não é dado de teste** — apagá-lo faz o host seguinte concluir que o banco está vazio.

**Por que é traiçoeiro:** `EnsureCreatedAsync` é no-op quando existe qualquer tabela, então ele tolerava as duas situações em silêncio. O sintoma também engana: falham testes de todos os agregados ao mesmo tempo, o que parece quebra de infraestrutura de teste, não de configuração de migração.

**Regra:** quem constrói `DbContext` fora do DI **replica a configuração do provedor**, não só a connection string. E `__ef_migrations_history` entra no `TablesToIgnore` do Respawn.

**Como pegar de novo:** `42P07 relation already exists` num banco que a suíte acabou de criar significa "migrei duas vezes achando que era a primeira" — procure onde o histórico está sendo gravado, e quem o está apagando.

**Armadilha de método:** a execução que "passou" logo antes desta rodou contra binários velhos, porque o build havia falhado (analyzers no arquivo gerado) e o `dotnet test` reaproveitou o `bin` anterior. **Suíte verde depois de build vermelho não é suíte verde** — confira o resultado do build antes de acreditar no do teste.

## O `href` não é o endereço que será visitado

**Quando:** 2026-08-12, ao desenhar a escada de resolução de link (sprint 2.5).

**O que aconteceu:** a primeira ideia era simples — allowlist de domínio sobre o `href` do e-mail. A varredura de um ano da caixa real mostrou que **todo** boleto por link chega embrulhado em rastreador de campanha: `https://vjmh2gkk.r.us-east-1.awstrack.me/L0/https:%2F%2F…destino…/1/…`. O endereço de verdade vive percent-encoded **dentro do caminho**, e o `href` aponta para o rastreador.

**Por que é traiçoeiro:** o texto visível da âncora mostra a URL certa; só o atributo é que aponta para o rastreador. Lendo o e-mail renderizado, a allowlist parece funcionar. E as duas saídas óbvias estão erradas nos dois sentidos: autorizar o host do rastreador autoriza redirecionamento para **qualquer** lugar (o mesmo rastreador serve qualquer campanha de qualquer remetente); recusá-lo sem desembrulhar perde **todos** os boletos por link que existem.

**Regra:** a allowlist decide sobre o endereço **desembrulhado**, e o desembrulho é feito **sem rede** — decodificar o segmento é mais barato que seguir o redirecionamento, não pode ser enganado por um `Location` diferente do anunciado, e não entrega ao remetente a confirmação de que a mensagem foi aberta.

**Como pegar de novo:** ao ver um host de rastreamento numa allowlist de saída, pergunte quem mais pode publicar naquele host. Se a resposta for "qualquer um", a allowlist não está protegendo nada.

## O host que serve o documento não é o do remetente

**Quando:** 2026-08-12, mesma sondagem.

**O que aconteceu:** a segunda ideia era derivar a autorização do domínio de quem mandou o e-mail — parecia o critério mais natural e o mais fechado. Medido: a **SABESP** publica o PDF da fatura em `file-pdf.7az.com.br:7446` e a **EDP** em `wwwl.montreal.com.br`. Nenhum dos dois tem relação com o domínio do remetente.

**Por que é traiçoeiro:** a regra erra nas duas direções ao mesmo tempo. Recusa os dois únicos casos reais **e** autoriza qualquer coisa hospedada no domínio de quem mandou o e-mail — que é justamente o que um remetente hostil controla.

**Regra:** allowlist é **explícita por receita** (host + porta + prefixo de caminho), nunca derivada do remetente. E a porta faz parte dela: `:7446` não é detalhe, é onde o único PDF direto vive.

**Como pegar de novo:** antes de escrever qualquer regra de "de onde eu aceito baixar", sonde os endereços reais e compare com o `From:` do e-mail.

## URL de boleto é credencial ao portador

**Quando:** 2026-08-12, ao sondar os quatro endereços.

**O que aconteceu:** os quatro responderam `200` sem autenticação nenhuma — `ssl.brcondos.com.br/Bill/<guid>`, `file-pdf.7az.com.br:7446/dx/<guid>.pdf`, `perfil.simplificamais.com.br/directScript?hash=<md5>` e `pagamento.sabesp.com.br/checkout?code=<guid>`. Quem tem o link tem o boleto.

**Por que é traiçoeiro:** URL parece metadado inofensivo — vai para log de aplicação, para telemetria de cliente HTTP, para mensagem de erro, para tela de diagnóstico. Nenhum desses lugares seria aceitável para o próprio PDF, e o efeito é o mesmo.

**Regra:** `CaptureItem.SourceUrl` sai por API só sob o portão do ADR-008 (o mesmo que esconde o `StorageKey`), e **o log só recebe o host**. No `HttpDocumentLinkResolver` o host é extraído para variável local antes de qualquer `Log*` — não por estilo, mas para não haver a tentação de logar `uri` inteiro.

**Como pegar de novo:** ao logar qualquer coisa derivada de um endereço de documento, pergunte se você logaria o documento. Se não, logue só o host.

## O plano da sprint pode estar resolvendo o problema errado

**Quando:** 2026-08-12, antes de escrever a primeira linha da 2.5.

**O que aconteceu:** a sprint estava desenhada como "escada de resolução de link", com três degraus de rede. A varredura de um ano da caixa mostrou que **dois dos cinco arquétipos já traziam o dado pagável escrito no corpo do e-mail** — a SABESP manda o BR Code inteiro no formato novo e a linha digitável de arrecadação no formato antigo. Ambos resolvem sem abrir arquivo e sem tocar a rede, com o `CandidateScanner` que já existia desde a 2.3.

**Por que importa:** o degrau novo (`ExtractionMethod.EmailBody`) é mais barato que todos os planejados e **não abre superfície de ataque nenhuma**. Sem a medição, a sprint teria entregue rede para buscar o que estava escrito no texto.

**Regra:** medir a realidade antes de implementar o plano, mesmo quando o plano parece óbvio — sobretudo quando parece óbvio. Já é o quarto achado desta natureza no BC (o `$top` da delta query, o `TryGetPng` em DCTDecode, o `MaxPages` nunca aplicado, e agora este).

## A chave que "identifica a conta" pode identificar o credor, não o devedor

**Quando:** 2026-08-12, ao medir a sprint 2.6 antes de criar o Aggregate `RoutingRule`.

**O que aconteceu:** o doc 07 previa aprender a rota por `(beneficiário, referência de conta)`, com a referência saindo do campo livre do código de barras. Agrupando 714 boletos de 14 meses por fornecedor, o campo livre parecia perfeito: 13 a 19 posições estáveis entre meses, com cara de número de conta. **Só que comparando dois pagadores diferentes do mesmo emissor, as posições estáveis eram as MESMAS** — DESPACON 19/25 idênticas entre dois tenants, SECONCI 17/25. O que se repete é a agência/conta do **beneficiário**; o que varia é o nosso número.

**Por que é traiçoeiro:** a primeira medição (estabilidade entre meses) confirma a hipótese com folga. É preciso fazer a segunda — estabilidade **entre pagadores** — para descobrir que a chave não discrimina. Uma regra criada pelo tenant A casaria com o boleto do tenant B e roteria a conta errada, que é a falha que o ADR-008 inteiro existe para impedir.

**Regra:** ao propor uma chave de identificação, meça as duas coisas: que ela é **estável no mesmo sujeito** e que ela **difere entre sujeitos**. Só a primeira é armadilha.

**Como pegar de novo:** `tools/analyze-account-reference.js` roda as duas comparações.

## Janela deslizante serve para linha digitável e NÃO serve para CNPJ

**Quando:** 2026-08-12, ao escrever o `TaxIdScanner`.

**O que aconteceu:** o reflexo era copiar a doutrina do `CandidateScanner` — gerar todas as janelas e deixar o dígito verificador reprovar. Medido sobre os mesmos 714 documentos: a regra deslizante **fabricaria um CNPJ aparentemente válido dentro do próprio código de barras em 46,9% deles**.

**Por que é traiçoeiro:** a doutrina é correta e está documentada — só que ela depende de o filtro ser forte. A linha digitável tem quatro dígitos verificadores; o CNPJ tem dois, e um bloco de 44 posições oferece trinta e uma janelas. Pior: um número fabricado pode cair ao lado de um rótulo de "Pagador" e mandar uma conta legítima para a quarentena cega, de onde o usuário não consegue reivindicá-la.

**Regra:** exigir a sequência **exata** (11 ou 14 dígitos) para documento fiscal. Não custa cobertura — emissor imprime documento fiscal isolado ou formatado, e a letra do rótulo seguinte encerra a sequência.

**Como pegar de novo:** antes de reusar uma estratégia de varredura, conte os dígitos verificadores do que ela vai validar e as janelas que a entrada oferece.

## Medir com uma ferramenta e executar com outra

**Quando:** 2026-08-12, quando os testes de integração da 2.6 falharam todos em `Unrouted`.

**O que aconteceu:** a medição dos 93,3% foi feita com `pdftotext -layout`. O sistema lê com **PdfPig**, cuja saída é diferente: os blocos vêm concatenados sem espaço (`CPF/CNPJ21.692.055/0001-80Registro2506564`). O fixture sintético do teste colava o CNPJ direto na linha digitável, produzindo uma sequência de 61 dígitos que a regra de tamanho exato recusa — corretamente.

**Por que é traiçoeiro:** a medição continuava válida (em documento real o rótulo separa os campos, e isso foi conferido), mas ela **não provava** o que o código faz, porque o extrator era outro. O teste é que estava descrevendo um documento que não existe.

**Regra:** quando a medição alimenta uma decisão de código, confira a mesma amostra **pelo caminho que o código usa**. E fixture sintético tem que reproduzir a forma real da saída do extrator, não a forma idealizada.

**Como pegar de novo:** se a medição e a implementação usam bibliotecas diferentes para o mesmo passo, sonde uma amostra pela biblioteca da implementação antes de confiar no número.

## Um achado negativo não se generaliza sem reler a pergunta

**Quando:** 2026-08-12, ao começar a 2.7 logo depois de a 2.6 ter abandonado a `RoutingRule`.

**O que aconteceu:** a 2.6 mediu e concluiu que a "referência de conta" do código de barras não serve de chave. A 2.7 usa uma referência com o mesmo nome, e o reflexo foi concluir que ela também não serve. **As duas perguntas são diferentes:** o roteamento precisa distinguir *tenants* — e para isso a referência é inútil, porque o campo livre carrega a agência/conta do beneficiário; a expectativa precisa distinguir *contas do mesmo tenant* — e para isso ela existe, medida: quatro instalações da EDP com 13 dígitos finais distintos e estáveis, três matrículas do DAE.

**Por que é traiçoeiro:** o achado da 2.6 está escrito, é forte e é verdadeiro. Aplicá-lo à pergunta errada teria produzido uma expectativa por beneficiário, cumprida pela primeira conta que chegasse, escondendo as outras três — a falha silenciosa que a sprint inteira existe para impedir.

**Regra:** ao reusar um achado, releia **qual pergunta** ele respondeu. "Esta chave não distingue X" não é o mesmo que "esta chave não distingue nada".

**Como pegar de novo:** `tools/multi-account` (a segunda medição da 2.6) responde a pergunta invertida — dentro do mesmo pagador, e não entre pagadores.

## A mediana confirma a hipótese que os dados negam

**Quando:** 2026-08-12, num teste de aprendizado de recorrência que passou quando deveria falhar.

**O que aconteceu:** o serviço deduzia a recorrência da mediana dos intervalos entre vencimentos. Ocorrências em 10/01, 25/02 e 08/07 têm intervalos de 46 e 133 dias — visivelmente irregulares —, e mediana 90, que é exatamente trimestral. O serviço propôs uma expectativa trimestral para uma sequência que não é periódica.

**Por que é traiçoeiro:** a mediana de dois valores muito diferentes cai no meio deles, e o meio tem chance real de coincidir com uma recorrência conhecida. O estatístico que existe para resistir a outlier vira, com poucas amostras, um gerador de falso positivo.

**Regra:** a mediana escolhe o candidato; **cada** observação tem que caber nele depois. Duas conferências, não uma.

**Como pegar de novo:** teste com dados propositalmente irregulares cujo intervalo médio caia em cima de uma recorrência válida — é o caso que passa despercebido.

## `RemoveAll<IAuthorizationHandler>()` desliga o `RequireAuthenticatedUser()`

**Quando:** 2026-08-15, ao montar os dublês de autorização da suíte. 134 testes vermelhos com 403, inclusive o que afirma que o tenant presente no claim **deveria** passar.

**O que aconteceu:** a fábrica de teste precisava tirar de cena o handler que fala com o Keycloak. `services.RemoveAll<IAuthorizationHandler>()` seguido de re-registrar os handlers desejados parecia o caminho limpo. Ele leva junto o `PassThroughAuthorizationHandler` do próprio ASP.NET Core — que é quem avalia os requirements que implementam `IAuthorizationHandler` **em si mesmos**. O `DenyAnonymousAuthorizationRequirement`, que o `RequireAuthenticatedUser()` acrescenta, é um deles: sem o pass-through ele nunca é avaliado, a policy nunca conclui, e toda requisição autenticada vira 403.

**Por que é traiçoeiro:** o 403 é indistinguível de "não tem permissão". Sem um teste afirmando o caminho do SUCESSO do guard, a leitura natural teria sido "os testes precisam declarar mais coisa" — e a correção seria afrouxar a suíte para contornar um defeito que estava na fábrica.

**Regra:** remova serviço de framework **pelo `ImplementationType`**, nunca por `RemoveAll<TService>()`, quando o contrato tiver registros do próprio ASP.NET Core.

**Como pegar de novo:** todo guard precisa de teste dos dois lados. Um que só afirma a negativa passa mesmo quando o guard nega tudo.

## O guard de tenant casa pelo NOME do parâmetro de rota

**Quando:** 2026-08-15, ao aplicar a autorização nos 55 endpoints.

**O que aconteceu:** `RouteAccessRequirementHandler` lê `GetRouteValue("tenantId")`. Achando `null`, ele **concede** — comportamento correto, é o que permite endpoint que não pertence a tenant nenhum (o back-office do TenantManagement depende disso, e por isso lá o parâmetro se chama `{id}` de propósito). A consequência é que um controller novo com `{tenant:guid}` ou `{id:guid}` nasce **sem** guard anti-IDOR.

**Por que é traiçoeiro:** não há erro, não há log, e o teste do endpoint novo passa — ele testa a funcionalidade, não a ausência de proteção. O defeito só aparece quando alguém acessa dado de outro tenant.

**Regra:** rota de tenant chama o parâmetro de `tenantId`, sempre. Quem garante é `Authorization/EndpointProtectionTests`, que varre o `EndpointDataSource`.

**Como pegar de novo:** teste que enumera endpoints em vez de testar um endpoint. É o único formato que cobre o controller que ainda não foi escrito.

## A ferramenta de medição preenche sozinha um cabeçalho que a implementação não preenche

**Quando:** 2026-08-25, ao chamar `AsaasPixLookupService.DecodeAsync` contra o provedor de verdade.

**O que aconteceu:** toda chamada ao Asaas voltava `400` com *"É obrigatório preencher User-Agent no cabeçalho da requisição"*. O provedor exige `User-Agent` e **o `HttpClient` do .NET não manda nenhum por padrão** — `ConfigureAsaasClient` configurava `BaseAddress`, `Timeout` e `access_token`, e mais nada. Valia para os dois adapters: consulta de boleto e decode de Pix falhavam igual, antes de o provedor olhar o corpo.

**Por que é traiçoeiro:** as duas sondas de fumaça de produção saíram **verdes** em 2026-08-06 contra exatamente esses endpoints, e o `12-official-lookup-coverage.md` registrou os campos que voltaram. As sondas são Node, e o `fetch` do Node manda `User-Agent: node` por conta própria — medido: `node fetch → "node"`, `dotnet HttpClient → null`. A medição preencheu um requisito que a implementação nunca preencheu, e ninguém tinha como notar a diferença lendo qualquer um dos dois lados.

É a terceira vez que o mesmo padrão morde este BC — as outras duas já estão registradas aqui ("Medir com uma ferramenta e executar com outra", `pdftotext` × PdfPig) e no CLAUDE.md (o `$top` da delta query). A diferença é que aqui o desvio não estava no que a ferramenta *lê*, e sim no que ela **acrescenta sem ser pedida**.

**Por que os 394 testes não pegaram:** `AsaasPixLookupServiceTests` e `AsaasBillLookupServiceTests` constroem o `HttpClient` à mão (`new HttpClient(handler)`) para exercitar a tradução da resposta sem rede — postura correta, e que por construção **não enxerga nada do que o `AddHttpClient` configura**. E a suíte roda sem `Asaas:ApiKey` de propósito, então o caminho de DI que registra os adapters de verdade nunca era percorrido.

**Regra:** cabeçalho exigido pelo provedor é **configuração do cliente**, e configuração do cliente precisa de um teste que passe pelo composition root. `Asaas/AsaasClientConfigurationTests` monta o contêiner a partir do `AddInfraDependencies` (sem Postgres — o `AddDbContext` não abre conexão), resolve o cliente tipado pelo `IHttpClientFactory` e **afirma o `BaseAddress` junto com o cabeçalho**: sem essa segunda asserção, uma mudança na derivação do nome do cliente tipado devolveria um homônimo vazio e o teste afirmaria sobre outra coisa.

E o valor é **constante**, não opção de configuração: ele identifica a aplicação, não o ambiente, e um campo configurável poderia chegar vazio — que é precisamente o estado quebrado.

**Como pegar de novo:** ao escrever adapter HTTP para provedor novo, faça a primeira chamada **pelo .NET**, não pela sonda em Node. Se a sonda for o único caminho exercitado, compare os cabeçalhos que os dois clientes emitem antes de declarar o contrato medido.

## O número medido pode ter vindo de uma ferramenta que o código não usa

**Quando:** 2026-08-26, ao avaliar a troca da varredura de documento fiscal por busca dirigida.

**O que aconteceu:** o CLAUDE.md registrava que a regra de sequência exata (11 ou 14 dígitos) achava o documento do tenant em **93,3%** dos boletos "sem custo de cobertura". Medindo 915 boletos reais do acervo **pela biblioteca que o sistema de fato usa** (PdfPig), a mesma regra acha em **469** — e a busca dirigida pelos documentos do cadastro acha em **523**. O custo de cobertura existia; ele é de 54 documentos, e estava invisível.

**Por que é traiçoeiro:** os 93,3% foram medidos com `pdftotext -layout`, que separa os campos em colunas. O PdfPig entrega `page.Text` **emendado, sem espaço** — `CNPJ: 22.359.919/0001-0918942151Recibo:` —, e aí um documento fiscal colado ao número vizinho deixa de ter 11 ou 14 dígitos e é descartado. A regra estava correta *para o texto medido* e errada *para o texto real*. Este BC já tinha registrado o mesmo padrão em "Medir com uma ferramenta e executar com outra" e no `User-Agent` do Asaas; a diferença é que aqui o número medido virou **documentação afirmativa**, foi citado em três lugares, e sobreviveu quatro meses.

**Regra:** número medido entra na documentação **com a ferramenta ao lado**. "93,3% (medido com `pdftotext -layout`)" teria feito o próximo leitor perguntar se o código lê assim — e a resposta era não. Sem a procedência, a medição vira fato.

**Como pegar de novo:** ao encontrar uma porcentagem no CLAUDE.md que sustenta uma decisão de código, procure a ferramenta que a produziu antes de confiar nela. Se a medição usa biblioteca diferente da implementação, ela mede outra coisa.

## Borda de palavra quebra em texto que o extrator entrega emendado

**Quando:** 2026-08-26, ao "corrigir" o detector de rótulo de pagador.

**O que aconteceu:** `sacado` (rótulo do devedor) casa dentro de `Sacador` (campo "Sacador / Avalista", presente em quase todo boleto). Diagnostiquei como defeito e acrescentei `\b` nos dois lados das duas expressões. **Quebrou o caso real**: o PdfPig entrega `PagadorRUFINO EMPREITEIRA LTDA`, sem espaço entre o rótulo e o nome, então a borda de palavra depois de `pagador` nunca fecha e o rótulo legítimo deixou de ser reconhecido.

**Por que é traiçoeiro:** duas vezes. Primeiro, o "defeito" não era defeito — os dois detectores casam no **mesmo índice** e o desempate é `>` estrito, então o empate já resolvia para "não é rótulo de pagador", que é a resposta certa. Segundo, a correção parecia inofensiva: `\b` é o reflexo de quem escreve regex, e em texto normal não muda nada. Só quebra em texto emendado — que é exatamente o que este BC lê.

**Regra:** antes de "corrigir" uma sobreposição de padrões, **trace o desempate** e confira se o resultado já não é o certo. E toda regex que roda sobre saída de extrator de PDF precisa ser pensada para texto **sem espaços**: `\b`, `^`, `$` e `\s` têm comportamento diferente ali.

**Como pegar de novo:** rodar a expressão contra um trecho real de `page.Text` — não contra um exemplo escrito à mão, que sempre sai com espaços.

## O filtro que protege a fila pode estar antes do registro que a fila não cobre

**Quando:** 2026-08-26, ao investigar três e-mails que não eram capturados.

**O que aconteceu:** o `GraphMailboxReader` descartava, dentro do adaptador, toda mensagem sem anexo utilizável e sem sinal de cobrança no corpo — `if (artifacts.Count == 0) continue`. O filtro existe por um motivo bom e medido: sem ele, toda conversa da caixa viraria `CaptureItem` e a fila de quarentena ficaria inútil. Só que o **livro-caixa** é escrito depois, na Application, a partir do que o adaptador devolve. Resultado: a mensagem filtrada não virava item **nem registro** — sumia de todas as telas.

**Por que é traiçoeiro:** o filtro está certo para o propósito que ele foi escrito (proteger a fila) e errado para um propósito que nasceu depois (o histórico). Nada no código liga os dois: o `if` fica no adaptador, o registro fica na Application, e a distância esconde que um governa o outro. E o sintoma é silêncio — nenhum erro, nenhum log, a mensagem simplesmente não existe.

**Regra:** quando um agregado novo passa a consumir a mesma leitura de um agregado antigo, **releia os filtros que estão ANTES da bifurcação**. Um filtro escrito para proteger o consumidor A restringe o consumidor B sem que ninguém tenha decidido isso.

**Como pegar de novo:** ao acrescentar um consumidor a um pipeline existente, liste os pontos onde dado é descartado antes de ele chegar. Se algum deles fica *acima* da bifurcação, ou ele muda de lugar, ou vira decisão explícita para os dois.

## Enumeração do provedor vai do mais antigo para o mais novo — e o teto fica no fim

**Quando:** 2026-08-26, mesma investigação.

**O que aconteceu:** a varredura lê no máximo `MaxPagesPerSync × PageSize` = 1.000 mensagens e guarda o `nextLink` para retomar. Numa caixa de **12.422 mensagens** isso são treze varreduras; com `PollingInterval` de uma hora, treze horas. E como a delta query enumera do mais antigo para o mais novo, **a mensagem de hoje é a última a ser alcançada** — e forçar releitura completa piora, porque descarta o progresso e recomeça do mais antigo.

**Por que é traiçoeiro:** o teto foi escrito para proteger contra varredura infinita, e faz isso bem. O que ninguém percebeu é que ele interage com a **ordem** da enumeração: um teto no fim de uma lista ordenada do mais novo para o mais antigo cortaria o histórico, que é aceitável; no sentido inverso ele corta exatamente o que mais importa. O sintoma parece "a captura parou de funcionar", quando na verdade ela está funcionando devagar demais para importar.

**Regra:** teto de paginação precisa vir com uma pergunta sobre **ordem**. Se a fonte enumera do mais antigo para o mais novo, o agendador tem de saber quando parou no teto e emendar a próxima passada, em vez de dormir o intervalo cheio.

**Como pegar de novo:** ao ver cursor de sincronização parado num `skipToken` por mais de um ciclo, conte quantos ciclos faltam para o fim e multiplique pelo intervalo. Se der horas, o teto está no lugar errado.

## A espera que protege a cota estava dentro do caminho de todo mundo

**Quando:** 2026-08-26, ao investigar por que a fila de processamento demorava.

**O que aconteceu:** o `ExtractionBudget` respeitava o limite do provedor com `await Task.Delay(...)` **dentro** do processamento do artefato, e sob um semáforo global. Com o intervalo de 6 s da conta gratuita, um item que precisava de IA parava o worker por até 6 s antes mesmo de a chamada sair — e o worker é serial, então **todos** os outros itens do lote esperavam junto. Medido: 27% dos itens consumindo 86% do tempo, num lote cujo item mediano leva 150 ms.

**Por que é traiçoeiro:** a trava está certa — sem ela o provedor devolve `429` e o artefato vai para a quarentena até o dia seguinte. O erro não é proteger a cota, é **onde** a proteção espera. Um `Task.Delay` parece barato porque não queima CPU; o custo real é o lugar na fila que ele ocupa. E o sintoma chega como "o sistema está lento", não como "a trava de cota está no lugar errado".

**Regra:** trava de recurso escasso não pode morar no caminho de quem não usa aquele recurso. Ou ela vira não-bloqueante e o item cede a vez, ou o trabalho que depende dela sai para uma fila própria.

**Como pegar de novo:** ao ver fila lenta, meça a **distribuição**, não a média. Média de 4 s com mediana de 150 ms não é "tudo lento" — é "alguns bloqueiam todos", e o conserto é separar, não acelerar.

## Concorrência ajuda I/O e não ajuda cota

**Quando:** 2026-08-26, ao decidir onde paralelizar o processamento.

**O que aconteceu:** a reação natural a "a fila está lenta" foi paralelizar o lote inteiro. Só que as duas metades do trabalho têm gargalos de natureza diferente: baixar anexo e gravar no balde é **I/O** e escala com concorrência; chamar a IA é **cota**, e o teto é da conta no provedor. Paralelizar a segunda metade não a acelera — troca espera por `429`, e o cliente de visão não retenta, então cada `429` vira artefato na quarentena até o dia seguinte.

**Regra:** antes de paralelizar, classifique o gargalo. **I/O** aceita concorrência; **cota de terceiro** não — ali o ganho vem de tirar o trabalho da frente dos outros, não de fazê-lo mais rápido.

**Como pegar de novo:** se acelerar uma coisa exige pedir mais ao provedor por minuto, concorrência é a ferramenta errada. Procure a fila separada.

## Retentar para sempre é o mesmo que não ter fila

**Quando:** 2026-08-26, investigando e-mails parados em "ainda não processado" por tempo indefinido.

**O que aconteceu:** o worker de captura tratava **toda** falha como passageira. O comentário no código explicava o raciocínio, e ele estava metade certo: *"o item permanece em `Received` e volta no ciclo seguinte — o que é seguro porque o processamento é idempotente"*. Idempotência garante que repetir não estraga; não garante que repetir vai funcionar. Medido no log da API: **1.815 falhas, das quais 1.709 eram o mesmo erro em quatro artefatos** — `BLP.BIL15`, um PDF com dois boletos de naturezas diferentes, que o domínio recusa por invariante legítima. Um item sozinho tentou **485 vezes em 62 minutos**, baixando o anexo do provedor com HTTP 200 em todas.

**Por que é traiçoeiro, em três camadas:**

1. **O desperdício não é o pior.** A fila é `ORDER BY received_at LIMIT 10`, então cada item envenenado ocupava **em caráter permanente** uma das dez vagas. Dez deles parariam a captura inteira — sem erro, sem alerta, sem nada em tela.
2. **A tela mentia.** `RecordCapturedOutcomeAsync` só roda no fim do handler, então o desfecho do anexo ficava `Pending` para sempre: o usuário via "ainda não processado" indefinidamente, que é o sintoma pelo qual o defeito foi relatado.
3. **A durabilidade escondia a ausência de política.** Como a fila vive no Postgres e o item continua `Received`, queda de processo não perde nada — o que é ótimo e mascara o problema. A mesma propriedade que salva de uma queda é a que transforma uma recusa determinística em laço eterno.

**A assimetria que denunciava:** o `OutboxProcessor`, no mesmo BC, já tinha `FOR UPDATE SKIP LOCKED`, `attempts`, `MaxAttempts` e dead-letter. A fila de captura era a única sem nada disso — e era justamente a que processa documento vindo de fora, a fonte de entrada mais imprevisível do sistema.

**Regra:** retentar existe para falha **transitória**; quarentena existe para falha **permanente**. Sem a segunda, a primeira vira fonte permanente de carga. Toda fila precisa de três coisas: teto de tentativas, estado terminal de falha com o erro guardado, e reivindicação atômica. Se uma fila do projeto tem e outra não, a que não tem é um defeito ainda não relatado.

**Como pegar de novo:** `docker logs <api> | grep -c "failed to process"` e agrupe por id. Contagem alta concentrada em poucos ids **é** o laço — não é "vários itens com problema". E se o mesmo id aparece mais vezes do que o número de ciclos plausível na janela, não há teto de tentativas em lugar nenhum.

## Aluguel em coluna vale mais que trava de transação, quando o trabalho é longo

**Quando:** 2026-08-26, ao acrescentar reivindicação atômica à fila de captura.

**O que aconteceu:** a tentação era copiar o outbox literalmente — `SELECT ... FOR UPDATE SKIP LOCKED` dentro de uma transação que dura o processamento inteiro. Funciona lá porque despachar um evento é rápido. Aqui o processamento baixa megabytes, lê PDF e pode chamar a IA por segundos: segurar uma transação de banco todo esse tempo prenderia conexão do pool à toa, e um worker morto travaria a linha até o `idle_in_transaction_session_timeout`.

**A saída:** `UPDATE ... WHERE id IN (SELECT ... FOR UPDATE SKIP LOCKED) RETURNING *`. Um comando só, atômico, que **grava um prazo em coluna** (`lease_expires_at`) em vez de manter uma trava. A exclusão passa a ser um dado, não um lock — e vence sozinha quando o worker morre, dispensando o faxineiro que filas assim costumam precisar.

**O bônus que quase se perde:** o mesmo campo serve de **backoff**. Depois de uma falha transitória o agregado empurra o prazo para o futuro, e o `WHERE` da reivindicação já pula o item até lá. Duas noções separadas de "quando este item pode ser mexido" divergiriam; uma só não tem como.

**Regra:** trava de banco para trabalho curto; prazo em coluna para trabalho longo. E se você for escrever "quando tentar de novo" num campo e "até quando é meu" noutro, pergunte se não é a mesma pergunta.

**Como pegar de novo:** ao ver `BLP.CPI03` de transição inválida partindo de estado terminal (`Promoted -> Parsed`), suspeite de duas execuções do mesmo item — não de bug na máquina de estados.

## Registrar a falha exige escopo novo, porque o agregado em memória está sujo

**Quando:** 2026-08-26, ao implementar o registro de falha do worker de captura.

**O que aconteceu:** a primeira ideia foi capturar a exceção **dentro** do handler e marcar o item por ali. Não funciona: quando o `BLP.BIL15` estoura, o item já passou por `StoreArtifact` e `MarkParsed` — ele está em `Parsed` na memória, e `Parsed -> Unrecognized` nem existe na matriz. Pior, o `DbContext` carrega alterações que **não podem** ser gravadas.

**Regra:** tratamento de falha de um passo transacional roda em escopo novo, recarregando o agregado do banco. O estado que vale é o **persistido**, não o que a execução abortada deixou em memória.

**Como pegar de novo:** se o `catch` precisa saber em que ponto do `try` a exceção aconteceu para escolher a transição, o `catch` está no lugar errado.


## O fallback de um cálculo virou o estado da maioria

**Quando:** 2026-08-26, com vários e-mails presos em "Na fila" na tela — e **nada** preso no backend.

**O que aconteceu:** o desfecho que a linha da tela mostra é calculado (`Dominant`): percorre os anexos da mensagem por gravidade decrescente e devolve o primeiro que casar. Não achando nenhum, devolvia `ArtifactOutcome.Pending`, que a UI traduz como "Na fila". Isso foi escrito quando toda mensagem do livro-caixa tinha pelo menos um anexo. Depois, mensagem **sem** anexo passou a entrar no registro — decisão certa, para quem mandou o e-mail ter resposta — e o fallback virou o estado da maioria: **23 de 39 mensagens** da caixa real, todas propaganda e notificação, eternamente "na fila" sem fila nenhuma.

**Por que é traiçoeiro:** o banco estava impecável — **zero** anexos em `Pending`, todos os itens em estado terminal. Quem olhasse a tela concluiria "o processamento travou" e iria depurar o worker, que é exatamente onde o problema **não** estava. O sintoma apontava para o lado oposto da causa.

**O irmão, no mesmo arquivo:** `ProcessingFailed` foi acrescentado ao catálogo e esquecido na lista de prioridade **no mesmo dia**. Efeito idêntico: escorre pelo laço, cai no fallback, aparece como "Na fila". Uma lista escrita à mão que precisa espelhar um catálogo é dívida — ela não quebra quando desatualiza, só mente.

**Regra:** o ramo "não achei nada" de um cálculo de estado não pode reusar um estado que significa "ainda vai acontecer". Ausência e espera são coisas diferentes, e confundi-las é o modo de falha do ADR-014 aparecendo na tela. E toda lista que espelha um catálogo precisa de um teste que compare os dois tamanhos.

**Como pegar de novo:** ao ver "preso em X" na tela, **conte X no banco antes de abrir o worker**. Se o banco não tem nenhum, o defeito é na projeção — não no processamento.


## Uma allowlist que só cresce à mão é um ponto cego que só cresce

**Quando:** 2026-08-26, com uma cobrança real desaparecendo sem deixar rastro.

**O que aconteceu:** o portão que decide se o corpo de um e-mail vira artefato aceitava link como sinal **apenas** quando o host tinha receita configurada. O raciocínio estava escrito no código e parecia impecável: *"link para host desconhecido não é sinal — o sistema não teria como buscar o documento, e o item nasceria só para morrer na quarentena."* A consequência não estava escrita: **o sistema só conseguia descobrir boleto de emissor que alguém já tinha sondado e cadastrado à mão.** Emissor novo era invisível, e invisível em silêncio.

**O caso:** uma cobrança da Asaas, sem anexo, com o boleto atrás de `www.asaas.com/i/{token}`. Sondando a mão: a página responde 200 sem autenticação e traz um `href` para o PDF, que responde 200 e rende duas linhas de 47 dígitos e um BR Code. Ou seja — **era alcançável o tempo todo**, só faltava alguém saber que precisava olhar. E ninguém saberia, porque o desaparecimento não gerava nem quarentena nem log.

**Por que é traiçoeiro:** a regra não estava errada em nenhum caso individual. Ela estava errada no agregado: transformava "ainda não sei buscar isto" em "isto não existe". Um sistema que descarta o desconhecido em silêncio nunca acumula a informação que o faria conhecer.

**Regra:** allowlist decide **como tratar**, nunca **se registrar**. O que cai fora dela vira fila de trabalho — com o host guardado —, não vira nada. Se acrescentar uma entrada nessa lista exige que um humano descubra sozinho que ela falta, a lista é um ponto cego que cresce com o tempo.

**Como pegar de novo:** para toda allowlist, pergunte "o que acontece com o que não está aqui, e como eu ficaria sabendo?". Se a resposta da segunda metade for "não ficaria", falta o registro.

## Substring casa dentro de palavra, e endereço de e-mail é onde isso dói

**Quando:** 2026-08-26, ao ligar o sinal de cobrança na triagem — um teste pré-existente ficou vermelho na hora.

**O que aconteceu:** a lista de sinais de cobrança tem "conta", e eu passei a comparar também contra o **endereço do remetente**. `faturas@fornecedor.com.br` casou por "fatura" — e o teste que exigia descarte de um contrato de locação quebrou. Investigando o efeito real na caixa: **"conta" casa dentro de "contato@" e "contabilidade@"**, e o segundo é o endereço do contador, que o corpus mediu como origem de 72 dos 95 itens de quarentena. Teria inundado a fila com holerite, rescisão e nota fiscal.

**Por que é traiçoeiro:** a medição que justificou a mudança tinha sido feita contra o **assunto**, e eu ampliei o alcance para o remetente na implementação sem remedir. O número que autorizava a decisão (3 de 23) deixou de descrever o que o código fazia, e nada avisaria — exceto o teste, por acaso.

**Regra:** o alcance implementado tem que ser o alcance medido. Ampliou o campo que a heurística lê? Refaça a medição antes, não depois. E casamento por substring nunca deve ver identificador estruturado (e-mail, domínio, caminho) — ali as palavras aparecem por dentro de outras.

**Como pegar de novo:** teste vermelho logo depois de ampliar uma heurística quase nunca é "o teste está velho". Antes de ajustá-lo, verifique se o dado que autorizou a mudança ainda descreve o que foi escrito.


## Dois fatos parecidos num campo só fazem o registro mentir

**Quando:** 2026-08-27, implementando a anexação manual do boleto.

**O que aconteceu:** ao receber o arquivo que a pessoa subiu, marquei `ExtractionMethod.Manual` no item — parecia óbvio, o valor existia desde a 2.1 e nunca tinha sido usado. Só que `ExtractionMethod` responde **como o instrumento foi lido** (texto embutido, QR, visão), e quem o preenche é a cascata, na passagem seguinte. O `Manual` era sobrescrito por `EmbeddedText` alguns milissegundos depois. O teste pegou, e a lição é maior que o bug: eu estava usando um campo para responder uma pergunta que não é a dele.

**A correção:** dois campos para dois fatos. `ManuallySupplied` diz que **uma pessoa trouxe o arquivo**; `Extraction` continua dizendo **como ele foi lido**. Um anexo manual resolvido por texto embutido é as duas coisas ao mesmo tempo, e nenhuma delas é redundante.

**Regra:** antes de reusar um campo existente para um conceito novo, pergunte que pergunta ele responde hoje. Se a resposta nova não substitui a antiga — se as duas podem ser verdadeiras ao mesmo tempo —, são dois campos.

**Como pegar de novo:** valor de enum declarado e nunca usado é convite a esse erro. Ele parece "o lugar certo" justamente por estar vago.

## Interpolação com aspas iguais às da string quebra o literal — e o erro aponta longe

**Quando:** 2026-08-27, escrevendo o diálogo de confirmação em Dart por script.

**O que aconteceu:** gerei `'De: ${item.sender ?? 'desconhecido'}'` — aspas simples dentro de uma interpolação dentro de uma string de aspas simples. O literal termina na segunda aspa, e o analisador reporta "unterminated string literal" **na linha seguinte**, junto com um punhado de erros derivados que não têm relação com a causa. Somado a isso, escapes de `\n` passando por heredoc de shell viraram quebras de linha reais dentro do literal — o mesmo modo de falha já registrado neste arquivo, agora em Dart.

**Regra:** interpolação usa a aspa **oposta** à da string que a contém. E edição precisa de código em arquivo com escapes é trabalho para ferramenta de edição, não para script de texto — a regra já estava aqui e eu a repeti mesmo assim.

**Como pegar de novo:** cascata de erros de sintaxe começando em "unterminated string" quase sempre tem a causa **uma linha acima** do primeiro erro relatado.

## O campo que abre o ciclo NAO e o campo que dispara o alerta (2026-08-27)

**O que aconteceu:** a varredura da expectativa decidia abrir o ciclo com `AlertLeadDays` — a
antecedencia do *aviso*. Cenario relatado pelo usuario: conta que vence dia 10 e **chega 20 dias
antes**, com aviso pedido para 2 dias antes. O ciclo de setembro so nascia em 08/09; o boleto
chegava em 21/08, nao encontrava ciclo, nao cumpria nada — e em 08/09 o sistema alertava "a conta
nao chegou" sobre um boleto capturado, validado e aprovado.

**Por que e traicoeiro:** os dois prazos tem nomes parecidos e o codigo funcionava para o caso
comum, em que a conta chega poucos dias antes do vencimento e os dois numeros quase coincidem. O
defeito so aparece quando a conta chega com folga — e ai ele produz exatamente a falha silenciosa
que o ADR-014 existe para impedir, com o agravante de ser um alerta FALSO, que e o que treina a
pessoa a ignorar alerta.

**A regra:** `ObservedLeadDays` responde "quando comeco a esperar" e governa `OpensAtFor`;
`AlertLeadDays` responde "quando reclamo" e governa `AlertAtFor`. Ao mexer em qualquer um dos
dois, pergunte qual das duas perguntas esta em jogo.

## Recorrencia sem ancora abre um ciclo por mes — e a anual se autodesativa (2026-08-27)

**O que aconteceu:** `OpenDueCycle` derivava a competencia do mes corrente e **nunca consultava
`Recurrence`**. Uma expectativa trimestral ou anual ganhava um ciclo todo mes; onze deles viravam
`Missing`, e a regra dos tres misses consecutivos **desativava a expectativa sozinha** em tres
meses.

**Por que e traicoeiro:** `ExpectedDueDay` da a impressao de descrever o calendario inteiro, mas
diz so o DIA. Em que MESES a conta vence e informacao que nao existia no agregado — e a falha nao
aparece em teste mensal nenhum, porque no mensal toda competencia esta na cadencia.

**A regra:** `AnchorCompetence` fixa a fase, e `Fulfill` a reancora na competencia que de fato
chegou. Teste de recorrencia nao-mensal e obrigatorio ao mexer na varredura.

## Ordenar fila de worker por `UpdatedAt` inverte a prioridade (2026-08-27)

**O que aconteceu:** `ListActiveForSweepAsync` fazia `OrderBy(UpdatedAt).Take(100)` sobre a
instalacao inteira. Como `UpdatedAt` so muda quando ha mudanca de negocio, a expectativa que **nada
faz** mantinha o carimbo antigo e ocupava as vagas do lote permanentemente, enquanto a que estava
sendo cumprida e alertada ganhava carimbo novo e ia para o fim. Passando de cem expectativas ativas,
as demais **nunca eram varridas** — sem erro, sem log, sem sintoma.

**Por que e traicoeiro:** o codigo parece a paginacao circular correta, e ate tinha comentario
dizendo que era ("sem isso, uma expectativa no fim da lista nunca seria varrida"). A intencao estava
certa; o campo escolhido produzia o oposto dela.

**A regra:** fila de worker ordena por carimbo **de varredura** (`LastSweptAt`), gravado em TODA
passagem — inclusive na que nao faz nada. E o lote deixa de ser teto de cobertura: o worker pede
lotes ate a fila secar. Cuidado com o laco: item que FALHA nao recebe o carimbo (de proposito, para
voltar no ciclo seguinte), entao o laco precisa lembrar quem ja teve a vez, senao gira nele ate o
teto.

## Metodo de dominio sem nenhum chamador de producao (2026-08-27)

**O que aconteceu:** `BillExpectation.RecordCaptureFailure` existia, tinha teste unitario, tinha
teste de integracao — e **nenhum codigo de producao o chamava**. O status `PartiallyCaptured` era
inalcancavel e a lista `captureFailed` do painel voltava sempre vazia. O mesmo valia para
`HintSourceId`, que o aprendizado preenchia com `null` literal e o cadastro nem aceitava.

**Por que e traicoeiro:** a suite inteira ficava verde. Teste que exercita o metodo direto prova
que o dominio funciona, nao que alguem o usa — e `grep` do nome do metodo devolve os testes, o que
da a impressao de cobertura.

**A regra:** ao fechar uma sprint, `grep` do metodo publico **excluindo os projetos de teste**. Se
o unico chamador e teste, a capacidade nao existe. Vale tambem para campo que so e escrito com
literal nulo.

## `Transition` e o unico lugar de onde eventos de estado saem (2026-08-27)

**O que aconteceu:** o `CaptureItem` passou a emitir "travou"/"destravou", e a tentacao foi emitir
dentro de cada `MarkXxx`.

**Por que e traicoeiro:** e exatamente assim que `VisionPending -> LinkFailed` ficou de fora da
matriz de transicoes e prendeu item na fila da IA para sempre. Declarar comportamento degrau a
degrau deixa buraco no degrau que ninguem lembrou.

**A regra:** um gancho so, na unica porta por onde todo estado passa. E o evento carrega o `Status`
alvo, **nao** o `Reason`: os `MarkXxx` escrevem o motivo DEPOIS de transicionar, e ler o campo
dentro do `Transition` devolveria o motivo anterior. **Agregado que passa a emitir evento precisa
entrar no `DrainDomainEvents`** no mesmo commit — senao os eventos sao acumulados e descartados no
fim do escopo.

## Porta de notificacao sem destinatario nao e "falta configurar SMTP" (2026-08-27)

**O que aconteceu:** o checklist listava "adapter de e-mail" como pendencia de configuracao. Mas
`INotificationSender.SendAsync` recebe o `TenantId`, e o BC **nao guardava nenhum dado de contato**
— o unico e-mail existente era o `MailboxAddress` da `CaptureSource`, que e a caixa DE CAPTURA e
nao a caixa de uma pessoa. Um adapter de e-mail escrito naquele estado nao teria para quem enviar.

**Por que e traicoeiro:** a pendencia estava escrita e parecia pequena. O trabalho real era um
Aggregate novo, uma tabela, um endpoint e uma decisao de produto (quem recebe alerta de conta a
pagar e o financeiro, nao quem administra o tenant).

**A regra:** antes de estimar "falta o adapter", confira se a porta tem todos os dados de que o
adapter precisa. Porta que recebe so o tenant precisa de uma fonte de contato.

## Escalonamento registrado nao e escalonamento enviado (2026-08-27)

**O que aconteceu:** `TryRecordAlert` gravava o nivel no agregado e **nao emitia evento nenhum**.
Quem notificava era a transicao para `Missing` — que acontece uma vez por ciclo. Dos quatro niveis
do escalonamento (`HeadsUp`, `Warning`, `Urgent`, `Overdue`), so o primeiro chegava ao usuario; os
outros tres apareciam em `LastAlertLevel` no painel e nunca saiam.

**Por que e traicoeiro:** a tabela de escalonamento estava no doc, o registro por nivel estava
implementado e testado, e o painel mostrava o nivel correto. Tudo o que faltava era o unico elo que
nenhum teste cobria — e a diferenca entre "registrado" e "enviado" nao aparece numa suite sem canal
externo.

**A regra:** ao ler uma tabela de escalonamento no doc, procure o evento que carrega o NIVEL. Se o
aviso sai de um evento de mudanca de status, ele sai uma vez so.

## Porta de integracao que nao distingue "nao achei" de "nao respondi" (2026-08-27)

**O que aconteceu:** `IDocumentIntelligence.ExtractAsync` devolvia `ExtractedDocument.Empty` em
quatro situacoes diferentes — o modelo leu e nao achou boleto, o provedor respondeu 503, respondeu
400, e o transporte estourou no timeout. Para quem chama, as quatro eram a mesma coisa: "nao e
boleto". Resultado medido em 614 chamadas de um dia: **24 documentos bons foram para a quarentena
por 503**, sem nenhuma retentativa.

**Por que e traicoeiro:** a fila de captura JA tinha a maquina de retentativa completa — aluguel,
backoff dobrando, teto de tentativas, tudo testado. Olhando o codigo da fila, a conclusao natural e
"isso ja esta resolvido". E estava: o que faltava era o **sinal** que a acionasse. Maquina de
retentativa nao serve de nada quando a falha chega disfarcada de desfecho normal.

**A regra:** toda porta de integracao devolve um status que responde "vale a pena tentar de novo?".
O BC ja fazia isso em `MailboxStatus` e `LookupStatus` — a de IA era a unica fora do padrao. Ao
criar porta nova, copie a forma do `LookupResult`, nao a do `ExtractedDocument`.

## `DomainException` classificada como permanente engole a retentativa nova (2026-08-27)

**O que aconteceu:** ao introduzir `ExtractionErrors.ProviderUnavailable` para devolver o item a
fila, a correcao quase nasceu morta: `CaptureFailureHandling.IsPermanent` classifica **todo**
`DomainException` como recusa deterministica, entao o item desistiria na primeira tentativa —
exatamente o comportamento que a mudanca existia para corrigir.

**Por que e traicoeiro:** a regra "DomainException e permanente" esta certa e bem documentada; o
caso novo e uma excecao legitima a ela, porque a exceção descreve a REDE e nao os bytes. Sem
perceber isso, a suite passaria (o item vai para a quarentena, como antes) e o defeito continuaria.

**A regra:** ao usar exceção de domínio como *sinal de fluxo* (e nao como recusa), confira quem
classifica exceção no caminho — e isente pelo `Id`, nunca pelo tipo.

## "Reprocessar" nao pode significar "reconsultar tudo" (2026-08-27)

**O que aconteceu:** quando o retrato da leitura por IA chega atrasado, e preciso refazer os checks.
O caminho obvio era chamar o `ValidateBillCommand` — que **reconsulta o provedor**.

**Por que e traicoeiro:** funciona, e por isso passa em revisao. Mas gasta cota do Asaas para
reobter um retrato que ja esta guardado, e ainda pode acionar a regra de retrato velho num boleto
que estava pronto para aprovacao. Pior: `AcceptsValidation` inclui `Approved`, e revalidar ali
**derruba a aprovacao incondicionalmente** — um enriquecimento de fundo desfazendo em silencio a
decisao de uma pessoa.

**A regra:** reprocessamento parcial reapresenta os retratos **ja armazenados** como resultado
resolvido e roda so a apuracao. E quando o dado chega depois de alguem ja ter decidido, ele e
anexado como metadado e a verificacao NAO e refeita (`Bill.AcceptsSilentRevalidation`).

## A guarda escrita para um caminho recusou o outro caminho inteiro (2026-08-28)

**O que aconteceu:** `BillOrigin.Create` exigia ao menos um identificador — fonte, remetente,
mensagem, arquivo ou hash. A regra nasceu junto com a captura por e-mail, onde sempre existe pelo
menos um. A importação manual, porém, nasce **só com os dígitos**: quem cola a linha digitável não
tem arquivo, nem remetente, nem mensagem. Resultado: **toda** importação feita pela tela de boletos
respondia `BLP.BIL12`, desde o primeiro commit do BC.

**Por que sobreviveu com a suíte verde:** os três testes de integração de `ManualUpload` mandavam
`StorageKey` — inclusive o que se chama `PostImport_FromManualUpload_ShouldBeAcceptedWithoutASource`,
que afirma sobre a ausência da **fonte de captura**, não sobre a ausência de identificador. A suíte
testava exatamente o corpo que o aplicativo **não** monta.

**A regra:** invariante que vale para um subconjunto das origens é **capacidade do Smart Enum**, não
`if` no VO — `BillSourceKind.RequiresOriginIdentifier`, irmão do `RequiresCaptureSource` que já
estava lá. E ao acrescentar guarda em VO compartilhado por mais de um caminho de entrada, escreva o
teste com **o corpo que o cliente real manda**, não com o corpo mais completo que a API aceita.

**Consequência honesta:** com o catálogo de hoje a `BLP.BIL12` ficou **inalcançável** — `Mailbox` e
`Portal` exigem `SourceId`, e tê-lo já satisfaz a guarda. Ela fica como defesa em profundidade, no
mesmo espírito de `BLP.BIL03`/`BLP.BIL04`, e volta a valer no dia em que existir um tipo de origem
que dispense a fonte e ainda assim precise de rastro.

## `[Required]` em record posicional vai no PARÂMETRO, nunca em `[property:]` (2026-08-28)

**O que aconteceu:** o modelo do `multipart/form-data` da importação foi escrito com
`[property: Required] DateTime? ReceivedAt`. Toda requisição com arquivo voltava **400**, com
`InvalidOperationException` do MVC: *"Record type ... has validation metadata defined on property
'ReceivedAt' that will be ignored. 'ReceivedAt' is a parameter in the record primary constructor
and validation metadata must be associated with the constructor parameter."*

**Por que é traiçoeiro:** não é aviso — o MVC **recusa o modelo inteiro**, e o 400 genérico lê como
"o cliente mandou coisa errada". A forma `[property: JsonRequired]`, que o modelo JSON irmão usa
duas linhas acima, está certa: o binder de JSON lê a propriedade, o de formulário usa o construtor.

**A regra:** validação em record posicional é `[Required] Tipo Nome`, sem prefixo de alvo. E o
defeito só apareceu porque o teste atravessa a **borda HTTP**: um teste que chamasse o handler pelo
mediator nunca teria visto o binding — a mesma lição que `AttachArtifactEndpointTests` já registra.

## O que o balde gravou não volta atrás com a transação do EF (2026-08-28)

**O que aconteceu:** a importação com arquivo grava no `IAttachmentStorage` **antes** de compor a
origem e capturar o boleto — a chave é dado da origem, então não há como inverter a ordem. Só que o
balde está fora da transação implícita do EF: quando a captura recusa (reenviar o mesmo boleto é o
engano mais comum e responde `BLP.BIL02`), o arquivo já estava gravado e ficaria órfão a cada
tentativa.

**A regra:** falha fechada em recurso externo à transação é **compensação explícita** — um
`RemoveAsync` no `catch`, com `CancellationToken.None`, porque desistir da limpeza porque o request
foi cancelado é justamente o que produz o órfão. Compare com `ConnectCaptureSourceCommandHandler`,
que não precisa disso: lá o cofre grava pelo mesmo `DbContext`, e a unidade de trabalho desfaz tudo.
