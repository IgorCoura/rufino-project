# 03 — Provisionamento de acesso

## O caminho

```
POST /tenants  ──► Tenant.Register ──► MembershipGranted (evento)
                        │                      │
                   commit no banco             │  (depois do commit)
                        │                      ▼
                        │            ITenantAccessProvisioner.GrantAccessAsync
                        │                      │
                        │           ┌──────────┴──────────┐
                        │        sucesso                falha
                        │           │                     │
                        ▼           ▼                     ▼
                   tenant existe   vínculo Done      vínculo Failed
                                                (visível na consulta,
                                                 curável por reprovision)
```

## O que o adapter faz no Keycloak

1. Procura a pessoa por e-mail (`GET /admin/realms/{realm}/users?email=&exact=true`).
2. Não existe → cria (`enabled`, `emailVerified=false`, ações obrigatórias `UPDATE_PASSWORD` e
   `VERIFY_EMAIL`) já com o tenant no atributo, e dispara o convite por e-mail.
3. Existe → acrescenta o tenant ao atributo multivalorado `tenants`.
4. Revogar → remove o tenant do atributo. **A pessoa não é apagada**: ela pode ter acesso a
   outros tenants, e apagá-la seria decidir por eles.

**A atualização parte da representação lida.** O `PUT` de usuário do Keycloak substitui o mapa de
atributos inteiro: montar um objeto novo só com `tenants` apagaria o `companies` de que o
PeopleManagement depende.

## O claim

O atributo `tenants` é exposto pelo client scope **`tenant-scope`** como claim **`tenants`**
(multivalorado), simetria exata com o `companies` do PeopleManagement — e o nome não é
negociável: ver [ADR-003](adr/ADR-003-claim-tenants.md).

## Duas consequências que precisam ser sabidas

- **O claim só muda no próximo token.** Revogar acesso não derruba a sessão em curso; o access
  token vive ~5 minutos.
- **Atributo multivalorado infla o token.** Um operador da plataforma **não** entra em todos os
  tenants pela lista — ele usa papel próprio (`tenant-admin`), e o guard de rota não se aplica às
  rotas de back-office.

## Quando falha

O provedor não participa da transação do banco. Por isso:

- O estado do provisionamento mora **no vínculo**, e o tenant o expõe agregado em
  `AccessProvisioning` (`Pending` → `Done` \| `Failed`).
- A falha é **engolida no handler de propósito**: derrubar a requisição faria o cliente reenviar
  um cadastro que já existe.
- `POST /tenants/{id}/access/reprovision` recoloca na fila tudo que não chegou ao provedor. É
  idempotente — reexecutar sem risco é o que faz alguém de fato usar o botão.

**Não há outbox.** Se o processo morrer entre o commit e o despacho, o vínculo fica `Pending` até
alguém reprovisionar. É o preço aceito por um cadastro operado por gente, com volume baixo. Se o
BC passar a registrar tenant por conta própria, o outbox do BillPayment é o próximo passo.

## Configuração

Dois clients no realm, de propósito separados:

| Client | Para quê | Poder |
|---|---|---|
| `tenant-management-api` | Resource server: avalia `[ProtectedResource]` por ticket UMA | Só responde permissão |
| `tenant-management-provisioner` | Service account do adapter | `manage-users` + `view-users` |

Seção `TenantProvisioning` do `appsettings`. **`Enabled=false` é o padrão**, e sem configuração o
provisionamento **falha alto** (`UnconfiguredTenantAccessProvisioner`) em vez de fingir sucesso:
acesso que ninguém concedeu não pode ser reportado como concedido. O segredo vai por variável de
ambiente (`TenantProvisioning__ClientSecret`), nunca no `appsettings.json`.
