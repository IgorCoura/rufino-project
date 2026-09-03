# ADR-010 — QR Code Pix é o trilho preferencial; código de barras é o fallback

**Status:** Aceito · **Data:** 2026-07-31

## Contexto

Um documento de cobrança pode trazer **código de barras**, **QR Code Pix**, ou os dois (boleto híbrido, cada vez mais comum). O requisito: quando houver QR Pix, pagar por Pix; senão, por código de barras.

A objeção óbvia contra Pix num sistema de contas a pagar era que ele é instantâneo e irreversível, enquanto o boleto pode ser agendado e cancelado antes da data. **Essa objeção não se sustenta com o provedor escolhido**: o `POST /v3/pix/qrCodes/pay` do Asaas aceita `scheduleDate`, tem status `SCHEDULED`, e a resposta traz `canBeCanceled`.

## Decisão

O `Bill` carrega **um ou dois instrumentos de pagamento** e um **trilho escolhido** (`PaymentRail`). A regra de escolha é do domínio, não do handler:

1. Há QR Pix válido e consultável → **`Pix`**.
2. Só código de barras → **`Boleto`**.
3. Os dois presentes mas **discordam** em beneficiário ou valor → **nenhum**; o Bill vai para `Rejected` com o check `PixBarcodeConsistency` falhando.

A escolha é registrada e visível na aprovação — o aprovador vê por qual trilho o dinheiro vai sair, e a evidência de por que aquele foi escolhido.

## Razões

- **A consulta oficial do Pix é tão boa quanto a do boleto — em alguns pontos melhor.** O `POST /v3/pix/qrCodes/decode` devolve `name`, `tradingName` e **`cpfCnpj` do recebedor**, além de `value`, `totalValue`, `interest`, `fine`, `discount`, `dueDate`, `expirationDate` e `canBePaidWithDifferentValue`. Ou seja, **todo o catálogo de verificações transfere sem alteração** — `PayeeMatch`, `AmountMatch`, `DueDateSanity` funcionam igual nos dois trilhos.
- **Agendamento e cancelamento existem** (`scheduleDate`, `SCHEDULED`, `canBeCanceled`), então o fluxo de aprovação → agendamento → pagamento na data é idêntico. A assimetria de risco que justificaria preferir boleto desapareceu.
- **Liquidação melhor**: Pix compensa na hora, 24/7, sem janela bancária, sem depender do boleto estar registrado. Boleto vencido de arrecadação frequentemente só é pago no dia seguinte; Pix não tem esse problema.
- **Custo menor por transação** que o pague-contas.
- **`ReceivingBankMatch` fica mais forte no Pix**: a chave Pix e o PSP recebedor são dado do payload, não inferência sobre o campo livre do código de barras.

## Consequências

- **`PixBarcodeConsistency` é check novo e bloqueante.** Boleto híbrido cujo QR aponta para um CNPJ diferente do código de barras é o vetor de fraude mais direto que existe hoje — o fraudador cola um QR por cima do documento legítimo. Comparar os dois é praticamente de graça e é a defesa mais barata do sistema. Divergência **nunca** vira "escolhe um e segue".
- **O `Bill` precisa modelar dois instrumentos**, não um. `DigitableLine` deixa de ser o campo obrigatório central: passa a existir `PaymentInstrument` (VO discriminado: `Barcode` \| `PixQr`), com o Bill exigindo **pelo menos um**. A unicidade global passa a ser sobre a **chave natural do instrumento escolhido** (linha digitável ou `txid`/payload hash do Pix).
- **Dedup precisa cobrir os dois lados.** O mesmo compromisso pode chegar duas vezes: uma como boleto, outra como Pix. Deduplicar só por linha digitável deixaria passar pagamento duplicado pelo outro trilho. A chave de dedup é `(beneficiário, valor, vencimento)` além da chave natural de cada instrumento.
- **QR Pix estático × dinâmico**: o estático não carrega valor nem vencimento (só chave + nome + cidade); o dinâmico carrega URL que o PSP resolve. Um QR estático **não** dispensa a consulta — e como não tem valor, `AmountMatch` depende inteiramente do `Payee.AmountPolicy`. Tratar `PixQrKind` como Smart Enum e marcar os checks correspondentes como `Skipped` com motivo.
- **`expirationDate` do Pix é real e curto** em QR dinâmico. Um Pix que expira antes da data de agendamento pretendida precisa falhar em `DueDateSanity` com motivo próprio (`pix_expires_before_schedule`) — não existe equivalente no boleto.
- Duas portas na Infra em vez de uma: `IPixLookupService` / `IPixPaymentGateway` ao lado das de boleto. Mesmo adapter Asaas, endpoints diferentes.
- O saldo da subconta Asaas serve os dois trilhos. (A verificação de saldo pré-agendamento citada aqui foi removida do escopo em 2026-09-03 — ver ADR-017, "Decisões posteriores".)

## Alternativa descartada

**Pagar sempre por boleto quando o código de barras existir**, ignorando o QR. Simplifica o modelo (um instrumento, um trilho) ao custo de perder liquidação instantânea, taxa menor e — o que pesa mais — **a checagem cruzada entre QR e código de barras**, que é uma defesa antifraude que não existe em nenhum outro lugar do sistema.
