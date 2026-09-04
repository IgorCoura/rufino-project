# Convenções de identidade e autorização — realm `rufino`

> Estabelecidas em 2026-09-04, na refatoração que saiu da auditoria comparativa entre
> PeopleManagement e BillPayment. **Este arquivo é normativo**: API nova, papel novo ou seção de
> configuração nova seguem o que está aqui, e desviar exige registrar o porquê.
>
> O problema que ele resolve: o projeto vai ganhar mais APIs, e o realm já tinha três clients com
> três convenções de nome diferentes, papéis chamados `admin` sem dono aparente, e uma seção de
> `appsettings` chamada `AuthorizationOptions` que guardava a credencial de um fornecedor de
> assinatura — colidindo com a classe de mesmo nome que faz a autorização de verdade.

## 1. A regra de ouro

**Nome diz o QUE a coisa é e DE QUEM ela é. Nunca diz qual fornecedor a implementa.**

Fornecedor entra numa chave `Provider` dentro da configuração, nunca no nome da seção, do client,
do papel ou do recurso. Trocar de fornecedor tem que ser um adapter novo e uma linha de
configuração — não uma renomeação que atravessa realm, código e variável de ambiente.

## 2. Tabela de convenções

| Coisa | Regra | Exemplo |
|---|---|---|
| **Client (resource server)** | `<contexto>-api`, kebab-case | `people-management-api`, `bill-payment-api`, `tenant-management-api` |
| **Client (aplicação)** | `<produto>-<plataforma>` | `rufino-app` |
| **Client (integração de saída)** | `<capacidade>-client`, **sem marca** | `document-signing-client` (era `zapsign`) |
| **Client role** | `<contexto>-<papel>`, com o contexto por extenso | `people-admin`, `bill-approver`, `tenant-support` |
| **Recurso UMA** | substantivo singular, kebab, **minúsculo**, idêntico à string do `[ProtectedResource]` | `document`, `capture-source`, `payer-profile` |
| **Escopo UMA** | verbo kebab, minúsculo | `view`, `send2sign`, `mark-not-applicable` |
| **Client scope OIDC** | `<contexto>-access` — sem o sufixo `-scope`, que é redundante | `people-management-access`, `bill-payment-access`, `tenant-access` |
| **Resource type (urn)** | `urn:<clientId>:resources:<nome>` — derivado do clientId, sempre | `urn:people-management-api:resources:default` |
| **Claim de tenant** | `<sigla>_tenants` | `pm_tenants`, `bp_tenants` |
| **Seção de `appsettings`** | nome da **capacidade**, PascalCase, sem sufixo `Options`, sem fornecedor | `DocumentSigning`, `Messaging`, `Storage`, `BackgroundJobs` |
| **Connection string** | nome do que a conexão **serve**, não do banco | `PeopleManagement`, `BackgroundJobs` |

### O contexto por extenso, não a sigla

Papel é `people-admin`, não `pm-admin`. A sigla economiza cinco caracteres e cobra o preço de
alguém precisar saber que `pm` é PeopleManagement — no console do Keycloak, onde os papéis de N
clients aparecem juntos, isso importa. O contexto por extenso também casa 1:1 com o clientId
(`people-` → `people-management-api`), o que torna óbvio de quem é o papel sem consultar nada.

BillPayment e TenantManagement já seguiam isso (`bill-*`, `tenant-*`). O PeopleManagement era o
único fora do padrão, com `admin`, `doc-send` e `zapsign-webhook`.

## 3. O que NÃO renomear, e por quê

**Os claims `pm_tenants` e `bp_tenants` ficam como estão.** Renomeá-los é migração distribuída em
três repositórios: o mapa `Produto → atributo` em
`TenantManagement.Infra/Identity/TenantProvisioningOptions.cs`, os mappers do client scope
`tenant-access`, a declaração no **User Profile** do realm — sem a qual o atributo é descartado na
escrita com HTTP 204, em silêncio — e o reprovisionamento de todos os tenants, nessa ordem exata.
O ganho seria cosmético; o modo de falha é todo cliente legítimo tomando 403.

**API nova só precisa de uma linha** naquele mapa, mais o mapper e a declaração no User Profile.

## 4. A trava que torna a convenção segura

O guard de rota casava o **tipo** do claim por `Contains`. Com ele,
`"bp_tenants".Contains("tenants")` é verdadeiro: uma API configurada para ler o `tenants` genérico
aceitaria também os valores do claim de outro produto. O sentido que nos protegia era acidente de
nomenclatura, não desenho — e a próxima API a se chamar `<sigla>_tenants` reabriria o buraco.

Desde 2026-09-04 a comparação é **igualdade exata**, nos dois BCs
(`RouteAccessRequirementHandler`). Com isso, o sufixo `_tenants` fica seguro para quantas APIs
vierem.

## 4-B. Papel de realm × papel de client: quando usar qual

| | Client role | Realm role |
|---|---|---|
| Responde | "esta pessoa pode X **nesta API**?" | "esta pessoa **é** X?" |
| Onde cai no token | `resource_access.<client>.roles` | `realm_access.roles` |
| Como se verifica | ticket UMA contra o resource server | leitura do claim, **sem rede** |
| Exemplos | `people-reviewer`, `bill-approver` | `developer` |

**A regra:** se a resposta depende de um recurso de uma API, é client role e passa pelo
`[ProtectedResource]`. Se descreve a pessoa e nenhuma API a consome, é realm role e se lê do token.

O `developer` (2026-09-04) é o caso que ensinou isso. Ele existia como recurso `debug` **dentro do
`people-management-api`**, com uma policy de usuário cravando um nome. Três defeitos de uma vez:

1. **Vínculo errado.** A ferramenta é do aplicativo — verifica o Sentry, mostra as permissões
   carregadas, exibe o `AppConfig` —, e nenhuma das três chama o servidor. Amarrada a um recurso do
   PeopleManagement, ela quebrava sempre que aquele BC era mexido; e quebrou, quando o recurso saiu
   na limpeza deste mesmo dia.
2. **Custo desnecessário.** Perguntar "é desenvolvedor?" por ticket UMA é uma ida de rede para uma
   resposta que já viaja assinada no token.
3. **Policy que morre calada.** `{"users":"[\"igor\"]"}` deixa de valer no dia em que o usuário for
   renomeado, sem erro em lugar nenhum.

Hoje: realm role `developer`, lida por `DeveloperAccess` (`rufino_core`) direto do
`realm_access.roles`. **O papel não concede nada no servidor** — nenhuma API o lê, e nenhum recurso
UMA corresponde a ele.

⚠️ **Papel de realm decide o que MOSTRAR, nunca o que o servidor aceita.** O `DeveloperAccess` não
valida assinatura, e não precisa: o token veio do login e é o mesmo que vai para as APIs, que o
validam a cada requisição. No dia em que uma ferramenta de diagnóstico chamar a API, aquele endpoint
precisa do seu próprio `[ProtectedResource]` com escopo próprio — a flag habilita exibição, o efeito
continua passando pelo escopo do recurso que ele toca.

## 5. Os dois testes que impedem a erosão

Todo BC tem os dois, e nenhum deles pode ser apagado por "parecer redundante":

| Teste | O que garante | O que acontece sem ele |
|---|---|---|
| `EndpointProtectionTests` | Toda rota `api/v1` declara o parâmetro de tenant com o nome certo e carrega `[ProtectedResource]` com escopo | Um controller novo que batize o parâmetro de `{id}` fica sem guard anti-IDOR — sem erro, sem log, e com o teste do próprio endpoint passando |
| `RealmContractTests` | Todo par `(recurso, escopo)` do código existe no `*-authz-config.json` versionado | Recurso que o código pede e o realm não tem faz **todo mundo, inclusive o administrador**, tomar 403 naquele endpoint. Foi o caso de `archive` e do `"Archive"` com A maiúsculo — nome de recurso UMA é *case-sensitive* |

`RouteGuardTests` completa o par: ele exige que o claim que a produção lê seja o **mesmo** que a
suíte envia. Sem ele, trocar o claim no `appsettings` deixa a suíte verde exercitando um guard que
o deploy não monta — que foi exatamente o que aconteceu quando `companies` virou `pm_tenants`.

## 6. Passo de deploy

O `*-authz-config.json` versionado é a **entrada** do import no realm, não a documentação dele. Os
dois testes acima obrigam o arquivo a não mentir sobre o código; o que eles **não** provam é que o
realm em produção foi atualizado. Esse é passo de deploy.

### Como aplicar: `migrate-realm.sh`

```bash
export KC_ADMIN_TOKEN=$(curl -s -X POST   "http://192.168.15.41:8082/realms/master/protocol/openid-connect/token"   -d grant_type=password -d client_id=admin-cli -d username=Admin -d password=Admin   | python -c 'import json,sys;print(json.load sys.stdin)["access_token"])')

./migrate-realm.sh            # confere e relata, sem alterar nada
./migrate-realm.sh --apply    # aplica
```

```bash
export KC_ADMIN_TOKEN=$(curl -s -X POST   "https://keycloak.couratechsafety.cloud/realms/master/protocol/openid-connect/token"   -d grant_type=password -d client_id=admin-cli -d username=<admin> -d password=<senha>   | python -c 'import json,sys;print(json.load(sys.stdin)["access_token"])')

./migrate-realm.sh            # confere e relata, sem alterar nada
./migrate-realm.sh --apply    # aplica
```

Idempotente: o que já está aplicado é reconhecido e pulado. O token de admin vale ~60 s — falhando
com 401 no meio, refaça o token e rode de novo.

### Por que não é um import de realm

**Importar o realm inteiro por cima de um existente não migra — ele cria ao lado.** Renomear
`zapsign` para `document-signing-client` por import deixaria os dois; o mesmo vale para todo papel
renomeado. E há duas perdas irreversíveis: as **atribuições de papel dos usuários** e o **segredo
dos clients confidenciais**, que o export mascara.

O que a migração faz, e em que ordem — e **a ordem não é arbitrária**:

| # | Passo | Por que aqui |
|---|---|---|
| 1 | realm role `developer` | independente do resto |
| 2 | papéis do PM (`PUT` renomeia, `POST` cria) | **PUT preserva a atribuição**; apagar e recriar a perderia |
| 3 | papéis de alçada do BP, e os compostos | idem |
| 4 | authz dos dois clients, em bloco | as policies citam papel **por nome**: com o papel ausente a policy nasce com referência vazia e **nega tudo em silêncio** |
| 5 | renomear o client de assinatura (`PUT`) | preserva UUID interno, **segredo** e service account |
| 6 | client scopes e mappers | ver a ressalva abaixo |

### O que o script deliberadamente NÃO faz

Mexer em **mapper** fica manual: errar um mapper de audience derruba a autenticação inteira, e o
diff precisa ser lido por gente. São cinco passos no console:

1. em `people-management-access`: corrigir a audience `people_management-api` → `people-management-api`;
2. criar `bill-payment-access` com dois mappers (`client roles` + audience `bill-payment-api`);
3. em `tenant-access`: remover o mapper de audience do BillPayment (passa a viver no scope próprio);
4. em `rufino-app`: acrescentar `bill-payment-access` aos Default client scopes;
5. remover `company-scope` e o atributo `companies` do User Profile.

⚠️ **Nunca escreva `unmanagedAttributePolicy: "DISABLED"`** no User Profile: os valores válidos são
`ENABLED`, `ADMIN_VIEW` e `ADMIN_EDIT`, e é a **ausência** da chave que significa "descarta atributo
não declarado" — que é o comportamento desejado. Foi um erro cometido e revertido em 2026-09-04.

### O que sobra para uma pessoa decidir

Atribuir papel. Renomear preserva a atribuição antiga (quem era `admin` passa a ter
`people-admin`), mas papel **novo** não se atribui sozinho — em especial as três alçadas de risco e
o `developer`. E lembre que **`bill-admin` deixou de aprovar**: quem precisa aprovar recebe
`bill-approver` explicitamente, mais a alçada do nível.

O `RufinoRealm/realm-import-*.json` embute os dois arquivos: ao mexer num deles, resincronize o
realm (é o que o script da refatoração faz) para os dois não divergirem.

## 7. Segredo

Nenhum segredo entra em `appsettings.json` versionado. Produção: variável de ambiente no Dokploy.
Desenvolvimento e testes: `dotnet user-secrets`.

⚠️ **Nunca injete segredo pelo compose com a forma `${VAR:-}`**: ela não deixa a variável ausente
quando `VAR` não está definida — define com **string vazia**, e variável de ambiente vem depois do
user-secrets na ordem de configuração do ASP.NET Core. O segredo é sobrescrito em silêncio. Use
`${VAR:?mensagem}`, que falha alto.

Ao renomear uma seção, renomeie a variável de ambiente correspondente **no mesmo deploy**
(`S3__SecretKey` → `Storage__SecretKey`) — **e o `dotnet user-secrets` de quem desenvolve**, que é
o que quase se perde: ele não está em lugar nenhum do repositório, então nenhum build, teste ou
análise estática acusa a chave órfã. O sintoma é o componente falhar **na primeira requisição que
o usa**, não no arranque, porque os registros são preguiçosos.

Aconteceu em 2026-09-04, no mesmo dia em que estas convenções nasceram: `S3` virou `Storage`, o
`S3:SecretKey` do user-secrets ficou órfão, e o erro que apareceu foi um
`ArgumentNullException` em `awsSecretAccessKey` vindo de dentro do SDK da AWS — sem dizer qual
configuração, nem que ela havia sido renomeada. **Toda renomeação de seção que carregue segredo
exige uma guarda no ponto de composição**, com mensagem que nomeie a chave e a origem esperada;
ver `InfraInjectionConfig` do PeopleManagement.

Migração daquele dia, para referência:

| De | Para |
|---|---|
| `S3:SecretKey` | `Storage:SecretKey` |
| `SignOptions:AccessToken` | `DocumentSigning:AccessToken` |
| `AuthorizationOptions:ClientSecret` | `DocumentSigning:ServiceAccount:ClientSecret` |
| `WhatsApp:ApiKey` | `Messaging:ApiKey` |
| `HangfireDashboard:Password` | `BackgroundJobs:Dashboard:Password` |
| `Keycloak:Credentials:secret` | *(não mudou)* |
