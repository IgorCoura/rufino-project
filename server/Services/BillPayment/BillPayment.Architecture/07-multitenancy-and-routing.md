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

### Degrau 1 — TaxId do pagador extraído (cobre ~38%)

Pagador extraído do PDF (nome + CPF/CNPJ, com DV validado) confrontado com `PayerProfile.PrimaryTaxId` + `AdditionalTaxIds`, e com a raiz do CNPJ quando `MatchByCnpjRoot`.

- Casou com o tenant da fonte → **rota definida**, confiança `Strong`.
- Casou com outro TaxId conhecido → `ForeignPayer`.
- Extraído mas não casa com ninguém → `ForeignPayer` (é boleto de terceiro).

### Degrau 2 — regra de roteamento aprendida (cobre a maior parte do resto)

`RoutingRule` casando por `(PayeeTaxId, AccountReference)` — o identificador que a concessionária usa: conta contrato, instalação, matrícula, inscrição, número de cliente. É o degrau que resolve energia, água, telefonia, sindicato, FGTS e condomínio, que são exatamente os que não trazem CNPJ.

Confiança `Learned`. A regra nasce quando o usuário reivindica um item `Unrouted` (ver abaixo).

### Degrau 3 — beneficiário exclusivo

Se o beneficiário do boleto é `Payee` de **um único** tenant entre os que monitoram aquela caixa, e nenhum outro degrau contradisse, rota definida com confiança `Weak`.

Este degrau **nunca** dispensa a aprovação humana e **nunca** sobrepõe um `ForeignPayer` do degrau 1. Ele existe para reduzir fila, não para decidir sozinho.

### Degrau 4 — quarentena `Unrouted`

Nada resolveu. O item fica na fila de reivindicação do dono da fonte.

### Reivindicação

`POST /capture-items/{id}/claim` promove o item a `Bill` do tenant que reivindicou e **cria a `RoutingRule`** correspondente, para o próximo boleto do mesmo par (beneficiário, referência de conta) rotear sozinho no degrau 2.

Guardas:

- Reivindicar exige `bill:import` e fica gravado com `UserId` e instante.
- Se o boleto tem pagador extraído que **contradiz** o tenant, a reivindicação é **recusada** (`BLP.CPI04`) — a escada já sabia que não era dele.
- Se outro tenant já reivindicou, o segundo recebe o aviso genérico da exceção 3.
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

**Check novo `TenantRouting`.** Registra por qual degrau o boleto chegou (`Strong` / `Learned` / `Weak` / `Claimed`) e com que evidência. Advisory, mas é o que dá ao aprovador o contexto de quanto confiar na atribuição.

## `RoutingRule` — Aggregate Root (`BLP.RTR`)

| Campo | Tipo |
|---|---|
| `Id` / `TenantId` | |
| `PayeeTaxId` | `TaxId` — beneficiário ao qual a regra se aplica |
| `AccountReference` | `string` normalizado — instalação, conta contrato, matrícula, inscrição |
| `ReferenceKind` | `AccountReferenceKind` — Smart Enum, para a UI explicar o que é |
| `LearnedFrom` | `CaptureItemId` |
| `CreatedBy` / `CreatedAt` | `UserId` / `DateTimeOffset` |
| `IsActive` | `bool` |

Único por `(TenantId, PayeeTaxId, AccountReference)` — `BLP.RTR01`.

**Conflito entre tenants é possível e precisa ser tratado:** dois tenants criando regra para o mesmo par significa que um dos dois reivindicou errado. O sistema detecta na criação, recusa a segunda (`BLP.RTR02`) com o aviso genérico, e a resolução é humana.

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
6. Uma linha digitável, uma `Bill` ativa — globalmente.
