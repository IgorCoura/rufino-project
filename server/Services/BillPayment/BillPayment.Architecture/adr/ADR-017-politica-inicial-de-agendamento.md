# ADR-017 — Política inicial de agendamento: 24h de antecedência, janela 9h–17h, vencido exige confirmação

**Status:** Aceito · **Data:** 2026-09-02 (decisão do usuário)

## Contexto

O pagamento é o passo irreversível do BC. O provedor oferece agendamento, mas duas propriedades
dele trabalham contra a segurança no lançamento: (a) nada impede submeter uma ordem minutos antes
da execução, o que elimina a janela de reação; (b) **conta vencida processa imediatamente, sem
agendamento** — justamente o caso com zero janela de reação é o que o provedor executa mais rápido.

## Decisão

Três regras valem no lançamento, como **política configurável da instalação** aplicada por regra
de domínio (`PaymentSchedulingService`) — afrouxar depois é configuração + revisão deste ADR, não
reescrita:

1. **Antecedência mínima de 24 horas.** A submissão ao provedor só acontece quando a data efetiva
   de pagamento está a pelo menos 24h. É a janela de reação: um agendamento indevido — fraude que
   passou, engano de aprovação, conta comprometida — pode ser cancelado antes de o dinheiro se
   mover (`canBeCancelled`/`canBeCanceled` existem nos dois trilhos).
2. **Janela de submissão das 9h às 17h**, fuso de São Paulo. Fora dela a ordem espera a próxima
   abertura — o aluguel da fila de submissão já sabe esperar. Se, ao entrar na janela, a data
   pedida não puder mais respeitar as 24h, **a data efetiva desliza para o dia útil seguinte**.
   O racional: submissão em horário comercial é submissão com gente acordada para reagir ao
   alerta, e o deslize prefere atrasar um dia a encolher a janela de reação.
3. **Boleto vencido nunca é pago em silêncio.** Quando o agendamento resolve para execução
   imediata (vencido na data possível de submissão), o sistema avisa e exige **confirmação
   explícita gravada na trilha** — no molde do aceite de risco do ADR-015. Em dois momentos:
   na aprovação (a folha de autorizar mostra o aviso e colhe o aceite) e na fila (se o vencimento
   passou **entre** a aprovação e a submissão, o worker não executa: estaciona a ordem em estado
   visível "aguardando confirmação", alerta pelo canal do ADR-014, e a confirmação vem por
   endpoint com o autor do token — ADR-007).

## Consequências

- A data efetiva pode diferir da pedida por até um dia além das regras do provedor (dia útil,
  corte das 14h, `minimumScheduleDate`). A diferença é gravada na ordem e mostrada ao usuário no
  ato de aprovar — nunca descoberta depois.
- Boleto aprovado em cima da hora tende a cair em execução imediata mediante confirmação — mais
  fricção, e é o ponto da regra. A mitigação é aprovar cedo; a rede de expectativas (ADR-014) já
  avisa com antecedência. Os parâmetros serão reavaliados depois do piloto.
- Os dois números (24h, 9h–17h) vivem em configuração (`Payments:*`) e entram no
  `PaymentSchedulingService` por parâmetro, como o relógio: o serviço continua puro e testável.

## Decisões posteriores

- **2026-09-03 — Corte das 14h do provedor: NÃO implementado, de propósito.** O
  `PaymentSchedulingService` não modela o corte same-day do provedor. Sob a política das 24h,
  submissão same-day só acontece no fluxo de execução imediata (vencido, com confirmação gravada) —
  o corte fica quase inalcançável. O contrato real do corte será **medido** quando a sonda de
  sandbox destravar; até lá, se o provedor recusar uma submissão por horário, a recusa vira
  `Failed` visível e reabrível — degradação controlada, nunca silenciosa. Reavaliar com a medição.
- **2026-09-03 — Saldo NUNCA bloqueia agendamento (decisão do usuário).** A verificação de saldo
  pré-submissão (`GET /v3/finance/balance`, planejada na sprint 3.2) foi **removida em definitivo
  do escopo**: saldo ausente hoje não diz nada sobre o saldo de amanhã, e agendar é justamente
  reservar o futuro. A cobertura é do próprio provedor — `AWAITING_BALANCE_VALIDATION` mapeia para
  `Pending` no adapter e a conciliação segue vigiando até o desfecho real. Não é pendência: é
  decisão. O aporte de saldo continua sendo operação do cliente na conta dele, fora do escopo.

## Alternativas descartadas

- **Confiar só nas regras do provedor** — o corte das 14h e o dia útil existem para a liquidação,
  não para a nossa janela de reação; nada impede submeter às 23h59 para pagar às 0h01.
- **Bloquear pagamento de vencido** — o vencido é justamente a conta com encargos correndo; o
  produto existe para pagá-la também. O que não pode é pagá-la sem ninguém olhar.
