# 12 — Cobertura da consulta oficial (medida)

**Sprint 1.0.** Mede o que o `POST /v3/bill/simulate` do Asaas realmente devolve, por tipo de documento, contra as 22 linhas digitáveis extraídas do corpus real ([doc 08](08-boleto-corpus-findings.md)).

A pergunta que a sprint precisava responder: **o check de beneficiário do [doc 03](03-bill-validation.md) tem dado para funcionar?** A resposta é diferente para cada tipo, e para um deles a sprint não consegue responder.

- **Ferramenta:** [`tools/probe-asaas-simulate.js`](tools/probe-asaas-simulate.js)
- **Ambiente:** `api-sandbox.asaas.com`, chave de sandbox com permissão de saque habilitada
- **Data:** 2026-07-31

## Pré-requisito descoberto no caminho

`bill/simulate` e `pix/qrCodes/decode` respondem **403 `insufficient_permission`** sem a permissão de *operações de saque via API*, mesmo não movimentando dinheiro. Leitura comum (`GET /customers`, `GET /finance/balance`) passa sem ela. Consequência de segurança em [`adr/ADR-001`](adr/ADR-001-asaas-como-provedor.md) → "Achado de campo".

## Resultado

| | Cobrança bancária | Arrecadação |
|---|---|---|
| Linhas consultadas | 12 | 10 |
| **Responderam** | **0 (0%)** | **10 (100%)** |
| `beneficiaryCpfCnpj` | — | **0 (0%)** |
| `companyName` | — | 10 (100%) |
| `beneficiaryName` | — | 6 (60%) |
| beneficiário identificável **por nome** | — | **10 (100%)** |
| `value` / `originalValue` | — | 10 (100%) |
| `isOverdue` | — | 10 (100%) |
| `allowChangeValue` | — | 6 (60%) |
| `dueDate` | — | **3 (30%)** |
| `bank` | — | **0 (0%)** |

Amostra de arrecadação: DARF, DAS, SABESP, DAE, EDP (×4) — a largura inteira do corpus.

## Sonda de produção — executada em 2026-08-06: **VERDE**

A lacuna está fechada. Uma consulta real, em `api.asaas.com`, contra um boleto de cobrança bancária **recente e não pago** (emissor com boleto registrado no Itaú, vencimento 2026-08-31):

| Campo | Voltou? | Observação |
|---|---|---|
| **`beneficiaryCpfCnpj`** | **SIM** | É o que a sprint 1.0 não conseguiu medir. O check bloqueante tem base |
| `beneficiaryName` | SIM | Razão social completa |
| `companyName` | não | Inverte-se em relação à arrecadação, onde ele é o único que volta |
| `bank` | SIM | Devolvido como **string** de três dígitos (`"341"`), não objeto |
| `value` / `originalValue` | SIM | Iguais — sem encargos, boleto a vencer |
| `dueDate` / `isOverdue` / `allowChangeValue` | SIM | |
| `minimumScheduleDate` | SIM | Igual a "hoje" para boleto a vencer |

**O que isso decide:**

1. **`PayeeMatch` funciona como projetado em cobrança bancária.** A comparação é documento contra documento, não nome contra nome. A tela pode prometer "beneficiário verificado" para este tipo — e **só** para este tipo.
2. **A assimetria entre os dois tipos está confirmada por medição dos dois lados**, e não é hipótese: cobrança tem documento e banco; arrecadação tem nome e valor. A interface precisa refletir isso.
3. **O formato de `bank` era desconhecido e agora não é**: string de três dígitos. O adapter já aceitava as duas formas (`AsaasHttp.ReadBankCode` lê string ou objeto com `code`), então nada muda no código — mas a tolerância deixou de ser palpite.
4. O 0/12 do sandbox está definitivamente explicado como limitação de ambiente.

> Um boleto verde não prova que **todo** emissor devolve documento. Prova que o caminho existe e funciona, que era a dúvida que travava a fase. Emissor que não devolver cai no ramo já implementado: `Inconclusive`/cotejo por nome, nunca falso "verificado".

## Sonda do decode Pix — executada em 2026-08-06: **VERDE**, com três achados

QR dinâmico real (`cobv` do Itaú, cobrança da Receita Federal, R$ 22.799,96, vencimento 2026-08-20):

| Campo | Voltou | Observação |
|---|---|---|
| **`receiver.cpfCnpj`** | SIM | O documento do recebedor existe no trilho Pix |
| `receiver.name` / `tradingName` | SIM | `MINISTERIO DA FAZENDA` / `RECEITA FEDERAL` — razão social **e** nome fantasia, separados |
| `receiver.ispb` / `ispbName` | SIM | `60701190` / `ITAÚ UNIBANCO S.A.` — traduz para COMPE 341 pelo diretório do Bacen |
| `receiver.personType` | SIM | `JURIDICA` |
| `type` | SIM | `DYNAMIC` |
| `value` / `totalValue` | SIM | iguais, sem encargos |
| `dueDate` / `expirationDate` | SIM | `2026-08-20` / `2026-08-20 23:59:59` |
| `canBePaid` / `canBePaidWithDifferentValue` | SIM | `true` / `false` |
| `conciliationIdentifier` | SIM | |
| `interest` / `fine` / `discount` | **não** | ausentes quando não há encargo — o modelo já os trata como opcionais |

### Achado 1 — o pagador **não vem mascarado**

A documentação diz mascarado; produção devolveu **`02.624.917/0001-92`, completo e formatado**.

Isso não quebra nada: `MaskedParty` tolera as duas formas (sem `*`, a comparação é exata) e o check já bloqueia quando contradiz. **Mas abre uma possibilidade que o [`ADR-004`](adr/ADR-004-pagador-nao-autoritativo.md) declarava inexistente**: naquele ADR, "não existe API que confirme *este documento foi emitido para o meu CNPJ*". Para **Pix dinâmico com cobrança registrada (`cobv`), existe** — o emissor grava o pagador na cobrança e o PSP devolve.

Consequências possíveis, **nenhuma aplicada ainda**:

- `PayerMatch` poderia ser um check **forte** neste trilho (hoje um `Passed` ali significa só "nada contradisse").
- O degrau 1 da escada de roteamento (fase 2) ganharia uma fonte autoritativa em vez de inferência.

> **Decisão em aberto, deliberadamente.** Mexer nisso é reabrir o ADR-004, e o escopo é: vale só para Pix dinâmico com `cobv` — não para QR estático, não para boleto, não para Pix sem cobrança registrada. Promover o check sem cravar esse escopo transformaria uma exceção numa regra geral falsa. O comportamento atual (contradição bloqueia, compatibilidade não confirma) **é seguro** e continua valendo até a decisão.

### Achado 2 — seis campos que a documentação não anunciava

`transactionOriginType`, `pixKey`, `finality`, `canModifyCashValue`, `description`, `cannotBePaidReason`.

- **`description` foi mapeado** (`PixLookupSnapshot.Description`): diz ao aprovador *do que se trata* a cobrança, e isso é exatamente o que a tela precisa.
- **`canModifyCashValue`** é o nome real do flag de Pix Troco — a documentação sugeria `canBeModifyChangeValue`. Fora de escopo, mas registrado para a fase 3 não procurar o nome errado.
- Os demais ficaram de fora por não alimentarem check nenhum.
- `cannotBePaidReason` **já estava mapeado** desde a 1.3; era falso positivo da própria sonda, corrigido.

### Achado 3 — o Pix cobre o buraco da arrecadação

O QR sondado é de **arrecadação** (Receita Federal) — e devolveu **documento do beneficiário**, que a consulta por código de barras não devolve em 100% dos casos medidos.

Ou seja: **num documento de arrecadação híbrido, o trilho Pix entrega a verificação forte que o trilho boleto não tem.** Isso reforça o [`ADR-010`](adr/ADR-010-pix-preferido-sobre-boleto.md) por um motivo novo, que não estava no ADR original: Pix não é só mais rápido e cancelável, ele **verifica mais** onde o boleto verifica menos.

## Lacuna original (fechada pelo teste acima)

A sonda acima cobre **um** dos dois trilhos. O `POST /v3/pix/qrCodes/decode` **nunca teve resposta de sucesso observada, em ambiente nenhum**:

- Da sprint 1.0 saiu só o **403 `insufficient_permission`** sem a permissão de saque — comportamento de erro, nada sobre o corpo de sucesso.
- O mapeamento de `AsaasPixLookupService`/`PixLookupSnapshot` veio da **documentação** do provedor, não de medição.
- Os testes de unidade usam um `StubHttpMessageHandler` cujo JSON foi escrito **a partir dessa mesma documentação**. Eles provam que a tradução está correta *dado aquele contrato*; **não provam que o contrato é o real**. É a mesma classe de lacuna que a cobrança tinha.

**Por que pesa mais do que parece:**

1. O [`ADR-010`](adr/ADR-010-pix-preferido-sobre-boleto.md) faz do Pix o **trilho preferencial** — havendo QR, é por ele que se paga. O trilho preferido é o de contrato não verificado.
2. O check `PixBarcodeConsistency` — a defesa contra **QR adulterado colado sobre boleto verdadeiro**, o vetor mais direto em circulação — compara as duas consultas. Uma delas nunca foi vista funcionando.
3. O documento do recebedor **só existe nessa resposta**: o BR Code carrega chave e nome, nunca CPF/CNPJ. Se o decode não devolver `cpfCnpj`, o trilho preferencial fica sem check forte de beneficiário e o ADR-010 precisa ser reaberto.

**Como fechar:** [`tools/smoke-probe-pix-decode.js`](tools/smoke-probe-pix-decode.js), mesmos pré-requisitos da sonda de boleto (chave de produção com permissão de saque, whitelist de IP), mais o **payload do BR Code** — o texto do "Pix Copia e Cola". O script confere o CRC localmente antes de enviar e, além dos campos, reporta **aderência ao contrato**: campos esperados que não vieram e campos novos que o adapter não mapeia.

> Obstáculo prático já observado: nos dois boletos de agosto do corpus o BR Code **não está como texto no PDF** — só como imagem de QR. Extrair payload de imagem é a cascata da fase 2 (`IDocumentIntelligence`); para a sonda, basta ler o QR com o celular e copiar o "Copia e Cola".

## Como ler o zero da cobrança: ele não é sobre a cobrança

As 12 falharam com `unregistered_bank_slip` — *"Boleto não registrado na rede bancária."* A leitura tentadora é "a consulta não cobre cobrança". Ela está errada.

**Experimento que separa as hipóteses:** emitimos um boleto **dentro do próprio sandbox** (`POST /customers` → `POST /payments` → `GET /payments/{id}/identificationField`) e consultamos a linha digitável dele no mesmo sandbox. Resultado: **`unregistered_bank_slip` também**.

O sandbox não resolve nem o boleto que ele mesmo acabou de emitir. Logo **não existe registro de cobrança em sandbox** e o 0/12 não mede nada sobre produção — mede a ausência do ambiente.

> **Isto é uma lacuna de validação, não um resultado.** O caminho de verificação de cobrança bancária — que é a maioria do volume — **só pode ser exercido com chave de produção contra um boleto real**. Ver "Consequências" abaixo.

## Como ler os zeros da arrecadação

Dois deles são de naturezas diferentes, e confundi-los levaria a decisão errada.

**`bank` = 0% é estrutural.** O código de barras de arrecadação (FEBRABAN, 48 dígitos iniciados em 8) **não tem campo de banco**: ele carrega produto, segmento, identificador de valor, valor e o identificador da empresa/convênio. Não há de onde tirar banco recebedor — contas de convênio liquidam fora da compensação bancária tradicional. O check de banco é **inaplicável** a arrecadação, em qualquer provedor.

> **E isso torna o zero irrelevante para cobrança.** Em cobrança bancária o banco não precisa vir da consulta: **as posições 1–3 do código de barras são o COMPE do banco liquidante**, protegidas pelo DV geral. `DigitableLine.BankCode` já lê esse campo e é a fonte do check 6 ([doc 03](03-bill-validation.md)) — a consulta virou conferência cruzada, não dependência. Um provedor fora do ar deixa de derrubar o check de banco.

**`beneficiaryCpfCnpj` = 0% é parcialmente estrutural.** O código de barras também não carrega o documento do beneficiário — carrega o **identificador de convênio** de 8 dígitos. Resolver isso para um CNPJ exige tabela de convênios do lado do provedor. O Asaas demonstra ter essa tabela (devolve `"SABESP"`, `"DAE DEPARTAMENTO AGUA"`, `"RECEITA FEDERAL/SP"`), mas **devolve só o nome**. Se produção enriquece com o documento, não é possível saber daqui.

**`dueDate` = 30% é esperado.** Arrecadação frequentemente usa fator de vencimento zerado; a data real vive no corpo do documento, não na linha digitável.

## Consequências para o modelo

### O check de beneficiário tem duas forças, não uma

O [doc 03](03-bill-validation.md) assume documento contra documento. Para arrecadação, isso não existe. O que existe é nome — e é exatamente para isso que `Payee` já tem `LegalName` + `Aliases` + `MatchesName`.

| Check | Cobrança bancária | Arrecadação |
|---|---|---|
| Beneficiário por documento | **disponível** (medido em produção) | **indisponível** |
| Beneficiário por nome | disponível | disponível (100%) |
| Valor | **conclusivo** | **conclusivo** (100%) |
| Banco recebedor | **disponível** (e o código de barras já bastava) | **inaplicável** (estrutural) |
| Vencimento | **disponível** | fraco (30%) |

Um check que compara nome não vale o mesmo que um que compara documento: nome é falsificável e varia em grafia. Ele **não pode reprovar sozinho** nem, isolado, autorizar pagamento — é evidência de apoio.

### O que protege o usuário na arrecadação, então

Não é o beneficiário. É a combinação de:

1. **Valor conclusivo** (100%) — o que vai ser pago é exatamente o que a linha digitável diz, cruzado com a `AmountPolicy` do `Payee`.
2. **A expectativa** ([doc 11](11-bill-expectations.md)) — o sistema sabe que espera uma conta da SABESP neste mês; um boleto de arrecadação de origem inesperada é anomalia por si só.
3. **A origem confiável** ([`TrustedOrigin`](02-domain-model.md)) — de onde o documento chegou.

Isso é mais fraco do que a defesa que a cobrança bancária terá. **É uma decisão de produto**, não um detalhe de implementação: arrecadação será aprovada com evidência mais fraca, e a interface de aprovação precisa dizer isso ao usuário em vez de mostrar um "verificado" que não se sustenta.

### Decisões que ficam em aberto

1. ~~**Severidade do check de beneficiário para arrecadação.**~~ **Decidido (usuário, 2026-07-31): `Warning`** — divergência de nome contra um `Payee` cadastrado conta e aparece, mas não bloqueia. Registrado no [doc 03](03-bill-validation.md), check 5, motivo `payee_name_divergence`.
2. ~~**Sonda de fumaça em produção**~~ — **feita em 2026-08-06, verde.** Ver a seção no topo deste documento.
3. **Vale reconsultar `beneficiaryCpfCnpj` de arrecadação em produção?** Se voltar preenchido, o check forte volta para arrecadação e este documento muda. Mesma sonda do item 2.

## A sonda de fumaça — como fechar a lacuna da cobrança

A ferramenta existe: [`tools/smoke-probe-production.js`](tools/smoke-probe-production.js). Ela faz **uma** chamada, read-only, e responde a única pergunta em aberto: *em produção, `bill/simulate` devolve `beneficiaryCpfCnpj` para cobrança bancária registrada?*

**Pré-requisitos** (nesta ordem):

1. Conta Asaas de **produção** com cadastro/KYC concluído.
2. **Whitelist de IP** configurada no painel do Asaas — antes de gerar a chave, não depois.
3. Chave de API com **permissão de saque via API** habilitada. Não há como fugir disso: é pré-requisito da própria consulta (ver acima). **A chave é capaz de pagar contas.**
4. Um boleto de **cobrança bancária** real, **recente e não pago**, de emissor grande. Boleto antigo, já pago ou de emissor pequeno tem chance maior de voltar `unregistered_bank_slip` por motivo que não é o que se quer medir.

```powershell
$env:ASAAS_PRODUCTION_API_KEY = "<chave>"
node tools/smoke-probe-production.js "<linha digitável de 47 dígitos>" --producao
Remove-Item Env:\ASAAS_PRODUCTION_API_KEY   # e revogue a chave no painel
```

**Os três desfechos e o que cada um decide:**

| Desfecho | O que significa | O que fazer |
|---|---|---|
| **Verde** — `beneficiaryCpfCnpj` preenchido | O check `PayeeMatch` bloqueante tem base para cobrança bancária | Marcar o item no checklist do `CLAUDE.md`, atualizar a tabela de Resultado acima. **A Fase 1 fica pronta.** |
| **Vermelho** — resolveu, sem documento | Cobrança degrada para cotejo **por nome**, igual à arrecadação | Rever o [doc 03](03-bill-validation.md) check 5 e a promessa da interface. **Não é detalhe** — muda o desenho da aprovação para a maioria do volume |
| **`unregistered_bank_slip`** | Não mediu nada ainda | Tentar outro boleto (recente, não pago, emissor grande). Repetindo com vários, vira evidência de que produção também não resolve |

O script recusa linha de arrecadação de propósito: aquela cobertura já foi medida e não tem lacuna.

## Reexecutar

```powershell
# 1. extrair as linhas do corpus (requer poppler para o pdftotext)
node tools/analyze-boleto-corpus.js <pasta-txt> --json > lines.json

# 2. medir a cobertura (lê a chave de ASAAS_SANDBOX_API_KEY ou do user-secrets)
node tools/probe-asaas-simulate.js lines.json
```

Atualize a tabela de "Resultado" acima sempre que reexecutar em ambiente diferente — sobretudo na primeira execução contra produção, que é a que fecha as lacunas.
