# ADR-003 — O claim se chama `tenants`, não `tenant_ids`

**Data:** 2026-08-13 · **Status:** aceito

## Contexto

O checklist pré-produção do BillPayment previa validar o `{tenantId}` da rota contra um claim
chamado `tenant_ids`. O PeopleManagement já faz o mesmo com um claim chamado `companies`.

## Decisão

O atributo do usuário e o claim se chamam **`tenants`**. O client scope que o expõe é
`tenant-scope`. A linha do checklist do BillPayment foi corrigida.

## Por quê

O `RouteAccessRequirementHandler` — copiado do PeopleManagement e reusado aqui — casa os claims
por **substring**:

```csharp
context.User.FindAll(x => x.Type.Contains(requirement.ClaimType, StringComparison.OrdinalIgnoreCase))
```

Com `RouteClaimTypeRequirement = "tenants"`, `"tenant_ids".Contains("tenants")` é **falso**. O
guard reprovaria todo mundo, e a falha apareceria como 403 sem explicação. Alinhar o nome custa
zero e mantém a simetria exata com `companies`.

## Consequências

- `companies` fica **intocado**. O PeopleManagement continua lendo o que sempre leu; migrá-lo
  para `tenants` é fase futura, explicitamente fora deste escopo.
- Quem for escrever o `TenantAuthorizationFilter` do BillPayment deve ler o claim `tenants`.
- O nome do parâmetro de rota importa tanto quanto o do claim: o guard casa por
  `RouteNameRequirement` (`tenantId`). As rotas de back-office chamam o parâmetro de `id` **de
  propósito** — um operador da plataforma não tem tenant no claim e seria trancado para fora.
