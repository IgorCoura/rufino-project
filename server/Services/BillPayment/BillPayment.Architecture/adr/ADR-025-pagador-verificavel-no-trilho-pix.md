# ADR-025 — O pagador é verificável no trilho Pix com cobrança registrada

**Data:** 2026-09-10
**Status:** Aceito
**Limita:** [ADR-004](ADR-004-pagador-nao-autoritativo.md) (que se declarava permanente — esta é a exceção que ele previa não existir)
**Estende:** [ADR-024](ADR-024-fonte-oficial-primeiro-e-a-regua-por-ausencia.md) (D1), [ADR-010](ADR-010-pix-preferido-sobre-boleto.md), [ADR-011](ADR-011-llm-propoe-codigo-dispoe.md)

## Contexto

Um boleto com QR Pix saía **Perigo** com a verificação 8 inconclusiva e a mensagem *"o documento
não traz o documento fiscal do pagador"* — sobre um pagamento cuja consulta oficial havia
devolvido o documento fiscal do pagador, completo.

O defeito é de leitura, não de dado. Desde o [ADR-024](ADR-024-fonte-oficial-primeiro-e-a-regua-por-ausencia.md)
o `PayerMatch` consulta o pagador do decode Pix **antes** do CNPJ inferido do PDF — mas só para
*contradizer*. Sendo o pagador oficial compatível com o cadastro, o método seguia adiante como se
nada tivesse sido consultado, caía na inferência do PDF e, não havendo CNPJ impresso, respondia
`payer_not_extractable`.

Duas coisas tornam isso pior do que um rótulo impreciso:

1. **`Inconclusive` + `Advisory` pesa Perigo** desde o ADR-020, e o ADR-024 (D3) cravou que
   `payer_not_extractable` pesar Perigo é **invariante** — é ele que segura o boleto adulterado de
   fornecedor novo. Rebaixar a severidade estava fora de questão; o que estava errado era o
   desfecho.
2. **O dado existe e é oficial.** O achado 1 do [doc 12](../12-official-lookup-coverage.md) mediu
   em produção que, num QR dinâmico com cobrança registrada (`cobv`), o provedor devolve o pagador
   **completo e não mascarado**. Aquele doc registrou a consequência possível e a deixou
   deliberadamente em aberto, porque aplicá-la é reabrir o ADR-004.

O ADR-004 afirma: *"a única fonte que tornaria o pagador verificável seria o DDA (...) Esta decisão
é permanente"*. Para o trilho Pix com cobrança registrada, a afirmação deixou de ser verdadeira —
não pelo DDA, mas porque **o emissor grava o pagador na `cobv` e o PSP o devolve no decode**.

## Decisão

### D1 — No trilho Pix com cobrança registrada, o pagador confirma

`PayerMatch` passa a sair `Passed` com o motivo `payer_confirmed_by_lookup` quando **todas** estas
condições valem ao mesmo tempo:

| Condição | Por quê |
|---|---|
| `Bill.Rail == PaymentRail.Pix` | é o trilho que vai pagar (ADR-024 D1) |
| `PixLookupSnapshot.IsDynamic` | QR estático não carrega cobrança, logo não carrega pagador |
| O documento voltou **inteiro**, sem um único caractere de máscara | máscara não identifica ninguém — milhões de documentos compartilham quatro dígitos |
| Os dígitos formam um CPF/CNPJ com **DV válido** | ADR-011: fonte produz candidato, o DV decide |
| `PayerProfile.Owns` responde sim | inclui principal, adicionais e — quando ligado — a raiz do CNPJ |

Falhando qualquer uma, **nada muda**: vale o ADR-004 como estava, e o desfecho continua sendo o do
documento impresso, ou `Inconclusive`.

### D2 — O escopo mora no tipo, não num `if` do serviço

`MaskedParty.ResolvedTaxId` só devolve documento quando ele veio inteiro e o DV confere;
`PixLookupSnapshot.RegisteredPayerTaxId` só o repassa quando o QR é dinâmico. O serviço de
validação pergunta uma coisa só, e **não tem como generalizar por acidente** — que é exatamente o
risco que o doc 12 apontou ao deixar a decisão em aberto ("promover o check sem cravar esse escopo
transformaria uma exceção numa regra geral falsa").

### D3 — A contradição oficial passa a respeitar a raiz do CNPJ

O ramo de contradição comparava contra a lista exata de documentos do tenant (principal +
adicionais) e ignorava `MatchByCnpjRoot`. Com um documento **completo** vindo do Pix, isso bloqueia
uma `cobv` registrada para uma filial não cadastrada de um tenant que declarou casar por raiz — um
pagamento legítimo, barrado por uma regra que o próprio tenant desligou.

A correção vale **só para documento completo**, e por um motivo: sobre máscara não há como saber se
os oito dígitos da raiz estão visíveis, e afrouxar ali criaria compatibilidade onde não há
evidência. Máscara continua sendo comparada contra a lista exata.

### D4 — O documento impresso que contradiz continua bloqueando, mesmo com o oficial confirmando

A ordem dos ramos é: contradições primeiro (beneficiário-é-o-pagador, oficial contradiz, documento
só dentro do código de barras, documento impresso contradiz), confirmações depois (oficial, e então
o documento impresso).

Um PDF que nomeia outro pagador enquanto o QR está registrado para o tenant é anomalia real — ou a
leitura errou, ou o par PDF/QR foi montado. *Fail closed* é a resposta, e o custo é o de sempre:
quem corrige um parser errado reporta, não paga.

## Consequências

**Boa, e é o pedido de origem:** boleto com QR Pix cuja cobrança está registrada no CNPJ do tenant
deixa de sair Perigo por falta de um dado que a consulta oficial tinha entregue. A verificação 8
deixa de ser, nesse trilho, um check que só sabe dizer não.

**Um `Passed` de `PayerMatch` passa a ter duas forças diferentes**, e a tela precisa distingui-las:

| Motivo | O que foi provado |
|---|---|
| `payer_confirmed_by_lookup` | a consulta oficial diz que esta cobrança foi **emitida contra** um documento deste tenant |
| (sem motivo) | o PDF disse, e nada contradisse — ADR-004, prova nenhuma |

Selos idênticos para os dois seriam a mesma mentira que o ADR-004 já proibia entre `PayerMatch` e
`PayeeMatch`.

**Ruim, e aceita:** a confirmação vale a confiança que se tem no emissor da `cobv`. Quem emite a
cobrança escolhe o que grava no campo do pagador, e nada impede um fraudador de gravar ali o CNPJ
da vítima. **Isso não é regressão** — é o mesmo patamar do documento impresso, que o ADR-004 já
tratava como não autoritativo. O que segura essa fraude continua sendo o `PayeeMatch` (quem
**recebe**, autoritativo e bloqueante), nunca o `PayerMatch`. A frase do ADR-004 que sobrevive
inteira: *"o check que de fato protege contra fraude de boleto é o `PayeeMatch`"*.

**Não muda:** trilho boleto, QR estático, e QR dinâmico cujo pagador volta mascarado. Nesses três a
assimetria do ADR-004 segue idêntica — contradição bloqueia, compatibilidade não confirma.

**Fecha** a "decisão em aberto" registrada no achado 1 do doc 12 e no `CLAUDE.md`.

## Alternativas descartadas

- **Rebaixar `payer_not_extractable` para `CheckSeverity.Notice`.** Trataria o sintoma criando o
  buraco que o ADR-024 D3 declarou invariante: não saber de quem é o boleto é o sistema não ter
  provado nada, e isso pesa Perigo.
- **Aceitar máscara compatível como confirmação.** Quatro dígitos visíveis não identificam
  ninguém. É a fronteira que o `MaskedParty` foi escrito para não cruzar.
- **Confirmar sem exigir `IsDynamic`.** QR estático não tem cobrança registrada; um pagador vindo
  dali não teria a procedência que sustenta esta decisão. Exigir o flag custa nada e mantém a
  exceção do tamanho medido.
- **Deixar o oficial confirmar por cima de um documento impresso que contradiz.** Seria trocar um
  bloqueio existente por um alerta, sobre o único sinal que aponta um par PDF/QR montado.
