# TenantManagement — índice de arquitetura

Design rationale do Bounded Context. **Todo documento novo de arquitetura deve ser registrado aqui.**

| Documento | O que responde |
|---|---|
| [`01-context-and-vision.md`](01-context-and-vision.md) | O que é um tenant, escopo do BC, o que fica de fora, linguagem ubíqua |
| [`02-domain-model.md`](02-domain-model.md) | Aggregate `Tenant`, VOs, entidades filhas, invariantes, eventos, portas, prefixos de erro |
| [`03-access-provisioning.md`](03-access-provisioning.md) | Como o acesso chega ao provedor de identidade, o claim `tenants`, e o que fazer quando falha |
| [`adr/`](adr/) | ADR-001 a ADR-004 — o **porquê** de cada decisão estrutural |

**Antes de propor mudança estrutural, leia o ADR correspondente.** Decisões fechadas e greppáveis:

- **BC próprio, e não uma pasta dentro do PeopleManagement** ([ADR-001](adr/ADR-001-bc-proprio.md)).
- **O Tenant é registro-mestre; `Company` e `PayerProfile` continuam sendo cadastro local de cada produto** ([ADR-002](adr/ADR-002-registro-mestre.md)).
- **O claim se chama `tenants`, não `tenant_ids`** ([ADR-003](adr/ADR-003-claim-tenants.md)).
- **A camada de autorização é copiada do PeopleManagement, não compartilhada** ([ADR-004](adr/ADR-004-autorizacao-copiada.md)).

## Ferramentas

| Ferramenta | Para quê |
|---|---|
| [`tools/backfill-tenants.js`](tools/backfill-tenants.js) | Migra os cadastros que já existem nos produtos **preservando o Guid** — é o que evita reemitir todo o acesso |
