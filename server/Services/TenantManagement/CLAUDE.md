# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

> Leia junto com `../../CLAUDE.md` (nível servidor). Este arquivo estende/sobrepõe o pai para o BC **TenantManagement**.

## O que é isto

O Bounded Context que **emite a identidade do cliente da plataforma e diz quem a acessa** —
pessoa física e jurídica no mesmo modelo. Existe porque o `Company` do PeopleManagement é
CNPJ-only e o BillPayment atende PF; até aqui, ninguém emitia `TenantId`, ele nascia à mão no
console do Keycloak.

O design rationale vive em `TenantManagement.Architecture/` — o ponto de entrada é
[`index.md`](TenantManagement.Architecture/index.md), e **todo documento novo deve ser registrado lá**.

**Arquitetura: Clean Architecture com DDD e CQRS**, idêntica à do BillPayment: `API → Application
→ Domain`, `Infra → Domain` por inversão de dependência. Toda geração e manutenção dessas camadas
é feita pelas skills `domain-codegen-ddd-dotnet`, `application-codegen-ddd-dotnet`,
`infra-codegen-ddd-dotnet`, `api-codegen-ddd-dotnet`, `tests-domain-ddd-dotnet` e
`tests-integration-ddd-dotnet` — **invoque-as via Skill em vez de escrever DDD à mão**.

### Cinco regras que não podem erodir

**Este BC não é chamado em runtime pelos produtos.** A única coisa que atravessa é o claim do
token. Um `HttpClient` apontando para cá dentro do PeopleManagement ou do BillPayment é violação
(ADR-002). O que o cadastro precisa **impor** aos produtos — suspensão e entitlement de produto —
viaja pelo provedor de identidade, nem por chamada nem por evento (ADR-005).

**Suspender um tenant corta o acesso de todo mundo, o titular incluído.** É o que
`TenantStatus.Suspended` declara e o que os handlers cumprem. A revogação passa pelo
**provisionador**, nunca por `RevokeMembership`: o método de domínio protege o último responsável
(`TNM.TNT20`) e recusaria cortar justamente o dono, deixando a suspensão pela metade. O vínculo
continua ativo no cadastro — suspender preserva o cadastro.

**Nenhum termo do provedor de identidade cruza a porta.** `grep -ri keycloak` fora de
`Infra/Identity/`, `API/Authentication/`, `API/Authorization/` e dos `appsettings` é violação.
Trocar de provedor é um adapter novo e uma linha de configuração.

**Acesso que ninguém concedeu não é reportado como concedido.** Sem configuração, o
provisionamento **falha alto** e o vínculo fica `Failed`, visível na consulta. Nunca troque o
`UnconfiguredTenantAccessProvisioner` por um dublê que finge sucesso.

**Cadastro sem dono não existe.** `Register` cadastra e concede o acesso do titular no mesmo ato,
e o último responsável não pode perder o acesso (`TNM.TNT20`).

## Build, Run & Test

Este BC tem **`.sln` própria** — e, desde 2026-08-18, os 6 projetos também estão incluídos na
`../../RufinoProject.sln` (que abre os 3 BCs juntos no Visual Studio). Para trabalhar SÓ neste BC,
continue operando desta pasta; o compose da raiz (`server/docker-compose.yml`) sobe os 3 BCs +
Keycloak + storage e publica **as mesmas portas** deste compose — use um OU outro.

```powershell
dotnet build TenantManagement.sln
dotnet run --project TenantManagement.API

dotnet test TenantManagement.UnitTests
dotnet test TenantManagement.IntegrationTests   # exige Docker (Testcontainers + postgres:17)

dotnet test --filter "FullyQualifiedName~ClassName"

docker compose up --build
docker compose up -d tenantmanagement.db        # só o banco, para rodar a API pelo VS/dotnet run

# Migrations — rodar de dentro de TenantManagement.API/
dotnet ef database update --project ../TenantManagement.Infra --startup-project .
dotnet ef migrations add <Nome> --project ../TenantManagement.Infra --startup-project . --output-dir Migrations

# Segredos de desenvolvimento (NUNCA no appsettings.json). Rodar de TenantManagement.API/
dotnet user-secrets set "TenantProvisioning:ClientSecret" "<segredo do client provisionador>"
dotnet user-secrets set "Keycloak:Credentials:secret" "<segredo do resource server>"

# Backfill dos cadastros que já existem, preservando o Guid
node TenantManagement.Architecture/tools/backfill-tenants.js --api=http://localhost:8110 --file=./tenants.local.json
```

### Port map (docker-compose)

| Serviço | Porta host | Porta container |
|---|---|---|
| `tenantmanagement.api` | 8110 | 8080 (HTTP) |
| `tenantmanagement.db` | 8112 | 5432 |

Postgres `postgres:17-alpine`, schema `tenant_management`, database `TenantManagementDb`. **A
connection string do `appsettings.json` aponta para `localhost:8112`** — tem que casar com a
tabela acima, senão todo `dotnet run` morre em `SocketException (10061)` antes do Kestrel subir.

**O schema é criado e evoluído por MIGRAÇÕES**, em produção, em desenvolvimento e na suíte de
integração. `MigrateAsync` roda no startup. Acrescentou Aggregate ou mudou mapping? **Gere uma
migração**; não recrie o banco à mão. Migração é código gerado e não se edita à mão — corrigir
uma migração aplicada é gerar outra.

### Swagger

`/` redireciona para `/swagger`, e ambos são **gated em `IsDevelopment()`**. Documento OpenAPI em
`/openapi/v1.json` (gerado por `Microsoft.AspNetCore.OpenApi`, renderizado pelo Swashbuckle — o
híbrido que o template .NET 10 entrega). Em `dotnet run` (perfil `http`): `localhost:5279`.

## Superfície HTTP

```
POST   /api/v1/tenants                            [ProtectedResource(tenant, create)]
GET    /api/v1/tenants?kind=&status=&product=&search=&cursor=&limit=
GET    /api/v1/tenants/{id}                       [tenant, view]
PUT    /api/v1/tenants/{id}                       [tenant, edit]
PUT    /api/v1/tenants/{id}/address               [tenant, edit]
PUT    /api/v1/tenants/{id}/contact               [tenant, edit]
POST   /api/v1/tenants/{id}/suspend|reactivate    [tenant, suspend]
POST   /api/v1/tenants/{id}/products/{product}    [tenant-product, edit]
DELETE /api/v1/tenants/{id}/products/{product}    [tenant-product, edit]
POST   /api/v1/tenants/{id}/members               [tenant-access, edit]
DELETE /api/v1/tenants/{id}/members?email=        [tenant-access, edit]
POST   /api/v1/tenants/{id}/access/reprovision    [tenant-access, edit]
GET    /api/v1/me/tenants                         [Authorize] — alimenta o seletor de contexto do cliente
```

**O parâmetro de rota do back-office se chama `id`, não `tenantId`, de propósito.** O
`RouteAccessRequirement` casa pelo **nome do parâmetro**; batizá-lo de `tenantId` trancaria o
operador da plataforma para fora com um 403 sem explicação, porque ele não tem tenant no claim.
Rotas do próprio tenant, se existirem no futuro, usam `/api/v1/{tenantId}/...` e passam pelo guard.

## Architecture — o que é não-óbvio

- **`AccessProvisioning` e `ActiveProducts` são derivados e `Ignore`dos no EF.** Um campo próprio
  seria uma segunda versão da mesma informação, livre para divergir. **Esquecer o `Ignore` não é
  falha estética**: o EF passa a tratar a coleção como mapeável, o modelo diverge das migrações e
  a suíte de integração inteira morre em `PendingModelChangesWarning` antes do primeiro teste.
- **Um único caminho sincroniza o acesso: `TenantAccessSynchronizer`.** Suspender, reativar, ativar
  produto e desativar produto mudam a mesma coisa — quem enxerga aquele tenant e em quais produtos
  —, então os quatro handlers delegam a ele, e ele **deriva o estado desejado do agregado**, nunca
  do payload do evento. Ler o evento faria cada handler recalcular a resposta por conta própria, e
  três deles acertariam. O quinto e o sexto caminhos (`MembershipGranted`/`Revoked`) continuam
  agindo sobre **um** e-mail, porque é isso que mudou ali.
- **A porta de provisionamento declara ESTADO DESEJADO, não incremento.** `GrantAccessAsync` recebe
  os produtos ativos do tenant e o adapter **retira** o tenant dos atributos dos produtos que não
  estão na lista. É o que faz a mesma chamada servir para conceder acesso, ativar produto e
  desativar produto — o provedor não precisa saber qual das três aconteceu, só qual é o resultado.
- **Um atributo por produto, além do `tenants` genérico.** `bp_tenants` e `pm_tenants` trazem os
  tenants em que a pessoa tem vínculo ativo **e** aquele produto está habilitado; é o que faz o
  produto governar o acesso. **O nome não é livre**: o guard casa o *tipo* do claim por `Contains`,
  e `"bp_tenants".Contains("tenants")` é verdadeiro. O sentido que importa está seguro
  (`"tenants".Contains("bp_tenants")` é falso, então o BillPayment não aceita o genérico), mas
  produto novo exige nome que nenhum outro contenha.
- **`RequeueFailedAccessProvisioning` consulta o status do tenant.** Num tenant suspenso ele emite
  **revogação**, não concessão — mesmo com o vínculo ativo, porque num tenant suspenso o estado
  desejado no provedor é "ninguém tem acesso". Sem isso, `POST /tenants/{id}/access/reprovision`
  seria a forma de burlar a suspensão: bastava pedir o conserto de um vínculo pendente.
- **O vínculo é chaveado por e-mail**, não pelo id da pessoa no provedor: na hora da concessão ela
  pode ainda não existir lá. Revogar não apaga a linha; reconceder reaproveita.
- **A porta de provisionamento NÃO recebe nome de pessoa, e isso é a correção de um bug real.**
  `GrantAccessAsync` recebia um `displayName`, o handler passava o `TenantLegalName` do evento, e o
  adapter gravava aquilo em `firstName` — o titular aparecia no Keycloak chamando-se
  "Padaria do Zé LTDA". Este BC **não conhece** o nome de quem está do outro lado do e-mail; o
  cadastro que ele guarda é o do TENANT. Quem informa o próprio nome é a pessoa, e por isso o
  convite pede `UPDATE_PROFILE` além de `UPDATE_PASSWORD` e `VERIFY_EMAIL`. A assinatura sem o
  parâmetro é o que torna o erro irrepresentável; `KeycloakUserPayloadTests` guarda o payload.
- **A atualização de quem já existe parte da representação lida.** É o que preserva o nome que a
  pessoa informou e o `companies` de que o PeopleManagement depende — montar um objeto novo só
  com `tenants` apagaria os dois.
- **Eventos são despachados DEPOIS do commit**, in-process, sem outbox. Despachar antes poderia
  conceder acesso a um tenant que a transação seguinte desfaz. O preço: se o processo morrer entre
  o commit e o despacho, o vínculo fica `Pending` até alguém reprovisionar — aceito para um
  cadastro operado por gente. Ver `03-access-provisioning.md`.
- **A falha do provedor é engolida no handler de propósito.** Derrubar a requisição faria o
  cliente reenviar um cadastro que já existe.
- **`LIKE` não funciona em propriedade com Value Converter.** A busca por documento na listagem
  usa uma consulta separada (`FindIdsByPartialTaxIdAsync`): o EF aplica o conversor **também ao
  parâmetro**, e o padrão `%11222333%` era empurrado por `TaxId.Parse`, matando a listagem inteira
  com `InvalidCastException` — 500, não "nenhum resultado". Coberto por teste de regressão.
- **Smart Enums são gravados pelo `Id`.** Renumerar não quebra compilação nenhuma e reescreve em
  silêncio o significado do que está no banco. `EnumerationPersistenceTests` congela os números.
- **`Products` e `Memberships` são owned collections** — carregadas com o agregado, sem `Include`.
- **O documento primário tem índice único global.** A checagem no handler evita o round-trip no
  caso comum; quem resolve a corrida é o índice.
- **Toda action de ESCRITA loga o Command; leitura não loga.** O `BaseController` expõe
  `SendingCommandLog`/`CommandResultLog` (o padrão do PeopleManagement), chamados nas 11 escritas do
  `TenantsController`. Três coisas não podem erodir: (1) o que se loga é o **Command**, não o Model,
  porque parte das actions constrói o Command com `new` e não tem Model; (2) o id de correlação é
  `identified.Id`, **não** o `requestId` cru — `EnsureRequestId` gera um Guid novo quando o header vem
  vazio, e logar o cru registraria `Guid.Empty`, perdendo o par com o `IdentifiedCommandHandler`;
  (3) o payload só sai no log de **envio**, não no de resultado — os dois se correlacionam pelo
  `RequestId`. No `Register` não há id de rota (o tenant ainda vai nascer): vai `null`, e o id novo
  aparece no `{@Result}`.
- **`ISensitiveCommand` (`Application/Mediator/`) existe sem nenhum implementador, de propósito.**
  Um Command que o implemente tem o payload trocado por `[omitido: ISensitiveCommand]` no log. Hoje
  nenhum comando deste BC carrega segredo — o `ClientSecret` do provisionador vem de configuração,
  não de request. O marcador está aqui para que o primeiro que carregar tenha onde declarar, em vez
  de a decisão ser tomada às pressas. **O cadastro do tenant (nome, documento, endereço, contato) É
  logado** — é dado pessoal, e quando o sink estruturado entrar isso precisa de decisão explícita.
- **`LoggingBehavior` desembrulha o `IdentifiedCommand` para nomear o request.**
  `typeof(TRequest).Name` devolvia `` IdentifiedCommand`2 `` em 100% das escritas — a duração era
  medida e atribuída a um nome que não distingue uma operação da outra.
- **O `DomainExceptionFilter` loga o que traduz — e só isso.** Marcar `ExceptionHandled` tira a
  exceção do caminho do middleware do ASP.NET Core, que é quem logaria: sem essas linhas, tudo que o
  filtro trata sumia do log. `DomainException` sai em **Information** (regra de negócio recusando é o
  sistema funcionando), `InvalidOperationException` em **Warning**. Exceção **inesperada** não é
  capturada aqui: ela segue para o middleware, que já a registra em `Error` — e
  `Microsoft.AspNetCore: Warning` não suprime `Error`.
- **`OnChallenge` do JwtBearer precisa de `HandleResponse()` antes de escrever.** Sem isso a
  resposta é commitada com **200** e uma requisição sem token vira sucesso para o cliente. Veio
  assim do PeopleManagement, onde **continua aberto**. Ver `gotchas.md` #1 — e note que a suíte
  **não** cobre isso: ela troca o esquema de autenticação pelo dublê.

## Mandatory testing workflow

**Toda alteração de código em qualquer camada exige rodar as duas suítes — unitária E de
integração — antes de encerrar a tarefa.** Mudanças em SeedWork, VOs, factories de erro, mappings
EF ou pipelines de Application quebram testes aparentemente não relacionados.

**Todo método de teste tem um comentário em português, uma linha, acima do `[Fact]`/`[Theory]`**,
descrevendo cenário + comportamento esperado em linguagem de negócio. Vale para testes novos e
para qualquer teste tocado.

**Bug encontrado → teste de regressão obrigatório**, no nível certo, com o comentário explicitando
que é regressão e qual era o bug.

**Se qualquer teste falhar após uma alteração, PARE e avise o usuário.** A falha pode ser
intencional (o comportamento mudou de propósito) ou regressão — só o usuário distingue.

## Mandatory documentation workflow

**Este `CLAUDE.md` precisa refletir o estado atual do código a cada alteração relevante, no mesmo
commit.** Atualize sempre que: Aggregate/VO/evento/erro novo, decisão arquitetural ou convenção
nova (vira ADR em `TenantManagement.Architecture/adr/`), estrutura de pastas muda, ou
build/run/test muda. Não duplique o que está em `TenantManagement.Architecture/` — aqui vive
*estado* e *convenção*, lá vive *design rationale*.

## Status

| Fase | Escopo | Estado |
|---|---|---|
| 1 | Esqueleto do BC (6 projetos, sln, compose, analyzers) | ✅ |
| 2 | Domain + 163 testes unitários | ✅ |
| 3 | Application (11 commands, 3 queries), Infra (EF + adapter Keycloak), API | ✅ |
| 4 | 59 testes de integração (Testcontainers + Respawn) | ✅ |
| 5 | Config do Keycloak: 2 clients, `tenant-scope`, resources/scopes/policies | ✅ (exportada; falta aplicar no realm) |
| 6 | Backfill dos cadastros existentes preservando o Guid | ✅ (ferramenta pronta; falta executar) |
| 7 | Documentação (Architecture, ADRs, este arquivo) | ✅ |
| 8 | **Integração com os produtos pelo claim** (ADR-005): suspensão corta acesso, produto governa entitlement | ✅ (2026-08-17) |

## Checklist pré-produção

- [ ] **Aplicar a config no realm** — importar `utils/KeyCloakConfig/tenant-management-authz-config.json`
      no client `tenant-management-api`. **O `tenant-management-provisioner` já existe**: ele está no
      export da nuvem de 2026-08-18 e no realm local, com `manage-users`/`view-users` do
      `realm-management` na conta de serviço (verificado em 2026-08-19). O que falta é o **segredo**,
      que os exports do admin console mascaram — regenere em Credentials → Regenerate.
- [ ] **Segredos por variável de ambiente EM PRODUÇÃO** — `Keycloak__Credentials__secret` e
      `TenantProvisioning__ClientSecret`. Nunca no `appsettings.json`. **Em desenvolvimento é o
      oposto**: o segredo vem do `dotnet user-secrets` e o compose **não pode** defini-lo, porque
      `${VAR:-}` injeta string vazia e variável de ambiente vence user-secrets — ver o aviso em
      `../../CLAUDE.md`.
- [x] **`TenantProvisioning:Enabled`** — está **`true`** no `appsettings.json`, não desligado como
      esta linha afirmava. Com ele ligado e sem segredo, `IsConfigured` é falso, o DI registra o
      `UnconfiguredTenantAccessProvisioner` e todo vínculo sai `Failed` — que é o modo de falha
      correto (acesso que ninguém concedeu não é reportado como concedido), mas engana quem procura
      o defeito no Keycloak em vez de na configuração.
- [ ] **CORS de produção** — popular `Cors:AllowedOrigins` (sem wildcard).
- [ ] **Executar o backfill** ANTES de o BillPayment ligar a validação do claim.
- [ ] **Declarar `bp_tenants` e `pm_tenants` no User Profile do realm** (Realm settings → User
      profile), multivalorados, `view: [admin, user]` / `edit: [admin]` — copie a entrada de
      `tenants`. **Isto é separado do mapper e não é opcional**: o mapper transforma atributo em
      claim, mas quem autoriza o atributo a existir é o User Profile. Com
      `unmanagedAttributePolicy` ausente (o caso do realm), o Keycloak **descarta atributo não
      declarado e responde HTTP 204** — o `KeycloakTenantAccessProvisioner` lê isso como sucesso,
      marca o vínculo `Done`, e o claim nunca chega ao token. Medido em 2026-08-19: era a causa de
      todo endpoint do BillPayment responder 403 com o papel `bill-admin` corretamente atribuído.
      Já está no `realm-import-2026-08-18.json`; realm importado antes dessa correção precisa do
      passo à mão.
- [ ] **Reprovisionar TODOS os tenants depois de importar os mappers novos** (`bp_tenants`,
      `pm_tenants`) e antes de o produto passar a ler o claim dele. A ordem é
      backfill → **User Profile** → mappers → reprovisionamento → produto lê o claim novo; fora
      dela o atributo nasce vazio e **todo cliente legítimo toma 403** (ADR-005).
      ⚠️ `POST /tenants/{id}/access/reprovision` **só recoloca na fila vínculo que não está
      `Done`** (`NeedsProvisioning() => !Done`). Vínculo já marcado `Done` por uma escrita que o
      Keycloak descartou **não** é reprocessado por ele: use desativar+reativar o produto, ou
      suspender+reativar o tenant, que passam pelo `TenantAccessSynchronizer`.
- [ ] **Papéis no realm** — `tenant-admin` e `tenant-support` atribuídos a quem opera o back-office.
- [ ] **Health check do Postgres** e logging estruturado com correlation id. **Parcialmente
      coberto**: as 11 escritas já logam Command + resultado, o `LoggingBehavior` mede duração com o
      nome real do Command, e o `DomainExceptionFilter` registra o que traduz. Falta o correlation id
      por request (hoje a correlação é pelo `x-requestid`, que só existe em escrita) e o sink
      estruturado — e, junto dele, decidir o que fazer com o dado pessoal do cadastro, que hoje é
      logado por inteiro.
- [ ] **CI** — build + as duas suítes, com `-p:TreatWarningsAsErrors=true`.

## Conventions inherited from the DDD skills

- Código em inglês; mensagens de `DomainException` em português; conversa em português.
- `TreatWarningsAsErrors` no Domain. NetAnalyzers + SonarAnalyzer via `Directory.Build.props`,
  severidades no `.editorconfig`. **Verificação: `dotnet build TenantManagement.sln
  -p:TreatWarningsAsErrors=true` fecha com 0 warnings e 0 erros.**
- Strongly-typed Ids (`record struct : IEntityId<TSelf>`), Smart Enums via `Enumeration`, VOs
  herdando de `ValueObject`. Sem `record` para VO, sem `enum` nativo, sem `Guid` solto.
- Aggregate Roots são os únicos a emitir Domain Events.
- Mediator próprio (sem MediatR) em `Application/Mediator/`, registrado por `AddCustomMediator`.
- Idempotência: comandos de escrita embrulhados em `IdentifiedCommand`, checados contra
  `IRequestManager` (`client_requests`), header `x-requestid`.
- Erros de regra vivem no Domain (`TenantErrors`); a Application só dispara. Não existe
  `ApplicationErrors`.
