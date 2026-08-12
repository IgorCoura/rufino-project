# 06 — Roadmap de fases e sprints

Ponto de partida: **Fase 0 concluída** (Walking Skeleton — quatro camadas compilando, outbox, idempotência, mediator, analyzers; zero Aggregates de negócio).

## Sequenciamento e por que ele é assim

O pedido original define a ordem funcional: coletar → verificar → autorizar → agendar → pagar → reportar. O roadmap **não** segue essa ordem, e a razão é uma só: **o núcleo de valor e de risco é a verificação, não a coleta.**

- A verificação é o que diferencia o produto e o que precisa estar certo antes de qualquer dinheiro se mover. Ela é testável ponta a ponta com boletos importados à mão, sem depender de OAuth de duas plataformas.
- A captura por e-mail (Graph + Gmail) é integração de plataforma: consome tempo, tem ciclo de aprovação de app e permissão de tenant, e alimenta uma porta de ingestão que **não muda** por causa dela. Adiá-la em uma fase não custa retrabalho.
- Pagar de verdade é o passo irreversível. Ele entra depois que a verificação está coberta por testes.

Por isso: **Fase 1 = verificação e aprovação; Fase 2 = captura por e-mail; Fase 3 = pagamento.** Se a prioridade for demonstrar a captura automática antes, dá para trocar 1 e 2 de ordem sem alterar o modelo — o custo é validar a verificação mais tarde, com dinheiro mais perto.

---

## Fase 1 — Núcleo de verificação e aprovação

**Entrega:** importar um boleto, consultar oficialmente, rodar as doze checagens, aprovar ou recusar com evidência. **Sem movimentar dinheiro** — a consulta do Asaas é read-only, então essa fase pode ir a produção sem risco financeiro.

| Sprint | Escopo | Saída |
|---|---|---|
| **1.0** ✅ | **Investigação, sem código:** cobertura do `POST /v3/bill/simulate` para arrecadação (DARF, DAS, SABESP, DAE, EDP — amostras reais no corpus) | **Concluída em 2026-07-31** — plano B acionado para arrecadação; ver [doc 12](12-official-lookup-coverage.md) |
| **1.1** ✅ | `PayerProfile` (PF e PJ) + `Payee` + `TrustedOrigin`: Aggregates, mappings EF, repositórios, Commands/Queries, controllers, testes | **Concluída** — cadastro fiscal e de beneficiários existe |
| **1.2** ✅ | VOs `PaymentInstrument`/`DigitableLine`/`Barcode`/`BankCode`/**`PixPayload`** + `BillKind` + `PaymentRail`; geração-e-validação de candidatos; CRC16 do BR Code; rollover do fator de vencimento; `Bill.Capture` escolhendo o trilho; `BillOrigin`; **unicidade global** por chave de instrumento; `POST /bills/import`; drain de eventos | **Concluída** — documento entra, é normalizado, trilho escolhido, outbox alimentado |
| **1.3** ✅ | Portas `IBillLookupService` + **`IPixLookupService`** + adapters Asaas (`/bill/simulate`, `/pix/qrCodes/decode`); `LookupSnapshot` + `PixLookupSnapshot` + **`LookupResult`/`LookupStatus`**; `HttpClient` + Polly de volta na Infra; `ISecretVault` com envelope encryption + master key em env var (ADR-009) | **Concluída** — consulta oficial nos dois trilhos, com credencial ausente degradando para `Unavailable` |
| **1.4** ✅ | `BillCheck`, `CheckType`/`CheckOutcome`/`CheckSeverity`; `BillValidationService` com os **doze** checks (inclui `PixBarcodeConsistency`); `PayeeResolutionService`; `Bill.RecordChecks`; handler disparado pelo outbox; revalidação; histórico de snapshots | **Concluída** — verificação completa, com o teste antifraude de trilho verde |
| **1.5** ✅ | `Approve`/`Deny`/`Cancel`; `ApprovalRecord`; alçada; expiração de snapshot; `IBillQueries.GetDetailAsync` com os checks; revalidação por endpoint | **Concluída** — fluxo de aprovação fechado. **Duas ressalvas**: a alçada é **teto único da instalação** (por pessoa depende do Keycloak, fase 6), e as "ações acessórias" são os Commands que já existem desde a 1.1 (`Payee.Register`, `AllowBank`, `TrustedOrigin.Register`) seguidos de `POST /bills/{id}/revalidate` — não houve endpoint novo combinando os dois |

> **Fase 1 concluída (2026-08-06).** As seis sprints estão entregues e cobertas por teste, e o critério de pronto que dependia de produção foi cumprido: a **sonda de fumaça da consulta de cobrança saiu verde** — `beneficiaryCpfCnpj` volta preenchido, então o check `PayeeMatch` bloqueante tem base em cobrança bancária ([doc 12](12-official-lookup-coverage.md)). Restam apenas itens do checklist pré-produção (whitelist de IP, migrações, Keycloak), que são da fase 6.

**Critérios de pronto da fase** (os três cobertos por teste de integração):

1. Um boleto legítimo e um boleto sósia (mesmo nome comercial, CNPJ diferente) entram pelo mesmo caminho e o segundo é bloqueado por `PayeeMatch` antes de chegar à aprovação.
2. Um boleto cujo PDF traz pagador de outro CNPJ é bloqueado por `PayerMatch` mesmo tendo sido importado à mão pelo próprio usuário.
3. Um documento híbrido cujo QR Pix aponta para CNPJ diferente do código de barras é bloqueado por `PixBarcodeConsistency`.

**Riscos:**

- ~~**Cobertura da consulta para arrecadação**~~ — **medido e resolvido** ([doc 12](12-official-lookup-coverage.md)). A consulta responde 100% das arrecadações, mas **sem `beneficiaryCpfCnpj` e sem `bank`**. O plano B foi acionado na forma prevista: para `BillKind.Utility` o check de beneficiário cai para **comparação por nome** contra `Payee.LegalName` + `Aliases`, e o de banco vira `Skipped` por ausência estrutural de dado. Valor segue conclusivo (100%).
- ~~**Risco: o caminho de cobrança bancária não foi validado.**~~ **Resolvido em 2026-08-06.** A sonda de produção saiu verde: a consulta devolve `beneficiaryCpfCnpj`, `beneficiaryName` e `bank` para boleto de cobrança registrado. O sandbox não resolvia nenhuma cobrança — nem a que ele mesmo emitia —, então o 0/12 era limitação de ambiente, como o experimento da sprint 1.0 já indicava. Ver [doc 12](12-official-lookup-coverage.md).
- **Risco novo: a credencial da Fase 1 pode pagar contas.** As consultas oficiais exigem a permissão de saque via API ([`adr/ADR-001`](adr/ADR-001-asaas-como-provedor.md)). Whitelist de IP no Asaas passa a ser item obrigatório do checklist pré-produção.
- Qualidade da heurística de `payee_lookalike` — calibrar limiar com dados reais, começando permissivo e apertando.
- `PayerProfile` sem cadastro deixa `PayerMatch` em `Skipped`. O onboarding precisa exigir o cadastro fiscal antes da primeira importação, senão a fase 1 entrega verificação com um check desligado.

---

## Fase 2 — Captura por e-mail

**Entrega:** boletos entram sozinhos das caixas de e-mail configuradas.

| Sprint | Escopo |
|---|---|
| **2.1** | `CaptureSource` + `CaptureItem` (com os cinco status e as duas projeções de visibilidade); fluxo OAuth com token no cofre; job de sincronização e `SyncCursor`; **aviso pós-OAuth de fonte já monitorada**; endpoints de gestão de fonte |
| **2.2** | Adapter Microsoft Graph: delta query, download de anexos, filtros de content-type e tamanho, tratamento de throttling |
| **2.3** | `IBoletoDocumentParser`: texto embutido → geração-e-validação de candidatos → **derivação de senha de PDF**; **leitura de QR Code (ZXing.NET) com validação de CRC** — degrau obrigatório, ver abaixo; `IAttachmentStorage` (S3) |
| **2.4** | `IDocumentIntelligence` (porta agnóstica) + adapter **Gemini** por HTTP direto; `responseSchema`; Batch na ingestão agendada; `NullDocumentIntelligence`; métricas de extração e de rejeição pós-validação ([`10-llm-extraction.md`](10-llm-extraction.md), [`adr/ADR-013`](adr/ADR-013-gemini-atras-de-porta-agnostica.md)) |
| **2.5** | `ILinkResolver`: allowlist, anti-SSRF, egresso isolado; escada de resolução degraus 1–3 (GET → um salto → **receita declarativa por domínio**); boleto no corpo do e-mail |
| **2.6** ✅ | **Concluída.** `BillRoutingService` (degraus 0, 1, 3 e 4 — o degrau 2 por `RoutingRule` foi **medido e abandonado**, ver [doc 07](07-multitenancy-and-routing.md)); promoção automática; quarentena; `POST /capture-items/{id}/claim`; check `TenantRouting` alimentado. Medido: o degrau 1 cobre **93,3%**, não os ~38% estimados |
| **2.7** | **`BillExpectation` + lembretes**: Aggregate, ciclos, `ExpectationMatchingService`, `ExpectationLearningService`, job diário de vigilância, escalonamento, `INotificationSender` por e-mail, painel de pendências ([`11-bill-expectations.md`](11-bill-expectations.md)) |
| **2.8** | Degrau 4 da resolução de link (agente de navegação com teto de passos/tempo/gasto) — **só se as receitas do 2.5 não bastarem**; o agente propõe receita nova, humano versiona |

**Critérios de pronto** (os três cobertos por teste de integração):

1. Caixa com dez e-mails contendo boletos, propaganda e PDFs não-boleto produz dez `CaptureItem` classificados corretamente e nenhuma Bill duplicada em reprocessamento.
2. **Isolamento**: dois tenants na mesma caixa; cada um recebe só os seus; item `ForeignPayer` não expõe valor nem beneficiário na projeção; reivindicação contraditória é recusada; segunda reivindicação recebe aviso genérico sem identificar o primeiro.
3. **Rede de segurança**: expectativa aberta cuja conta não chega gera alerta na data certa e no nível certo; conta que chega mas fica presa em `Locked`/`LinkFailed` gera alerta de *falha de captura*, com link para o item.

**Riscos:**

- **Nenhum prazo de aprovação a proteger.** O registro no Entra ID é autosserviço (o usuário é admin do tenant) e o Gmail entra por encaminhamento, sem integração ([`adr/ADR-006`](adr/ADR-006-captura-email-oauth.md)). Faça o registro na 2.1, não antes.
- **A cascata de extração não fecha sem o passo de visão**: 18% dos boletos reais não têm camada de texto. Meta ≥ 90% sobre o corpus real, calibrada em [`08-boleto-corpus-findings.md`](08-boleto-corpus-findings.md).
- **Nem sem o leitor de QR** (sprint 2.3, decisão do usuário em 2026-08-06). **Medido:** nos boletos reais o BR Code existe **só como imagem**, nunca como texto no PDF. Sem leitor, o trilho que o [`ADR-010`](adr/ADR-010-pix-preferido-sobre-boleto.md) elege como preferencial só funcionaria pedindo ao usuário escanear e colar o "Pix Copia e Cola" — o que devolve trabalho manual justamente no caminho preferido, e ainda derruba o check `PixBarcodeConsistency` nos documentos híbridos. ZXing.NET (MIT, local), com o CRC-16 do `PixPayload` como filtro. Ver [doc 09](09-capture-channels.md) → "Leitura de QR Code".
- **Fatura digital por e-mail é ação de fase 1, não de fase 5.** Cadastrar "conta por e-mail" em EDP, SABESP, ENEL, CPFL, VIVO e DAE é o único degrau que retira volume da fase 5 sem escrever conector — custo zero ([`adr/ADR-012`](adr/ADR-012-portais-reduzir-residuo.md)). Faça antes de escrever a 2.2. **Não elimina a fase 5**: sobrará portal.
- **A resolução de links (2.5) é a maior superfície de ataque do sistema.** Não entrega sem allowlist de domínio, anti-SSRF e egresso isolado — os três, não dois. E o caso comum **não** é o link direto: a maioria exige navegar até o PDF, o que é o motivo de existir a receita declarativa.
- **A 2.7 é o que torna a automação confiável.** Sem DDA, nenhum canal garante que a conta chegou; sem expectativa, a falha de captura é silenciosa e automatizar *aumenta* o risco de esquecimento. Não é feature de conforto ([`adr/ADR-014`](adr/ADR-014-expectativa-e-lembretes.md)).

---

## Fase 3 — Agendamento e pagamento

**Entrega:** o boleto aprovado é pago na data.

| Sprint | Escopo |
|---|---|
| **3.0** | **Subcontas Asaas por tenant**: `POST /v3/accounts` para PF e PJ, chave de cada subconta no cofre, `PayerProfile.AsaasAccountRef`, estado de onboarding/KYC bloqueando agendamento |
| **3.1** | `PaymentOrder`; portas `IBillPaymentGateway` + `IPixPaymentGateway`; adapters Asaas `POST /v3/bill` e `POST /v3/pix/qrCodes/pay` (ambos com agendamento) e `externalReference`; handler de `BillApprovedDomainEvent`; `Bill.LinkPaymentOrder` |
| **3.2** | `IWorkingDayCalendar` (feriados bancários) + `PaymentSchedulingService` (dia útil, corte das 14h, `minimumScheduleDate`, boleto vencido); verificação de saldo e alerta de saldo insuficiente |
| **3.3** | Webhook `BILL_*`: autenticação, idempotência por id de evento, `ApplyProviderStatus` monotônica, eventos que refletem no `Bill`; job de conciliação por polling |
| **3.4** | Cancelamento de ordem; tratamento de `FAILED`/`REFUNDED`; reabertura para nova tentativa; fila operacional de falhas e alertas |

**Critério de pronto:** em sandbox, um pagamento agendado percorre `Pending → BankProcessing → Paid` com o `Bill` refletindo cada estado, e um webhook reentregue em duplicidade não produz efeito nenhum.

**Riscos:** o irreversível mora aqui. Toda a fase roda em sandbox até haver teste de integração cobrindo idempotência de submissão (timeout na criação **não** pode gerar dois pagamentos) e ordenação de webhooks. O KYC das subcontas depende do cliente e pode atrasar o piloto — começar a 3.0 cedo.

---

## Fase 4 — Histórico e relatórios

| Sprint | Escopo |
|---|---|
| **4.1** | `IPaymentReportQueries`: histórico paginado com a trilha completa (origem → checks → aprovador → pagamento → comprovante) |
| **4.2** | Relatórios agregados (por beneficiário/mês/status, previsto × realizado, encargos evitáveis, fila de exceção, confiabilidade da captura); export CSV |
| **4.3** | Endpoint de indicadores para o painel; notificações de boleto aguardando aprovação e de vencimento próximo |

---

## Fase 5 — Captura por portais

| Sprint | Escopo |
|---|---|
| **5.0** | **Esgotar os degraus 1–3 do [`ADR-012`](adr/ADR-012-portais-reduzir-residuo.md)** — fatura digital por e-mail, débito automático, integração oficial — e **medir o resíduo** pelos ciclos de expectativa que exigiram busca manual (métrica da 2.7) |
| **5.1** | `CaptureSource` tipo `Portal` + status `RequiresReauth`; framework de conector (isolamento em processo próprio sem acesso ao banco, credenciais cifradas, receita declarativa) |
| **5.2** | Conectores para o resíduo, em **automação assistida**: perfil de navegador persistente, humano resolve login/CAPTCHA/2FA uma vez, cadência humana, sem evasão de anti-bot. Priorizar por volume medido na 5.0 |
| **5.3** | Detecção de quebra (zero resultados por N execuções = falha), desativação automática, alerta, fluxo de reautenticação |

Fase por último, mas **com peso real**: sem DDA, sobrará portal mesmo migrando o máximo para e-mail. A diferença que a 5.0 faz é escrever conector **para o que sobrou de fato**, medido, em vez de para o que se imagina.

---

## Fase 6 — Endurecimento para produção

Puxa o "Checklist pré-produção" do `CLAUDE.md` do BC, mais o que este BC acrescenta:

- Migrações EF Core substituindo `EnsureCreatedAsync` (aplicação e testes).
- Keycloak: JWT Bearer, `TenantAuthorizationFilter` validando `{tenantId}` da rota contra o token, `[ProtectedResource]` nos endpoints com os recursos de [`05-use-cases.md`](05-use-cases.md).
- **Alçada e segregação de funções**: teto por usuário; avaliar exigir aprovador diferente de quem importou.
- Auto-aprovação por política, se aprovada — condições em [`adr/ADR-007-aprovacao-humana-obrigatoria.md`](adr/ADR-007-aprovacao-humana-obrigatoria.md).
- Observabilidade: log estruturado com correlation id, métricas de backlog do outbox, alerta de falha de sincronização e de pagamento.
- CI com `TreatWarningsAsErrors` em todos os projetos + as duas suítes.
- Rotação de segredos e revisão de acesso ao cofre.

---

## Resumo

| Fase | Entrega | Move dinheiro? |
|---|---|---|
| 1 | Verificação e aprovação de boletos importados | Não |
| 2 | Captura automática por e-mail | Não |
| 3 | Agendamento e pagamento | **Sim** |
| 4 | Histórico e relatórios | Não |
| 5 | Captura por portais | Não |
| 6 | Endurecimento para produção | — |
