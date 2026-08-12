# 07 — Multi-tenancy, isolamento e roteamento de boletos

O BC atende **pessoas físicas e jurídicas** no mesmo modelo, e uma mesma fonte de captura (uma caixa de e-mail) pode trazer boletos de mais de um tenant. Este documento define quem enxerga o quê, e como cada boleto encontra o dono certo.

Base empírica das decisões: [`08-boleto-corpus-findings.md`](08-boleto-corpus-findings.md).

## O tenant

`TenantId` é a unidade de isolamento e de dinheiro: um tenant tem seu cadastro fiscal, suas fontes, seus beneficiários, sua subconta Asaas e seu saldo. Um tenant é **uma pessoa física ou uma pessoa jurídica** — a diferença mora em três lugares e em nenhum outro:

| | PF | PJ |
|---|---|---|
| `TaxIdKind` do documento principal | `CPF` | `CNPJ` |
| Subconta Asaas | pessoa física | pessoa jurídica (exige `companyType`) |
| Alçada / segregação de funções | opcional (o titular aprova) | aplicável |

O Aggregate é o mesmo. Nenhum `if (isPessoaFisica)` deve aparecer em regra de captura, verificação ou pagamento.

### `PayerProfile` — Aggregate Root (`BLP.PRF`)

A identidade fiscal do tenant. É contra ele que o check de pagador compara.

| Campo | Tipo | Nota |
|---|---|---|
| `Id` / `TenantId` | | 1:1 com o tenant |
| `Kind` | `PayerKind` — `Individual` \| `Company` | |
| `LegalName` | `string` | |
| `PrimaryTaxId` | `TaxId` | CPF ou CNPJ |
| `AdditionalTaxIds` | `IReadOnlyCollection<TaxId>` | Filiais (PJ), CPF do titular junto do CNPJ (MEI), cônjuge |
| `MatchByCnpjRoot` | `bool` | Quando `true`, CNPJ com a mesma raiz (8 primeiros dígitos) casa como próprio — cobre filial cujo boleto chega sem cadastro prévio |
| `AsaasAccountRef` | `string?` | Ponteiro no cofre para a chave da subconta |

Invariantes: `PrimaryTaxId` obrigatório e coerente com `Kind` (`BLP.PRF01`); `MatchByCnpjRoot` só é permitido quando `Kind = Company` (`BLP.PRF02`); TaxId não se repete entre tenants como *primário* — dois tenants com o mesmo CNPJ primário é erro de cadastro (`BLP.PRF03`).

## Isolamento

Regra base: **todo Aggregate Root carrega `TenantId`, e toda leitura filtra por ele.** Nenhuma tela mostra dado de outro tenant. As três exceções abaixo são as únicas, são estreitas e são deliberadas.

### Exceção 1 — aviso de fonte já monitorada

Ao conectar uma caixa de e-mail que outro tenant já monitora, o usuário recebe: *"esta caixa já é monitorada por outra conta do sistema"*. Sem dizer **quem**, sem dizer quantos, sem listar nada.

O aviso só aparece **depois** de o OAuth concluir com sucesso — ou seja, depois de o usuário provar que controla a caixa. Perguntar antes transformaria o endpoint em oráculo para descobrir quais endereços estão cadastrados na plataforma.

Consequência: existe um índice global de endereços de fonte, fora do filtro de tenant, acessível por **um** caminho de código, que devolve `bool` e nada mais. Qualquer outro uso desse índice é violação.

### Exceção 2 — unicidade global da linha digitável

Um boleto é pago **uma vez**. Se o tenant A já tem uma Bill ativa com a linha X, o tenant B não pode criar outra — o check `Duplicate` é **global**, não por tenant.

O tenant B vê: *"este boleto já está sob gestão de outra conta do sistema"*, sem identificação. É informação mínima e necessária: a alternativa é pagamento duplicado, que é irreversível.

### Exceção 3 — conflito de reivindicação

Quando dois tenants tentam reivindicar o mesmo `CaptureItem` não roteado, o segundo recebe o mesmo aviso genérico. Mesma justificativa da exceção 2.

### O que continua isolado sem exceção

- Fontes de captura: um usuário **nunca** vê a fonte, o cursor, o histórico de sincronização ou os itens capturados de outro tenant.
- Beneficiários, regras de roteamento, boletos, aprovações, pagamentos, relatórios, saldo.
- O conteúdo de qualquer boleto de outro tenant — inclusive um que tenha entrado pela fonte dele. Ver "quarentena" abaixo.

## Fontes compartilhadas

Duas pessoas usam `contas@empresa.com` para receber contas de dois CNPJs diferentes. O modelo:

**Cada tenant conecta a caixa como sua própria `CaptureSource`.** São dois Aggregates distintos, dois `CredentialRef` distintos, dois cursores de sincronização, duas pipelines de ingestão. Elas não se conhecem.

```
contas@empresa.com
   ├── CaptureSource(tenant=RUF)  → ingere tudo → roteia → Bills do RUF; resto em quarentena
   └── CaptureSource(tenant=RBC)  → ingere tudo → roteia → Bills do RBC; resto em quarentena
```

Cada pipeline lê **todas** as mensagens (não há como filtrar no servidor por dono do boleto) e só promove a `Bill` o que roteia para o seu próprio tenant. O resto vira `CaptureItem` em quarentena.

Por que duplicar a ingestão em vez de compartilhar uma fonte entre tenants:

- **Isolamento por construção.** Uma fonte com dois donos exigiria autorização por linha em cima do mesmo Aggregate — o tipo de coisa que vaza na primeira query esquecida.
- **Credencial é de quem conectou.** Se o tenant A revoga o OAuth, o tenant B continua funcionando.
- **Sem privilégio implícito.** Quem conectou a caixa já consegue lê-la no cliente de e-mail; o sistema não amplia o acesso de ninguém.

Custo aceito: a mesma mensagem é lida N vezes. Para caixas de contas a pagar, o volume é irrisório.

## Quarentena

`CaptureItem` cuja rota não é o tenant da fonte **não vira Bill**. Fica com status:

| Status | Significado | O que o dono da fonte vê |
|---|---|---|
| `Promoted` | Roteou para este tenant, virou Bill | tudo |
| `ForeignPayer` | Pagador identificado e **não é** deste tenant | remetente, assunto, data, e o motivo — **não** o valor, beneficiário ou linha digitável |
| `Unrouted` | Não foi possível determinar o dono | remetente, assunto, data, beneficiário e valor — pode reivindicar |
| `Unrecognized` | Nenhum boleto válido no anexo | remetente, assunto, data |
| `Discarded` | Duplicata de item já processado | ponteiro para o item original |

A distinção entre `ForeignPayer` e `Unrouted` importa: no primeiro caso o sistema **sabe** que não é do usuário, e mostrar o conteúdo seria vazamento gratuito; no segundo o sistema não sabe, e o usuário precisa de dado suficiente para decidir se é dele.

## A escada de roteamento

Um `CaptureItem` com boleto válido passa pelos degraus na ordem. O primeiro que resolve, resolve. Executada pelo Domain Service **`BillRoutingService`**.

> ⚠️ **Medido na sprint 2.6 (2026-08-12) — este capítulo foi corrigido pela realidade.** 714 documentos de 14 meses do arquivo real. Dois números do desenho original estavam errados, e o **degrau 2 foi abandonado**. O que está implementado é a escada 0 → 1 → 3 → 4 descrita abaixo; o degrau 2 fica registrado como achado negativo, não como pendência. Ferramenta: [`tools/analyze-account-reference.js`](tools/analyze-account-reference.js).
>
> | Degrau | Estimado no desenho | Medido | Situação |
> |---|---|---|---|
> | 0 — senha derivada | — | funciona (11 PDFs na 2.3) | ✅ implementado |
> | 1 — TaxId do pagador | ~38% | **93,3%** | ✅ implementado |
> | 2 — `RoutingRule` | "a maior parte do resto" | **chave não distingue pagadores** | ❌ abandonado |
> | 3 — beneficiário exclusivo | resíduo | resíduo | ✅ implementado |
> | 4 — `Unrouted` | — | — | ✅ implementado |

### Degrau 1 — TaxId do pagador extraído (medido: 93,3%)

Pagador extraído do PDF (nome + CPF/CNPJ, com DV validado) confrontado com `PayerProfile.PrimaryTaxId` + `AdditionalTaxIds`, e com a raiz do CNPJ quando `MatchByCnpjRoot`.

- Casou com o tenant da fonte → **rota definida**, confiança `Strong`. **Não exige rótulo**: medido, em 0% dos documentos o TaxId do tenant apareceu do lado do beneficiário, e exigir rótulo custaria 31 pontos de cobertura (93,3% → 62,3%).
- Extraído **sob rótulo de pagador** e não casa com este tenant → `ForeignPayer`.
- Extraído **sem rótulo** e não casa → **`Unrouted`, nunca `ForeignPayer`.** Esta é a correção mais importante da 2.6: todo boleto traz o CNPJ do **beneficiário** impresso, e ele nunca é do tenant. Tratar isso como "é de terceiro" mandaria toda conta de concessionária para a quarentena cega — que não expõe valor e **não pode ser reivindicada** —, e o usuário perderia a própria conta sem ter como recuperá-la. Só 66,8% das ocorrências têm rótulo por perto, e é sobre essas que a negativa pode se apoiar.

### ~~Degrau 2 — regra de roteamento aprendida~~ — ABANDONADO na 2.6

O desenho previa `RoutingRule` casando por `(PayeeTaxId, AccountReference)`, com a referência de conta — instalação, conta contrato, matrícula — saindo do documento. **A medição derrubou a premissa: essa referência não existe de forma estável e, onde parece existir, não distingue pagadores.**

O que se repete entre meses no campo livre do código de barras é a **agência/conta do beneficiário**; o que varia é o nosso número, que muda a cada emissão. Consequência medida em dois pagadores do mesmo emissor:

```
DESPACON   RBC2    109000······3036997801000     19/25 posições estáveis
DESPACON   RUFINO  109000······3036997801000     as MESMAS 19, idênticas
SECONCI    RUFINO  90545357000000·······010·     17/25 posições estáveis
SECONCI    RBC2    90545357000000·······010·     as MESMAS 17, idênticas
```

Uma regra aprendida com essa chave casaria com o boleto **dos dois tenants** e roteria o do outro — exatamente a falha que o [`ADR-008`](adr/ADR-008-fontes-compartilhadas-e-isolamento.md) existe para impedir. Em arrecadação é pior: DAS e DARF têm 5–6 dígitos estáveis, todos identificadores do tributo.

**O aprendizado passou para o degrau 3**, chaveado pelo `Payee` vinculado ao tenant — que é chave que de fato distingue. O Aggregate `RoutingRule` e a sigla `BLP.RTR` **não foram criados**; a travessia de tenant que o doc previa (`IRoutingRuleRepository.ExistsForPairInAnyTenantAsync`) foi substituída por `IPayeeRepository.IsRegisteredByAnotherTenantAsync`.

**Para reabrir:** refaça a medição com `tools/analyze-account-reference.js`. Ela só muda de conclusão se aparecer referência de conta **no texto** do documento (não no código de barras) com cobertura que justifique o Aggregate.

### Degrau 3 — beneficiário exclusivo

Se o beneficiário do boleto é `Payee` de **um único** tenant, e nenhum outro degrau contradisse, rota definida com confiança `Weak`. A exclusividade é apurada por `IPayeeRepository.IsRegisteredByAnotherTenantAsync`, que devolve `bool` e nada mais.

**É também o mecanismo de aprendizado da escada**, no lugar do degrau 2: cadastrar o beneficiário — à mão ou como desdobramento de uma reivindicação — faz o próximo boleto dele rotear sozinho.

Este degrau **nunca** dispensa a aprovação humana e **nunca** sobrepõe um `ForeignPayer` do degrau 1. Ele existe para reduzir fila, não para decidir sozinho.

### Degrau 4 — quarentena `Unrouted`

Nada resolveu. O item fica na fila de reivindicação do dono da fonte.

### Reivindicação

`POST /capture-items/{id}/claim` promove o item a `Bill` do tenant que reivindicou. **Não cria `RoutingRule`** (ver o degrau 2): o que faz o próximo boleto do mesmo beneficiário rotear sozinho é o `Payee` cadastrado, pelo degrau 3.

O artefato guardado é **relido pelo mesmo parser** — quem reivindica escolhe de *quem* é o boleto, nunca *o que* ele diz, e a linha volta a passar pelos mesmos dígitos verificadores do caminho automático. A `Bill` nasce **sem `ExtractedPayer`**: ninguém constatou o pagador, e preencher o campo com o CNPJ do credor faria o check `PayerMatch` reprovar o boleto por contradizer o cadastro.

Guardas:

- Reivindicar exige `bill:import` e fica gravado com `UserId` e instante.
- Se o boleto tem pagador extraído que **contradiz** o tenant, a reivindicação é **recusada** (`BLP.CPI04`) — a escada já sabia que não era dele.
- Se outro tenant já tem o mesmo instrumento sob gestão, o segundo recebe o aviso genérico da exceção 2 (`BLP.BIL02`) — a unicidade global do instrumento é quem resolve a corrida, inclusive entre duas reivindicações simultâneas.
- A `Bill` criada por reivindicação nasce com o check de roteamento registrado como `Claimed`, visível na tela de aprovação. Aprovar um boleto reivindicado é uma decisão consciente, não um caminho silencioso.

## Impacto nas verificações

Duas mudanças no catálogo de [`03-bill-validation.md`](03-bill-validation.md):

**`PayerMatch` deixa de ser puramente Advisory.** A severidade passa a depender do que se conseguiu apurar:

| Situação | Outcome | Severidade |
|---|---|---|
| Pagador extraído e casa com o `PayerProfile` | `Passed` | — |
| Pagador extraído e **não** casa | `Failed` | **Blocking** |
| Pagador não extraível, rota veio do degrau 1 ou 2 | `Inconclusive` | Advisory |
| Pagador não extraível, rota veio do degrau 3 ou de reivindicação | `Inconclusive` | Advisory, **destacado** |

O ponto que o requisito exige e que esta tabela garante: **um usuário nunca paga a conta de outro por acidente do sistema.** Se o boleto diz de quem é e não é dele, o pagamento é impedido. Se o boleto não diz, o usuário assume a decisão explicitamente, com registro.

**Check novo `TenantRouting`.** Registra por qual degrau o boleto chegou (`Strong` / `Weak` / `Claimed` — `Learned` ficou sem produtor quando o degrau 2 foi abandonado) e com que evidência. Advisory, mas é o que dá ao aprovador o contexto de quanto confiar na atribuição.

## ~~`RoutingRule` — Aggregate Root (`BLP.RTR`)~~ — NÃO IMPLEMENTADO

Projetado, medido e abandonado na sprint 2.6 — ver o degrau 2 acima. A chave `(PayeeTaxId, AccountReference)` não distingue pagadores, então a regra roteria o boleto de um tenant para outro.

**A sigla `BLP.RTR` fica queimada de propósito**: não a reutilize para outro Aggregate, para que o achado continue greppável.

## Subcontas Asaas

Decidido: **uma subconta Asaas por tenant** (`POST /v3/accounts`), com a chave de API guardada no cofre e referenciada por `PayerProfile.AsaasAccountRef`.

- Segregação de dinheiro entre clientes não depende do nosso código — é o provedor que garante.
- Saldo, extrato, taxas e comprovantes já saem por tenant, sem rateio nosso.
- PF abre subconta com CPF; PJ com CNPJ e `companyType`.
- Custo: um passo de onboarding a mais (criar a subconta e o cliente completar o cadastro/KYC no Asaas antes de conseguir pagar). Entra como estado do tenant: `AsaasAccountRef` nulo ⇒ o tenant usa o sistema até `Approved`, mas não consegue agendar.
- A chave da conta-plataforma (usada só para criar subcontas) é segredo de infraestrutura, separado das chaves de subconta.

## Regras invioláveis

1. Toda query, todo `ExistsAsync`, todo repositório filtra por `TenantId`. Sem exceção fora das três listadas acima.
2. As três exceções devolvem **booleano ou aviso genérico** — nunca identidade, nunca conteúdo.
3. Nenhum boleto vira `Bill` sem rota determinada. Não existe atribuição por default ao dono da fonte.
4. `ForeignPayer` não expõe conteúdo financeiro.
5. Reivindicação é ato humano registrado, e é recusada quando o pagador extraído contradiz.
7. **Atribuir exige casar com o cadastro do próprio tenant; recusar exige rótulo de pagador.** A assimetria é deliberada — ver o degrau 1.
6. Uma linha digitável, uma `Bill` ativa — globalmente.
