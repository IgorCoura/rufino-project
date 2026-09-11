# ADR-021 — A antecedência de 24h sai, a janela vira 9h–18h e só limita o pagamento de HOJE

**Status:** Aceito · **Data:** 2026-09-08 (decisão do usuário) · **Supera:** regra 1 do
[ADR-017](ADR-017-politica-inicial-de-agendamento.md); **emenda:** regra 2 do mesmo.

## Contexto

O ADR-017 lançou três regras. A primeira — **24h de antecedência mínima** — custou mais do que
entregou. Ela empurrava a *data* pedida: às 10h de uma segunda, o expediente que honrava as 24h
começava na quarta, e o boleto pedido para hoje virava depois de amanhã. Na prática, ninguém
conseguia pagar no dia em que decidia pagar, e a régua não protegia contra o caso que mais
importa: **boleto vencido o provedor processa na hora, ignorando agendamento** — exatamente onde
não existe janela de reação nenhuma.

Ao mesmo tempo, a medição desfez a premissa que sustentava a antecedência como *espera nossa*:
**não temos como controlar a hora em que o Asaas efetiva o pagamento.** O provedor recebe uma
data, não um horário. Qualquer tentativa de "segurar" a ordem no nosso lado para encurtar ou
alongar o intervalo até a liquidação seria um relógio nosso fingindo governar um relógio que é
dele.

## Decisão

**1. A antecedência mínima de 24h sai.** `PaymentSchedulingPolicy.MinimumLead` e
`PaymentSchedulingService.EarliestDateHonoringLead` foram removidos, não parametrizados com zero
— a fórmula media a antecedência contra a abertura do expediente e, com lead zero, ainda
empurraria para o dia seguinte a qualquer hora depois das 9h. O piso da data efetiva passa a ser
simplesmente **hoje**.

**2. A janela de submissão vai das 9h às 18h** (era 9h–17h), fuso de São Paulo, e continua sendo
o único horário em que a fila fala com o provedor.

**3. A janela é sobre a hora da SUBMISSÃO, não a do pagamento — e por isso só limita HOJE.**
(⚠️ **Revisto em 2026-09-10**, ver "Decisões posteriores": data futura deixou de esperar a
abertura e é submetida a qualquer hora.)
Datas futuras são submetidas na próxima abertura da janela e o provedor honra a data; o horário
da liquidação é dele. "Pagar hoje", ao contrário, depende de submeter hoje: fora da janela a
próxima submissão possível já seria amanhã, quando "hoje" virou ontem. Então **a opção de pagar
hoje é bloqueada fora da janela**, com o motivo dito — em vez de aceita e deslizada um dia em
silêncio.

O veredito é do domínio (`PaymentSchedulingService.CanScheduleForToday` → `SameDayScheduling`) e
tem **dois consumidores obrigatórios**: a guarda do agregado (`BLP.BIL40`, em `Bill.Schedule`) e
a lista de sugestões da tela. Bloqueio que só existe na tela não é bloqueio; sugestão que a tela
oferece e o servidor recusa é a tela levando alguém a um erro já conhecido.

A ordem das perguntas dentro do veredito é normativa: **janela → vencido → piso do provedor → dia
útil**. Vencido dispensa piso e calendário (o provedor paga na hora), mas não dispensa a janela —
sem submissão hoje não há pagamento hoje. Checar dia útil antes de vencido negaria justamente o
caso que funciona: pagar um vencido num sábado dentro da janela.

**4. A tela oferece quatro datas prontas, e o servidor as resolve.**

| Sugestão | Data | Indisponível quando |
|---|---|---|
| `Today` | hoje | fora da janela · hoje não é dia útil · hoje < `minimumScheduleDate` |
| `Tomorrow` | hoje + 1 | nunca |
| `DayBeforeDue` | vencimento − 1 | sem vencimento · a data já passou |
| `OnDueDate` | vencimento | sem vencimento · a data já passou |

Elas chegam por `GET /bills/{id}/schedule-options`, **uma chamada que desenha a folha inteira**.
Fossem quatro prévias, o cliente teria de derivar as datas para pedi-las — e derivar dia no
cliente é exatamente o que o descompasso de fuso quebra (a mesma dor que o cinto do `BLP.BIL35`
remenda). O contrato de **escrita não muda**: `scheduleFor` continua sendo uma data.

**5. A data livre continua existindo**, pelo seletor de sempre e pelo `schedule-preview?date=`.
Data anterior a hoje segue **recusada** (`BLP.BIL05`), não avisada. Data posterior ao vencimento
é **aviso, nunca bloqueio** (`afterDueDate` na prévia): pagar a conta atrasada é justamente o que
o produto precisa saber fazer — o que não pode é fazer isso sem ninguém ver.

**6. O que NÃO muda:** a regra 3 do ADR-017 continua inteira. Boleto vencido exige confirmação
explícita gravada na trilha, na aprovação e na fila.

## Consequências

- **A janela de reação encolheu, de propósito.** Sob as 24h, entre submeter e o dinheiro sair
  havia um dia para cancelar. Agora o intervalo é o que a data escolhida der — e para "pagar
  hoje" pode ser quase nada. A contrapartida é que o cancelamento continua existindo nos dois
  trilhos enquanto a ordem não liquidou, e que a régua de risco do ADR-020 já endureceu o que
  chega à aprovação. **Foi troca consciente, não descuido.**
- Quem depender de `Payments:MinLeadHours` no `appsettings` de alguma instalação vai ver a chave
  ser ignorada em silêncio pelo binder. Não é erro; também não faz mais nada.
- `BLP.BIL40` é uma recusa que a tela **precisa** tratar no lugar, como o `BLP.BIL35`: a janela
  pode fechar entre abrir a folha e confirmar, e fechar o sheet perderia o formulário por causa
  de um relógio que andou um minuto.

## Decisões posteriores

### 2026-09-10 — A janela para de segurar o que não executa hoje (decisão do usuário)

O item 3 acima estava certo sobre o alcance da janela e **errado sobre o custo de esperar por
ela**. "Datas futuras são submetidas na próxima abertura" significava, na prática, que um boleto
aprovado e agendado às 21h para o dia seguinte dormia doze horas **dentro de casa** — e enquanto
ele dorme aqui, nada está agendado em lugar nenhum. Submetê-lo na hora transforma a ordem em
agendamento no provedor, e **agendamento no provedor sobrevive a uma queda nossa**. Segurar era
o risco, não a proteção.

A janela passa a valer para o pagamento que **executa hoje**, e só:

| Ordem, com a janela FECHADA | Desfecho |
|---|---|
| data pedida depois de hoje, boleto no prazo | **submetida agora** — vira agendamento no provedor |
| data pedida é hoje | espera a abertura |
| data pedida já passou (resolve para hoje) | espera a abertura |
| data pedida depois de hoje, **boleto vencido** | espera a abertura |

A última linha é o que o recorte tem de menos óbvio e mais importante: **o provedor processa
conta vencida na hora, ignorando a data** (é a premissa do ADR-017, regra 3). Um vencido enviado
às 23h não é agendamento — é dinheiro saindo com ninguém acordado para reagir ao alerta, que é
exatamente o que a janela existe para impedir. Data futura num boleto vencido não descreve o que
vai acontecer.

**O recorte vive na REIVINDICAÇÃO da fila** (`IPaymentOrderWorkQueries.ClaimPendingSubmissionsAsync`),
não no `SubmitPaymentOrderCommand`. A tentativa de submissão é contada na saída da fila, então
reivindicar o que não vai ser submetido gastaria tentativa e empurraria a ordem para a
desistência sem ela nunca ter falado com o provedor. É também por isso que o vencimento do
boleto entra na consulta do claim: é o único lugar onde ele pode ser lido a tempo de não
reivindicar.

O que **não** mudou: a janela em si (9h–18h, configurável), o bloqueio de "pagar hoje" fora dela
(`BLP.BIL40`, item 3), a ordem das perguntas do veredito, e as quatro sugestões da tela. Quem
agenda continua não podendo escolher hoje fora do horário — o que deixou de existir é a espera
de quem escolheu outro dia.

## Alternativas descartadas

- **Manter as 24h como configuração afrouxável.** Era o desenho do ADR-017 e não sobreviveu ao
  uso: o número certo era zero, e uma fórmula que só funciona com o número errado é código morto
  esperando para confundir.
- **Segurar a ordem no nosso lado para criar a janela de reação** (submeter só N horas depois de
  agendar). Foi considerado e descartado: adiciona um relógio nosso, um estado novo na ordem e
  uma coluna no banco para simular controle sobre uma execução que é do provedor. O provedor
  liquida quando quiser dentro do dia; a espera não compraria a garantia que aparentava comprar.
- **Bloquear data posterior ao vencimento.** O boleto vencido é a conta com encargos correndo, e
  o produto existe para pagá-la também (é o mesmo argumento do ADR-017). O que não pode é pagá-la
  sem avisar.
