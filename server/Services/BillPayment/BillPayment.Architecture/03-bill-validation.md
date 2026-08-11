# 03 — Verificação do boleto

Este é o coração do BC: provar que o boleto é legítimo **antes** de qualquer autorização. Cada verificação é um `BillCheck` materializado no Aggregate — com resultado, severidade, código de motivo e evidência — e não um booleano volátil. Racional em [`adr/ADR-003-checks-materializados.md`](adr/ADR-003-checks-materializados.md).

## Fontes de dado e o que cada uma prova

| Fonte | Confiabilidade | O que entrega |
|---|---|---|
| **Linha digitável / código de barras** | Alta para *estrutura*, nenhuma para *identidade* | Dígitos verificadores, banco emissor, moeda, valor, fator de vencimento. É autoconsistente mas totalmente forjável — um fraudador emite um boleto real da conta dele |
| **Payload Pix (BR Code)** | Alta para *estrutura*, nenhuma para *identidade* | CRC16, chave, nome e cidade do recebedor, valor (só em QR dinâmico). Igualmente forjável |
| **Consulta oficial do boleto** (`POST /v3/bill/simulate`) | **Autoritativa** | `beneficiaryName`, `beneficiaryCpfCnpj`, `bank`, `value`, `dueDate`, `minValue`/`maxValue`, `allowChangeValue`, `isOverdue`, `fee`, `minimumScheduleDate` |
| **Consulta oficial do Pix** (`POST /v3/pix/qrCodes/decode`) | **Autoritativa** | `name`, `tradingName`, **`cpfCnpj` do recebedor**, `value`, `totalValue`, `interest`, `fine`, `discount`, `dueDate`, `expirationDate`, `canBePaidWithDifferentValue` |
| **PDF do documento** (texto embutido, senha derivada, visão) | Baixa | Nome e CNPJ do **pagador**, referência de conta, descrição. Único lugar onde o pagador existe. Saída de LLM entra aqui como *candidato* ([`adr/ADR-011`](adr/ADR-011-llm-propoe-codigo-dispoe.md)) |
| **Senha do PDF** | Alta para *propriedade* | Derivada do documento fiscal do pagador pelo emissor — abrir com o CNPJ do tenant é evidência forte de posse ([`09-capture-channels.md`](09-capture-channels.md)) |
| **Metadados da origem** (remetente, domínio) | Média | Sinal de procedência, não de conteúdo |
| **Cadastro do tenant** (`PayerProfile`, `Payee`, `TrustedOrigin`, `RoutingRule`) | É a expectativa contra a qual tudo é comparado | |

> **O ponto que define o desenho:** a consulta oficial devolve o **beneficiário**, mas **não devolve o pagador**. Não existe API que confirme "esse boleto foi emitido para o meu CNPJ" — o pagador impresso é informativo, e no corpus real só está presente em 38% dos boletos. É o único check que trabalha com dado não autoritativo, e ao mesmo tempo é o que sustenta o isolamento entre tenants numa caixa de e-mail compartilhada. A resolução dessa tensão — bloquear quando contradiz, não liberar quando confirma, e rotear por uma escada de cinco degraus — está em [`adr/ADR-004`](adr/ADR-004-pagador-nao-autoritativo.md) e [`07-multitenancy-and-routing.md`](07-multitenancy-and-routing.md).

## Catálogo de checks

Severidade: **`Blocking`** = falha impede a aprovação (o aprovador não consegue autorizar até o motivo ser resolvido). **`Advisory`** = falha é destacada na tela mas o aprovador pode autorizar assumindo o risco, com o motivo gravado na trilha.

| # | `CheckType` | Pergunta | Fonte × expectativa | Severidade |
|---|---|---|---|---|
| 1 | `BarcodeIntegrity` | A linha digitável é estruturalmente válida? | DV mod 10 / mod 11 dos campos e do código de barras | **Blocking** |
| 2 | `Duplicate` | Já pagamos (ou já vamos pagar) esse mesmo boleto? | `DigitableLine` × Bills ativas — **de todos os tenants** | **Blocking** |
| 3 | `LookupAvailability` | A consulta oficial respondeu? | porta `IBillLookupService` | **Blocking** |
| 4 | `LookupConsistency` | O que está impresso bate com o que o sistema bancário diz? | valor/vencimento/banco do parse offline × `LookupSnapshot` | **Blocking** |
| 5 | `PayeeMatch` | **O beneficiário condiz?** | `Lookup.BeneficiaryTaxId` × `Payee.TaxId` | **Blocking** |
| 6 | `ReceivingBankMatch` | **O banco recebedor condiz?** | `DigitableLine.BankCode` (posições 1–3) × `Payee.AcceptedBanks`, com `Lookup.BankCode` como conferência cruzada | Advisory (Blocking se as duas fontes divergirem) |
| 7 | `AmountMatch` | **O valor condiz?** | `Lookup.Amount` × `Payee.AmountPolicy` | Advisory |
| 8 | `PayerMatch` | **O pagador condiz?** | `Bill.ExtractedPayer.TaxId` × `PayerProfile` do tenant | Advisory (extraído e divergente → **Blocking**) |
| 9 | `OriginTrust` | **Veio de e-mail/site confiável?** | `Origin.SenderAddress` × `TrustedOrigin` | Advisory (`Blocked` → **Blocking**) |
| 10 | `DueDateSanity` | Dá tempo de pagar? | `Lookup.DueDate`, `IsOverdue`, `MinimumScheduleDate` × hoje | Advisory |
| 11 | `TenantRouting` | Por qual caminho este boleto foi atribuído a este tenant? | degrau da escada de roteamento | Advisory |
| 12 | `PixBarcodeConsistency` | O QR Pix e o código de barras contam a mesma história? | `PixLookupSnapshot` × `LookupSnapshot` | **Blocking** |

### Os checks valem nos dois trilhos

O catálogo foi escrito para boleto, mas **transfere quase inteiro para Pix** — porque o `decode` devolve o CPF/CNPJ do recebedor, que é o dado que sustenta a verificação principal. Diferenças por trilho:

| Check | No trilho Pix |
|---|---|
| `BarcodeIntegrity` | vira validação do **CRC16** do payload BR Code |
| `LookupAvailability` | `POST /v3/pix/qrCodes/decode` em vez de `/bill/simulate` |
| `LookupConsistency` | compara o parse offline do payload contra o `PixLookupSnapshot` |
| `PayeeMatch` | `PixLookup.ReceiverTaxId` × `Payee.TaxId` — **idêntico em força** |
| `ReceivingBankMatch` | mais forte: o PSP recebedor é dado do payload, não inferência sobre campo livre |
| `AmountMatch` | usa `totalValue` (com juros/multa/desconto). QR **estático** não carrega valor → depende inteiramente da `AmountPolicy` do `Payee` |
| `DueDateSanity` | acrescenta `expirationDate` — Pix dinâmico que expira antes da data de agendamento falha com `pix_expires_before_schedule`, situação sem equivalente no boleto |
| `PayerMatch`, `OriginTrust`, `Duplicate`, `TenantRouting` | inalterados — não dependem do trilho |

### 1. `BarcodeIntegrity`

Valida os dígitos verificadores. Cobrança bancária (47 dígitos): mod 10 nos três primeiros campos + mod 11 no DV geral do código de barras (posição 5). Arrecadação/concessionária (48 dígitos, inicia em `8`): quatro blocos de 12, com mod 10 **ou** mod 11 conforme o 3º dígito (identificador de valor efetivo/referência).

- Falha → `Outcome=Failed`, Bill vai direto a `Rejected`. Linha digitada errada é o caso comum; boleto adulterado sem recalcular DV é o caso raro e grave.
- Este check roda em `Bill.Capture` (invariante `BLP.BIL01`) e é registrado como `Passed` no conjunto de checks para a auditoria ficar completa.

### 2. `Duplicate`

Já existe Bill com a mesma `DigitableLine` em status ≠ `Cancelled`/`Denied`? Falha → `Rejected` com a evidência.

**A busca é global, não por tenant.** Um boleto é pago uma vez, e uma caixa de e-mail compartilhada entre tenants torna a colisão provável. Quando a Bill existente é de outro tenant, a evidência é o aviso genérico *"este boleto já está sob gestão de outra conta do sistema"* — sem id, sem nome, sem valor. Quando é do mesmo tenant, a evidência traz o id da Bill original. É uma das três travessias de tenant autorizadas ([`adr/ADR-008`](adr/ADR-008-fontes-compartilhadas-e-isolamento.md)).

Motivo de ser bloqueante: pagamento duplicado é irreversível na prática. O caminho legítimo (segunda via reenviada por e-mail) resolve sozinho, porque a segunda via de um boleto **não pago** tem a mesma linha digitável — o item de captura vira `Discarded` apontando para a Bill existente, e o usuário não vê ruído.

### 3. `LookupAvailability`

A consulta é obrigatória. Se o provedor está fora do ar ou a linha não é consultável, o check fica `Failed` (bloqueante) com retentativa automática — o Bill fica visível em `Rejected` com motivo `lookup_unavailable` e um botão de revalidar. **Nunca** cai para "aprova sem consulta".

### 4. `LookupConsistency`

Compara o que dá para ler offline com o que o sistema bancário devolveu:

| Campo | Comparação | Falha significa |
|---|---|---|
| Banco | `DigitableLine.BankCode` × `Lookup.BankCode` | Divergência estrutural grave |
| Valor | `DigitableLine.Amount` × `Lookup.OriginalAmount` | Valor embutido ≠ valor registrado |
| Vencimento | fator de vencimento × `Lookup.DueDate` | Erro de parser ou boleto atípico |

Tolerância: nenhuma para banco; centavos exatos para valor original; ±1 dia para vencimento (fuso/arredondamento). Boleto com valor em aberto (`allowChangeValue = true`, típico de arrecadação) **pula** a comparação de valor com `Skipped` e o motivo registrado — não é falha.

### 5. `PayeeMatch` — o beneficiário condiz?

> **Correção da sprint 1.4, e ela é substantiva.** A primeira implementação tentava documento, e **caía para nome quando o documento não casava com cadastro nenhum**. Isso transformava o pior cenário no melhor: consulta devolvendo "PADARIA SAO JOSE LTDA" com CNPJ de terceiro casava por nome e virava `Passed` com `matched_by_name_only`. A regra correta, e a que está no código: **quando a consulta traz documento, o documento decide sozinho** — não casou, o cotejo por nome vira detecção de sósia, nunca confirmação. O fallback por nome só existe quando a consulta **não trouxe documento**, que é o caso de 100% da arrecadação. Um teste unitário cobre exatamente isso (`Resolve_WithAKnownNameButAnotherTaxId_ShouldFlagLookalike`).

1. `PayeeResolutionService` resolve `Lookup.BeneficiaryTaxId` → `Payee` do tenant.
2. Sem `Payee` com esse TaxId → `Inconclusive`, motivo `payee_not_registered`. A tela oferece "cadastrar como beneficiário" na aprovação, e a partir do próximo boleto o check passa. **Não é falha** — beneficiário novo é rotina.
3. Com `Payee`: TaxId igual → `Passed`. TaxId igual mas `Payee` inativo → `Failed` (`payee_inactive`).
4. Nome muito diferente do cadastro e dos aliases, com TaxId igual → `Passed` com evidência da divergência de nome + oferta de aprender o alias. Razão social muda; CNPJ não.
5. TaxId diferente de todos os cadastros mas nome parecido com um cadastrado (distância de edição baixa sobre nome normalizado) → `Failed` com motivo `payee_lookalike`. **Este é o cenário de fraude de boleto** e é o que justifica a severidade bloqueante do check.

#### Arrecadação não tem documento — o check degrada para nome

**Medido:** a consulta oficial devolve `beneficiaryCpfCnpj` nulo em **100%** dos boletos de arrecadação, e `companyName` preenchido em 100% ([doc 12](12-official-lookup-coverage.md)). Parte disso é estrutural: o código de barras de arrecadação carrega identificador de convênio, não CNPJ.

Então os passos 1 a 5 acima, que giram em torno do TaxId, **não se aplicam a `BillKind.Utility`**. O que resta:

- Sem `Payee` cujo `MatchesName` case com `companyName`/`beneficiaryName` → `Inconclusive`, motivo `payee_not_registered`, mesmo fluxo de "cadastrar como beneficiário".
- Casou por nome → **`Passed` rebaixado**: a evidência registra `matched_by_name_only`, e a tela de aprovação precisa mostrar isso como **verificação parcial**, não como o mesmo "verificado" da cobrança bancária. Nome é falsificável; documento não é.
- **Nome do cadastro diverge do nome que voltou na consulta** → **`Warning`** (decisão do usuário, 2026-07-31), motivo `payee_name_divergence`, com oferta de aprender o alias. Não bloqueia — grafia de concessionária varia muito ("SABESP" × "CIA SANEAMENTO BASICO EST SP") — mas é a **única** evidência de beneficiário que arrecadação oferece, e silenciá-la jogaria fora o pouco que se tem.
- Casou por nome com `Payee` inativo → `Failed` (`payee_inactive`), igual ao caminho por documento.

**Severidade permanece Blocking**, mas um `Passed` por nome nunca sustenta sozinho a aprovação — ele entra junto com valor conclusivo (100% em arrecadação), expectativa ([doc 11](11-bill-expectations.md)) e origem confiável. Ver "Decisões em aberto" no doc 12.

### 6. `ReceivingBankMatch` — o banco recebedor condiz?

**A fonte é o próprio código de barras, não a consulta.** Em cobrança bancária, as **posições 1–3 do código de barras são o código COMPE do banco** — o mesmo campo que determina onde o título liquida. `DigitableLine.BankCode` é a fonte de verdade; `Lookup.BankCode` serve só como conferência cruzada.

Isso importa por três razões:

1. **Não depende do provedor.** O check funciona mesmo se a consulta oficial falhar, estiver fora do ar ou não devolver `bank` — e hoje o caminho de cobrança da consulta está [sem validação](12-official-lookup-coverage.md).
2. **É protegido por DV.** O dígito geral (posição 5, módulo 11 sobre os outros 43) cobre as posições 1–3: **mexer no banco invalida o código de barras**. Recomputar o DV é possível, mas aí o título não existe no registro daquele banco e a consulta oficial não o encontra.
3. **É para onde o dinheiro vai de verdade.** É exatamente o campo que a fraude clássica de boleto precisa trocar — manter o nome do beneficiário e redirecionar a liquidação.

Comparação: `DigitableLine.BankCode` ∈ `Payee.AcceptedBanks`.

- `AcceptedBanks` vazio → `Inconclusive` (`bank_expectation_not_set`), com oferta de aprender o banco na aprovação.
- Fora da lista → `Failed`. Advisory, porque troca de banco por fornecedor é evento legítimo e frequente; combinada com `PayeeMatch=Passed` não indica fraude, mas merece o olho do aprovador.
- **`DigitableLine.BankCode` ≠ `Lookup.BankCode`** (quando a consulta devolve banco) → `Failed` **bloqueante**. Duas fontes autoritativas discordando sobre o destino do dinheiro não é evento legítimo.
- **Trilho Pix** → a instituição vem como **ISPB de 8 dígitos**, não COMPE de 3. `IBankDirectory.FromIspb` faz a tradução a partir da relação de participantes do STR publicada pelo Bacen, e aí a comparação contra `Payee.AcceptedBanks` é a mesma dos dois trilhos. ISPB sem correspondência de três dígitos → `Inconclusive` (`ispb_without_compe_code`), nunca `Failed`: instituição só de Pix é legítima.
- **Banco desconhecido no diretório do Bacen** → `Failed`. Um código de três dígitos que não corresponde a instituição nenhuma denuncia código de barras fabricado — é o segundo filtro de plausibilidade que o [doc 08](08-boleto-corpus-findings.md) pediu, depois do guard de banco não atribuído que já vive no VO.
- **Banco existe mas não participa da Compe** → `Warning`. Boleto liquida pela Compe; um emissor fora dela é anomalia que merece o olho do aprovador sem bloquear, porque a tabela do Bacen pode estar mais velha que a realidade.
- `BillKind.Utility` → `Skipped`, motivo `bank_not_available_for_utility`. **Não é escolha de desenho, é ausência de dado.** O código de barras de arrecadação não tem campo de banco em posição nenhuma — as posições 1–3 são produto (`8`), segmento e identificador de valor —, e a consulta devolve `bank` nulo em 100% dos casos medidos. Contas de convênio liquidam fora da compensação bancária tradicional. Trocar de provedor não muda isso.

### 7. `AmountMatch` — o valor condiz?

`Lookup.Amount` (valor a pagar hoje, já com juros/multa/desconto) × `Payee.AmountPolicy`:

| Política | Passa quando |
|---|---|
| `Fixed(valor, tolerância%)` | \|valor − esperado\| ≤ tolerância |
| `Range(min, max)` | min ≤ valor ≤ max |
| `Unbounded` | sempre — resultado é `Inconclusive`, não `Passed` (nada foi provado) |

A evidência registra **valor original × valor atualizado × juros/multa**, para o aprovador distinguir "cobraram a mais" de "está vencido e acumulou encargos". Um segundo teto entra junto: `Lookup.Amount` acima da alçada do aprovador → `Failed` bloqueante (`above_approval_limit`), independente do `Payee`.

### 8. `PayerMatch` — o pagador condiz?

Compara o TaxId do pagador extraído do PDF (com DV validado) contra o `PayerProfile` do tenant: `PrimaryTaxId`, `AdditionalTaxIds`, e a raiz do CNPJ quando `MatchByCnpjRoot` está ligado.

| Situação | Outcome | Severidade | Motivo |
|---|---|---|---|
| Extraído e casa | `Passed` | — | — |
| Extraído e **não** casa | `Failed` | **Blocking** | `payer_mismatch` |
| Não extraível | `Inconclusive` | Advisory | `payer_not_extractable` |
| `PayerProfile` sem TaxId cadastrado | `Skipped` | — | `payer_profile_missing` |

**A assimetria é o ponto do check**: presença de contradição bloqueia, ausência de confirmação não libera. Um `Passed` aqui não prova propriedade — prova só que nada contradisse, num dado que ninguém certifica. Um `Failed` é evidência suficiente de que o boleto é de outra pessoa, e é o que garante o requisito de que **um usuário não pague a conta de outro**.

`Inconclusive` é o caso majoritário **por medição**: o CNPJ do pagador aparece em apenas 38% dos boletos reais ([`08-boleto-corpus-findings.md`](08-boleto-corpus-findings.md)). Justamente as contas recorrentes de concessionária identificam o pagador por conta contrato ou matrícula, não por documento fiscal — é por isso que existe a escada de roteamento, e é por isso que este check não pode ser a única defesa.

Racional completo em [`adr/ADR-004`](adr/ADR-004-pagador-nao-autoritativo.md).

### 9. `OriginTrust` — a origem é confiável?

Resolve `Origin.SenderAddress` (ou o domínio do portal) contra `TrustedOrigin`, casando endereço exato antes de domínio:

| Situação | Resultado |
|---|---|
| `Trusted` | `Passed` |
| `Blocked` | `Failed` **Blocking** — origem explicitamente banida não passa |
| Sem registro | `Inconclusive` (`origin_unknown`), com ação "confiar nesta origem" na tela de aprovação |
| Upload manual por usuário autenticado | `Passed`, evidência com o `UserId` |

Sutileza que precisa estar na UI: **um remetente confiável não torna o boleto confiável** — e-mail é trivialmente falsificável no envelope e contas legítimas são comprometidas. `OriginTrust=Passed` nunca compensa `PayeeMatch=Failed`. A ordem de leitura da tela deve ser identidade do beneficiário primeiro, origem por último.

### 10. `DueDateSanity`

- Vencido (`isOverdue`) → `Failed` (`overdue`) com o valor atualizado destacado. Asaas processa boleto vencido imediatamente, sem agendamento.
- Vence hoje após o horário de corte do provedor → `Failed` (`same_day_after_cutoff`).
- `MinimumScheduleDate` posterior ao vencimento → `Failed` (`cannot_schedule_before_due`).
- Caso contrário → `Passed` com a janela de agendamento disponível na evidência.

### 11. `TenantRouting` — por que este boleto é deste tenant?

Registra o degrau da escada de roteamento que atribuiu o boleto, com a evidência correspondente ([`07-multitenancy-and-routing.md`](07-multitenancy-and-routing.md)):

| Confiança | Origem da atribuição | Outcome |
|---|---|---|
| `Strong` | TaxId do pagador extraído casou com o `PayerProfile` | `Passed` |
| `Learned` | `RoutingRule` aprendida casou por (beneficiário, referência de conta) | `Passed` |
| `Weak` | Beneficiário é exclusivo deste tenant entre os que monitoram a fonte | `Inconclusive` |
| `Claimed` | Um usuário reivindicou explicitamente o item na quarentena | `Inconclusive`, com `UserId` e instante na evidência |
| Importação manual | O usuário trouxe o boleto | `Skipped` |

Advisory sempre: o check não decide, informa. Ele existe para o aprovador saber **quanto** a atribuição foi inferida — aprovar um boleto que chegou por `Weak` é decisão diferente de aprovar um que chegou por `Strong`, e a tela precisa deixar isso visível.

### 12. `PixBarcodeConsistency` — o QR e o código de barras concordam?

Só roda quando o documento traz **os dois** instrumentos. Compara as duas consultas oficiais:

| Campo | Comparação | Divergência significa |
|---|---|---|
| Beneficiário | `PixLookup.ReceiverTaxId` × `Lookup.BeneficiaryTaxId` | **QR colado por cima do documento legítimo** |
| Valor | `PixLookup.Value` × `Lookup.OriginalAmount` | valor adulterado em um dos trilhos |
| Vencimento | `PixLookup.DueDate` × `Lookup.DueDate` | ±1 dia de tolerância |

Divergência de beneficiário ou de valor → `Failed` bloqueante, Bill vai a `Rejected`. **Nunca "escolhe um e segue"** — divergência entre trilhos é exatamente o que a fraude produz, e o documento inteiro passa a ser suspeito.

É a defesa mais barata do catálogo: duas consultas que o sistema já faz, comparadas entre si. E é a única que pega o vetor mais direto em circulação hoje — QR Pix adulterado sobre boleto verdadeiro. Racional em [`adr/ADR-010`](adr/ADR-010-pix-preferido-sobre-boleto.md).

QR estático (sem valor) → compara só o beneficiário; o resto sai `Skipped` com motivo.

## Como a validação roda

```
BillCapturedDomainEvent
  → (outbox handler) ValidateBillCommand
      1. carrega Bill (tracked, por TenantId)
      2. para CADA instrumento presente:                        ← I/O de orquestração
           Barcode → IBillLookupService.SimulateAsync(...)
           PixQr   → IPixLookupService.DecodeAsync(...)
      3. bill.AttachLookups(snapshots)   ← os dois, quando houver os dois
      4. PayeeResolutionService → PayeeId?
      5. bill.ResolvePayee(payeeId)
      6. carrega Payee + TrustedOrigin + PayerProfile do tenant
      7. BillValidationService.Evaluate(bill, payee, origin, payerProfile, today)
           → IReadOnlyCollection<CheckResult>          ← valores, não entidades
      8. bill.RecordChecks(results)                    ← o Aggregate decide o status
      9. SaveEntitiesAsync
```

Regras que o handler **não** pode quebrar (doutrina do `CLAUDE.md`):

- Não decide status: quem transiciona é `RecordChecks`.
- Não compõe VO: `LookupSnapshot` é montado pelo adapter da porta, não pelo handler.
- Não inspeciona `bill.Checks` para decidir nada — se precisar do resumo, `RecordChecks` retorna.
- Não define erro próprio: as factories vivem no Domain.

## Revalidação

O `LookupSnapshot` envelhece: valor de boleto vencido muda todo dia. Regras:

- Snapshot com mais de **N horas** (config, default 12) na hora da aprovação → a aprovação é recusada com `BLP.BIL06` e a UI dispara revalidação automática.
- Revalidar **substitui** o snapshot e reexecuta todos os checks. Snapshots anteriores ficam na trilha de auditoria (tabela append-only `bill_lookup_history`), nunca são sobrescritos em silêncio.
- Revalidação que muda o valor de um Bill já `Approved` mas ainda não `Scheduled` derruba a aprovação de volta para `AwaitingApproval` — mudança de valor invalida o consentimento dado.

## Matriz de decisão

| Situação | Status resultante |
|---|---|
| Algum `Blocking` = `Failed` | `Rejected` |
| Nenhum `Blocking` falhou, algum `Advisory` falhou ou está `Inconclusive` | `AwaitingApproval` — destacado como "requer atenção" |
| Todos `Passed` ou `Skipped` | `AwaitingApproval` — caminho limpo |

Não existe transição automática para `Approved` nesta fase. Auto-aprovação por política é decisão adiada, com condições já escritas em [`adr/ADR-007-aprovacao-humana-obrigatoria.md`](adr/ADR-007-aprovacao-humana-obrigatoria.md).

## Cobertura de teste exigida

**Unitários (Domain):** cada DV de linha digitável (cobrança e arrecadação, válidos e corrompidos em cada posição), rollover do fator de vencimento em 22/02/2025, fator `0000`, cada `AmountPolicy` no limite da tolerância, cada transição de `BillStatus` (inclusive as proibidas), `RecordChecks` decidindo status para as três linhas da matriz acima, imutabilidade do Bill `Paid`.

**Unitários (Domain Service):** cada `CheckType` nos seus resultados possíveis, com atenção ao `payee_lookalike` (TaxId diferente + nome parecido), ao `Skipped` de `allowChangeValue`, e à assimetria do `PayerMatch` (extraído-e-diverge bloqueia; não-extraível não bloqueia). `BillRoutingService`: cada degrau da escada, incluindo o caso em que o degrau 1 diz `ForeignPayer` e o degrau 3 diria o contrário — o degrau 1 vence.

**Integração:** fluxo captura → consulta (adapter falso determinístico) → validação → aprovação → persistência, com verificação do estado gravado; unicidade **global** da linha digitável sob concorrência entre dois tenants; revalidação substituindo o snapshot e preservando o histórico.

**Integração — antifraude de trilho (obrigatório):** documento híbrido cujo QR Pix aponta para um CNPJ diferente do código de barras é bloqueado por `PixBarcodeConsistency` antes de chegar à aprovação. É o teste que prova a defesa contra QR adulterado.

**Integração — extração por LLM (obrigatório):** com o `IDocumentIntelligence` de teste devolvendo uma linha digitável **alucinada**, o Bill não é criado — o candidato morre no DV ou na consulta oficial. Ver [`adr/ADR-011`](adr/ADR-011-llm-propoe-codigo-dispoe.md); é o teste que prova que o funil determinístico segura o modelo.

**Integração — isolamento multi-tenant (obrigatório):** dois tenants conectados à mesma caixa; boleto do tenant A não vira Bill do tenant B; item `ForeignPayer` não expõe valor nem beneficiário na projeção; reivindicação por tenant errado é recusada quando o pagador contradiz; segunda reivindicação do mesmo item recebe aviso genérico sem identificar o primeiro. Este bloco é o que prova o requisito central de "um usuário não paga a conta de outro" — nenhuma sprint que toque roteamento fecha sem ele verde.
