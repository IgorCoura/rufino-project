# 15 — A escada de link no regime aberto: o que muda, e como configurar o ambiente

> Complementa o [ADR-023](adr/ADR-023-escada-de-link-aberta-e-a-fronteira-de-rede.md), que registra
> **por quê**. Este documento é **operacional**: o que ligar, em que ordem, e como conferir que
> funcionou. Ambiente de referência: contêiner no **Dokploy**, VPS **Hostinger**.

## 1. O que está ligado por padrão depois desta entrega

| Trava | Sem proxy | Com proxy |
|---|---|---|
| **IP conferido e pinado no `connect`** | ✅ é a barreira | ⛔ **desligada** — quem confere é o proxy |
| Faixas reservadas completas (`::`, `::x.x.x.x`, NAT64, 6to4, `198.18/15`, TEST-NET) | ✅ | via `squid.conf` |
| Faixas configuráveis (`BlockedCidrs` / `AllowedCidrs`) | ✅ | via `squid.conf` |
| Sem redirecionamento, sem cookie, só `GET` | ✅ | ✅ |
| Teto de bytes + teto de leitura do corpo (slowloris) | ✅ | ✅ |
| Magic bytes para PDF **e** imagem | ✅ | ✅ |
| Allowlist de porta (`AllowedPorts`) | ✅ (no fechado, a receita a dispensa) | ✅ |
| Profundidade, orçamento, teto por host, visitados | ✅ | ✅ |
| Teto diário por remetente | ✅ | ✅ |

⚠️ **As três primeiras linhas trocam de dono quando há proxy, e não se somam.** Com
`LinkResolution:Proxy` preenchido, a conexão TCP que o processo abre é para o *proxy* — conferir o
endereço ali validaria o IP dele, que é interno, e derrubaria toda busca. Por isso o pinning é
desligado e **o `squid.conf` passa a ser o lugar onde as faixas proibidas vivem**. Errar o
`squid.conf` com proxy ligado é o mesmo que não ter barreira nenhuma.

**`Mode` nasce `Allowlist`, e sem proxy.** Tudo da coluna "Sem proxy" já está valendo hoje, sem você
ligar nada — inclusive num deploy que nunca abra o regime.

## 2. Configuração mínima da VPS, na ordem

### Passo 1 — descobrir o IP de SAÍDA do contêiner

Não é o IP do painel da Hostinger nem necessariamente o da VPS: é por onde o contêiner sai. Rode
**de dentro do contêiner** (Dokploy → *Terminal* do serviço):

```bash
curl -s https://api.ipify.org;  echo
curl -s -6 https://api64.ipify.org; echo   # vazio = sem IPv6 de saída
```

⚠️ **Se o IPv6 responder, anote-o também.** É a mesma armadilha já registrada para a whitelist do
Asaas: com IPv6 ativo o contêiner pode resolver um host por AAAA e sair por um endereço que você não
cadastrou.

### Passo 2 — bloquear os seus próprios endereços públicos

É o passo que o código **não tem como fazer sozinho**: ele recusa faixa reservada, mas não sabe que
`203.0.113.10` é a sua própria API. No Dokploy → *Environment*:

```
LinkResolution__BlockedCidrs__0=203.0.113.10/32
LinkResolution__BlockedCidrs__1=2001:db8:1234::/48
```

Troque pelos endereços do passo 1. **A sintaxe de lista do .NET é por índice** (`__0`, `__1`, …) —
não existe forma separada por vírgula, e escrever `BlockedCidrs=a,b` é lido como **uma** faixa
inválida, que derruba o arranque (de propósito).

Se você tem outros serviços na mesma VPS ou na mesma rede do provedor, bloqueie a **faixa** em vez
do endereço solto — a rede interna do Dokploy costuma ser privada e já está coberta, mas um segundo
IP público seu, não.

### Passo 3 — o egresso, e por que ele exige separar o worker

⚠️ **Esta seção foi reescrita em 2026-09-10.** A versão anterior mandava filtrar por IP de contêiner
no `DOCKER-USER` e tratava o proxy como alternativa equivalente. **As duas coisas estavam erradas
para o Dokploy**, e a correção veio de uma revisão externa:

> No Docker, um contêiner ganha rota para a internet se estiver ligado a **qualquer** rede
> não-`internal`. O Dokploy liga todo serviço à overlay `dokploy-network` — é assim que o Traefik
> roteia. Enquanto o worker estiver nela, ele alcança a internet direto e **o proxy vira
> decoração**: o código pode simplesmente não usá-lo.

Está correto, e tem duas consequências.

**Consequência A — a regra que governa todo o resto.** A barreira só existe se o contêiner **não
tiver para onde ir** a não ser o proxy. Isso significa: o contêiner que busca boletos só pode estar
ligado a redes com `internal: true` (mais a rede do proxy).

**Consequência B — a API não pode ser esse contêiner.** Ela recebe tráfego pelo Traefik, então tem
de estar na `dokploy-network`, e aí o isolamento é impossível. E hoje **a busca roda dentro da API**:
`CaptureSyncBackgroundService`, `CaptureProcessingBackgroundService` e `CaptureVisionBackgroundService`
são `AddHostedService` no `Program.cs` (linhas 39, 43 e 47).

> **Por que a receita de `iptables` que estava aqui não substitui isso:** o Dokploy roda em **Docker
> Swarm**, e a saída de um contêiner de overlay para a internet passa pelo `docker_gwbridge` com um
> IP de origem **efêmero, que muda a cada deploy**. Uma regra `-s <ip-do-container>` fica órfã no
> primeiro redeploy — e falha **aberta**, sem avisar. Não use.

#### O que fazer: dois papéis, uma imagem só

**Você não precisa de um projeto .NET novo.** O que precisa é de um segundo *serviço* no Dokploy,
com a mesma imagem e um conjunto de variáveis diferente:

| | `billpayment-api` | `billpayment-worker` |
|---|---|---|
| Traefik / domínio | sim | **não** |
| `dokploy-network` | sim | **destacada** |
| Redes | `dokploy-network` + `bp-data` | `bp-data` (`internal: true`) + `bp-egress` |
| `Capture__Enabled` | `false` | `true` |
| `BillReading__Enabled` | `false` | `true` |
| `Outbox__Enabled` | `false` | `true` |
| `Expectations__Enabled` | `false` | `true` |
| `PaymentSubmission__Enabled` | `false` | `true` |
| `PaymentReconciliation__Enabled` | `false` | `true` |
| `PaymentWebhookSweep__Enabled` | `false` | `true` |
| `LinkResolution__Proxy` | — | `http://bp-proxy:3128` |

⚠️ **Ligue cada worker em UM deployment só.** O `CLAUDE.md` já adverte isso para o outbox e para as
expectativas; com dois serviços a advertência passa a valer para todos. O worker fica com tudo, a
API com nada.

O worker continua subindo o Kestrel — é inofensivo, já que nada roteia para ele. Se incomodar,
`ASPNETCORE_URLS=http://127.0.0.1:5000` o mantém preso ao loopback.

#### Por que `internal: true` sozinho **não** funciona

O worker também precisa alcançar **Microsoft Graph, Asaas, Gemini e o balde**. Uma rede internal
pura mata a captura junto com o SSRF. O desenho é `internal` **mais um proxy**:

```yaml
networks:
  bp-data:    { internal: true }   # worker ↔ Postgres ↔ balde. Sem saída.
  bp-egress:  {}                   # SÓ o worker e o proxy vivem aqui.

services:
  bp-proxy:
    image: ubuntu/squid:latest
    networks: [bp-egress]
    volumes: [./squid.conf:/etc/squid/squid.conf:ro]

  billpayment-worker:
    image: <sua-imagem>
    networks: [bp-data, bp-egress]
    environment:
      LinkResolution__Proxy: "http://bp-proxy:3128"
      HTTP_PROXY:  "http://bp-proxy:3128"
      HTTPS_PROXY: "http://bp-proxy:3128"
      NO_PROXY:    "billpayment.db,billpayment.storage,localhost,127.0.0.1"
```

E o `squid.conf` faz o *default-deny* que interessa:

```
acl interna dst 10.0.0.0/8 172.16.0.0/12 192.168.0.0/16 169.254.0.0/16 127.0.0.0/8 ::1/128
acl seguras port 80 443
http_access deny interna
http_access deny !seguras
http_access allow all
http_port 3128
```

⚠️ **`bp-egress` não é `internal`** — é ela que dá internet ao proxy. O worker está nela também, então
tecnicamente ainda tem rota. Para o isolamento ser real, o proxy fica numa terceira rede com saída e
`bp-egress` vira `internal: true`:

```yaml
networks:
  bp-data:   { internal: true }
  bp-egress: { internal: true }    # worker ↔ proxy, sem saída
  bp-wan:    {}                    # SÓ o proxy
```

Aí o worker está **apenas** em redes internal, e o proxy é a única porta. É a configuração que
satisfaz a regra do começo desta seção.

#### `LinkResolution__Proxy` é obrigatório, e é por causa de um detalhe do nosso código

Os clientes do Graph, do Asaas e do Gemini são handlers padrão e **respeitam `HTTP_PROXY`
automaticamente**. O da escada de link **não**: ele é construído à mão e nasceu com
`UseProxy = false`, porque é ele que faz o IP pinado. Sem preencher `LinkResolution__Proxy`, você
fecharia o egresso de tudo **menos** do único cliente que busca endereço escolhido por terceiro.

**Preencher esta opção desliga o IP pinado**, e a troca é deliberada: com proxy, a conexão TCP que o
processo abre é para o *proxy*, e conferir o endereço ali validaria o IP do próprio proxy — interno,
portanto recusado, derrubando toda busca. Quem decide o que é alcançável passa a ser o proxy.

### Passo 4 — só então ligar o regime aberto

```
LinkResolution__Mode=Open
```

### Passo 5 — tetos (opcional; os padrões são bons)

```
LinkResolution__MaxDepth=5
LinkResolution__MaxFetchesPerMessage=12
LinkResolution__MaxFetchesPerHost=3
LinkResolution__MaxFetchesPerSenderPerDay=200
LinkResolution__TotalTimeoutSeconds=90
LinkResolution__AllowedPorts__0=80
LinkResolution__AllowedPorts__1=443
LinkResolution__BlockedHosts__0=bit.ly
```

`BlockedHosts` casa por sufixo de domínio e serve a encurtador que o desembrulho não desfaz.

### Quanto disso é obrigatório?

| Você quer | Precisa separar o worker? |
|---|---|
| Ficar em `Mode: Allowlist` (**o padrão**) | **Não.** As travas do código são a barreira, e elas já valem. |
| `Mode: Open` com **uma** camada de defesa | Não — mas você fica dependendo de o código estar certo, e o achado das faixas de IPv6 mostra que faixa esquecida acontece. |
| `Mode: Open` com defesa em profundidade | **Sim.** É a única forma de o proxy não ser decoração. |

Separar o worker é bom desenho de qualquer jeito — escala e falha independente da API, e a latência
da IA (5 a 30 s por documento) deixa de competir com a resposta HTTP. Mas **não é pré-requisito para
esta entrega**: em `Allowlist`, tudo funciona hoje.

## 3. Como conferir que está funcionando

### 3.1 As faixas configuradas foram lidas

Se `BlockedCidrs` tiver qualquer entrada malformada, **a aplicação não sobe** e o log traz:

```
LinkResolution:BlockedCidrs tem uma faixa inválida: '...'. Use CIDR (10.0.0.0/8) ou um endereço solto (203.0.113.10).
```

Subiu = as faixas foram compiladas. É a confirmação positiva mais barata que existe aqui.

### 3.2 O bloqueio da rede interna funciona (o teste que importa)

Mande para a caixa monitorada um e-mail com assunto contendo `boleto` e um link para um endereço
interno:

```html
<a href="http://169.254.169.254/latest/meta-data/">Acessar boleto</a>
```

Depois abra a quarentena e confira o `linkOutcome` do item. **O desfecho esperado depende de quem
recusou:**

| Configuração | `linkOutcome` esperado | Log |
|---|---|---|
| **Sem proxy** (pinning ativo) | `Refused` | `warn: Endereço de documento recusado: 169.254.169.254 não resolve para um endereço público permitido.` |
| **Com proxy** | `Unreachable` | `info: 169.254.169.254 respondeu 403 ao pedido do documento no nível 1.` + a linha de negativa no log do Squid |

A diferença não é cosmética: com proxy quem nega é ele, o cliente vê um `403` comum, e o resolvedor
não tem como saber que aquilo foi uma recusa de política. **Confira o log do proxy também**
(`docker logs bp-proxy`) — é lá que a negativa aparece nomeada.

⚠️ **O que reprova em qualquer configuração é `Resolved`.** Se o item resolver, a barreira não
existe — é o único teste desta lista cujo resultado errado é uma vulnerabilidade aberta, e não um
incômodo.

Repita com o **seu próprio IP público** (o do passo 1) para provar que o passo 2 pegou.

#### Prova direta, sem esperar e-mail

De dentro do contêiner do worker:

```bash
# Sem proxy: tem que falhar por recusa da política (o processo nem disca).
# Com proxy: tem que voltar 403 do Squid.
curl -sS -o /dev/null -w '%{http_code}\n' --max-time 5 \
     --proxy "$HTTP_PROXY" http://169.254.169.254/latest/meta-data/ ; echo

# E o caminho legítimo tem que continuar funcionando:
curl -sS -o /dev/null -w '%{http_code}\n' --max-time 10 \
     --proxy "$HTTP_PROXY" https://api.asaas.com/ ; echo
```

Os dois juntos são o que separa "isolado" de "quebrado": o primeiro tem de falhar, o segundo tem de
passar. Rodar só o primeiro não distingue barreira de rede caída.

### 3.3 O regime aberto alcança emissor novo

Mesmo formato, com um link para um PDF público qualquer. O item deve resolver, e o `LinkOutcome`
sair `Resolved`.

### 3.4 A URL parou de vazar no log

```bash
docker logs <container> 2>&1 | grep -i "Sending HTTP request GET" | grep -i "getguia\|/i/\|boleto"
```

**Tem que vir vazio.** Antes desta entrega, os loggers padrão do `IHttpClientFactory` escreviam a
URI completa do boleto — que é credencial ao portador — em nível `Information`.

### 3.5 O desfecho chegou à tela

`GET /api/v1/{tenantId}/capture-items?status=Unrecognized` passa a devolver `linkOutcome` e
`linkNeedsAttention` por item. `linkOutcome: "NoRecipe"` com `linkHost` preenchido é a fila de
emissores a cadastrar; `Refused` repetido do mesmo host merece olhar.

## 4. O que NÃO foi resolvido, e é honesto dizer

- **Injeção de prompt indireta (M2, PARCIALMENTE fechado em 2026-09-10).** O prompt ganhou
  `system_instruction` e cerca com nonce por chamada, e o regime aberto amplia a superfície (o
  conteúdo que chega ao extrator passa a vir de qualquer página). **Não existe defesa que elimine** —
  OWASP LLM01 e o próprio Google tratam o assunto como camadas. O que sobra alcançável são os campos
  **narrativos** (`payeeName`, `description`, `amount`, `dueDate`): eles não passam por dígito
  verificador e chegam à tela de quem aprova. Boleto injetado continua **não sendo pagável** — o que
  ele consegue é mentir na tela.
- **Teto por remetente é em memória** e não é compartilhado entre instâncias — mesma limitação do
  `ExtractionBudget`. Ao escalar horizontalmente, o teto efetivo vira o múltiplo.
- **Com proxy, o IP pinado sai de cena.** Quem confere passa a ser o proxy, e o `ConnectCallback`
  deixa de ser a barreira — por isso o `squid.conf` precisa estar certo, e por isso a tabela do §1
  tem duas colunas. As travas não se somam nessa linha; elas se substituem.
- **A captura ainda roda dentro da API.** Separar o worker é configuração de deploy (§2, passo 3),
  não refatoração — mas enquanto não for feito, `Mode: Open` tem **uma** camada, não duas.
- **Os demais achados da auditoria de 2026-09-03** seguem no checklist do `CLAUDE.md`. Esta entrega
  fechou A4, A5, A6, M5 e M14, e parte de M9 e de M2.
