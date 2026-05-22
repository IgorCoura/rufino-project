# AccountsPayable.Domain — Plano de Implementação Faseado

> **Objetivo**: implementar o Bounded Context `AccountsPayable.Domain` em sprints curtas, cada uma entregando software **funcional ponta a ponta**, usável e testável em produção mesmo sem todas as features. Walking Skeleton sobre o `Payable` (Aggregate central), expandindo em capacidades.
>
> **Convenções herdadas da skill `domain-codegen-ddd-dotnet`** (não vou repetir em cada sprint):
> - Cada Bounded Context é um projeto `.NET` separado → tudo aqui mora em `AccountsPayable.Domain/`.
> - Aggregates ficam na raiz do projeto (sem subcamada `BoundedContext/`).
> - Código em inglês; mensagens de `DomainException` em português; conversas em português.
> - Strongly-typed Ids (`record struct : IEntityId<TSelf>`), Smart Enums via `Enumeration`, VOs herdando de `ValueObject`.
> - Factory de erros por Aggregate (`<Aggregate>Errors.cs`), padrão de ID `AP.<AGG>##`.
> - Eventos só no Aggregate Root.
>
> **Decisão estrutural (D-405)**: `Payable` é Aggregate **Event-Sourced**. Os demais Aggregates deste BC (`Supplier`, `Contract`, `AutoApprovalPolicy`, `ExpectedRecurringBill`, etc.) são **tradicionais** (snapshot via EF).
> - As sprints abaixo descrevem o modelo conceitual e a ordem; a decisão de A+ES vs tradicional está marcada por Aggregate.
> - Quando chegar a hora de codar, a skill atual cobre os tradicionais; `Payable` exige o template A+ES (Apply/Mutate/When) descrito em `references/event-sourcing-overview.md`.

---

## Visão geral do BC e do que está fora dele

`AccountsPayable.Domain` é responsável por: **representar a obrigação financeira em si** (a conta a pagar), classificá-la, decidir se pode ser paga automaticamente, e disparar a ordem de pagamento. **Não** é responsável por: capturar PDFs/emails (isso é `BillIngestion`), executar o PIX/boleto (isso é `PaymentExecution`), nem contabilizar (isso é `Accounting`).

**Fluxo macro do `Payable`**:

```
Captured → Classified → Scheduled → Approved → DispatchedForPayment → Paid
   │           │            │           │              │                │
   └─ Manual ──┴─ Auto via ─┴─ Manual ──┴─ Emite ──────┴─ Conciliado ───┘
      ou auto   regra        ou auto    PaymentOrder    via evento
                                        Requested      de outro BC
```

---

## SPRINT 0 — Fundação (1 semana)

**Objetivo**: deixar o projeto pronto para receber qualquer Aggregate. **Não** entrega feature de negócio — entrega base reutilizável.

### Entregáveis

1. **Projeto `AccountsPayable.Domain/`** criado com `<Nullable>enable</Nullable>`, `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`.
2. **SeedWork** (compartilhado entre BCs — pode vir de um projeto `Shared.Domain`):
   - `IEntityId<TSelf>` — contrato de Id strongly-typed.
   - `Entity<TId>` — base de Entity com `CreatedAt`, `UpdatedAt`.
   - `AggregateRoot<TId>` — herda `Entity<TId>`, com `AddDomainEvent` / `PullDomainEvents`.
   - `ValueObject` — abstract com `GetEqualityComponents`.
   - `Enumeration` — base de Smart Enum.
   - `IDomainEvent` — contrato (`EventId`, `OccurredAt`).
   - `DomainException` — com `ErrorId`, `MessageTemplate`, `Parameters`, `SourcePath`.
3. **Adapter A+ES** (esqueleto vazio): `EventSourcedAggregateRoot<TId>` com `Apply` / `Mutate` / `When` reflection helper, `Changes` list, ctor de reidratação. Templates baseados em `references/event-sourcing-overview.md`.
4. **Tenancy básica**: `TenantId` como `record struct : IEntityId<TenantId>`. Todo Aggregate Root deste BC carrega `TenantId` para filtragem multi-tenant.
5. **Convenção de IDs de erro**: documentar prefixo `AP` (Accounts Payable) e tabela de siglas de Aggregate (`PAY` para Payable, `SUP` para Supplier, etc.).

### Como já dá pra usar/testar

- Compila e roda testes unitários do SeedWork (ex.: `Enumeration.FromValue`, `ValueObject` igualdade).
- Validação de que o template `EventSourcedAggregateRoot<TId>` consegue reidratar de uma lista vazia de eventos.

### Critério de aceite

- ✅ Projeto compila sem warnings.
- ✅ Bateria de testes do SeedWork passa.
- ✅ `dotnet pack` produz um `.nupkg` reutilizável de `Shared.Domain` (se optar por extrair).

### Dependências

Nenhuma. É a primeira sprint.

---

## SPRINT 1 — Supplier (Fornecedor) — 1 semana

**Objetivo**: cadastrar e gerir fornecedores. **Não tem nada a ver com Payable ainda** — é puramente cadastro. Permite o usuário cadastrar fornecedores antes mesmo de receber a primeira conta. Esta sprint **viabiliza** as próximas.

### Modelagem

**Aggregate Root: `Supplier`** (tradicional, snapshot via EF)

- **Identidade**: `SupplierId : IEntityId<SupplierId>`.
- **Atributos**:
  - `TenantId TenantId` (multi-tenancy).
  - `LegalName` (VO `LegalName` — `MAX_LENGTH = 200`).
  - `TradeName?` (VO `TradeName` — opcional).
  - `TaxId TaxId` (VO — CPF ou CNPJ validado; `Type` enum interno).
  - `ContactInfo` (VO — email, telefone, endereço opcional).
  - `BankAccounts` (coleção de Entities internas `SupplierBankAccount`).
  - `Status` (Smart Enum `SupplierStatus`: `Active`, `Inactive`, `Blocked`).
- **Comportamentos** (verbos do negócio):
  - `Create(...)` (factory) → emite `SupplierCreated`.
  - `Rename(LegalName newName, DateTime occurredAt)` → emite `SupplierRenamed`.
  - `UpdateContact(ContactInfo, ...)` → emite `SupplierContactUpdated`.
  - `AddBankAccount(...)` → método retorna a Entity interna; emite `SupplierBankAccountAdded`.
  - `RemoveBankAccount(SupplierBankAccountId, ...)` → emite `SupplierBankAccountRemoved`.
  - `Block(string reason, ...)` → muda status; emite `SupplierBlocked`.
  - `Unblock(...)` → emite `SupplierUnblocked`.
  - `Deactivate(...)` / `Reactivate(...)` → emite eventos correspondentes.
- **Invariantes**:
  - `TaxId` é único por tenant (essa unicidade é checada por **Domain Service** `SupplierUniquenessChecker` que recebe um `ISupplierTaxIdLookup` — porta no domínio, implementação na Infra).
  - Bloqueado → não pode receber pagamento (essa regra é **lida** depois pelo `Payable`, mas a Supplier só sinaliza o status).
  - Não pode remover a última `BankAccount` se status = `Active`.
- **Eventos de domínio**: `SupplierCreated`, `SupplierRenamed`, `SupplierContactUpdated`, `SupplierBankAccountAdded`, `SupplierBankAccountRemoved`, `SupplierBlocked`, `SupplierUnblocked`, `SupplierDeactivated`, `SupplierReactivated`.

**Entity interna: `SupplierBankAccount`**

- **Identidade**: `SupplierBankAccountId`.
- **Atributos**: `BankCode`, `Branch`, `AccountNumber`, `AccountType` (Smart Enum: `Checking`, `Savings`, `Salary`), `PixKey?` (VO opcional).
- **Mutação**: só via métodos `internal` chamados pela `Supplier`.

**VOs novos**:
- `LegalName`, `TradeName`, `TaxId` (com validação CPF/CNPJ), `ContactInfo`, `PhoneNumber`, `EmailAddress`, `Address`, `PixKey`.

### Como já dá pra usar/testar

- Sistema permite **cadastrar, editar, bloquear e listar fornecedores**. Vale como produto isolado: muitos clientes querem só esse cadastro estruturado pra centralizar contatos antes de pensar em pagamento.
- Já dá pra rodar fluxo de:
  - Criar fornecedor PJ → adicionar conta bancária → trocar nome fantasia → bloquear → desbloquear.
  - Tentar criar dois com mesmo CNPJ no mesmo tenant → falha controlada.

### Critério de aceite

- ✅ 100% de cobertura de testes unitários sobre invariantes de `Supplier` e VOs (`TaxId` valida CPF/CNPJ corretamente).
- ✅ Domain Service `SupplierUniquenessChecker` testado com fake de `ISupplierTaxIdLookup`.
- ✅ Sistema persiste em PostgreSQL via EF Core e o caso "criar 2 com mesmo CNPJ" falha de forma idempotente.

### Dependências

Sprint 0.

---

## SPRINT 2 — Payable mínimo (cadastro manual + ciclo até "pago manualmente") — 2 semanas

**Objetivo**: permitir cadastrar uma conta a pagar **manualmente** (sem captura, sem classificação automática, sem aprovação), agendá-la, marcá-la como paga, e cancelá-la. **Já é um sistema utilizável** pra cliente que quer só registrar contas e dar baixa manual.

### Modelagem

**Aggregate Root: `Payable`** ⚠️ **Event-Sourced** (D-405)

> Como esta skill não gera A+ES, a implementação deste Aggregate seguirá o template descrito em `references/event-sourcing-overview.md`: ctor de reidratação por stream, `Apply` → acumula em `Changes` + chama `Mutate`, `When(SpecificEvent)` muda estado, sem `private set` mutável fora dos `When`. **Não usar o template tradicional.**

- **Identidade**: `PayableId : IEntityId<PayableId>`.
- **Estado interno** (representado como classe `PayableState`):
  - `TenantId TenantId`.
  - `SupplierId SupplierId`.
  - `Amount` (VO `Money` — valor + moeda; `Currency` Smart Enum).
  - `DueDate` (VO `DueDate` — `DateOnly` com validação não-passado-na-criação).
  - `Description` (VO `Description` — `MAX_LENGTH = 500`).
  - `Status` (Smart Enum `PayableStatus`: `Draft` → `Scheduled` → `Paid` | `Cancelled`).
  - `ScheduledFor?` (DateOnly).
  - `PaidAt?` (DateTime).
  - `PaymentProof?` (VO `PaymentProof` — URI + tipo, simples nesta sprint).
- **Comandos / métodos de comportamento** (cada um emite evento via `Apply`):
  - `Initialize(PayableId, TenantId, SupplierId, Money, DueDate, Description, DateTime occurredAt)` → `PayableCreated`.
  - `Schedule(DateOnly scheduledFor, DateTime occurredAt)` → `PayableScheduled`. **Invariante**: só de `Draft`.
  - `MarkAsPaidManually(PaymentProof proof, DateTime paidAt, DateTime occurredAt)` → `PayableMarkedAsPaid`. **Invariante**: de `Scheduled` ou `Draft` (cliente pode pagar antes de agendar).
  - `Cancel(string reason, DateTime occurredAt)` → `PayableCancelled`. **Invariante**: não em `Paid`.
- **Invariantes**:
  - `Amount > 0`.
  - `DueDate` não pode ser anterior ao `OccurredAt` da criação (data passada não cria; data passada pode pagar atrasado).
  - Status segue máquina: `Draft → Scheduled | Paid | Cancelled` ; `Scheduled → Paid | Cancelled` ; `Paid` e `Cancelled` são terminais.
  - Não pode mudar `Supplier`, `Amount`, `DueDate` depois de `Scheduled` (se precisar, cancela e cria nova).
- **Eventos**: `PayableCreated`, `PayableScheduled`, `PayableMarkedAsPaid`, `PayableCancelled`.

**VOs novos**: `Money`, `Currency` (Smart Enum), `DueDate`, `Description`, `PaymentProof`.

**Factory de erros**: `PayableErrors.cs` com IDs `AP.PAY01..AP.PAY##` (ex.: `AP.PAY01 - InvalidAmount`, `AP.PAY02 - InvalidStatusTransition`).

### O que NÃO entra nesta sprint

- ❌ Classificação (plano de contas, centro de custo). Vai pra Sprint 4.
- ❌ Aprovação. Vai pra Sprint 5.
- ❌ `PaymentOrder` (execução automatizada via PIX/boleto). Vai pra Sprint 6.
- ❌ Captura via OCR/email. Vai num BC separado (`BillIngestion`), referenciado por `CapturedBillId?` na Sprint 7.
- ❌ Parcelamento. Vai pra Sprint 8.

### Como já dá pra usar/testar

- Cliente cadastra fornecedor (Sprint 1), registra conta a pagar (descrição + valor + vencimento + fornecedor), agenda, marca como paga quando pagou pelo banco do jeito antigo, anexa o comprovante.
- **Já é um substituto digital do controle em planilha** — entrega de valor real.

### Critério de aceite

- ✅ Testes unitários: cada transição válida e cada transição inválida da máquina de estados.
- ✅ Teste **Given-When-Expect** (estilo A+ES):
  - Given: `[PayableCreated, PayableScheduled]`.
  - When: `MarkAsPaidManually(...)`.
  - Expect: `[PayableMarkedAsPaid]`.
- ✅ Reidratação a partir de stream funciona (carregar `Payable` por replay e estado final igual ao snapshot esperado).
- ✅ Tentativa de pagar `Cancelled` lança `PayableErrors.CannotPayCancelled()`.

### Dependências

Sprints 0 e 1.

---

## SPRINT 3 — Chart of Accounts (Plano de Contas) e Cost Center — 1 semana

**Objetivo**: dar ao usuário a estrutura de classificação (plano de contas hierárquico + centro de custo). **Não classifica o Payable ainda** — apenas cadastra as opções. Essa separação garante que a classificação na Sprint 4 já tenha de onde puxar dados.

### Modelagem

**Aggregate Root: `ChartOfAccounts`** (tradicional)

- **Identidade**: `ChartOfAccountsId`.
- **Modelo**: uma árvore por tenant. O Aggregate inteiro É a árvore (não Aggregate por conta).
- **Atributos**:
  - `TenantId TenantId`.
  - `Name` (VO).
  - `Accounts` (coleção interna de Entity `Account`).
- **Comportamentos**:
  - `Create(ChartOfAccountsId, TenantId, Name, ...)` → `ChartOfAccountsCreated`.
  - `AddAccount(AccountId parentId?, AccountCode code, AccountName name, AccountType type, ...)` → `AccountAdded`.
  - `RenameAccount(AccountId, AccountName, ...)` → `AccountRenamed`.
  - `DeactivateAccount(AccountId, ...)` → `AccountDeactivated`.
- **Invariantes**:
  - `AccountCode` único dentro do plano.
  - Não pode desativar conta que tem filhos ativos.
  - Profundidade máxima da árvore: `Account.MAX_DEPTH = 5` (`public const`).

**Entity interna: `Account`** com `ParentId?`, `Code` (VO), `Name`, `Type` (Smart Enum: `Asset`, `Liability`, `Expense`, `Revenue`, `Equity`), `IsActive`.

**Aggregate Root: `CostCenter`** (tradicional, MUITO simples)

- `CostCenterId`, `TenantId`, `Code`, `Name`, `IsActive`.
- Métodos: `Create`, `Rename`, `Deactivate`, `Reactivate`.
- Eventos correspondentes.

### Como já dá pra usar/testar

- Cliente configura o plano de contas (ex.: `4.01.01 - Fornecedores PJ`, `4.01.02 - Aluguel`, ...) e os centros de custo (`OBRA-SYRAH`, `ESCRITORIO`).
- **Ainda não muda nada no fluxo de `Payable`** — mas o usuário já vê a estrutura no app e prepara o terreno.

### Critério de aceite

- ✅ Não consegue criar 2 contas com mesmo `Code` no mesmo plano.
- ✅ Não consegue desativar nó com filhos ativos.
- ✅ Profundidade > `MAX_DEPTH` rejeitada.

### Dependências

Sprint 0.

---

## SPRINT 4 — Classificação do Payable (manual) — 1 semana

**Objetivo**: permitir classificar uma `Payable` em conta contábil e centro de custo. Apenas classificação **manual** ainda. A classificação **automática** vem depois (Sprint 9).

### Modelagem

**Mudança no `Payable`** (expansão do Aggregate Event-Sourced):

- Adiciona ao estado: `AccountId? AccountId`, `CostCenterId? CostCenterId`, `ClassifiedAt?`, `ClassifiedBy? UserId`.
- Novo comando: `Classify(AccountId, CostCenterId, UserId classifiedBy, DateTime occurredAt)` → emite `PayableClassified`.
- Invariante: só pode agendar (`Schedule`) se estiver classificado **OU** se o tenant tiver um setting de "permite agendar não classificado" (decisão de produto — manter ligado por default = `false`).

**Domain Service**: `PayableClassificationValidator` (stateless)

- Recebe `Payable` + `AccountId` + `ChartOfAccounts` (ou um `IAccountResolver`) + `CostCenter`.
- Valida que:
  - `Account` existe, está ativa e é tipo `Expense` (ou outras permitidas conforme regra de negócio).
  - `CostCenter` existe e está ativo.
  - Ambos pertencem ao mesmo `TenantId` do `Payable`.
- Lança erros via factory `PayableClassificationErrors.cs` (ID `AP.PCL##`).

> Justificativa do Domain Service: a regra envolve **3 Aggregates** (`Payable`, `ChartOfAccounts`, `CostCenter`). Não pode caber dentro do `Payable` (ele não tem referência por objeto, só por Id). Service é o caminho.

### Eventos novos

- `PayableClassified` (payload: `PayableId`, `AccountId`, `CostCenterId`, `ClassifiedBy`, `OccurredAt`).

### Como já dá pra usar/testar

- Cliente cadastra conta a pagar → escolhe categoria (conta contábil) e centro de custo → vê relatórios por categoria/centro de custo (relatório é responsabilidade da camada de Application/Query, mas o dado contábil já está lá).

### Critério de aceite

- ✅ Classificar com `Account` inativa falha.
- ✅ Classificar com `Account` tipo `Asset` (não-despesa) falha.
- ✅ Classificar com `CostCenter` de outro tenant falha (IDOR).
- ✅ Reclassificar uma `Payable` já classificada é permitido até `Paid` (emite novo `PayableClassified` — A+ES preserva histórico).

### Dependências

Sprints 2 e 3.

---

## SPRINT 5 — Aprovação manual (single approver) — 1 semana

**Objetivo**: introduzir o conceito de "alguém precisa aprovar antes de pagar". Apenas **uma alçada, um aprovador**, manual. Alçadas múltiplas e regras complexas vêm depois (Sprint 10).

### Modelagem

**Mudança no `Payable`**:

- Adiciona ao estado: `ApprovedBy? UserId`, `ApprovedAt?`, `ApprovalRequiredAbove? Money`.
- Novo comando: `RequestApproval(DateTime occurredAt)` → `PayableApprovalRequested`. Permitido de `Classified` (informalmente — não há status formal `Classified`; é estado da máquina).
- Novo comando: `Approve(UserId approver, DateTime occurredAt)` → `PayableApproved`.
- Novo comando: `Reject(UserId approver, string reason, DateTime occurredAt)` → `PayableRejected`. Status vai pra `Cancelled` (terminal) ou um novo `Rejected` (se quiser permitir reapresentar — sugiro `Rejected` separado para reabrir depois).
- Invariante: só pode `Schedule` ou `MarkAsPaidManually` se valor ≤ `ApprovalRequiredAbove` **OU** se `Status == Approved`.
- A regra "quem pode aprovar" **não mora aqui** — mora na camada de Application/Authorization (Keycloak). O domínio só registra **quem** aprovou, recebido como `UserId`.

### Smart Enum atualizada

`PayableStatus` ganha: `AwaitingApproval`, `Approved`, `Rejected`.

Máquina de estados atualizada:

```
Draft ──(Classify)──> Classified ──(Schedule if no approval needed)──> Scheduled ──> Paid
                          │
                          ├──(Schedule if approval needed)──> AwaitingApproval ──> Approved ──> Scheduled ──> Paid
                          │                                       │
                          │                                       └─ Rejected (terminal ou reabre pra Draft)
                          └──> Cancelled (qualquer estado não-terminal)
```

> **Decisão de produto**: o threshold (`ApprovalRequiredAbove`) é uma config do tenant lida na hora do `Schedule`, **não atributo do Payable**. Pode ser passada como parâmetro do método `Schedule(DateOnly, Money? approvalThreshold)` para evitar acoplar o domínio à infra de configuração.

### Como já dá pra usar/testar

- Cliente define que contas acima de R$ 5.000 precisam de aprovação. Tenta agendar uma conta de R$ 3.000 → entra direto pra `Scheduled`. Tenta agendar uma de R$ 10.000 → entra em `AwaitingApproval`. Sócio aprova no app → vai pra `Scheduled`. Aprovação rejeitada → `Rejected`.

### Critério de aceite

- ✅ Conta ≤ threshold: agendamento direto, sem `PayableApprovalRequested`.
- ✅ Conta > threshold: bloqueada até `Approve`.
- ✅ Tentativa de `Schedule` sem aprovação numa conta acima do threshold falha.
- ✅ `Rejected` não pode ir pra `Paid`.

### Dependências

Sprints 2 e 4.

---

## SPRINT 6 — PaymentOrder (Ordem de pagamento) — 2 semanas

**Objetivo**: introduzir o Aggregate `PaymentOrder`. Quando uma `Payable` é aprovada **e** chega na data agendada, o sistema cria uma `PaymentOrder` que representa a **intenção de pagar via canal X (PIX, boleto, TED)**. A execução em si é responsabilidade do BC `PaymentExecution` (fora deste BC).

> **Importante**: este Aggregate vive em `AccountsPayable.Domain` **OU** em `PaymentExecution.Domain` — é decisão estratégica do usuário. A skill `domain-codegen-ddd-dotnet` lembra explicitamente que **não decide BC**. Aqui vou propor que `PaymentOrder` vá em `PaymentExecution.Domain` (porque ele orquestra retries, conciliação, callbacks de banco — pouco a ver com a obrigação financeira em si). O `AccountsPayable.Domain` apenas emite o evento `PayablePaymentRequested` e escuta de volta `PayablePaid` (via integração de eventos entre BCs).

### Modelagem (escopo deste BC)

**Mudança no `Payable`** (apenas a porta de entrada/saída do BC):

- Novo comando: `RequestPayment(PaymentMethod method, DateTime occurredAt)` → emite `PayablePaymentRequested` (payload com `PayableId`, `Amount`, `SupplierId`, `BankAccountId` selecionado, `Method`).
- Reage a integration event externo: handler na Application Layer recebe `PaymentOrderExecuted` (vindo de `PaymentExecution`) e chama `Payable.ConfirmPaid(PaymentOrderId, DateTime paidAt, ...)` → emite `PayablePaid`.
- Reage também a `PaymentOrderFailed` → chama `Payable.MarkPaymentFailed(reason, ...)` → emite `PayablePaymentFailed`. Status vai pra `PaymentFailed` (não terminal — pode tentar de novo).
- Smart Enum `PaymentMethod`: `Pix`, `BankSlip`, `Ted`, `Manual`.

### O que fica em `AccountsPayable.Domain` nesta sprint

- ✅ Comando `RequestPayment` + evento `PayablePaymentRequested`.
- ✅ Comando `ConfirmPaid` + evento `PayablePaid`.
- ✅ Comando `MarkPaymentFailed` + evento `PayablePaymentFailed`.
- ✅ Status `PaymentRequested`, `PaymentFailed`.
- ❌ O Aggregate `PaymentOrder` (com retries, idempotency, conciliação) **NÃO está aqui**.

### Como já dá pra usar/testar

- Conta aprovada e na data de vencimento → sistema dispara `PayablePaymentRequested` no event bus.
- Mesmo sem o `PaymentExecution` implementado de verdade, dá pra simular o retorno num teste de integração (fake do consumer de eventos) e validar que o `Payable` chega em `Paid`.
- **Cliente real**: nesta sprint o pagamento ainda é manual, mas o app já mostra "aguardando pagamento" e tem o gancho pronto pra plugar a integração bancária na próxima entrega.

### Critério de aceite

- ✅ `RequestPayment` numa `Payable` não aprovada (acima do threshold) falha.
- ✅ `ConfirmPaid` numa `Payable` que não pediu pagamento falha (proteção contra evento órfão).
- ✅ Idempotência: receber `PaymentOrderExecuted` duas vezes não gera dois `PayablePaid`.

### Dependências

Sprints 2 e 5.

---

## SPRINT 7 — Integração com Bill Ingestion — 1 semana

**Objetivo**: permitir que uma `Payable` nasça **a partir de uma `CapturedBill`** (PDF/email/foto processado pelo BC `BillIngestion`). Não vamos implementar o `BillIngestion` aqui — apenas o gancho de entrada.

### Modelagem

**Mudança no `Payable`**:

- Adiciona ao estado: `CapturedBillId? CapturedBillId` (referência fraca ao Aggregate de outro BC).
- Nova factory: `Payable.InitializeFromCapture(PayableId, TenantId, CapturedBillId, SupplierId, Money, DueDate, Description, ...)` → emite `PayableCreatedFromCapture` (variante de `PayableCreated` com `CapturedBillId` no payload).
- Handler na Application Layer escuta `CapturedBillApproved` (evento de `BillIngestion.Domain`) e chama `Payable.InitializeFromCapture`.

### Como já dá pra usar/testar

- Mesmo sem o `BillIngestion` real, o handler aceita um evento simulado em testes e gera um `Payable` corretamente vinculado.
- Quando o `BillIngestion` ficar pronto (sprint paralela ou posterior), o cliente recebe boletos por email → sistema extrai → humano revisa → `Payable` aparece pronto pra agendar.

### Critério de aceite

- ✅ Reenviar o mesmo `CapturedBillApproved` (mesmo `CapturedBillId`) não cria dois `Payable` (dedup por `CapturedBillId` único).
- ✅ Estado inicial: `Draft`, sem classificação (vai para a fila de revisão humana até que regra de auto-classificação rode — Sprint 9).

### Dependências

Sprints 2 e 6 (idealmente; pode rodar antes da 6).

---

## SPRINT 8 — Parcelamento (Installments) — 1 semana

**Objetivo**: suportar contas parceladas (ex.: aluguel anual em 12x). Cada parcela é um `Payable` independente, mas todas conhecem o "pai" via `InstallmentPlanId`.

### Modelagem

**Aggregate Root: `InstallmentPlan`** (tradicional — não é A+ES; é só agrupador)

- **Identidade**: `InstallmentPlanId`.
- **Atributos**:
  - `TenantId`, `SupplierId`, `TotalAmount` (Money), `InstallmentCount` (int), `FirstDueDate`, `Description`.
  - `PayableIds` (coleção `IReadOnlyCollection<PayableId>`).
- **Comportamentos**:
  - `Create(...)` → emite `InstallmentPlanCreated`.
  - `RegisterPayable(PayableId, int installmentNumber)` → emite `PayableLinkedToInstallmentPlan`.
- **Invariantes**:
  - `InstallmentCount ≥ 2` (1 não é parcelamento).
  - Soma dos `Amount` dos `Payable` vinculados = `TotalAmount` (tolerância de centavo na última parcela).

**Mudança no `Payable`**:

- Adiciona ao estado: `InstallmentPlanId? InstallmentPlanId`, `InstallmentNumber? int` (1-based).
- Nova factory: `Payable.InitializeAsInstallment(...)`.

**Domain Service**: `InstallmentPlanFactory` (stateless)

- Dado total, número de parcelas, primeira data, intervalo (mensal/semanal) → produz `InstallmentPlan` + N `Payable` (cada um via sua factory).
- Distribui centavos: se `1000.00 / 3`, gera `333.34 + 333.33 + 333.33` (resíduo na primeira; ou na última, é decisão de produto).

### Como já dá pra usar/testar

- Cliente registra uma conta de R$ 12.000 parcelada em 12x → sistema gera 12 `Payable` agendadas mês a mês.
- Cancelar uma parcela do meio é permitido (afeta só aquela `Payable`).
- Cancelar o `InstallmentPlan` inteiro → cancela todas `Payable` não pagas (evento → handler → cancel em cascata).

### Critério de aceite

- ✅ Soma das parcelas = total (com resíduo de centavo controlado).
- ✅ Não dá pra criar plano com `InstallmentCount = 1`.
- ✅ Cancelar plano cancela as parcelas em `Draft`/`Scheduled`/`AwaitingApproval`; deixa intocadas as `Paid`.

### Dependências

Sprints 2 e 5.

---

## SPRINT 9 — Classificação automática (ExpenseClassification rules) — 1 a 2 semanas

**Objetivo**: aprender e aplicar regras "fornecedor X categoria Y centro Z" para classificar automaticamente. Substitui parte da Sprint 4 (manual) por automação.

### Modelagem

**Aggregate Root: `ExpenseClassificationRule`** (tradicional)

- **Identidade**: `ExpenseClassificationRuleId`.
- **Atributos**:
  - `TenantId`.
  - `Match` (VO `ClassificationMatcher` — pode ser por `SupplierId`, por palavra-chave na descrição, por faixa de valor, ou combinação).
  - `Action` (VO `ClassificationAction` — `AccountId` + `CostCenterId` + `AutoApprove?` bool).
  - `Priority` (int — menor número = maior prioridade).
  - `IsActive`.
  - `LearnedFromUserId?` (se foi aprendida automaticamente após N classificações manuais consistentes).
- **Comportamentos**:
  - `CreateManual(...)` / `LearnFromHistory(...)` (factories diferentes).
  - `Activate` / `Deactivate`.
  - `Update(...)`.

**Domain Service**: `PayableAutoClassifier` (stateless)

- Recebe `Payable` (não classificada) + lista de `ExpenseClassificationRule` ativas do tenant.
- Retorna `ClassificationAction?` (null se nenhuma regra casa).
- A Application Layer aplica: se retornou ação, chama `Payable.Classify(...)`; se não, deixa pra revisão humana.

**Mudança no `Payable`**:

- Quando `PayableCreatedFromCapture` é processado, a Application Layer roda o `PayableAutoClassifier` antes de apresentar pro usuário.
- O `ClassifiedBy` no evento agora pode ser um `UserId` ou um placeholder `UserId.System` (decisão: criar Smart Enum `Classifier` com `Manual(UserId)` / `Automatic(RuleId)`).

### Como já dá pra usar/testar

- Cliente classifica manualmente 3 contas do mesmo fornecedor pra mesma categoria → sistema sugere criar regra automática → próxima conta desse fornecedor já vem classificada.
- Sistema "aprende" sozinho (com confirmação humana primeiro).

### Critério de aceite

- ✅ Regra com prioridade 1 vence regra com prioridade 5 quando ambas casam.
- ✅ Regra inativa é ignorada.
- ✅ Casa por `SupplierId` exato; casa por palavra-chave na descrição; casa por faixa de valor.

### Dependências

Sprints 4 e 7.

---

## SPRINT 10 — Auto-approval Policy (alçadas) — 1 semana

**Objetivo**: permitir múltiplos níveis de aprovação por valor, por categoria, por fornecedor recorrente. Substitui a regra simples da Sprint 5.

### Modelagem

**Aggregate Root: `AutoApprovalPolicy`** (tradicional)

- **Identidade**: `AutoApprovalPolicyId`.
- **Atributos**:
  - `TenantId`.
  - `Rules` (coleção interna de Entity `ApprovalRule`).
- **Entity `ApprovalRule`**:
  - `MatchCriteria` (VO — fornecedor recorrente, ou conta contábil específica, ou faixa de valor, ou combinação).
  - `ThresholdAmount` (Money — abaixo passa direto).
  - `RequiredApproverRoles` (VO `ApproverRoles` — lista de roles).
  - `RequiredApprovalCount` (int — quantos precisam aprovar).
  - `IsActive`.

**Domain Service**: `ApprovalRequirementCalculator` (stateless)

- Recebe `Payable` + `AutoApprovalPolicy` → retorna `ApprovalRequirement` (`Required: bool`, `Roles: List<Role>`, `Count: int`).
- A Application Layer usa o resultado para decidir se chama `Payable.Schedule` direto ou `Payable.RequestApproval`.

**Mudança no `Payable`**:

- `Approve(UserId, DateTime)` ganha versão `Approve(UserId, ApprovalRequirementId, ...)` para registrar **qual requisito** essa aprovação satisfaz.
- Novo estado interno: `RequiredApprovals` (lista de `{Role, Status}`) — emite `PayableApprovalRecorded` por aprovação; quando todos cumpridos, emite `PayableFullyApproved` → status `Approved`.

### Como já dá pra usar/testar

- Cliente define: "contas > R$ 10k de fornecedores novos precisam de 2 sócios; > R$ 50k precisam dos 3 sócios; recorrentes de fornecedor X com contrato ativo passam direto até R$ 20k".
- Cada conta nova passa pelo calculator → cai na alçada correta automaticamente.

### Critério de aceite

- ✅ Conta que casa em duas regras: usa a mais restritiva.
- ✅ Conta sem regra que case: cai no default (sempre aprovar manualmente).
- ✅ Mudança da política não retroage em `Payable` já aprovado.

### Dependências

Sprints 2 e 5.

---

## SPRINT 11 — Contracts (contratos) e ExpectedRecurringBill — 1 a 2 semanas

**Objetivo**: representar contratos com fornecedores (aluguel, assinaturas) e contas recorrentes esperadas. Permite previsão de fluxo de caixa e alertas de "conta esperada não chegou".

### Modelagem

**Aggregate Root: `Contract`** (tradicional)

- `ContractId`, `TenantId`, `SupplierId`, `Description`, `StartDate`, `EndDate?`, `MonthlyAmount` (Money), `PaymentDay` (1-31), `AutoCreatePayable` (bool), `Status` (Smart Enum: `Draft`, `Active`, `Suspended`, `Terminated`).
- Comportamentos: `Activate`, `Suspend`, `Resume`, `Terminate`, `UpdateAmount` (com versionamento — emite `ContractAmountChanged` com `OldAmount`/`NewAmount`/`EffectiveDate`).

**Aggregate Root: `ExpectedRecurringBill`** (tradicional)

- Gerado pelo `Contract` ativo, mês a mês.
- `ExpectedRecurringBillId`, `ContractId`, `ExpectedDueDate`, `ExpectedAmount` (Money), `Status` (`Pending`, `MatchedToPayable`, `Missed`).
- Quando uma `Payable` é criada e casa (mesmo fornecedor + mesmo mês + valor próximo): matching automático → `ExpectedRecurringBillMatched`.

**Domain Service**: `RecurringBillMatcher` — casa `Payable` recém-criada com `ExpectedRecurringBill` pendente do mesmo tenant.

### Como já dá pra usar/testar

- Cliente cadastra contrato de aluguel de R$ 5k, dia 10 → sistema gera `ExpectedRecurringBill` mensalmente.
- Boleto chega via captura → casa automaticamente, conta é pré-classificada com a categoria do contrato.
- Dia 15 e a `ExpectedRecurringBill` continua `Pending` → alerta de "conta esperada não chegou".

### Critério de aceite

- ✅ Contract gera 12 `ExpectedRecurringBill` no primeiro ano (a partir da `StartDate`).
- ✅ Matching tolera variação de valor configurável (ex.: ±5%).
- ✅ `Terminate` no contrato cancela `ExpectedRecurringBill` futuros não-matched.

### Dependências

Sprints 2, 4 e 7.

---

## SPRINT 12 — `PaymentMethod` + `PaymentInstrument` na criação do Payable

**Objetivo**: fechar a lacuna identificada após a Sprint 11 — o `Payable` referenciava `PaymentMethod` mas não tinha como armazenar o **código** do pagamento (linha digitável, código de barras, EMV BR Code, PIX copia-e-cola, dados bancários). Sem esse código, o BC `PaymentExecution` não conseguiria executar nada. Esta sprint introduz o VO `PaymentInstrument` polimórfico, o decide na criação do Payable, e cobre o ciclo desde criação até aviso proativo de divergência.

### Decisões de design fixadas durante o planning

- `PaymentMethod` tem **3 valores** (não 4): `SupplierTransfer`, `DynamicPix`, `BankSlip`. TED foi unificado com PIX-cadastro em `SupplierTransfer` — o PSP (Asaas) decide entre PIX-via-DICT e TED na execução.
- `DynamicPix` unifica boleto-PIX, copia-e-cola e chave temporária — todos têm a mesma estrutura EMV BR Code.
- `SupplierBankAccount` deixou de ser Entity e virou **VO selado polimórfico** (`SupplierPixAccount` ou `SupplierBankTransferAccount`, XOR — nunca ambos juntos).
- `Supplier` ganha **coleção dupla** ativos/inativos com soft delete preservando snapshot histórico.
- Snapshot bancário no instrumento é **congelado na criação** do Payable (não relê o Supplier no momento do RequestPayment).
- Aviso de Supplier desatualizado é **B+C combinados**: integration event + handler proativo (B) + flag/evento no Payable (C). Comando idempotente — emite uma única vez ever.

### Sub-fases entregues

| Sub-fase | Escopo | Entregáveis |
|---|---|---|
| 12.0 | Refactor estrutural do Sprint 1 | `SupplierBankAccount` Entity → VO hierárquico selado; deleção de `SupplierBankAccountId`; `Supplier` com `ActiveBankAccounts`/`InactiveBankAccounts`; eventos `SupplierBankAccountAdded`/`SupplierBankAccountDeactivated` com payload polimórfico flat |
| 12.0t | Testes Sprint 1 ajustados | `SupplierMother` com factories `BankTransferAccount`/`PixAccount`; `SupplierBankAccountTests` cobre as 2 sub-variantes + igualdade estrutural cross-variantes; `SupplierTests` cobre soft delete + reativação via re-add |
| 12.A | VOs base | `PaymentMethod` Smart Enum (3 valores); `EmvPayload` (CRC16-CCITT-FALSE poly 0x1021 init 0xFFFF); `BarcodeDigits` (44 dígitos + DV mod-11; método `ToDigitableLine()` deriva a forma de 47 dígitos com DVs mod-10 nos campos 1–3); `DigitableLine` (47 dígitos — VO standalone, **não vive no `BankSlipInstrument`** por ser representação derivada do barcode); `PaymentInstrument` abstract + 4 variantes seladas (`BankSlipInstrument` carrega apenas `BarcodeDigits`); erros `AP.EMV`/`AP.BCD`/`AP.DLN` |
| 12.B | Payable factories + Eventos | `Payable.Initialize*` recebem `PaymentInstrument` (refinado em 12.G para remover `PaymentMethod` redundante); estado ganha `PaymentInstrument`; eventos `PayableCreated*` ganham 12 campos extras (serialização flat — `InstrumentKind` + 11 nullables); helper `PaymentInstrumentSerialization` faz `Expand`/`Rebuild`; erro `AP.PAY18` |
| 12.G | Remoção do `PaymentMethod` redundante | Refactor pós-12.F: factories do Payable e `InstallmentPlanFactory.Create` perdem o parâmetro `PaymentMethod`; `Payable.PaymentMethod` removido do estado (consumidores acessam via `PaymentInstrument.Method`); `PaymentMethodName` removido dos 4 eventos; `AP.PAY19` (`PaymentMethodInstrumentMismatch`) descartado e reservado — inconsistência impossível por construção |
| 12.C | RequestPayment simplificado + Resolver | `RequestPayment(occurredAt)` sem `method`/`bankAccountId`; evento `PayablePaymentRequested` perde `BankAccountId`/`Method` e ganha serialização flat do instrumento; `PaymentBankAccountId` removido do estado; Domain Service `PaymentInstrumentResolver` valida cross-aggregate; erro `AP.PAY21`; `SupplierBankAccountId.cs` zumbi deletado |
| 12.D | InstallmentPlanFactory com N instrumentos | `InstallmentPlanFactory.Create` aceita `IReadOnlyList<PaymentInstrument>` de tamanho `N`; erro `AP.IPL08` para mismatch de tamanho |
| 12.E | Detector + Flag idempotente | `Payable.FlagInstrumentOutdated(reason, occurredAt)` idempotente; estado `IsInstrumentOutdated`/`OutdatedAt`/`OutdatedReason`; evento `PayableInstrumentOutdated`; Domain Service `OutdatedInstrumentDetector`; erro `AP.PAY22` |
| 12.F | Documentação | Atualização do plano, do `CLAUDE.md` do BC, e do `Consolidado.md` (D-147 retificado, §9.11.4 atualizado) |

### Critério de aceite global

- ✅ Suite completa `dotnet test AccountsPayable.UnitTests` verde (721 testes ao final da 12.F).
- ✅ Domain compila 0 erros / 0 avisos (`TreatWarningsAsErrors=true`).
- ✅ Cada sub-fase termina com testes verdes antes da próxima começar.

### O que NÃO entrou nesta sprint

- ❌ Implementação do handler na Application (escuta `SupplierBankAccountDeactivated` → dispara `OutdatedInstrumentDetector` → chama `Payable.FlagInstrumentOutdated`). Backlog Application.
- ❌ Migração do `PaymentInstrument` para `PaymentExecution.Domain` quando esse BC materializar (vive em `AccountsPayable.Domain` por hora).
- ❌ Notificação ao usuário em si (email/push) — backlog Application + camada de notificação.

### Dependências

Sprints 0–11 (todas).

---

## Resumo visual — Roadmap

| Sprint | Entrega | Aggregate(s) afetado(s) | Cliente pode usar? |
|---|---|---|---|
| 0 | Fundação / SeedWork | — | Não (interno) |
| 1 | Cadastro de fornecedor | `Supplier` (refatorado em 12.0 — `SupplierBankAccount` virou VO selado) | ✅ Sim — substitui Excel de fornecedores |
| 2 | `Payable` manual (ciclo completo) | `Payable` (A+ES) | ✅ Sim — substitui controle em planilha de contas |
| 3 | Plano de contas + centros de custo | `ChartOfAccounts`, `CostCenter` | ✅ Sim — config; pode mapear como categorias hoje |
| 4 | Classificação manual da Payable | `Payable` (expansão) | ✅ Sim — já permite relatórios por categoria |
| 5 | Aprovação manual single approver | `Payable` (expansão) | ✅ Sim — controla pagamento por sócio |
| 6 | Hooks de PaymentOrder | `Payable` (expansão) | ⚠️ Parcial — depende de `PaymentExecution` em outro BC |
| 7 | Integração com Bill Ingestion | `Payable` (expansão) | ⚠️ Parcial — depende de `BillIngestion` em outro BC |
| 8 | Parcelamento | `InstallmentPlan` | ✅ Sim — atende contas parceladas |
| 9 | Classificação automática | `ExpenseClassificationRule` | ✅ Sim — reduz trabalho manual |
| 10 | Alçadas múltiplas | `AutoApprovalPolicy` | ✅ Sim — escala aprovação |
| 11 | Contratos + recorrência | `Contract`, `ExpectedRecurringBill` | ✅ Sim — previsão de fluxo de caixa |
| 12 | `PaymentMethod` + `PaymentInstrument` na criação | `Payable` + `Supplier` + 4 VOs novos + 2 Domain Services | ✅ Sim — fecha lacuna que bloqueava PaymentExecution |

**Total estimado**: 12 a 16 semanas, dependendo do ritmo e da equipe.

---

## Princípios para o roteiro

1. **Cada sprint entrega valor isolado**. Cliente pode pagar pelo sistema na Sprint 1 mesmo sem Payable; pode pagar mais na Sprint 2; e assim por diante.
2. **`Payable` cresce gradualmente** — começa cru na Sprint 2 e ganha capacidade a cada sprint subsequente, sem nunca quebrar.
3. **Aggregates novos vêm quando o `Payable` precisa deles** (`ChartOfAccounts` antes da classificação, `AutoApprovalPolicy` antes das alçadas, etc.).
4. **Integrações entre BCs são opcionais** até as Sprints 6 e 7 — antes disso, o BC roda 100% isolado.
5. **A+ES só no `Payable`** dentro deste BC. Demais Aggregates são tradicionais. Isso reduz o atrito de adoção: tem ganho de auditoria onde importa, sem o custo onde não compensa.

---

## Ambiguidades resolvidas (para você confirmar)

1. **Tenancy**: assumi `TenantId` em todo Aggregate Root. Confirma que é multi-tenant single-DB com `TenantId` como discriminator?
2. **Currency**: assumi suporte multi-moeda no VO `Money` desde a Sprint 2 (mesmo que MVP só use BRL). Quer fixar BRL e simplificar?
3. **`PaymentOrder`**: propus colocar em `PaymentExecution.Domain` (outro BC). Confirma a separação, ou prefere manter em `AccountsPayable.Domain`?
4. **Aprovação rejeitada**: status `Rejected` é terminal (cancela) ou permite reabrir voltando pra `Draft`? Propus terminal por simplicidade.
5. **Sigla**: usei `AP` (Accounts Payable) como prefixo de IDs de erro (`AP.PAY01`, `AP.PCL##`, etc.). Pode confirmar?
6. **Parcelamento (Sprint 8)**: cada parcela vira um `Payable` próprio com sua identidade e seu stream A+ES — confirma que é o que quer? Alternativa seria parcelas como Entities internas, o que conflitaria com a granularidade da A+ES.

Se quiser, na próxima rodada posso **detalhar uma sprint específica** com markdown completo de modelagem (estilo `references/markdown-format.md`) já no formato que a skill de codegen lê pra gerar o código direto.
