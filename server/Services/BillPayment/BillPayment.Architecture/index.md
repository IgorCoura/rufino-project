# BillPayment.Architecture — índice

Design rationale do Bounded Context **BillPayment**: captura de boletos (e-mail e sites), verificação, autorização, agendamento, pagamento e relatórios.

**Estes documentos são a fonte de verdade do design — leia antes de modelar.** Todo documento novo de arquitetura deve ser registrado nesta tabela.

## Documentos

| Documento | Descrição |
|---|---|
| [`01-context-and-vision.md`](01-context-and-vision.md) | Problema, missão do BC, escopo (dentro/fora), premissas de negócio e linguagem ubíqua |
| [`02-domain-model.md`](02-domain-model.md) | Aggregates, Value Objects, máquina de estados do `Bill`, eventos, invariantes, Domain Services, portas e prefixos de erro |
| [`03-bill-validation.md`](03-bill-validation.md) | As doze verificações em detalhe: fonte, expectativa, severidade, resultados e cobertura de teste exigida |
| [`04-integrations.md`](04-integrations.md) | Asaas (consulta, pagamento, webhooks), Microsoft Graph, Gmail API, extração de PDF, storage, portais, IA e segredos |
| [`05-use-cases.md`](05-use-cases.md) | Casos de uso por fase, contratos de API, recursos de autorização e o que deliberadamente não tem endpoint |
| [`06-roadmap.md`](06-roadmap.md) | Fases e sprints, critérios de pronto, riscos e o racional do sequenciamento |
| [`07-multitenancy-and-routing.md`](07-multitenancy-and-routing.md) | Tenant PF/PJ, isolamento e suas três exceções, fontes compartilhadas, escada de roteamento, quarentena, subcontas Asaas |
| [`08-boleto-corpus-findings.md`](08-boleto-corpus-findings.md) | Medição de 39 boletos reais: taxa de extração, OCR obrigatório, arrecadação como 45% do volume, ambiguidade do fator de vencimento, e a presença do pagador — **leia o Achado 5 com o denominador certo** (38% de todos os arquivos, 93,3% dos que têm linha válida) |
| [`09-capture-channels.md`](09-capture-channels.md) | PDF com senha (e a senha como prova de propriedade), link com navegação e seus controles de segurança, portais, e a cascata de extração |
| [`10-llm-extraction.md`](10-llm-extraction.md) | Extração por IA: porta agnóstica, adapter Gemini, structured outputs, custo por documento, Batch API, guardrails e métricas |
| [`11-bill-expectations.md`](11-bill-expectations.md) | Expectativa de boleto e lembretes: o que o tenant espera receber, ciclos, escalonamento e defesa contra falso positivo |
| [`tools/`](tools/) | **Medição:** `analyze-boleto-corpus.js` (extração sobre um corpus), `analyze-account-reference.js` (estabilidade da referência de conta entre meses **e entre pagadores** — sustenta o abandono da `RoutingRule` na 2.6 e a chave da expectativa na 2.7). **Sondas read-only:** `probe-asaas-simulate.js`, `smoke-probe-production.js`, `smoke-probe-pix-decode.js` (consulta oficial), `smoke-probe-mailbox.js` (leitura de caixa no Graph). **Operação:** `run-capture-chain.js` (ensaio da cadeia de captura ponta a ponta), `seed-tenant.js` (repovoa o cadastro de um tenant pela API), `fetch-bacen-participants.js` (atualiza a tabela de bancos do Bacen) |
| [`12-official-lookup-coverage.md`](12-official-lookup-coverage.md) | **Medição** da cobertura do `bill/simulate` por tipo de documento: o que cada check tem de dado, o que é estrutural e o que ficou por validar em produção |
| [`13-dead-letter-replay.md`](13-dead-letter-replay.md) | **Operação:** replay da dead-letter do outbox por SQL — identificar, corrigir a causa, reemitir com segurança (idempotência handler a handler) e conferir o efeito |

## ADRs

| ADR | Decisão |
|---|---|
| [`ADR-001`](adr/ADR-001-asaas-como-provedor.md) | Asaas como provedor de consulta **e** de pagamento, atrás de duas portas separadas |
| [`ADR-002`](adr/ADR-002-bill-e-paymentorder-separados.md) | `Bill` e `PaymentOrder` são Aggregates distintos; a ordem é a fonte de verdade da execução |
| [`ADR-003`](adr/ADR-003-checks-materializados.md) | Verificação é entidade com evidência e quatro resultados, não booleano |
| [`ADR-004`](adr/ADR-004-pagador-nao-autoritativo.md) | O pagador não é autoritativo, mas bloqueia quando contradiz; assimetria deliberada |
| [`ADR-005`](adr/ADR-005-confianca-de-origem.md) | Confiança é do remetente, não da caixa; allowlist por tenant com promoção pelo usuário |
| [`ADR-006`](adr/ADR-006-captura-email-oauth.md) | Microsoft Graph + Gmail API em vez de IMAP genérico |
| [`ADR-007`](adr/ADR-007-aprovacao-humana-obrigatoria.md) | Nenhum pagamento sem `UserId` autorizando; auto-aprovação adiada com condições escritas |
| [`ADR-008`](adr/ADR-008-fontes-compartilhadas-e-isolamento.md) | Fonte compartilhada = uma `CaptureSource` por tenant; isolamento por construção com três exceções |
| [`ADR-009`](adr/ADR-009-cofre-de-segredos.md) | Sem cofre por ora: env vars + `secrets.json`; envelope encryption no Postgres permanece |
| [`ADR-010`](adr/ADR-010-pix-preferido-sobre-boleto.md) | QR Pix é o trilho preferencial; código de barras é fallback; divergência entre os dois bloqueia |
| [`ADR-011`](adr/ADR-011-llm-propoe-codigo-dispoe.md) | LLM extrai candidatos; DV + consulta oficial decidem. LLM nunca toca dinheiro |
| [`ADR-012`](adr/ADR-012-portais-reduzir-residuo.md) | **DDA está fora.** Fatura digital → débito automático → integração oficial → automação assistida. Sem evasão de anti-bot |
| [`ADR-013`](adr/ADR-013-gemini-atras-de-porta-agnostica.md) | Gemini como provedor, atrás de porta agnóstica; nenhum termo de IA cruza a fronteira do BC |
| [`ADR-014`](adr/ADR-014-expectativa-e-lembretes.md) | O sistema sabe o que espera receber e avisa quando não recebeu — rede de segurança obrigatória sem DDA |
| [`ADR-015`](adr/ADR-015-risco-classificado-humano-decide.md) | Fim da rejeição automática: a validação classifica Seguro/Atenção/Perigo, e quem decide é sempre o humano — Perigo exige aceite explícito gravado na trilha |
| [`ADR-016`](adr/ADR-016-conta-asaas-trazida-pelo-tenant.md) | A conta Asaas é do tenant, trazida e provada por ele; sem chave-plataforma, sem fallback — webhook, saldo e whitelist passam a ser por conta |
| [`ADR-017`](adr/ADR-017-politica-inicial-de-agendamento.md) | Política inicial de agendamento: 24h de antecedência, submissão só das 9h às 17h, e boleto vencido exige confirmação explícita gravada na trilha |

## Como ler

Começando do zero: `01` → `02` → `07` → `03`. Para implementar uma sprint: `06` para o escopo, `02`/`07`/`11` para o modelo, `03` para as verificações, `09` para o canal de entrada, `05` para o contrato de API, `04`/`10` para a integração envolvida. `08` é a medição de campo que sustenta várias decisões — leia antes de mexer no parser ou no roteamento. Os ADRs explicam **por que** cada decisão é assim; consulte antes de propor mudança estrutural.
