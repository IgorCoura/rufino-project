# ADR-020 — A expectativa entra na validação, e a régua de risco endurece

**Data:** 2026-09-08
**Status:** Aceito
**Substitui parcialmente:** [ADR-015](ADR-015-risco-classificado-humano-decide.md) (a matriz de decisão)

## Contexto

O ADR-014 deu ao sistema uma rede de segurança contra a falha silenciosa: ele sabe o que espera
receber e avisa quando não recebeu. Mas essa rede só operava num sentido. `BillExpectation` vivia
inteiramente **a jusante** da verificação — `BillValidatedDomainEvent` disparava o cumprimento do
ciclo, e o boleto nunca ficava sabendo do resultado. Um boleto que chegava sem nenhuma expectativa
correspondente passava exatamente como um que cumpria a conta esperada do mês.

Falta a pergunta inversa, e ela é a mais barata que existe: **alguém estava esperando esta conta?**
Cobrança de fornecedor que o tenant nunca teve, com valor plausível e beneficiário desconhecido, é
o formato da fraude por e-mail — e é justamente o caso em que a expectativa tem algo a dizer.

Ao mesmo tempo, a régua do ADR-015 acumulava dois problemas apontados pelo usuário:

1. **`Atenção` era um balde grande demais.** Falha advisory, aviso e inconclusivo caíam todos ali,
   e o nível deixou de comunicar risco: praticamente todo boleto era amarelo.
2. **O nome do beneficiário produzia alarme falso crônico.** `Payee.MatchesName` comparava com
   `Trim()` e mais nada, enquanto o serviço vizinho — que detecta sósia — derrubava acento e
   pontuação. A regra frouxa estava no caminho que só levanta suspeita, e a estrita no caminho que
   decide: `"EDP SÃO PAULO ... S.A."` da consulta oficial não casava com `"EDP SAO PAULO ... S/A"`
   do cadastro, e a diferença de grafia virava evidência.

## Decisão

**1. A expectativa vira a 14ª verificação.** `CheckType.ExpectationMatch` cruza o boleto com os
ciclos das expectativas do beneficiário, usando o **mesmo** `ExpectationMatchingService` que o
cumprimento usa. Não é uma segunda implementação da regra: se as duas divergissem, a tela diria
"conta esperada" sobre um ciclo que segue alertando sozinho.

**2. Tudo que era `Atenção` passa a ser `Perigo`** — falha advisory, `Warning` e `Inconclusive`.
Aprovar passa a exigir o aceite explícito e a alçada `approve-danger`.

**3. Três assuntos ficam com teto de `Atenção`**, pelo degrau novo `CheckSeverity.Notice`:

| Assunto | Por quê |
|---|---|
| A **expectativa** (check 14) | Não haver expectativa não desmente nada — a maior parte dos boletos legítimos nunca teve uma |
| O **prazo** (check 10, `DueDateSanity`) | Vencido, fora do corte, sem data agendável: é problema de calendário, não sinal de fraude |
| O **nome** do beneficiário (check 5) | Grafia divergente com o CNPJ conferindo, e cotejo só por nome: a identidade está confirmada em parte |

**4. Uma normalização de nome só.** `PartyName.Normalize` no SharedKernel, usada pelo cadastro e
pelo serviço de resolução. A tolerância é de **grafia**, não de semelhança: depois de normalizar, a
comparação continua sendo igualdade exata.

## Consequências

**A régua fica declarativa.** `RiskLevel.Of(outcome, severity)` é a regra inteira num lugar só, e
`RecordChecks` agrega por "o pior vence". A cadeia de `if` que vivia dentro do agregado sumiu — com
ela, o teto do `Notice` teria de ser um caso especial escondido num `else if`.

**A revalidação exigiu uma exceção no casamento.** O cumprimento fecha o ciclo, e revalidar é
rotina — a leitura por IA chega depois da captura e refaz a apuração. Sem reconhecer o ciclo
`Fulfilled` **por aquele mesmo boleto**, o segundo passe encontraria "nenhuma expectativa" e
rebaixaria o risco de Seguro para Atenção sozinho. Daí o parâmetro `alreadyFulfilledBy` do
`ExpectationMatchingService.Match`, que serve aos dois chamadores.

**Ambiguidade continua não desempatando.** Duas contas do mesmo beneficiário — as quatro
instalações da EDP — devolvem `expectation_ambiguous`, nunca uma escolha.

**A decisão de 2026-08-31 sobre `matched_by_name_only` sobrevive intacta.** Ela era sobre
confirmação incompleta de **nome**, e confirmação incompleta de nome nunca foi o alvo deste
endurecimento. Sem o `Notice`, os 100% da arrecadação — que não trazem documento fiscal na consulta
oficial — passariam a exigir "assumo o risco" todo mês, **pelo nome ter batido**, que é o desfecho
bom daquele ramo.

**O que NÃO mudou:** o sósia (`payee_lookalike`) segue Perigo, a blacklist segue Extremo Perigo, a
validação continua não rejeitando ninguém, e DV inválido continua não virando `Bill`.

## Consequências de implantação

⚠️ **Varredura de revalidação obrigatória.** `EnsureChecksAreComplete` compara contra o catálogo
inteiro, então todo boleto em `AwaitingApproval` reprova com `BLP.BIL03` até ser revalidado. É o
comportamento desejado — um check novo é uma pergunta que ninguém respondeu para aquele boleto —,
mas sem a varredura a fila trava.

⚠️ **Keycloak antes do deploy.** Quem tem só `bill-approver-attention` deixa de aprovar boleto de
fornecedor novo ou de remetente desconhecido. A atribuição de `bill-approver-danger` precisa ser
revista **antes**, não depois.

## Alternativas descartadas

**Deixar a expectativa fora da validação e só mostrá-la na tela.** Descartada: o risco é derivado
dos checks, e informação que não entra na régua não muda decisão nenhuma.

**Promover `Inconclusive` sem nenhum carve-out.** Descartada com o usuário: `Inconclusive` é o
desfecho mais comum do catálogo, e mandar todos para Perigo tornaria o "assumo o risco" rotina —
que é exatamente como o alerta que importa passa batido, o que o ADR-003 mandou evitar.

**Remover o casamento por nome e decidir só por CNPJ.** Avaliada e descartada pelo usuário no
mesmo dia: a consulta oficial não devolve documento fiscal em 100% da arrecadação medida, então
toda conta de concessionária sem QR Pix ficaria em Perigo permanente.
