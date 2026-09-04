# ADR-014 — O sistema sabe o que espera receber

**Status:** Aceito · **Data:** 2026-07-31

## Contexto

Todo o desenho até aqui protege contra **pagar errado**: beneficiário sósia, valor adulterado, QR trocado, conta de outro tenant. Nenhuma peça protege contra o risco oposto e mais provável no dia a dia — **não pagar**, porque a conta simplesmente não chegou e ninguém notou.

Com o DDA fora ([`ADR-012`](ADR-012-portais-reduzir-residuo.md)), não existe fonte que liste o que foi emitido contra o CNPJ. O sistema só sabe o que lhe entregaram. Um conector de portal que quebrou, um e-mail que caiu em spam, um link que expirou, um PDF que não abriu — todos falham do mesmo jeito: **em silêncio**, e a primeira notícia é a multa.

Uma pasta bem organizada não tem esse problema porque a pessoa percebe a ausência. Um sistema automatizado tem: quanto melhor ele funciona, menos a pessoa confere.

## Decisão

O BC passa a modelar **expectativa** como conceito de primeira classe, ao lado de boleto e pagamento.

**`BillExpectation`** (Aggregate Root, `BLP.EXP`) declara: *"deste beneficiário, nesta referência de conta, espero uma cobrança com esta periodicidade, vencendo por volta deste dia."* Cada período abre um **ciclo**; um ciclo que não é cumprido até a data-limite gera alerta.

Três propriedades que definem o desenho:

1. **A expectativa nasce sozinha.** Depois de N ocorrências regulares do mesmo par `(Payee, AccountReference)` — a mesma chave que a `RoutingRule` já usa — o sistema cria a expectativa e avisa que passou a monitorar. Cadastro manual existe, mas é a exceção.
2. **O alerta diz o que fazer.** Não "faltou algo": *"a conta da EDP da Casa Florentino costuma chegar até o dia 5 e vence dia 12; não chegou. Acesse [portal] ou verifique [remetente]."*
3. **"Não chegou" e "chegou e não consegui ler" são alertas diferentes**, porque a ação do usuário é diferente. Um item parado em `Locked`, `LinkFailed`, `Unrecognized` ou `Unrouted` cumpre parcialmente o ciclo e produz um alerta de *falha de captura*, com link direto para o item.

Modelo completo em [`11-bill-expectations.md`](../11-bill-expectations.md).

## Razões

- **É a única defesa contra falha silenciosa.** Todos os canais de captura falham em silêncio por natureza — a ausência de um e-mail não gera evento. Só uma expectativa declarada transforma ausência em sinal.
- **Sem DDA, não há alternativa.** Era o DDA que responderia "o que foi emitido contra mim?". Sem ele, a única fonte dessa resposta é o histórico do próprio sistema.
- **O dado já existe.** `Payee`, `AccountReference`, vencimentos e valores históricos estão todos modelados. A expectativa é inferência sobre dado que o sistema já tem, não coleta nova.
- **Fecha o argumento do produto.** Sem isso, automatizar a captura *aumenta* o risco de esquecimento: a pessoa para de conferir a pasta porque confia no sistema, e o sistema não avisa quando falha. Com isso, o sistema é estritamente melhor que o processo manual — que é o mínimo que ele precisa ser.
- **Aproveita a periodicidade real.** A medição do corpus mostrou que metade do volume é recorrente e previsível ([`08-boleto-corpus-findings.md`](../08-boleto-corpus-findings.md)) — exatamente o material de que a inferência precisa.

## Consequências

- **Aggregate novo** (`BillExpectation` + entidade interna `ExpectationCycle`), dois Domain Services (`ExpectationMatchingService`, `ExpectationLearningService`), um job diário de vigilância e uma porta `INotificationSender`.
- **Falso positivo é o risco principal.** Fornecedor que pula um mês, contrato encerrado, valor sazonal — tudo vira alerta indevido, e alerta indevido treina o usuário a ignorar alertas, que destrói o mecanismo inteiro. Mitigações obrigatórias: `Waive` por ciclo ("este mês não vem"), `Pause` por período, desativação automática após K ciclos consecutivos não cumpridos **e** não reivindicados (o silêncio do usuário é sinal de que a expectativa morreu), e nunca mais de um alerta por ciclo por nível.
- **A janela de alerta é aprendida, não fixa.** Guardar o intervalo observado entre chegada e vencimento e alertar quando ele é ultrapassado com folga. Um dia fixo geraria alerta cedo demais para uns e tarde demais para outros.
- **Notificação é infraestrutura nova.** Porta `INotificationSender`; começa por e-mail. O BC `PeopleManagement` já integra Evolution API para WhatsApp — canal óbvio de reuso quando fizer sentido, não na primeira entrega.
- **`CompetencePeriod` do SharedKernel é reaproveitado** para identificar o ciclo. Foi desenhado para isso.
- **Entra na fase 2**, não na 4. É rede de segurança da captura, não relatório: só faz sentido depois que a ingestão automática existe, e é obrigatório antes de o cliente confiar nela.
- O relatório de confiabilidade da captura (doc 05) ganha a métrica que importa: **quantos ciclos foram cumpridos sozinhos, quantos exigiram busca manual, e por qual fonte** — que é como se decide onde investir na fase 5.
