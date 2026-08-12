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
