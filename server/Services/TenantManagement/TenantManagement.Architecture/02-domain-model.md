# 02 — Modelo de domínio

Sigla do BC: **`TNM`**. Um Aggregate Root, duas entidades filhas, quatro Value Objects.

## `Tenant` — Aggregate Root (`TNM.TNT`)

| Campo | Tipo | Nota |
|---|---|---|
| `Id` | `TenantId` | **É o valor que viaja no claim e na rota dos produtos.** Aceita ser informado no cadastro |
| `Kind` | `TenantKind` — `Individual` \| `Company` | Único lugar onde PF e PJ se diferenciam |
| `LegalName` | `string` (200) | Nome civil (PF) ou razão social (PJ) |
| `TradeName` | `string` (200) | Nome fantasia. Vazio em PF, por invariante |
| `PrimaryTaxId` | `TaxId` | CPF ou CNPJ com dígito verificador conferido |
| `Contact` | `ContactInfo` | E-mail obrigatório, telefone opcional |
| `Address` | `Address` | **Obrigatório** |
| `Status` | `TenantStatus` — `Active` \| `Suspended` | |
| `SuspensionReason` | `string` (300) | Vazio quando ativo |
| `Products` | `TenantProduct[]` | Owned collection |
| `Memberships` | `TenantMembership[]` | Owned collection |
| `AccessProvisioning` | `ProvisioningStatus` | **Derivado** dos vínculos; sem coluna |

### Invariantes

| Código | Regra |
|---|---|
| `TNM.TNT01` | Tipo obrigatório |
| `TNM.TNT02/03` | Nome obrigatório, até 200 caracteres |
| `TNM.TNT04` | Documento obrigatório |
| `TNM.TNT05` | **PF exige CPF, PJ exige CNPJ** |
| `TNM.TNT06/07` | Nome fantasia só em PJ, até 200 caracteres |
| `TNM.TNT08/09` | Contato e endereço obrigatórios |
| `TNM.TNT10` | **Documento primário único globalmente** (conflito) |
| `TNM.TNT11` | Tenant não encontrado |
| `TNM.TNT12` | Tenant suspenso não aceita alteração |
| `TNM.TNT13/14` | Suspender o que está suspenso / reativar o que está ativo |
| `TNM.TNT15` | Suspender exige motivo |
| `TNM.TNT16/17` | Produto obrigatório / produto não habilitado |
| `TNM.TNT18/19` | E-mail do vínculo obrigatório e válido |
| `TNM.TNT20` | **O último responsável não pode perder o acesso nem ser rebaixado** |
| `TNM.TNT21` | Vínculo não encontrado |
| `TNM.TNT22` | Papel obrigatório |
| `TNM.TNT23/24/25/26` | Tipo, produto, papel ou situação desconhecidos vindos da borda |

Outros prefixos: `TNM.TAX` (documento), `TNM.ADR` (endereço), `TNM.CTC` (contato), `SWK` (SeedWork).

### Comportamentos

`Register` · `Rename` · `ChangeContact` · `ChangeAddress` · `Suspend` · `Reactivate` ·
`ActivateProduct` · `DeactivateProduct` · `GrantMembership` · `RevokeMembership` ·
`ConfirmAccessProvisioned` · `MarkAccessProvisioningFailed` · `RequeueFailedAccessProvisioning`

**`Register` concede o acesso do titular no mesmo ato.** Cadastro sem dono é um tenant que
ninguém consegue abrir — e é o estado que ninguém percebe até precisar dele.

### Eventos

`TenantRegistered` · `MembershipGranted` · `MembershipRevoked` · `TenantSuspended` ·
`TenantReactivated` · `ProductActivated` · `ProductDeactivated`

Os dois de vínculo são o gatilho do provisionamento — ver [`03-access-provisioning.md`](03-access-provisioning.md).

## `TenantMembership` — entidade filha

A chave natural é o **e-mail**, não o identificador da pessoa no provedor: no momento da
concessão ela pode ainda não existir lá, e é o provisionamento que devolve o identificador.
Enquanto ele não volta, o vínculo já existe e fica visível como pendente.

Revogar **não apaga a linha** (`IsActive = false`): reconceder reaproveita o mesmo vínculo, e o
índice único `(tenant_id, email)` garante que nunca haja dois para a mesma pessoa.

## `TenantProduct` — entidade filha

Desabilitar preserva a linha com `DeactivatedAt`. O histórico de quando o produto esteve ligado é
o que explica cobrança e acesso passados.

## Value Objects

| VO | Regras |
|---|---|
| `TaxId` | 11 ou 14 dígitos, DV conferido, repetidos recusados. O tipo é deduzido do comprimento |
| `Address` | CEP de 8 dígitos, UF de 2 letras, país default `BRASIL`, complemento é o único opcional. Tudo em caixa alta |
| `ContactInfo` | E-mail obrigatório e sintaticamente válido; telefone de 10 ou 11 dígitos, opcional |

## Portas

| Porta | Implementação |
|---|---|
| `ITenantRepository` | `TenantRepository` (EF Core) |
| `ITenantAccessProvisioner` | `KeycloakTenantAccessProvisioner`, ou `UnconfiguredTenantAccessProvisioner` quando não configurado |
| `IRequestManager` | `RequestManager` sobre `client_requests` |
| `IUnitOfWork` | `TenantManagementDbContext` |

**Nenhum termo do provedor de identidade cruza a porta.** `grep -ri keycloak` fora de
`Infra/Identity/`, `API/Auth*` e dos `appsettings` é violação.

## Persistência

Schema `tenant_management`. Quatro tabelas: `tenants`, `tenant_products`, `tenant_memberships`,
`client_requests`. Smart Enums gravados pelo `Id` — renumerar um valor reescreve o significado do
que já está no banco, e é por isso que existe `EnumerationPersistenceTests`.
