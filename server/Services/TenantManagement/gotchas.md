# gotchas — TenantManagement

Armadilhas que já custaram tempo neste BC. Cada uma falha em **silêncio** — é por isso que estão
escritas.

## 1. `OnChallenge` do JwtBearer sem `HandleResponse()` devolve **200** em vez de 401

**Sintoma:** `POST /api/v1/tenants` sem token nenhum respondia `200 OK` com o corpo
`{"error": "Unauthorized access"}`. Qualquer cliente que olhe o status — o nosso olha — trataria a
negativa de acesso como sucesso.

**Causa:** escrever o corpo dentro de `OnChallenge` **commita a resposta** com o status padrão
(200) antes de o framework definir o 401. O código veio assim do PeopleManagement.

**Correção:** `context.HandleResponse()` → `StatusCode = 401` → `ContentType` → `WriteAsync`,
nessa ordem. E `OnAuthenticationFailed` só registra: escrever nos dois produz dois corpos JSON
concatenados.

> ⚠️ **O mesmo defeito está no `PeopleManagement.API/Authentication/AuthenticationExtesion.cs`.**
> Não foi corrigido lá porque está fora do escopo deste BC — mas está aberto.

**Como pegar de novo:** a suíte de integração **não** cobre isto (ela troca o esquema de
autenticação pelo dublê). A verificação é em contêiner:
`curl -s -o /dev/null -w "%{http_code}" -X POST localhost:8110/api/v1/tenants -d '{}' -H "content-type: application/json"` → tem que ser `401`.

## 2. `LIKE` em propriedade com Value Converter explode em tempo de execução

**Sintoma:** buscar tenant por CNPJ na listagem devolvia **500**, não "nenhum resultado".

**Causa:** `PrimaryTaxId` é um Value Object com `ValueConverter`. O EF aplica o conversor
**também ao parâmetro** do `LIKE`, e o padrão `%11222333%` era empurrado por `TaxId.Parse` →
`InvalidCastException`. Vale para `EF.Functions.Like(t.PrimaryTaxId.Value, ...)` **e** para
`EF.Property<string>(t, nameof(Tenant.PrimaryTaxId))` — as duas formas falham.

**Correção:** consulta separada (`TenantQueries.FindIdsByPartialTaxIdAsync`) com SQL explícito,
e o resultado entra como `Contains` de ids. Custa uma ida a mais ao banco, só quando o termo tem
dígito. Coberto por teste de regressão.

## 3. O guard de rota casa pelo **nome do parâmetro**, e o claim por **substring**

- `RouteAccessRequirementHandler` lê `RouteNameRequirement` (`tenantId`). Batizar um parâmetro de
  rota de back-office de `tenantId` tranca o operador da plataforma para fora com 403 — ele não
  tem tenant no claim. Por isso as rotas de back-office usam `{id}`.
- Os claims são procurados com `Type.Contains(claimType)`. O claim **tem** que se chamar
  `tenants`: `"tenant_ids".Contains("tenants")` é falso e reprovaria todo mundo. Ver ADR-003.

## 4. `MigrationsHistoryTable` precisa ser repetido fora do DI

Quem constrói o `DbContext` à mão (a fábrica da suíte de integração) não herda o
`MigrationsHistoryTable` do `AddInfraDependencies`. Sem repetir, o histórico vai para o schema
`public`, o host não acha registro nenhum, tenta criar tudo de novo e morre em `42P07`. E
`__ef_migrations_history` entra no `TablesToIgnore` do Respawn pelo mesmo motivo.

## 5. Smart Enum é gravado pelo `Id`

Renumerar um valor não quebra compilação nenhuma e reescreve em silêncio o significado do que já
está no banco — trocar `MembershipRole.Owner` de 1 para 2 promoveria ou rebaixaria gente calada.
`EnumerationPersistenceTests` congela os números.

## 6. O import de autorização não aceita policy do tipo `js`

**Sintoma:** `Could not import the resource due to unknown_error`, e no log do servidor
`java.lang.RuntimeException: Script upload is disabled`.

**Causa:** o `tenant-management-authz-config.json` trazia a "Default Policy" do Keycloak, que é do
tipo `js`. Desde o Keycloak 21 o upload de script vem **desabilitado** (`JSPolicyProviderFactory`
recusa no `onImport`), e o import inteiro morre por causa dela.

**Correção:** a "Default Policy", a "Default Permission" e a "Default Resource" saíram do arquivo.
Eram boilerplate — nada neste BC pede permissão para o recurso default: o
`AuthorizationServerClient` sempre manda `permission=<recurso>#<escopo>` com os recursos nomeados.

## 7. O claim precisa se chamar `tenants`, no plural, e ser multivalorado

**Sintoma:** ninguém passa no guard de rota do tenant, ou passa só quem tem **um** tenant.

**Causa:** duas configurações do mapper `oidc-usermodel-attribute-mapper` no client scope
`tenant-scope`:

- **`claim.name` = `tenant`** (singular). O `RouteAccessRequirementHandler` procura o claim com
  `Type.Contains("tenants")`, e `"tenant".Contains("tenants")` é falso — reprova todo mundo.
- **`multivalued` ausente.** Com dois ou mais tenants, o claim sai como uma string só em vez de
  lista, e a comparação com o id da rota falha.

Ambas falham em silêncio: o login funciona, o token chega, e o acesso é negado sem explicação.

## 8. Nome de tenant virando nome de pessoa

**Sintoma:** o titular aparece no Keycloak chamando-se "Padaria do Zé LTDA".

**Causa:** `ITenantAccessProvisioner.GrantAccessAsync` recebia um `displayName`, e o único nome à
mão no handler era o do tenant (`domainEvent.TenantLegalName`). O adapter gravava aquilo em
`firstName`. Nada acusa: o usuário é criado, o acesso funciona, e o erro só aparece quando alguém
olha a lista de usuários.

**Correção:** o parâmetro **saiu da porta**. Este BC não conhece o nome da pessoa — o vínculo é
chaveado por e-mail justamente por isso. O convite passou a pedir `UPDATE_PROFILE`, e quem informa
o nome é a própria pessoa no primeiro acesso.

**Como pegar de novo:** `KeycloakUserPayloadTests` afirma que o payload de criação não tem
`firstName`, que o convite pede `UPDATE_PROFILE`, e que pessoa **já existente** conserva o nome que
tinha. Verificado por sonda: reintroduzir o `FirstName` faz o primeiro teste falhar.

> ⚠️ **Quem já foi criado errado não se conserta sozinho.** A atualização parte da representação
> lida, então o `firstName` errado é preservado de propósito. Limpe o campo no console do Keycloak
> — a pessoa preenche o próprio nome no primeiro acesso.
