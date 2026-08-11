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
