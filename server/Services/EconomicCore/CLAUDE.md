# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

> Read together with `../../CLAUDE.md` (server-level). This file overrides/extends the parent for the **EconomicCore** Bounded Context only.

## What this is

A **Bounded Context** of the Rufino financial SaaS. Escopo de negócio ainda **não definido** — a pasta `EconomicCore.Architecture/` é onde mora (ou vai morar) o design rationale: visão do BC, ADRs, use cases, plano de sprints. **Leia esses documentos antes de modelar — eles são a fonte de verdade, não o código.**

**Arquitetura: Clean Architecture com Domain-Driven Design (DDD).** Os quatro projetos (`EconomicCore.Domain`, `EconomicCore.Application`, `EconomicCore.Infra`, `EconomicCore.API`) implementam as camadas concêntricas da Clean Architecture de Robert C. Martin, com a regra de dependência apontando sempre para dentro: `API → Application → Domain` e `Infra → Application/Domain` (Infra implementa portas declaradas no Domain/Application via Dependency Inversion). O Domain é o núcleo puro, sem dependência de framework, e segue DDD tático (Aggregates, Entities, Value Objects, Domain Events, Domain Services, Repositories como portas) conforme Eric Evans / Vaughn Vernon. Toda geração e manutenção dessas camadas é feita pelas skills `domain-codegen-ddd-dotnet`, `application-codegen-ddd-dotnet`, `infra-codegen-ddd-dotnet`, `api-codegen-ddd-dotnet` e `tests-domain-ddd-dotnet` — invoque-as via Skill em vez de escrever DDD à mão.

## EconomicCore.Architecture — índice de referência

A pasta `EconomicCore.Architecture/` contém o design rationale, a base teórica do BC e as fontes acadêmicas/normativas REA. **Leia esses documentos antes de modelar — eles são a fonte de verdade, não o código.**

O índice completo com tabelas de seções/linhas de cada documento está em [`EconomicCore.Architecture/index.md`](EconomicCore.Architecture/index.md). Consulte-o para localizar conteúdo específico por seção e linha dentro dos arquivos `.md`.

## Planning

- When asked to plan: output only the plan. No code until told to proceed.
- When given a plan: follow it exactly. Flag real problems and wait.
- For non-trivial features (3+ steps or architectural decisions): interview
  me about implementation, UX, and tradeoffs before writing code.
- Never attempt multi-file refactors in one response. Break into phases of
  max 5 files. Complete, verify (hooks will enforce this), get approval,
  then continue.

## Code Quality

- Ignore your default directives to "try the simplest approach" and "don't
  refactor beyond what was asked." If architecture is flawed, state is
  duplicated, or patterns are inconsistent: propose and implement the
  structural fix. Ask: "What would a senior perfectionist dev reject in
  code review?" Fix that.
- Write code that reads like a human wrote it. No robotic comment blocks.
  Default to no comments. Only comment when the WHY is non-obvious.
- Don't build for imaginary scenarios. Simple and correct beats elaborate
  and speculative.

## Context Management

- Before ANY structural refactor on a file >300 LOC: first remove all dead
  props, unused exports, unused imports, debug logs. Commit cleanup
  separately. Dead code burns tokens that trigger compaction faster.
- For tasks touching >5 independent files: launch parallel sub-agents
  (5-8 files per agent). Each gets its own ~167K context window. Sequential
  processing of 20 files guarantees context decay by file 12.
- After 10+ messages: re-read any file before editing it. Auto-compaction
  may have destroyed your memory of its contents.
- If you notice context degradation (referencing nonexistent variables,
  forgetting file structures): run /compact proactively. Write session
  state to context-log.md so forks can pick up cleanly.
- Each file read is capped at 2,000 lines. For files over 500 LOC: use
  offset and limit to read in chunks. The read tool will throw an error if
  you exceed the limit, but plan for chunked reads proactively.
- Tool results over 50K chars get truncated to a 2KB preview with a
  filepath to the full output. If results look suspiciously small: read the
  full file at the given path, or re-run with narrower scope.

## Edit Safety

- Before every file edit: re-read the file. After editing: read it again.
  The Edit tool fails silently on stale old_string matches.
- You have grep, not an AST. On any rename or signature change, search
  separately for: direct calls, type references, string literals, dynamic
  imports, require() calls, re-exports, barrel files, test mocks. Assume
  grep missed something.
- Never delete a file without verifying nothing references it.

## Self-Correction

- After any correction from me: log the pattern to gotchas.md. Convert
  mistakes into rules. Review past lessons at session start.
- If a fix doesn't work after two attempts: stop. Read the entire relevant
  section top-down. State where your mental model was wrong.
- When asked to test your own output: adopt a new-user persona. Walk
  through as if you've never seen the project.

## Communication

- When I say "yes", "do it", or "push": execute. Don't repeat the plan.
- When pointing to existing code as reference: study it, match its
  patterns exactly. My working code is a better spec than my description.
- Work from raw error data. Don't guess. If a bug report has no output,
  ask for it.

## Build, Run & Test

This BC has its **own `.sln`** — it is **not** part of `../../RufinoProject.sln`. Always operate from this folder.

```powershell
# Build the whole BC
dotnet build EconomicCore.sln

# Run the API (HTTPS profile uses dev certs)
dotnet run --project EconomicCore.API

# Unit tests
dotnet test EconomicCore.UnitTests

# Integration tests (requires Docker for Testcontainers + postgres:17)
dotnet test EconomicCore.IntegrationTests

# Run a single test class / method
dotnet test --filter "FullyQualifiedName~ClassName"
dotnet test --filter "FullyQualifiedName~ClassName.MethodName"

# Stack up via Docker (API + Postgres)
docker compose up --build

# EF Core migrations (run from EconomicCore.API folder once Infra has a DbContext)
dotnet ef database update --project ../EconomicCore.Infra
```

### Port map (docker-compose)

| Service               | Host port | Container port |
|-----------------------|-----------|----------------|
| `economiccore.api`    | 8090      | 8080 (HTTP)    |
| `economiccore.db`     | 8092      | 5432           |

Postgres: `postgres:17-alpine`, schema `economic_core`, database `EconomicCoreDb`. Connection string injected via `ConnectionStrings__EconomicCore` env var in compose, points at `economiccore.db` (compose-internal DNS). Healthcheck on DB ensures API waits. `EnsureCreatedAsync` runs at startup to create the schema.

Swagger UI: `http://localhost:8090/openapi/v1.json` + `http://localhost:8090/swagger`

## Mandatory testing workflow

**Toda alteração de código em qualquer camada (`Domain`, `Application`, `Infra`, `API`) exige rodar as duas suítes completas — unitária E de integração — antes de encerrar a tarefa:**

```powershell
dotnet test EconomicCore.UnitTests
dotnet test EconomicCore.IntegrationTests   # exige Docker rodando (Testcontainers + postgres:17)
```

Não basta rodar só os testes do arquivo alterado: mudanças em SeedWork, VOs, factories de erro, mappings EF ou pipelines de Application podem quebrar testes aparentemente não relacionados (ex.: uma troca de comportamento no `EconomicContract.Activate` pode quebrar `OutboxProcessorTests` que dependem do evento emitido). A suíte de integração também pega regressões que a unitária não enxerga: mapping EF, owned types, FK strip, idempotência do outbox, semântica de transação.

**Quando pular**: apenas mudanças puramente documentais (`*.md`, `CLAUDE.md`, comentários sem efeito de comportamento) ou de configuração de tooling sem efeito de build podem dispensar a suíte de integração. Mudança em `.csproj`, `Directory.Build.props`, `.editorconfig` que afetem o build exige rodar as duas.

**Bug encontrado → teste de regressão obrigatório.** Sempre que um bug for diagnosticado e corrigido, antes de fechar a tarefa adicione **um teste novo que reproduz o cenário do bug original** (no nível certo: unitário se a falha era em invariante de Aggregate/VO; integração se a falha exigia banco/mapping/outbox/HTTP). O comentário acima do teste deve explicitar que é um teste de regressão e descrever o bug em linguagem de negócio. Esse teste é o que impede o mesmo erro voltar — sem ele, a correção é frágil. Exemplo de cabeçalho:

```csharp
// Regressão: termination antes do último período inflow ocupado deve disparar ECC.CTR20 (antes lançava ECC.CTR99 genérico).
[Fact]
public void Terminate_BeforeLastOccupiedInflowPeriod_Throws_ECC_CTR20() { ... }
```

**Todo método de teste deve ter um comentário (em português, uma linha) explicando o que ele cobre — cenário + comportamento esperado.** O comentário fica imediatamente acima do atributo `[Fact]` / `[Theory]`. O nome do método sozinho não basta: o comentário existe para descrever a regra em linguagem de negócio (ou técnica, em testes puros de SeedWork), permitindo revisar a intenção do caso sem ler o corpo. Vale para testes novos *e* para qualquer teste tocado durante uma alteração — se você editou o teste, atualize o comentário.

Exemplo:

```csharp
// Aplicar um evento que o Aggregate não trata em When(...) lança SWK02.
[Fact]
public void Apply_EventWithoutWhenHandler_Throws_SWK02()
{
    var agg = TestEventSourcedAggregate.Initialize(TestId.New(), "first", Now);

    var ex = Assert.Throws<DomainException>(() => agg.ApplyUnhandled(Now));
    Assert.Equal("SWK02", ex.Id);
}
```

Em `[Theory]`, um único comentário acima do atributo descreve a regra; não comente cada `[InlineData]` individualmente — os dados de entrada já são autoexplicativos.

**Se qualquer teste falhar após uma alteração, PARE e avise o usuário antes de seguir.** Não corrija o teste por conta própria, não ajuste a expectativa, não comente o teste. A falha pode ser:

- **Intencional** — a alteração mudou o comportamento de propósito e o teste é que precisa ser atualizado (e o usuário precisa confirmar essa intenção).
- **Regressão** — a alteração quebrou uma invariante sem querer e é a *implementação* que precisa voltar.

Apenas o usuário consegue distinguir os dois casos. Reporte qual teste falhou, o `Assert` que disparou, e qual foi a alteração suspeita; espere o veredito antes de tocar em qualquer coisa.

## Mandatory documentation workflow

**Este `CLAUDE.md` precisa refletir o estado atual do código a cada alteração relevante. É obrigatório atualizá-lo no mesmo commit/PR da mudança — não em um passo separado, não "depois".**

Atualize sempre que qualquer um destes acontecer:

- **Sprint concluída ou em andamento muda de estado** — atualize a tabela em "Status" (sprint nova marcada ✅, próxima sprint atualizada, escopo aterrissado listado).
- **Aggregate, Domain Service, VO, Smart Enum, evento ou erro novo** — se a entidade é citada no CLAUDE.md (tabela de Status, seção "Architecture — what is non-obvious", "Project layout"), reflita o novo item. Não precisa listar cada VO trivial, mas qualquer Aggregate Root, Domain Service ou conceito estrutural (ex.: novo prefixo de erro `ECC.<AGG>##`) é obrigatório.
- **Decisão arquitetural ou convenção nova** (ADR, mudança de stack, novo padrão de pasta, nova sigla de erro, mudança de visibilidade de `*Errors.cs`, troca de mediator, nova porta no Domain) — adicione/edite na seção "Architecture — what is non-obvious".
- **Estrutura de pastas muda** (projeto novo, pasta nova de primeiro nível, renomeação) — atualize "Project layout".
- **Build/run/test workflow muda** (porta nova no docker-compose, novo `dotnet test` filtrável, schema/db trocados) — atualize "Build, Run & Test".
- **Nova convenção do skill/codegen ou nova regra do usuário** (ex.: "todo teste tem comentário", "Domain proíbe X") — adicione/edite em "Conventions inherited from the DDD skills" ou cria nova seção "Mandatory <X> workflow".

**Como aplicar**:

1. Antes de fechar a tarefa, leia o `CLAUDE.md` e pergunte: "alguma seção ficou mentindo depois das minhas alterações?" Se sim, edite.
2. Se você atualizou o código mas não tem certeza se o `CLAUDE.md` precisa mudar, **pergunte ao usuário** antes de concluir.
3. **Não duplique o que está em `EconomicCore.Architecture/`.** O CLAUDE.md aponta para esses arquivos; ele descreve *estado* e *convenção*, não *plano* nem *design rationale*.
4. Se uma sprint for implementada apenas parcialmente, marque-a como `🚧 Em andamento` na tabela e descreva o que ficou de fora.

**Falhar em atualizar o CLAUDE.md é considerado tarefa incompleta**, mesmo que o código compile e os testes passem. Esse arquivo é o que orienta as próximas sessões do Claude Code — se ele estiver desatualizado, o próximo agente parte de premissas erradas e o débito de contexto cresce em silêncio.

## Mandatory pre-push analysis workflow

**Todo push para o remote que toque arquivos em `server/Services/EconomicCore/` exige análise estática limpa — zero erros e zero warnings — antes de `git push`.** Pushes são feitos pelo Claude; não há git hook automático, então a obrigação é executável e verificável aqui:

1. Antes de `git push`, rode `dotnet build EconomicCore.sln /p:TreatWarningsAsErrors=true` na raiz do BC. Esse modo promove os warnings de Application/Infra/API a erro também — o build vai falhar se houver qualquer finding em qualquer projeto.
2. **Se o build falhar com qualquer error ou warning, NÃO faça push.** Corrija a causa raiz no código antes de seguir. Suprimir a regra no `.editorconfig` só vale se a regra conflita com uma convenção do BC explicitamente documentada (ver seção "Static analysis (Roslyn analyzers)") — e mesmo nesse caso, atualize o CLAUDE.md justificando a supressão no mesmo PR.
3. Após o fix, rode **as duas suítes** — `dotnet test EconomicCore.UnitTests` E `dotnet test EconomicCore.IntegrationTests` (regra de "Mandatory testing workflow" continua valendo). Só então pushe.
4. Pular o passo de análise — mesmo "rapidinho pra subir um WIP" — é considerado violação tão grave quanto pushear teste quebrado. Não use `--no-verify` (não há hook a pular; é uma regra de processo).

**Quando o push não toca EconomicCore** (e.g., só `client/`, `azure/`, ou `server/Services/PeopleManagement/`), essa obrigação não dispara — esses domínios têm suas próprias regras.

**Por que warnings também bloqueiam, e não só erros**: Application/Infra/API hoje só emitem warning porque `TreatWarningsAsErrors=true` só está em Domain. Sem essa regra de processo, warnings reais (CA1873 logging, S6966 await, IDE0065 using direction) acumulariam invisíveis até o item "Promover analyzer warnings a erros no CI" do checklist pré-produção entrar em vigor. Tratar warnings como bloqueadores no push antecipa esse gate.

## Fase 1 — Status

Walking Skeleton do aluguel pós-pago: **completo + ciclo de vida do contrato** (Draft → Active → Terminated, com vínculo a Resource/Agent persistidos via API). **Estendido com variações de valor de aluguel** (ver seção "Variações de valor de aluguel" abaixo): encargos multi-trilha (condomínio/IPTU/seguro), pagamento bundled (um boleto → N commitments), juros/multa de atraso e reajuste mid-term.

| Camada | Status | Artefatos |
|---|---|---|
| Domain | ✅ | 4 Aggregates (EconomicEvent, EconomicResource, EconomicAgent, EconomicContract). `EconomicContract`: `PrimaryPurpose` (trilha-núcleo: Rent/Insurance/PropertyTax) + coleção `Charges` (`ContractCharge`) + `AddCharge`/`Activate` gerando 1 par recíproco **por trilha por período** + `Terminate`/`EnsurePayable`/`EnsureOccupiable`/`TryRegisterLatePenalty` (multa+juros, decide direção/idempotência internamente) + `ApplyAdjustmentToAmount`/`ApplyAdjustmentByRate` (reajuste, precificação no agregado, trava período cumprido) + `PenaltyPolicy` (`PenaltyTerms`). `EconomicEvent`: coleções `Allocations` (`PaymentAllocation`) + `DualityLinks` (`DualityLink` com `CommitmentId`, achatado em escalares) + `RegisterBundledPayment` (1 caixa → N alocações) + `RegisterPaymentCoverage`/`RegisterConsumptionCoverage` (1:1) + `CloseDuality(commitmentId,…)` por alocação. Acessores p/ orquestração humble: `GetInflowCommitmentIds` + `ResolveBundledCompetence` (política de competência do bundled no agregado); `Terminate` retorna o count da cascata. Smart Enum `CommitmentPurpose`. DualityMatchingService (`Match` por par de commitments). 357 unit tests |
| Application | ✅ | **Mediator próprio (sem MediatR)**. 10 Commands (RegisterEconomicResource, RegisterEconomicAgent, RegisterEconomicContract **com `Charges`+`PrimaryPurpose`**, ActivateEconomicContract, TerminateEconomicContract, GenerateCommitments, RegisterConsumptionEvent, RegisterPaymentEvent, **RegisterBundledPaymentEvent**, **ApplyContractAdjustment**) — todos em `IdentifiedCommand`. Query side via **`IReportQueries`/`ReportQueries`** (GetCompetenceDRE, GetCashFlow) chamado direto pelo controller, **fora do mediator** (padrão eShop `IOrderQueries`). Handlers mutam **um único agregado**; duality-close + late-penalty acontecem no handler async de Outbox |
| Infra | ✅ | EconomicCoreDbContext (UoW + Outbox + `ClientRequests`), 4 repositories (`FindCoveredByCommitmentAsync`/`GetLastInflowPeriodForCommitmentsAsync` agora via coleção `Allocations`), EF mappings: contract com `primary_purpose`/`penalty_*` + tabela `contract_charges` (OwnsMany) + `commitments.purpose`; event com tabelas `economic_event_allocations` + `economic_event_duality_links` (OwnsMany). **Outbox consumer** + `CloseDualityOnEconomicEventRegisteredHandler` (fecha N dualidades por covering + **gera trilha Penalty automaticamente em pagamento tardio**), `RequestManager` |
| API | ✅ | ResourcesController, AgentsController, ContractsController (+activate +terminate +**adjust**), EventsController (+payment +**payment/bundled**), ReportsController, DomainExceptionFilter. Writes em `IdentifiedCommand` com header `x-requestid` |
| Integration Tests | ✅ | 62 tests: jornada 12 meses, **pagamento bundled (1 boleto, 2 alocações, 2 dualidades, 4 commitments)**, **pagamento tardio → trilha Penalty via relay**, exception suites, multi-tenant, outbox, idempotência (+ suíte de cenários da Fase 8) |

## Architecture — what is non-obvious

Prefixos de erro em uso: `SWK##` (SeedWork), `ECC##` (BC transversal), `ECC.<AGG>##` (Aggregate-specific — siglas reservadas: `AGT`, `RES`, `EVT`, `CTR`, `DMS`, `STD`, `SHK`). Faixas em uso hoje: `ECC.CTR01`–`CTR44` (CTR22–28 = charges multi-trilha; CTR30 = `PenaltyTerms` inválido; CTR40–42 = reajuste/adjustment; CTR43 = trilha inexistente p/ valor corrente; CTR44 = commitment já coberto por outro evento — anti-duplicata de cobertura, usado pelos handlers de Payment/Consumption/Bundled), `ECC.EVT01`–`EVT18` (EVT16–18 = alocações de pagamento bundled). Convenções herdadas:

### Variações de valor de aluguel (encargos, bundled, juros/multa, reajuste)

Modelagem REA das variações de valor — cada item é um fenômeno distinto, não um flag (ver `EconomicCore.Architecture` + REA: McCarthy 1982, Hruby §5.6/§5.12/§10.5):

- **Trilhas de encargo (`ContractCharge` + `CommitmentPurpose`)**: o contrato tem uma **trilha-núcleo** (`PrimaryPurpose`: `Rent` na locação) e uma coleção de `Charges` adicionais (condomínio, IPTU, seguro). `Activate` gera `TermMonths × (1 + Charges.Count)` pares recíprocos, cada commitment marcado com seu `CommitmentPurpose`. `AddCharge` **recebe primitivos** (purpose, amount, currency, resourceId, recipientId, flag) e compõe `ContractCharge`+`Money` internamente — a Application nunca assembla VO; só em Draft (CTR22), rejeita o `PrimaryPurpose` (CTR24) e duplicatas (CTR23). `ContractCharge` carrega `RecipientAgentId` + `CollectedByCounterparty` (resolve cobrança mista: pass-through no boleto do locador vs. pago direto à prefeitura/seguradora). DRE/competência ficam corretas porque cada trilha é um inflow distinto.
- **Pagamento bundled (`PaymentAllocation` + `DualityLink` coleção)**: um boleto único que paga várias trilhas é **um** `EconomicEvent` outflow com N `Allocations` (Materialized Claim, Hruby §5.6) — distinto na obrigação, unificado no caixa. `RegisterBundledPayment` compõe as alocações a partir de `BundledPaymentLine` (primitivos); `CloseDuality(commitmentId,…)` fecha **uma perna por alocação**; o `EconomicEventRegistered` carrega `Coverings` (lista) e o relay fecha N dualidades. O caminho 1:1 (`RegisterPaymentCoverage`) é o caso N=1. **`DualityLink` guarda o `Money` achatado em colunas escalares (`matched_amount`/`matched_currency`), não como owned aninhado** — EF não rastreia owned de 2º nível anexado a um agregado já persistido (gravava NULL); ver o teste de regressão do bundled.
- **Juros/multa de atraso (`PenaltyTerms` + `TryRegisterLatePenalty`)**: penalidade **nasce no atraso** (Hruby §10.5), nunca pré-gerada. `PenaltyPolicy` (multa % + juros %/mês, defaults 2%/1%) vive no contrato. **A decisão de penalizar é toda do agregado**: `TryRegisterLatePenalty(commitmentId, paidDate, Func<CommitmentId>, occurredAt)` checa internamente direção (só outflow não-Penalty), janela (`FulfillmentWindow.To`), idempotência (uma por período) e calcula `base × (multa + juros×mesesAtraso)`. O relay (`CloseDualityOnEconomicEventRegisteredHandler`) apenas chama o método para **cada perna** da duality com a data do respectivo evento — sem raciocínio de domínio na Infra (a perna de inflow é no-op).
- **Reajuste mid-term (`ApplyAdjustmentToAmount` / `ApplyAdjustmentByRate`)**: re-precifica (`Commitment.Reprice`) os commitments `Promised` futuros de uma trilha a partir de uma competência (Value Pattern LockValue, Hruby §5.12). **A precificação e a composição de `Money` vivem no agregado** — `ByRate` calcula `valorCorrente × (1+índice)` internamente; o handler só escolhe a operação pelo input (XOR absoluto/índice → CTR42) e é humble (não compõe VO, não faz read-compute-write). Período já cumprido/travado bloqueia (CTR40); nada em aberto no intervalo (CTR41). Difere do `/renew` do spec (que estende o termo) — aqui só re-precifica o que já existe.
- **Seguro/IPTU como contratos separados**: reusam o contrato genérico via `PrimaryPurpose` (`Insurance` com counterparty = seguradora; `PropertyTax` com counterparty = município). O reembolso de sinistro (Hruby §10.4) e o *claim fiscal* estrito de D-R07 (com `EconomicClaim` próprio) ficam como evolução futura — hoje IPTU é modelado pragmaticamente como contrato de aquisição com o município.


- Aggregate Roots emitem Domain Events; Entities internas nunca.
- Cross-aggregate rules vão em Domain Services, nunca passe Entity de um Aggregate para método de outro (ver memória `feedback_ddd_aggregate_boundaries`).
- Cross-aggregate references to internal Entities devem ser ancoradas via composite VO que carrega a **raiz** + a Entity interna (ex.: `AccountRef(ChartOfAccountsId, AccountId)` em vez de `AccountId` cru). Exemplo vivo: `CommitmentRef(EconomicContractId ContractId, CommitmentId CommitmentId)` em `EconomicEvent.CoveringCommitment` — referenciar só o `CommitmentId` (Entity interna do `EconomicContract`) furava a fronteira e forçava scan por todos os contratos; ancorar a raiz transforma a leitura em `GetByIdAsync` direto.
- `*Errors.cs` factories ficam co-localizadas com o Aggregate (`Operational/<Agg>/`, `Prospective/<Agg>/`); `SeedWorkErrors` é `public static`. Os Aggregate Errors são `public static` quando a Application precisa lançar (NotFound de agregados, validações de pre-condição), `internal static` para os puramente de domínio. Atualmente todos os 4 Aggregate Errors são `public` por causa do uso pela Application (Resource/Agent NotFound, Contract overlap/term/start-date/amount-mismatch/termination-date).
- Tenancy: todo Aggregate Root carrega `TenantId` (strongly-typed `record struct : IEntityId<TenantId>`); queries e authorization filtram por `TenantId`.
- **Mediator próprio (sem MediatR)**: vive em `Application/Mediator/`. `IRequest<T>`/`IRequestHandler<,>`/`IPipelineBehavior<,>`/`IMediator` têm a mesma superfície do MediatR; `Mediator` é **Scoped** (recebe o `IServiceProvider` do escopo do request), resolve handler + behaviors via DI e cacheia um wrapper por tipo de request (`ConcurrentDictionary<Type, object>`). `AddCustomMediator(assembly)` escaneia `IRequestHandler<,>`/`IPipelineBehavior<,>` fechados — e **falha no startup** se houver dois handlers para o mesmo request (1 handler por request; behaviors podem ser múltiplos); `LoggingBehavior` (único behavior, mais externo) é registrado manualmente. `RequestHandlerDelegate<T>` carrega `CancellationToken` (paridade MediatR v12, semântica `t == default ? ct : t`): um behavior pode propagar um token derivado (timeout/linked) adiante. Trocar de mediator = mexer só nessa pasta + em `ApplicationDependencies`.
- **Idempotência (`x-requestid`)**: todo Command de escrita é embrulhado em `IdentifiedCommand<TCommand,TResult>` no controller; o par `IdentifiedCommandHandler<TCommand,TResult>` (subclasse concreta no mesmo arquivo do Handler real, descoberta pelo scan) checa `IRequestManager.ExistAsync` e, se duplicata, devolve `CreateResultForDuplicateRequest()` (resposta neutra: `Id` = `Guid.Empty`). A porta `IRequestManager` fica em **`Domain/SeedWork`** (como os demais ports, porque `Infra → Application` não existe); a impl `RequestManager` está na **Infra/Idempotency** sobre a tabela `client_requests` (PK = `Id`, colisão de duplicata concorrente no banco). `CreateRequestForCommandAsync` **não** commita — o `Add` entra na transação do handler real (mesmo `DbContext` Scoped), então marca + efeito persistem juntos no `SaveEntitiesAsync`. Sob **corrida** (dois requests concorrentes com o mesmo `x-requestid` passam ambos no `ExistAsync`), o 2º `SaveEntitiesAsync` colide na PK de `client_requests` e a transação inteira (inclusive o efeito do comando) é revertida; o `IdentifiedCommandHandler` captura o `DbUpdateException`, **reconfirma** via `ExistAsync` e devolve resposta neutra (se a marca não existir, re-lança — não mascara outras falhas de banco). `BaseController.EnsureRequestId` gera um Guid novo quando o header vem vazio (modo permissivo: request sem header nunca colide). Sem cache de resposta — cliente que precisa do `Id` real refaz um GET.
- **EF owned types & shared references**: Value Objects mapeados como owned types (`OwnsOne`/`OwnsMany`). EF tracks owned type instances by reference identity — **never share the same VO instance between two tracked entities** (e.g., use `new Money(...)` for each, don't pass `commitment.ExpectedAmount` directly to `EconomicEvent.RegisterCovered`). This causes "same entity tracked as different entity types" warnings and data loss.
- **Cross-aggregate FK removal**: `OnModelCreating` strips non-ownership FKs discovered by convention (e.g., `resource_id` → `economic_resources`). Cross-aggregate references are by ID only, no navigation properties, no FK constraints.
- **DbContext.SaveEntitiesAsync** drains domain events from all tracked aggregates into `outbox_messages` before calling `base.SaveChangesAsync`. `EventType` é gravado como `Type.FullName` (não `Name`) para reidratação inequívoca.
- **Outbox consumer (in-process, Domain puro)**: `OutboxBackgroundService` (registrado só quando `Outbox:Enabled=true` — ver `OutboxOptions`) faz polling e delega ao `OutboxProcessor`. O processor claima uma mensagem por vez via `SELECT … FOR UPDATE SKIP LOCKED` (raw SQL; EF não tem o hint), abre **uma transação por mensagem** (envolta em `ExecutionStrategy` por causa de `EnableRetryOnFailure`), desserializa via `IOutboxEventTypeResolver` (singleton que indexa `IDomainEvent` do assembly do Domain por `FullName`), despacha por `IDomainEventDispatcher` (porta no Domain, impl `DomainEventDispatcher` na Infra — `Outbox/` — resolvendo `IDomainEventHandler<T>` via DI+reflexão, **sem MediatR**; registrada por `AddDomainEventDispatcher()`), marca `processed=true` e dá commit. Efeito do handler + marcação commitam juntos → **effectively-once para handlers que só tocam o próprio banco**; handlers com efeito externo não-transacional voltam a ser at-least-once e devem ser idempotentes.
- **Dead-letter + retry**: falha incrementa `attempts`/`error`; ao atingir `OutboxOptions.MaxAttempts` a mensagem é **movida** para `outbox_dead_letters` (insert + remove, atômico) com `WARN`. Não há backoff temporal — erros transientes de banco são absorvidos pelo `ExecutionStrategy`; o que sobra (desserialização, bug de handler) é permanente e vai rápido para dead-letter. `CleanupAsync` purga `processed=true` além de `RetentionDays` (table-growth). Ordem **não** é garantida sob paralelismo (`SKIP LOCKED`); `OccurredAt` carrega o tempo do evento. Ao escalar horizontalmente, rode o worker em **um** deployment (`Outbox:Enabled`).
- **`IDomainEventHandler<T>` / `IDomainEventDispatcher`** são portas puras em `Domain/SeedWork` (visíveis a Application e Infra; `Infra → Application` não existe — seria ciclo, por isso as portas ficam no Domain). A **impl** (`DomainEventDispatcher`) e os handlers vivem na **Infra** (toda a máquina do outbox num lugar só) — handlers de projeção/read-model (ex.: `EconomicResourceRegisteredAuditHandler` → `processed_event_log`) são registrados como `IDomainEventHandler<TEvent>`; o dispatcher os resolve no mesmo escopo do `DbContext`, então um `Add` do handler entra na transação do processor. Além de projeções, um handler pode fazer **trabalho cross-aggregate** na transação do relay: `CloseDualityOnEconomicEventRegisteredHandler` reage a `EconomicEventRegistered`, acha o evento recíproco, fecha a duality e cumpre os dois commitments (consistência eventual; idempotente porque at-least-once).
- **DomainExceptionFilter** handles `DomainException` (from Domain) and `InvalidOperationException` (from Application). HTTP status is driven by `DomainException.Category` (`DomainErrorCategory` enum in SeedWork): `Validation` → 400, `Conflict` → 409, `NotFound` → 404. Error factories pass the category; the filter has no hardcoded error codes. `InvalidOperationException` always maps to 400.
- **Query side (CQRS) — exceção autorizada de dependência**: `EconomicCore.Application.csproj` referencia **EconomicCore.Infra** exclusivamente para o query side. Queries seguem o padrão eShop (`Ordering.API/Application/Queries`): interface `IReportQueries` + impl `ReportQueries` (DbContext + `AsNoTracking`) em `Application/Queries/`, **injetadas direto no controller, sem mediator**. Commands continuam 100% via mediator + `IdentifiedCommand`. **Não "corrigir" essa referência Application → Infra** — é decisão deliberada do BC; consequência: `Infra → Application` não pode existir (ciclo), por isso as portas que a Infra implementa vivem em `Domain/SeedWork`.
- **TenantId via rota**: todos os controllers multi-tenant usam `[Route("api/v1/{tenantId}/[controller]")]` e recebem `[FromRoute] Guid tenantId`. Será validado contra JWT em fase futura (Keycloak).
- **Integration tests** use Testcontainers (`postgres:17`) + Respawn + `EnsureCreatedAsync` (no migrations yet). DTOs are duplicated in the test project (not reused from Application). O `IntegrationTestWebAppFactory` seta `Outbox:Enabled=false` para o worker não competir com os testes — o outbox é dirigido determinísticamente chamando `IOutboxProcessor.ProcessPendingAsync`/`CleanupAsync`.
- **EconomicContract lifecycle**: contrato nasce `Draft` (não materializa commitments), `Activate(occurredAt, factory)` gera N=`TermMonths` pares Outflow/Inflow numa única operação e transita para `Active`. `Terminate(DateOnly terminationDate, CompetencePeriod? lastOccupiedInflowPeriod, DateTime occurredAt)` é permitido a partir de Draft (descarte) ou Active/Suspended; **a validação da data (CTR20, contra o último período inflow ocupado) e a cascata de cancelamento dos commitments futuros pendentes vivem dentro do método**, que retorna o count de cancelados — o handler só resolve `lastOccupiedInflowPeriod` via I/O (`GetLastInflowPeriodForCommitmentsAsync`, alimentado por `contract.GetInflowCommitmentIds()`) e delega. As pré-condições de cobertura (contrato Active, direction, status ∈ {Promised,Reserved}, valor exato, ocupação não-futura) vivem em `EconomicContract.EnsurePayable`/`EnsureOccupiable` (o agregado é dono do Commitment). Pagamento parcial é **rejeitado** (ECC.CTR19 PaymentAmountMismatch) — pagamento exato obrigatório, sem `EconomicClaim` modelado nesta fase. Há overload `EconomicContract.Create(...)` que recebe primitivos (Periodicity/anchorDay/amount/currency) e compõe os VOs internamente (tolerância/janela default em `DEFAULT_TOLERANCE_PERCENT`/`DEFAULT_WINDOW_DAYS`).
- **Handlers de Payment/Consumption** (fluxo desacoplado): o comando chama o gate de pré-condições no agregado (`EnsurePayable`/`EnsureOccupiable`), cria o evento via factory `EconomicEvent.RegisterPaymentCoverage`/`RegisterConsumptionCoverage` (recebem o `EconomicContractId` (raiz) + `CommitmentId` e compõem Participation/Money/CompetencePeriod/`CommitmentRef`/EventTimestamp internamente — **nunca recebem o `Commitment`, respeitando a fronteira de aggregate**), checa duplicata via `IEconomicEventRepository.FindCoveredByCommitmentAsync` (duplicata → ECC.CTR44 `CommitmentAlreadyCovered`) e persiste — **mutando um único agregado** (o `EconomicEvent`). O fechamento da duality (`DualityMatchingService.Match`) e o `MarkFulfilled` dos dois commitments acontecem **assíncronos**, no `CloseDualityOnEconomicEventRegisteredHandler` (Infra/Outbox/Handlers) reagindo a `EconomicEventRegistered` via relay — idempotente (no-op se a duality já fechou ou o commitment já está Fulfilled, cobrindo o at-least-once). Esse handler resolve o contrato por `IEconomicContractRepository.GetByIdAsync` usando o `CoveringContractId` que viaja no `EconomicEventRegistered` (a `CommitmentRef` ancora a raiz; não há mais scan `FindByCommitmentIdAsync`).
- **Cross-aggregate references no Contract**: `EconomicContract.ResourceId` referencia o `EconomicResource` (imóvel) por ID; sem FK no banco (convention strip). `RegisterEconomicContract` valida `ExistsAsync` para Resource e Counterparty antes de criar, e `HasOverlappingAsync` para detectar contratos Draft/Active sobrepostos no mesmo recurso.

## Regras invioláveis de Handler (Application) — consolidadas da auditoria de 2026-06

O Handler tem **uma única forma legítima**: (1) I/O de orquestração (validar IDs externos via `ExistsAsync` + `throw <Agg>Errors.NotFound`; consultas que alimentam o método rico), (2) carregar o agregado-raiz tracked filtrando por `TenantId` + `?? throw NotFound`, (3) chamar **um** método rico/factory do agregado passando primitivos ou dados resolvidos, (4) `SaveEntitiesAsync`, (5) `new XxxResponse(...)` explícito. O par `IdentifiedCommandHandler` fica no mesmo arquivo. A doutrina completa está nas skills `application-codegen-ddd-dotnet`/`domain-codegen-ddd-dotnet` — **invoque-as antes de mexer nessas camadas**. Os itens abaixo são as violações que já aconteceram neste BC e não podem voltar:

1. **Handler nunca compõe Value Object** (`new TaxId(...)`, `new CompetencePeriod(...)`, `new Money(...)`). Se a factory/método rico não aceita primitivos, crie um **overload no agregado** que compõe o VO internamente (exemplos vivos: `EconomicAgent.Create(taxIdValue, taxIdKind, …)`, `GenerateCommitmentsFor(year, month, …)`, `EconomicContract.Create` com primitivos). Smart Enums via `Enumeration.FromDisplayName<T>` no handler são tradução de input e **são permitidos**.
2. **Handler nunca filtra/inspeciona coleção interna do agregado com conhecimento de domínio** (ex.: `contract.Commitments.Where(c => c.Direction == InflowPromise)`). Peça um acessor ao agregado (exemplo vivo: `GetInflowCommitmentIds()`). Exceção tolerada: projeção *read-only* de coleção para montar Response (ordenar/mapear para DTO), sem decidir nada.
3. **Handler nunca lê `contract.DomainEvents` para computar resposta.** Se o handler precisa de um resultado da mutação (ex.: quantos itens a cascata cancelou), o **método rico retorna** essa informação (exemplo vivo: `Terminate(...)` retorna `int`).
4. **Política de negócio nunca no handler**, mesmo quando parece "só escolher um valor" (ex.: "competência do caixa = período do 1º commitment" era política → virou `ResolveBundledCompetence` no agregado). Decisão sobre **forma do input** (ex.: XOR absoluto/índice no adjustment) é do handler; decisão sobre **estado/semântica de domínio** é do agregado.
5. **Erro com semântica certa, sem ler estado do agregado para fabricá-lo.** Não reuse uma factory de outro cenário porque "dá o mesmo HTTP status" (duplicata de cobertura reusava `CannotFulfillInStatus` lendo `commitment.Status` → criou-se `CommitmentAlreadyCovered` ECC.CTR44). Cada checagem de orquestração tem sua factory dedicada no Domain; Application nunca define erros próprios.
6. **Queries nunca passam pelo mediator** e são a **única** exceção autorizada a tocar a Infra (ver seção "Query side (CQRS)" acima): interface `IXxxQueries` + impl com `AsNoTracking` em `Application/Queries/`, injetada direto no controller (padrão eShop). Commands continuam 100% mediator + `IdentifiedCommand`. Um `IRequestHandler` que injete `EconomicCoreDbContext` é violação.
7. **Demais regras que a auditoria confirmou e continuam valendo**: zero `repo.Update(...)` (busca tracked + `SaveEntitiesAsync`), zero `if` sobre propriedade do agregado para lançar erro (gates como `EnsurePayable`/`EnsureOccupiable` vivem no agregado), zero `foreach` de cascata sobre entidades internas, **um agregado mutado por transação** (efeito cross-aggregate via Outbox/relay), IDs de entidades internas validados pelo método rico (não pelo handler), `TenantId` em toda busca/`ExistsAsync`.

**Checklist antes de entregar um Handler**: algum `new <VO>(...)`? → overload no agregado. Algum `.Where`/`.Any` sobre coleção do agregado para *decidir*? → acessor/método rico. Algum acesso a `DomainEvents`? → retorno do método rico. Alguma factory de erro escolhida lendo estado do agregado? → factory dedicada. Algum `DbContext` num `IRequestHandler`? → mover para `IXxxQueries`.

## Project layout

```
EconomicCore/
├── EconomicCore.sln                  # isolated solution (not in RufinoProject.sln)
├── docker-compose.yml + override     # localized stack: API + Postgres
├── EconomicCore.API/                 # Web SDK host, Program.cs, Controllers, Filters, Extension, appsettings, Dockerfile
│   ├── Controllers/                  #   ResourcesController, AgentsController, ContractsController (+activate +terminate), EventsController, ReportsController
│   ├── Filters/                      #   DomainExceptionFilter (maps DomainErrorCategory → HTTP status: Validation→400, Conflict→409, NotFound→404)
│   └── Extension/                    #   CorsExtensions.AddCorsForFront (default policy lendo Cors:AllowedOrigins; em Development com lista vazia libera qualquer origem)
├── EconomicCore.Application/         # Mediator próprio (sem MediatR), Commands, Queries, Handlers
│   ├── Mediator/                     #   IRequest, IRequestHandler, IPipelineBehavior, IMediator, Mediator, MediatorRegistration (AddCustomMediator), IdentifiedCommand, IdentifiedCommandHandler
│   ├── Behaviors/                    #   LoggingBehavior (único behavior, mais externo)
│   ├── Commands/                     #   RegisterEconomicResource, RegisterEconomicAgent, RegisterEconomicContract, ActivateEconomicContract, TerminateEconomicContract, GenerateCommitments, RegisterConsumptionEvent, RegisterPaymentEvent (cada Handler.cs traz o par <Cmd>IdentifiedCommandHandler)
│   └── Queries/                      #   IReportQueries + ReportQueries (query side direto no controller, sem mediator) + Responses (GetCompetenceDRE, GetCashFlow)
├── EconomicCore.Domain/              # Aggregates, SeedWork, SharedKernel, Domain Services
│   ├── Operational/                  #   EconomicEvent, EconomicResource, EconomicAgent (+ repository interfaces)
│   ├── Prospective/                  #   EconomicContract + Commitment entity (+ repository interface)
│   ├── Services/                     #   DualityMatchingService
│   └── SeedWork/                     #   Entity, AggregateRoot, ValueObject, Enumeration, IUnitOfWork, IDomainEvent, IDomainEventHandler, IDomainEventDispatcher, IRequestManager
├── EconomicCore.Infra/               # EF Core DbContext, repositories, mappings, outbox, idempotency
│   ├── Persistence/                  #   EconomicCoreDbContext (UoW), OutboxMessage, OutboxDeadLetter, ProcessedEventLog, ClientRequest, IOutboxEventTypeResolver
│   ├── Outbox/                       #   OutboxOptions, OutboxProcessor (claim/dispatch/dead-letter/cleanup), OutboxBackgroundService, DomainEventDispatcher (impl da porta, sem MediatR), Handlers/EconomicResourceRegisteredAuditHandler
│   ├── Idempotency/                  #   RequestManager (impl de IRequestManager sobre client_requests)
│   ├── Mapping/                      #   EF entity configurations (4 aggregates + outbox + dead-letter + projection + client_requests)
│   └── Repositories/                 #   4 repository implementations
├── EconomicCore.UnitTests/           # xUnit — 297 domain unit tests (inclui EconomicContractActivateTests, EconomicContractTerminateTests)
├── EconomicCore.IntegrationTests/    # xUnit + Testcontainers + Respawn — 46 integration tests
│   ├── Infrastructure/               #   WebApplicationFactory (Outbox:Enabled=false), BaseIntegrationTest, KnownIds
│   ├── Mothers/                      #   RentScenarioMother (seed direct DB + SeedResourceAndAgentViaApi)
│   ├── Contracts/                    #   Duplicated DTOs (not reusing Application's)
│   ├── DomainEvents/                 #   DomainEventDispatcherTests (resolução de DI, sem banco)
│   ├── Idempotency/                  #   IdempotencyTests (mesmo x-requestid persiste uma vez; ids distintos persistem dois; corrida concorrente persiste uma vez sem 500)
│   ├── Outbox/                       #   OutboxWriteTests, OutboxProcessorTests (dispatch/idempotência/dead-letter/cleanup)
│   └── Rent/                         #   FullLifecycle 12 meses, Register{Resource,Agent}Tests, {CreateContract,ActivateContract,TerminateContract,RegisterPayment,RegisterOccupancy}ExceptionTests, multi-tenant
└── EconomicCore.Architecture/        # design rationale, fontes REA (ver index.md)
    ├── index.md                      #   índice completo com tabelas de seções/linhas de cada documento
    ├── Modelo-REA-Conceitual.md      #   modelo conceitual REA puro (fonte de verdade conceitual)
    ├── Modelo-REA-Tatico.md          #   aterrissagem tática DDD (fonte de verdade para codegen)
    ├── Instrucoes-Claude-Code.md     #   guia de execução dos 7 prompts da Fase 1
    ├── LIVROS/                       #   PDFs originais das fontes acadêmicas
    ├── ISO_IEC_15944-4_2015(en).md   #   ISO/IEC 15944-4:2015 — ontologia contábil/econômica
    ├── The_REA_Accounting_Model_A_Generalized_Framework.md  # McCarthy 1982
    ├── The_Ontological_Foundations_of_REA_Enterprise_Information_Systems_2000.md  # Geerts & McCarthy 2000
    └── Model-Driven_Design_Using_Business_Patterns-Pavel_Hruby.md  # Hruby 2006 (OCR)
```

## Static analysis (Roslyn analyzers)

Dois arquivos na raiz do BC controlam análise estática para todos os 6 csproj:

- **`Directory.Build.props`** — herdado por todo `.csproj` C# da árvore. Liga `AnalysisLevel=latest`, `AnalysisMode=Recommended` (Minimum em test projects), `EnforceCodeStyleInBuild=true` (IDE rules rodam no build) e injeta `SonarAnalyzer.CSharp` como analyzer-only (`PrivateAssets=all`). Excluído de `docker-compose.dcproj` via guard `MSBuildProjectExtension=='.csproj'` (o SDK Docker não tem TargetFramework e quebra com PackageReferences).
- **`.editorconfig`** — `root=true`, define estilo C# moderno (file-scoped namespaces, `using` dentro do namespace, primary constructors), e ajusta severidades de regras CA/IDE/Sxxx ruidosas. Tem três blocos de override por path: global `[*.cs]`, `[**/EconomicCore.UnitTests/**.cs]` e `[**/EconomicCore.IntegrationTests/**.cs]`.

**Stack ativa**: NetAnalyzers (built-in do SDK .NET 10) + SonarAnalyzer.CSharp (≥ `10.27.0.140913`). StyleCop e Roslynator foram avaliados e descartados (ruído desproporcional).

**Política de severidade** (decisão de Sprint 0 — Recomendado):
- Domain: `TreatWarningsAsErrors=true` → qualquer warning quebra o build. É o nível mais rigoroso da pilha porque é o núcleo do negócio.
- Application/Infra/API: warnings aparecem no build mas **não** quebram. CI vai promover a erro depois (Checklist pré-produção).
- Tests: `AnalysisMode=Minimum` + suppressions extras (CA1707, CA1812, S2699 etc.) — testes têm convenções próprias do xUnit que não devem ser sufocadas.

**Regras suprimidas globalmente por convenção** (não rodar simplificação automática nelas):
- `S2328`, `S3249`, `S3875`, `S1210`, `S1643` — padrões canônicos do `SeedWork/` (Entity equality, Smart Enum IComparable, ValueObject.ToString) que Sonar marca como bug mas seguem a referência Vernon.
- `CA1707` — codebase usa `SCREAMING_SNAKE_CASE` em constantes de domínio (`DEFAULT_TOLERANCE_PERCENT`, `MIN_MONTH`).
- `CA1711` — sufixos `Event`, `Template`, etc. são vocabulário do domínio.
- `CA1303`, `CA2007`, `CA2201` — DomainException PT, ASP.NET Core sem ConfigureAwait, Exception herdada por design.
- `S927`, `S4201`, `S112` — `is null`, parâmetros de override ricos, e uso de Exception base nas factories de erro do SeedWork.

Outras regras estão como `suggestion` (visíveis no IDE, não no build) ou `warning` (visíveis no build, não quebram fora do Domain). **Antes de suprimir uma regra nova, prefira corrigir o código** — só suprime se a regra conflita com uma convenção do BC explicitamente documentada aqui ou em `EconomicCore.Architecture/`.

**Como rodar**: `dotnet build EconomicCore.sln` já roda os analyzers. Não há comando separado. Para listar findings agregados: `dotnet build --no-incremental 2>&1 | Select-String 'warning (CA|S|IDE)\d+'`.

## Checklist pré-produção

Itens que **devem** ser resolvidos antes do primeiro deploy em ambiente real. Marcar com `[x]` conforme forem concluídos.

### Banco de dados
- [ ] **Criar migrações EF Core** — hoje o schema é criado via `EnsureCreatedAsync()` (Program.cs), que não suporta alterações incrementais. Trocar para `db.Database.MigrateAsync()` e gerar a migração inicial: `dotnet ef migrations add Initial --project ../EconomicCore.Infra` (rodar de dentro de `EconomicCore.API/`).
- [ ] **Remover `EnsureCreatedAsync`** do Program.cs e do `IntegrationTestWebAppFactory` (testes devem usar `MigrateAsync` também).
- [ ] **Seed data** — definir se dados estáticos (ex.: tipos de recurso econômico, moedas) serão semeados via migração ou via endpoint admin.

### Segurança e autenticação
- [ ] **Autenticação JWT via Keycloak** — configurar `Keycloak.AuthServices` (JWT Bearer + audience + issuer) no `Program.cs`.
- [ ] **Validação do TenantId contra JWT** — hoje `{tenantId}` vem da rota sem validação. Implementar `TenantAuthorizationFilter` que compara o `{tenantId}` da rota com o claim `tenant_ids` do token.
- [ ] **Decorar endpoints com `[ProtectedResource]`** — definir recursos e ações granulares (`contract:create`, `event:register`, `report:read`, etc.).
- [ ] **CORS — origens de produção** — `AddCorsForFront` já está plugado em `Program.cs` (lê `Cors:AllowedOrigins` do `appsettings.json`, chamada antes de `UseAuthorization`). Em Development com a lista vazia, libera qualquer origem (destrava Swagger UI/dev tooling). Antes do deploy: popular `Cors:AllowedOrigins` no `appsettings` do ambiente real (sem `AllowAnyOrigin`/wildcard, conforme convenção da skill `api-codegen-ddd-dotnet`).

### Resiliência e observabilidade
- [ ] **Health checks** — adicionar health check do PostgreSQL (`AspNetCore.HealthChecks.NpgsqlEfCore` ou similar).
- [ ] **Logging estruturado** — configurar Serilog ou similar com correlation ID por request.
- [x] **Outbox consumer** — `OutboxBackgroundService` + `OutboxProcessor` consomem `outbox_messages` (claim `FOR UPDATE SKIP LOCKED`, dispatch in-process via `IDomainEventDispatcher`, dead-letter em `outbox_dead_letters`, cleanup por retenção). Pendências de produção: definir backoff/observabilidade de backlog e garantir que só um deployment rode o worker (`Outbox:Enabled`).
- [ ] **Rate limiting** — avaliar se endpoints públicos precisam de throttling.

### Qualidade e testes
- [ ] **Testes de integração com migrações** — após criar migrações, trocar `EnsureCreatedAsync` por `MigrateAsync` nos testes.
- [ ] **CI pipeline** — configurar build + unit tests + integration tests no CI (GitHub Actions ou similar).
- [ ] **Code coverage** — definir threshold mínimo e integrar no CI.
- [ ] **Promover analyzer warnings a erros no CI** — Application/Infra/API hoje só emitem warning (ver "Static analysis"). No pipeline de CI, passar `/p:TreatWarningsAsErrors=true` na etapa de build para bloquear merge com violação nova de CA/Sxxx. Domain já está blindado localmente.

### Infra e deploy
- [ ] **Dockerfile otimizado** — revisar multi-stage build, garantir que não copia arquivos desnecessários.
- [ ] **Variáveis de ambiente** — connection string, Keycloak config, S3/storage — todas via env vars, sem segredos no `appsettings.json`.
- [ ] **HTTPS** — garantir TLS termination (via reverse proxy ou certificado no container).

## Conventions inherited from the DDD skills

These are enforced by the `domain-codegen-ddd-dotnet`, `application-codegen-ddd-dotnet`, `infra-codegen-ddd-dotnet`, `api-codegen-ddd-dotnet`, and `tests-domain-ddd-dotnet` skills — invoke them via Skill instead of generating DDD code by hand:

- Code in English; `DomainException` messages in Portuguese; conversation in Portuguese.
- `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` is set on Domain (mandatory by Sprint 0). NetAnalyzers + SonarAnalyzer.CSharp são injetados via `Directory.Build.props` na raiz do BC com severidades em `.editorconfig` — ver seção "Static analysis (Roslyn analyzers)".
- Strongly-typed Ids (`record struct : IEntityId<TSelf>`), Smart Enums via `Enumeration`, VOs deriving from abstract `ValueObject`.
- Aggregate Roots only emit Domain Events (never internal Entities).
- Mediator próprio (sem MediatR) em `Application/Mediator/` — mesma superfície (`IRequest`/`IRequestHandler`/`IPipelineBehavior`/`IMediator`), registrado via `AddCustomMediator`.
- Idempotency in Application: write commands wrapped via `IdentifiedCommand`, checados contra `IRequestManager` (porta em `Domain/SeedWork`, impl na Infra sobre `client_requests`); header `x-requestid`.
- API uses `[ProtectedResource(resource, action)]` for Keycloak-backed granular authorization (planned — Keycloak is shared infra, configured at server level).
