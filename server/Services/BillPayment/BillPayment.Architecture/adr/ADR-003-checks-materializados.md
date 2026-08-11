# ADR-003 — Verificação materializada como entidade, não como booleano

**Status:** Aceito · **Data:** 2026-07-31

## Contexto

A verificação poderia ser um método que devolve `bool` (ou lança exceção) e deixa no `Bill` apenas o resultado final: aprovável ou não.

## Decisão

Cada verificação vira um `BillCheck` — entidade interna do `Bill`, uma por `CheckType`, com `Outcome` (`Passed`/`Failed`/`Inconclusive`/`Skipped`), `Severity` (`Blocking`/`Advisory`), `ReasonCode` estável e `Evidence` textual com os dois lados da comparação. O conjunto é persistido e nunca editável por endpoint.

`Bill.RecordChecks(IReadOnlyCollection<CheckResult>)` substitui o conjunto inteiro e é o **único** ponto que decide o status a partir das severidades.

## Razões

- **O aprovador precisa da evidência, não do veredito.** "Não aprovável" não ajuda ninguém a decidir; "banco esperado 341, boleto emitido pelo 237" ajuda. A qualidade da decisão humana é o produto.
- **Quatro resultados, não dois.** `Inconclusive` é o estado mais comum e mais importante: beneficiário ainda não cadastrado, origem nunca vista, pagador não extraível. Colapsar isso em `false` transformaria operação normal em alarme e treinaria o usuário a ignorar alertas — que é como o alerta que importa passa batido.
- **Auditoria.** Meses depois é preciso responder "com que informação essa aprovação foi dada". Só um registro imutável do que foi apurado responde.
- **Evolução barata.** Um check novo é um valor de Smart Enum e um ramo no Domain Service; não muda o schema nem o Aggregate.
- **Severidade separada do resultado** permite calibrar rigor sem reescrever a lógica de apuração.

## Refinamentos da implementação (sprint 1.4)

- **Cinco resultados, não quatro.** `Warning` entrou pela decisão de 2026-07-31 sobre divergência de nome em arrecadação. Ele existe porque as duas alternativas falhavam: um `Failed` num check `Blocking` travaria o pagamento por uma grafia diferente de concessionária, e um `Passed` jogaria fora a única evidência de beneficiário que arrecadação oferece. **`Warning` nunca bloqueia**, qualquer que seja a severidade do check — é o que o distingue de `Failed`.
- **`BillCheck` é Value Object, não Entity.** Um check não tem ciclo de vida próprio: `RecordChecks` substitui o conjunto inteiro, como este ADR já determinava. Identidade seria ficção — o que identifica um check dentro do boleto é o `CheckType`, e disso a chave `(bill_id, type)` da tabela filha dá conta. A decisão de persistência (`bill_checks` como coleção owned) **não mudou**.
- **A severidade viaja no resultado**, e não só no `CheckType`. Três checks escapam do peso usual em situações específicas: banco cujas duas fontes autoritativas discordam, pagador extraído que contradiz o cadastro, e origem explicitamente banida — todos `Advisory` por natureza que viram `Blocking` naquele caso.
- **`RecordChecks` exige o catálogo completo** (`BLP.BIL19`). Conjunto parcial deixaria pergunta sem resposta parecendo respondida.

## Consequências

- Tabela filha `bill_checks` (owned collection), reescrita a cada validação. O histórico do que mudou entre validações fica no histórico de snapshots (`bill_lookup_history`), não em versionamento dos checks.
- Não existe endpoint para alterar o resultado de um check. O aprovador aprova **apesar** do check, e essa decisão fica gravada com motivo; o check permanece `Failed` para sempre.
- `ReasonCode` é contrato de UI — a tela traduz o código. Mudar um código existente é mudança quebrante; código novo é aditivo.
- Aprovar exige que todos os `CheckType` obrigatórios tenham sido executados (`BLP.BIL03`), então adicionar um check novo invalida aprovações pendentes até a revalidação. É o comportamento desejado.
