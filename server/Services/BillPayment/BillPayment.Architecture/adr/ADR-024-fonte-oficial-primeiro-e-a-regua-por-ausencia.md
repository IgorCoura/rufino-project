# ADR-024 — A fonte oficial vem primeiro, e a régua passa a distinguir que tipo de ausência é

**Data:** 2026-09-10
**Status:** Aceito
**Substitui parcialmente:** [ADR-020](ADR-020-expectativa-na-validacao-e-a-regua-endurecida.md) (a régua de risco)
**Estende:** [ADR-004](ADR-004-pagador-nao-autoritativo.md), [ADR-011](ADR-011-ia-extrai-dv-decide.md), [ADR-015](ADR-015-risco-classificado-humano-decide.md)

## Contexto

Um DAS do Simples Nacional chegou classificado como Perigo com duas verificações reprovando, e o
relato do usuário foi direto: *"o sistema está confundindo os dados do pagador como se fosse do
beneficiário"*. O diagnóstico achou três defeitos independentes (registrados na seção de
2026-09-10 do `CLAUDE.md`), e ao procurar o padrão deles apareceu o que este ADR decide.

O padrão é este: **em quatro verificações o sistema preferia o dado do documento ao dado da
consulta oficial, ou descartava a fonte que respondeu porque a outra não respondeu.**

| Onde | O que fazia |
|---|---|
| Check 4 `LookupConsistency` | Com código de barras presente e consulta do boleto indisponível, saía `Skipped` — o ramo do Pix era inalcançável, e é justamente em arrecadação que o boleto não resolve e o Pix resolve |
| Check 6 `ReceivingBankMatch` | Confrontava com o cadastro o banco do código de barras mesmo num documento que liquida por Pix — um banco que não vai receber nada |
| Check 8 `PayerMatch` | Consultava o CNPJ inferido do PDF **antes** do pagador que o decode do Pix devolve, que é fonte oficial e, medido no doc 12, vem completo |
| Check 10 `DueDateSanity` | Refazia a precedência de vencimento à mão, sempre pelo boleto primeiro e pulando a linha digitável — e a verificação 14 lia outra data no mesmo boleto |

Ao mesmo tempo, a régua do ADR-020 tinha um efeito não previsto. Ela promoveu a Perigo todo
`Inconclusive` que não fosse `Notice`, e o resultado é que um boleto **cujos dados oficiais batiam
com o cadastro** ainda assim saía Perigo, porque o beneficiário não tinha bancos aceitos
cadastrados, ou não tinha política de valor. Isso é exatamente o que o ADR-003 mandou evitar: se
quase tudo é Perigo, "assumo o risco" vira rotina.

E havia um terceiro assunto, apontado pelo usuário como preocupação de segurança: **consulta
oficial sem resposta valia Perigo, o mesmo peso de "o banco do beneficiário não confere"**. Quem
conseguisse derrubar ou saturar a consulta ganharia a janela em que o boleto não pode ser
conferido — e ela custaria tão pouco quanto qualquer outra divergência.

## Decisão

### D1 — Para decidir contra o cadastro, vale a consulta oficial do trilho que paga

Os checks fazem **três coisas diferentes** com os dados, e a regra só vale para a primeira:

| Função | Checks | Regra |
|---|---|---|
| **Decidir contra o cadastro** | 5, 6, 7, 14 | Oficial do trilho que paga → outro trilho → **nunca** o documento |
| **Confrontar fonte contra fonte** | 4, 12, 13 | O documento entra por definição — confrontar é o objetivo |
| **Preencher lacuna** | 10 | Oficial → linha digitável (protegida por DV) → leitura por IA, com a procedência na evidência |

Sem essa distinção, "use sempre o oficial" e "o check 13 compara documento com oficial" se
contradizem. Com ela, os quatro defeitos acima são o mesmo defeito.

O vencimento passa a sair de `Bill.DueDate` — o consolidado pelo agregado — em vez de ser
recalculado por cada consumidor. `Bill.DueDateOrigin` diz a procedência, para a evidência
continuar dizendo em quem se está confiando.

### D2 — Consulta oficial sem resposta é Extremo Perigo

`CheckType.LookupAvailability` passa de `Blocking` para `CheckSeverity.Critical`. Sem resposta
ninguém confirmou quem recebe, quanto e quando; ausência de confirmação não é o mesmo que ausência
de suspeita, e deixá-la em Perigo tornaria a indisponibilidade um caminho barato para aprovar o
que não pode ser conferido.

`CheckSeverity.Critical` passa a ter **duas famílias**: a declaração explícita do tenant
(blacklist, origem bloqueada) e a ausência total de verificação. O que as une é o que exigem de
quem aprova; o que as separa é o remédio — e por isso o **motivo** importa tanto quanto a
severidade. São três, com ações diferentes:

| Motivo | Significa | O que resolve |
|---|---|---|
| `lookup_unavailable` | provedor fora do ar, timeout | esperar — e a varredura (D4) espera sozinha |
| `lookup_unresolved` | o provedor respondeu que não conhece o título | nada; é o sinal mais forte de fabricação que o sistema tem |
| `lookup_not_configured` | o tenant não vinculou a chave do provedor | vincular a conta |

O banner de Extremo Perigo no cliente foi reescrito: ele afirmava que o boleto estava na lista de
bloqueio, o que passaria a ser falso na maioria dos casos.

### D3 — A régua distingue ausência de CADASTRO de ausência de IDENTIDADE

Teto de Atenção (`CheckSeverity.Notice`) para o que é apenas cadastro incompleto:
`bank_expectation_not_set`, `amount_policy_unbounded`, `routing_inferred`, e os desfechos do
check 13 em que não havia campo oficial para confrontar.

**Continuam pesando Perigo** `payee_not_registered` e `payer_not_extractable`. A fronteira é: não
ter declarado bancos aceitos é o tenant não ter configurado nada; não saber quem é o beneficiário
ou de quem é o boleto é o sistema não ter provado nada.

⚠️ **`payee_not_registered` pesar Perigo é invariante desta decisão, não detalhe.** É ele que
segura o boleto adulterado de fornecedor novo depois de D5 — as duas regras são individualmente
defensáveis e, se ele cair para Atenção, juntas abrem a porta.

### D4 — A ausência de consulta se resolve sozinha

`BillRevalidationBackgroundService` reconsulta os boletos parados em `lookup_unavailable`, com
backoff exponencial e teto de doze horas — que não é o que um boleto espera, e sim onde a cadência
para de crescer: as tentativas acontecem em 5 min, 15 min, 35 min, 1h15, 2h35, 5h15 e 10h35, e o
teto só é alcançado depois de cerca de um dia e meio de provedor fora. Sem ela, D2 seria uma armadilha operacional: o aviso
"revalide mais tarde" viraria trabalho manual que ninguém faz, e o boleto ficaria exigindo a
alçada máxima por um incidente já terminado.

Quatro regras dela que não podem erodir:

1. **Só boleto que aceita revalidação silenciosa** (`Captured`/`AwaitingApproval`). Revalidar um
   `Approved` derruba a aprovação, e um worker fazendo isso desfaz decisão humana em silêncio.
2. **Só `lookup_unavailable`.** `lookup_unresolved` devolve o mesmo e martelaria o provedor à toa;
   `lookup_not_configured` não depende do tempo — quem o resolve é o evento de vínculo da conta,
   que devolve à fila os boletos daquele tenant.
3. **Aborto precoce do ciclo** após N indisponibilidades seguidas. O cliente de consulta tem
   disjuntor por cliente nomeado, e martelá-lo derrubaria junto as validações interativas.
4. **Sem teto de tentativas.** Desistir deixaria o boleto em Extremo Perigo para sempre por causa
   de uma queda que passou. O que cresce é a espera, e o que alerta é o log — e o alerta conta
   **ciclos que tentaram**, nunca ciclos do relógio: com o backoff crescendo, a fila vazia é o
   desfecho normal, e tratá-la como "nada travado" zerava a contagem e tornava o aviso
   inalcançável (`BlockedCycleStreak`).

### D5 — O check 13 pesa conforme a força da fonte oficial e a procedência da leitura

| Situação | Desfecho |
|---|---|
| Documento fiscal lido divergente, **trilho boleto** | `Failed` + `Blocking` (`document_payee_mismatch`) |
| Documento fiscal lido divergente, **trilho Pix** | `Warning` + `Notice` (`document_payee_suspicion`) |
| Documento fiscal lido que veio do **corpo do e-mail** | `Warning` + `Notice` (`document_payee_from_email_body`) |
| Documento fiscal lido igual a um documento **do próprio tenant** | descartado antes de comparar |

No boleto a consulta devolve menos — em arrecadação, nada de documento —, e o impresso é parte do
que sustenta a verificação. No Pix o decode devolve o CNPJ do recebedor, a identidade já está
verificada sem ajuda do papel, e a divergência é mais provável ser erro da leitura por IA.

A procedência (`ReadingFieldSource`) é apurada **procurando os dígitos no corpo do e-mail**, nunca
perguntando ao extrator: perguntar seria confiar no mesmo canal que a injeção de prompt controla.
É o que fecha metade do achado M2 da auditoria de 2026-09-03 — sem ela, quem manda o e-mail podia
plantar um documento fiscal para **travar** os pagamentos de quem recebe, ou casá-lo de propósito
com o oficial para **calar** a verificação.

## Consequências

**Boa:** um boleto cujos dados oficiais conferem com o cadastro sai Verde, que era o pedido de
origem. Arrecadação híbrida passa a casar o beneficiário por documento (via decode Pix) em vez de
só por nome, e a ter banco recebedor conferido.

**Ruim, e aceita:** durante uma indisponibilidade do provedor **todos** os boletos exigem a alçada
máxima (`bill:approve-extreme`). É *fail closed* por decisão explícita. Distribuir o papel antes
do deploy é pré-requisito, e tenant sem chave Asaas vinculada terá 100% dos boletos em Extremo —
comportamento correto, que precisa estar no roteiro de onboarding.

**A vigiar:** o único cenário que perde defesa com D5 é o QR adulterado que paga um beneficiário
**já cadastrado** pelo tenant — ali o check 5 passa, o check 12 é `Skipped` num documento só-Pix, e
o check 13 vira Atenção. É estreito (exige desviar para quem o próprio tenant cadastrou) e o
alerta continua existindo com texto explícito, mas é o preço da decisão.

## Alternativas descartadas

- **Manter `lookup_unavailable` em Perigo e promover só `lookup_unresolved`.** Foi a proposta
  inicial da análise e o usuário a recusou com um argumento melhor: um atacante capaz de provocar
  indisponibilidade usaria exatamente essa brecha.
- **Fazer o check 13 nunca escalonar quando o documento não foi usado na verificação.** Teria
  desarmado a defesa no trilho boleto, que é onde a consulta oficial devolve menos.
- **Perguntar ao modelo de onde ele leu cada campo.** Autodeclaração no mesmo canal que a injeção
  controla não é evidência.
