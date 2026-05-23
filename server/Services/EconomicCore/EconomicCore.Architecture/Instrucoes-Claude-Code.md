# Instruções — gerar o código do domínio REA via Claude Code

> Guia prático para implementar a **Fase 1** (Walking Skeleton do aluguel pós-pago) do modelo REA usando o Claude Code com as skills `domain-codegen-ddd-dotnet` (gera o domínio) e `tests-domain-ddd-dotnet` (gera os testes).
>
> **Insumos que você leva para o Claude Code:** este arquivo + `Modelo-REA-Tatico.md` (a fonte da verdade da modelagem) + `Modelo-REA-Conceitual.md` (referência ontológica, opcional). A skill de codegen tem um cenário "Markdown → código" feito exatamente para consumir o documento tático.

---

## 0. Pré-requisitos do projeto

Antes de pedir geração, tenha a solução .NET criada (a skill **não** cria projetos nem solução):

```bash
dotnet new sln -n EconomicCore
dotnet new classlib -n EconomicCore.Domain -f net9.0      # net8.0 se preferir (ver nota de Guid)
dotnet new classlib -n SharedKernel       -f net9.0
dotnet sln add EconomicCore.Domain SharedKernel
dotnet add EconomicCore.Domain reference SharedKernel
```

- **`<LangVersion>`**: garanta C# 11+ (idealmente 12) — o `IEntityId<TSelf>` usa `static abstract` em interface.
- **`<Nullable>enable</Nullable>`** e **`<ImplicitUsings>enable</ImplicitUsings>`** no `.csproj`.
- **.NET 9** se quiser `Guid.CreateVersion7()` nos IDs (recomendado). Em **.NET 8**, avise a skill para usar `Guid.NewGuid()` ou o package `UUIDNext`.
- Para os testes: `dotnet new xunit -n EconomicCore.Domain.Tests` e referencie `EconomicCore.Domain`.

---

## 1. Princípios para conversar com as skills

As skills são **codificadoras com fundamento**, não consultoras de modelagem — toda a modelagem já está no `Modelo-REA-Tatico.md`. Então:

1. **Dê o documento tático como fonte.** Abra o `Modelo-REA-Tatico.md` no contexto do Claude Code e diga "siga este modelo". A skill de codegen tem o cenário A (Markdown → código) para isso.
2. **Gere um aggregate por vez.** Não peça "gera tudo". Peça aggregate a aggregate, na ordem da §4 abaixo. Aggregates pequenos, revisão fácil.
3. **Confirme o BC e as siglas no primeiro prompt.** A skill exige a sigla do BC. Já está decidido: BC = `EconomicCore`, sigla = `ECC`, namespace = `EconomicCore.Domain`. Diga isso explicitamente para ela não perguntar.
4. **Não deixe a skill inventar invariante.** Se ela propuser regra que não está no modelo, recuse e aponte o documento. O modelo tático lista as invariantes com código de erro (`ECC.EVT01` etc.) — são essas, nem mais nem menos.
5. **Peça os testes logo após cada aggregate**, com a skill de testes, enquanto o código está fresco no contexto.

---

## 2. Decisões já tomadas (cole no primeiro prompt)

Para a skill não reabrir o que já foi decidido, inicie a sessão colando este bloco:

```
Contexto do projeto (decidido, não reabrir):
- Bounded context único: EconomicCore. Namespace EconomicCore.Domain. Sigla de erro: ECC.
- Persistência tradicional (EF Core + Outbox), NÃO event-sourced. Gere aggregates tradicionais.
- Multi-tenant por TenantId em linha (TenantId faz parte da identidade lógica).
- .NET 9, C# 12. IDs com Guid.CreateVersion7().
- Stack de testes: xUnit + Assert nativo + Object Mother manual. Sem mocks no domínio.
- Fonte da verdade da modelagem: Modelo-REA-Tatico.md (em anexo). Siga-o; não invente invariantes.
- SeedWork já especificado na §13 do documento — gere conforme, sigla SWK reservada.
```

> **Importante sobre event sourcing:** a skill de codegen **se recusa** a gerar aggregates event-sourced com os templates tradicionais e vai perguntar se algum aggregate é A+ES. Responda: "nenhum é event-sourced no MVP; todos tradicionais". O `EconomicEvent` é quase-imutável e poderia ser A+ES no futuro, mas **não agora** (decisão §10 do documento tático).

---

## 3. Ordem de geração (a skill segue esta ordem; reforce se preciso)

Conforme a própria skill (`Ordem de geração`):

1. **SeedWork** — `IEntityId`, `Entity<TId>`, `AggregateRoot<TId>`, `ValueObject`, `Enumeration`, `IDomainEvent`, `DomainException`, `SeedWorkErrors`. Já está na §13 do documento tático; peça para gerar **idêntico**.
2. **Strongly-typed IDs** — um `record struct : IEntityId<TSelf>` por entidade.
3. **Smart Enums** — herdando de `Enumeration`.
4. **Value Objects** — herdando de `ValueObject`.
5. **Domain Events** — `sealed record : IDomainEvent`, IDs strongly-typed no payload.
6. **Entities internas** — herdando de `Entity<TId>` (só `Commitment`, dentro de `EconomicContract`).
7. **Factory de erros** por aggregate (`<Aggregate>Errors.cs`, ID `ECC.<AGG>##`).
8. **Aggregate Root**.
9. **Domain Services** — `DualityMatchingService` por último (depende dos aggregates).

---

## 4. Roteiro de prompts (Fase 1, um aggregate por vez)

### Prompt 1 — SeedWork

```
Gere a pasta SeedWork de EconomicCore.Domain exatamente como especificado na §13
do Modelo-REA-Tatico.md (IEntityId, Entity, AggregateRoot, ValueObject, Enumeration,
IDomainEvent, DomainException, SeedWorkErrors + EconomicCoreErrors). Use os templates
canônicos completos da skill (incluindo o ToString por reflexão do ValueObject e os
métodos auxiliares do Enumeration que omiti por brevidade no documento).
```

### Prompt 2 — SharedKernel (Value Objects compartilhados)

```
Gere os Value Objects do projeto SharedKernel: Money, CompetencePeriod, DateRange, TaxId,
conforme a §7.2 do Modelo-REA-Tatico.md. Cada um sealed class : ValueObject, com validação
no construtor via factory de erros própria (sigla SHK). Money em BRL, arredondamento 2 casas.
```

### Prompt 3 — Aggregate `EconomicAgent` (o mais simples; bom para validar o pipeline)

```
Gere o aggregate EconomicAgent (região Operational) conforme §5.3 do Modelo-REA-Tatico.md:
ID EconomicAgentId, smart enum AgentScope (Inside|Outside), VO TaxId, invariantes
ECC.AGT01..03, evento EconomicAgentRegistered, factory EconomicAgentErrors.
```

Depois, **os testes**:

```
Gere os testes unitários de EconomicAgent com a skill de testes: Object Mother,
factory Create (válida e inválida por cada invariante AGT01..03), igualdade,
emissão de EconomicAgentRegistered com payload correto. xUnit + Assert nativo.
```

### Prompt 4 — Aggregate `EconomicResource`

```
Gere o aggregate EconomicResource conforme §5.2: ID, smart enum ResourceKind
(Cash|Service|LaborService|FiscalObligation), invariantes ECC.RES01..03, evento
EconomicResourceRegistered. Saldo NÃO é campo do aggregate (é read model) — não gere saldo.
```
+ testes (mesmo padrão do Prompt 3).

### Prompt 5 — Aggregate `EconomicEvent` (o coração — gere com atenção)

```
Gere o aggregate EconomicEvent conforme §5.1, incluindo:
- VOs internos: Participation (AgentId + ParticipationRole Provider|Recipient), DualityLink
  (CounterpartEventId + MatchedAmount), CommitmentRef, CompetencePeriod, EventTimestamp.
- smart enum FlowDirection (Inflow|Outflow).
- factories Create: RegisterCovered (exige CommitmentRef) e RegisterPaired (exige par).
- método CloseDuality (fechamento total e parcial).
- invariantes ECC.EVT01..07, com destaque para EVT04 (anti-orfandade: Duality XOR
  CoveringCommitment, nunca ambos ausentes).
- eventos EconomicEventRegistered, DualityClosed.
Diagrama de estados de referência na §5.1.
```

+ testes — peça **explicitamente** a cobertura da anti-orfandade:

```
Gere os testes de EconomicEvent. Cubra obrigatoriamente:
- RegisterCovered cria evento coberto por commitment (sem duality).
- RegisterPaired cria evento já pareado.
- Tentar registrar sem duality E sem commitment lança ECC.EVT04 (OrphanEvent) — caso crítico.
- CloseDuality total e parcial (pré-pago 1/12); MatchedAmount > saldo lança ECC.EVT06.
- Axiom 3: evento sem Provider+Recipient lança ECC.EVT01.
- Payload de EconomicEventRegistered e DualityClosed.
```

### Prompt 6 — Aggregate `EconomicContract` (com Entity interna `Commitment`)

```
Gere o aggregate EconomicContract conforme §6.1, com a Entity interna Commitment
(Entity<CommitmentId>, criada/mutada só pela Root). Inclua:
- VOs RecurrencePattern, CommitmentTerms, ReciprocalLink.
- smart enums ContractDirection, ContractStatus, CommitmentDirection, CommitmentStatus, Periodicity.
- métodos Create, GenerateCommitmentsFor (gera o par recíproco em Promised),
  MarkFulfilled, Expire, Suspend, Terminate.
- invariantes ECC.CTR01..05 (CTR01 = todo commitment outflow tem ReciprocalLink).
- eventos EconomicContractCreated, CommitmentsGenerated, CommitmentFulfilled,
  CommitmentExpired, CommitmentCancelled.
Lembre: eventos só na Root; Commitment comunica via retorno de método.
```
+ testes (cubra a matriz de transições de `CommitmentStatus` e a geração idempotente CTR02).

### Prompt 7 — Domain Service `DualityMatchingService`

```
Gere o Domain Service DualityMatchingService (pasta Services/) conforme §5.4:
stateless, sem interface, sem async, sem infra. Recebe o paymentEvent, o
coveringCommitmentId e o consumptionEvent; valida que o consumo estava coberto pelo
mesmo commitment; chama CloseDuality nos dois eventos. NÃO emite eventos (os aggregates
emitem). Factory de erros DualityMatchingErrors com sigla ECC.DMS.
```
+ testes (com aggregates reais via Object Mother, sem mocks — a skill faz assim).

### Prompt 8 — `StandaloneCommitment` (avulso) — pode ficar para a Fase 2

Se quiser já deixar pronto o caminho avulso:

```
Gere o aggregate StandaloneCommitment conforme §6.2: mesma forma do Commitment interno,
mas aggregate próprio (sem contrato-pai). Sigla de erro ECC.STD.
```

---

## 5. Checklist de revisão após cada aggregate gerado

A skill tem um checklist próprio (no fim de `templates.md`); confira ao menos:

- [ ] `sealed` no aggregate, VOs, smart enums, entities internas.
- [ ] IDs strongly-typed, **sem** `implicit operator Guid` (extração via `.Value`).
- [ ] Outros aggregates referenciados por ID tipado, nunca `Guid` solto.
- [ ] Construtor privado + factory `Create`; props com `private set`.
- [ ] Coleções públicas como `IReadOnlyCollection<T>` + `.AsReadOnly()`.
- [ ] Eventos só na Root; `sealed record : IDomainEvent`; IDs tipados no payload.
- [ ] Métodos de mutação recebem `DateTime occurredAt` (sem `UtcNow` inline).
- [ ] Erros via factory `<Aggregate>Errors`, IDs `ECC.<AGG>##` únicos; nada de `throw new DomainException` direto.
- [ ] Zero referência a `Microsoft.EntityFrameworkCore`, `System.Text.Json`, `Newtonsoft.Json` no domínio.
- [ ] Código em inglês; mensagens de `DomainException` em português.

E para os testes:

- [ ] xUnit + `Assert` nativo; Object Mother manual; sem mocks.
- [ ] Cada invariante tem teste de violação; cada transição de status (válida e inválida) coberta.
- [ ] Cada evento tem teste de emissão **e** de payload.
- [ ] A anti-orfandade (`ECC.EVT04`) tem teste dedicado.

---

## 6. Ordem macro e o que vem depois do domínio

A skill de codegen **não** gera Repository, Application Service, EF config, controllers ou migrations sem pedido explícito — e isso é proposital (mantém o domínio puro). Sequência recomendada depois que o domínio + testes da Fase 1 passarem:

1. **Domínio + testes** (este guia) — Fase 1 fechada quando os testes passam.
2. **Application** (camada de casos de uso) — use a skill `application-codegen-ddd-dotnet`: commands/handlers `RegisterEconomicContract`, `GenerateCommitments`, `RegisterConsumptionEvent`, `RegisterPaymentEvent`, e as queries de read model (claims, DRE, caixa, previsão).
3. **Infra** — use `infra-codegen-ddd-dotnet`: EF Core + PostgreSQL, repositórios, Outbox, mapeamento dos VOs/smart enums, multi-tenancy por `TenantId`.
4. **API** — use `api-codegen-ddd-dotnet`: controllers, autenticação (Keycloak, que você já usa), filtros de exceção que traduzem `DomainException` em resposta HTTP.
5. **Read models** — projeções que consomem `EconomicEventRegistered` / `DualityClosed` / `CommitmentsGenerated`.

> Cada uma dessas skills é tática e espera o domínio já pronto. O `Modelo-REA-Tatico.md` (§9) lista os commands, queries e read models da Fase 1 que servem de roteiro para os passos 2–5.

---

## 7. Critério de "Fase 1 pronta" (do documento tático §9)

A Fase 1 está concluída quando, ponta a ponta:

1. Consumo do aluguel de outubro é registrado, coberto pelo commitment do contrato — **sem órfão** (teste de `ECC.EVT04` passa).
2. Pagamento em novembro fecha a dualidade automaticamente (`DualityClosed` emitido).
3. DRE de **outubro** mostra a despesa; fluxo de caixa de **novembro** mostra a saída — as duas **não divergem**.
4. Multi-tenant: tenant A não vê dado de B (teste explícito).
5. A previsão lista o commitment de novembro **antes** de ele virar fato.

Atingidos os cinco, o Walking Skeleton anda: substitui a planilha para contas recorrentes pós-pagas, com DRE por competência correta.

---

*Instruções de geração via Claude Code. Pareiam com `Modelo-REA-Tatico.md` (modelagem) e as skills `domain-codegen-ddd-dotnet` e `tests-domain-ddd-dotnet`. Gere um aggregate por vez, na ordem da §4, com testes logo após cada um.*
