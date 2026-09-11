# Auditoria de perímetro da VPS — 2026-09-10

> Varredura defensiva da infraestrutura que hospeda a plataforma: VPS, firewall de borda, DNS,
> domínios, Traefik e os serviços publicados. Feita em **2026-09-10** pela API da Hostinger
> (VPS/domínios/DNS), pela API do Dokploy e por sondagem HTTPS externa dos hosts publicados.
> **Nada foi executado contra a máquina e nenhuma alteração foi feita durante o levantamento** —
> todas as chamadas foram de leitura.
>
> **Escopo:** plataforma inteira, não um Bounded Context. Por isso este documento mora em
> `docs/infra-audit/` e não em `BillPayment.Architecture/`.
>
> **Status:** ao contrário dos achados de código (cujo status vive no checklist do `CLAUDE.md` do
> BC), estes não têm um `CLAUDE.md` dono — **o status vive na tabela deste documento**. As
> descrições dos achados retratam o estado em 2026-09-10 e não mudam; só a coluna Status muda.

## Ambiente auditado

| | |
|---|---|
| Host | `srv1489763.hstgr.cloud` (VPS id 1489763) |
| IPv4 / IPv6 | `85.31.61.241` / `2a02:4780:66:47e4::1` |
| Plano | KVM 2 — 2 vCPU, 8 GB RAM, 100 GB |
| SO | Ubuntu 24.04 com Dokploy |
| Orquestrador | Docker Swarm, **nó único** |
| Domínios | `couratechsafety.cloud`, `rufino.app` |

Tudo roda nesse nó: as três APIs (BillPayment, TenantManagement, PeopleManagement), o app Flutter,
Keycloak, Garage, um Postgres compartilhado, duas instâncias Evolution e o próprio Dokploy.

## Placar

| Severidade | Quantidade |
|---|---|
| Crítica | 3 |
| Alta | 5 |
| Média | 6 |
| Baixa | 3 |
| Controles já corretos (não refazer) | 5 |

## Status dos achados

| # | Achado | Sev. | Status |
|---|---|---|---|
| C1 | Nenhuma regra de firewall na borda | Crítica | **RESOLVIDO 2026-09-10** (ver ressalva IPv6) |
| C2 | Evolution API 2.3.7 publicada, com `/manager` aberto | Crítica | ABERTO |
| C3 | Painel do Dokploy exposto à internet | Crítica | ABERTO |
| A1 | IP de origem direto, sem CDN nem WAF | Alta | ABERTO |
| A2 | Traefik sem rate limiting em nenhuma rota | Alta | ABERTO |
| A3 | Garage Web UI em domínio público | Alta | ABERTO |
| A4 | Console de administração do Keycloak acessível | Alta | ABERTO |
| A5 | `squid.conf` planejado cobre menos que a barreira que substitui | Alta | ABERTO |
| M1 | Cinco nomes publicados sem serviço atrás | Média | ABERTO |
| M2 | Nenhum registro CAA nos dois domínios | Média | ABERTO |
| M3 | `rufino.app` sem SPF/DMARC/MX — spoofável | Média | ABERTO |
| M4 | `couratechsafety.cloud` com DMARC `p=none` e sem SPF | Média | ABERTO |
| M5 | Scanner de malware sem varredura efetiva | Média | ABERTO |
| M6 | Nó único sem redundância e sem limites de recurso | Média | ABERTO |
| B1 | Evolution anuncia o manager em `http://` | Baixa | ABERTO |
| B2 | `couratechsafety.cloud` duplicado na conta | Baixa | ABERTO |
| B3 | Recursão de diretório de build no repositório | Baixa | ABERTO |

## Superfície observada

Dezesseis registros A apontam para `85.31.61.241`. Doze foram sondados por HTTPS em 2026-09-10.

| Host | HTTP | O que serve | Leitura |
|---|---|---|---|
| `admin` | 200 | Painel Dokploy (login) | Controle total da infra, exposto |
| `evolution` | 200 | Evolution API 2.3.7 + `/manager` | Plano de controle do WhatsApp |
| `garageui` | 200 | Garage Web UI | Console do storage, exposto |
| `keycloak` | 200 | Console de administração | Esperado público; o admin não |
| `rufino.app` | 200 | App Flutter | Correto |
| `billing` | 401 | Bill Payment API | Recusa sem vazar superfície |
| `api` | 401 | People Management API | Recusa sem vazar superfície |
| `garage` | 403 | Endpoint S3 | Anônimo recusado |
| `monitor` | 502 | — | Rota viva, serviço morto |
| `le-api` | 502 | — | Rota viva, serviço morto |
| `le-evolution` | 502 | — | Rota viva, serviço morto |
| `tenant` | 404 | — | DNS sem rota no Traefik |
| `blob` | 404 | — | DNS sem rota no Traefik |
| `www`, `@` (.cloud) | — | — | Não sondado |
| `www` (rufino.app) | — | — | Não sondado |

---

## Achados

### C1 — Nenhuma regra de firewall na borda

```
VPS_getFirewallListV1     -> {"data":[],"meta":{"total":0}}
VPS_getVirtualMachinesV1  -> "firewall_group_id": null
```

Zero grupos criados na conta e nenhum atribuído à máquina. As 65.535 portas TCP e UDP chegam ao
host. Se o Postgres, o Dokploy (3000), a API admin do Garage (3903) ou as portas do Swarm
(2377, 7946, 4789) escutarem em `0.0.0.0` em vez da loopback ou de uma rede Docker, estão
publicadas.

**É o achado que torna os demais exploráveis.** Também é o único que o Docker não consegue
contornar: o firewall da Hostinger fica **fora** da VM, então não sofre o problema clássico de
`ufw` sendo furado pelas regras que o Docker escreve direto no iptables.

**Correção:** grupo de firewall liberando 22, 80, 443 e ICMP, atribuído à VPS. Confirmar antes que
o painel do Dokploy é alcançável por `admin.couratechsafety.cloud` — senão fechar a 3000 tira o
acesso ao painel.

#### Resolução — 2026-09-10

Grupo `rufino-baseline` (id **359367**) criado e ativado na VPS 1489763. A API confirma
`"firewall_group_id": 359367`. Modelo de allowlist, conforme a própria documentação do endpoint:
*"By default, the firewall drops all incoming traffic"*.

| Regra | Protocolo | Porta | Origem |
|---|---|---|---|
| 1313456 | TCP | 22 | any |
| 1313457 | TCP | 80 | any |
| 1313458 | TCP | 443 | any |
| 1313461 | ICMP | any | any |

SSH ficou aberto para qualquer origem por decisão explícita: com IP residencial dinâmico, restringir
por origem arrisca perder o acesso. A contrapartida é que a 22 continua sendo alvo de força bruta —
**mitigar com fail2ban e autenticação por chave** é a pendência que sobra deste achado.

Validado logo após a ativação: `billing.couratechsafety.cloud/swagger` seguiu em 401 e o painel do
Dokploy seguiu carregando. Nada quebrou em 443.

**Ressalva em aberto — IPv6.** A VPS tem endereço público `2a02:4780:66:47e4::1` e não ficou
provado que as regras com origem `any` alcançam IPv6. O enum de protocolos da API oferece `ICMPv6`
como item separado, o que sugere que sim, mas isso é indício e não prova. Se o filtro for só IPv4,
**toda a superfície continua aberta pelo IPv6** e C1 está apenas meio fechado. Verificar de fora:

```bash
nmap -6 -Pn --top-ports 100 2a02:4780:66:47e4::1
```

### C2 — Evolution API 2.3.7 publicada, com o manager aberto

```
GET https://evolution.couratechsafety.cloud -> 200
version: 2.3.7 · WhatsApp Web 2.3000.1047236770
anuncia: /manager e doc.evolution-api.com
```

A raiz responde sem autenticação e entrega a versão exata e o caminho do manager, que carrega.
Instância Evolution sob controle de terceiro envia mensagem em nome dos números da plataforma — e
a versão declarada permite procurar CVE conhecida sem uma única tentativa contra o alvo. Há duas
instâncias no inventário (`evolution` e `le-evolution`).

**Correção:** middleware de allowlist de IP ou basic auth no Traefik cobrindo `/manager`, e
`AUTHENTICATION_API_KEY` forte confirmada e rotacionada. Idealmente o manager não fica em domínio
público.

### C3 — Painel do Dokploy exposto à internet

```
GET https://admin.couratechsafety.cloud -> 200, tela de login Dokploy
```

Quem entra ali faz deploy, lê e reescreve as variáveis de ambiente com todos os secrets da
plataforma, troca senha de root de serviço e apaga projeto. É a chave-mestra atrás de um formulário
de e-mail e senha, sem segundo fator obrigatório e sem restrição de origem.

**Correção:** allowlist de IP no Traefik para esse host, ou expor o painel só por túnel. Se
precisar continuar público: 2FA obrigatório e rate limit agressivo no login.

### A1 — IP de origem direto, sem CDN nem WAF

Todos os registros A apontam para `85.31.61.241` sem intermediário. Não há absorção de volume,
filtro de camada 7 nem ocultação da origem. Um ataque volumétrico chega inteiro num nó de 2 vCPU e
8 GB que hospeda tudo.

**Correção:** Cloudflare (o plano gratuito resolve) como proxy dos hosts HTTP, **e** o firewall da
Hostinger aceitando 80/443 só das faixas da Cloudflare. Sem esse segundo passo o atacante ainda
alcança a origem pelo IP.

### A2 — Traefik sem rate limiting em nenhuma rota

```
application-readTraefikConfig (Bill Payment Api):
  rufino-project-bill-payment-api-6ecoty-router-websecure-63:
    middlewares: []
```

O roteador HTTPS não tem middleware nenhum. Nada limita requisições por origem: vale para força
bruta contra o Keycloak, contra o login do Dokploy, e para o custo de CPU de cada requisição
autenticada nas APIs.

**Correção:** middleware `rateLimit` aplicado a todos os roteadores `websecure`, com teto menor nos
caminhos de autenticação.

### A3 — Garage Web UI em domínio público

```
GET https://garageui.couratechsafety.cloud -> 200, <title>Garage Web UI</title>
```

O console do storage responde publicamente. É uma SPA, então a sondagem **não provou** se exige
credencial — falta conferir com olho humano. O endpoint S3 ao lado (`garage`, 403) guarda os
artefatos capturados: boletos e comprovantes de clientes, que o `04-integrations.md` descreve como
evidência de auditoria que não é apagada enquanto a `Bill` existir.

**Correção:** verificar se pede login e, de qualquer forma, allowlist de IP.

### A4 — Console de administração do Keycloak acessível

```
GET https://keycloak.couratechsafety.cloud -> 200
<title>Keycloak Administration Console</title> · "Loading the Administration Console"
```

A raiz do host serve o console administrativo. O endpoint de emissão de token precisa ser público;
o console, não. Somado à ausência de rate limit (A2), é alvo de força bruta contra a conta que
governa o realm `rufino` inteiro.

**Correção:** `KC_HOSTNAME_ADMIN` separado, ou middleware barrando `/admin` fora da allowlist. O
`/realms/*` continua aberto.

### A5 — O `squid.conf` planejado cobre menos que a barreira que substitui

No plano de isolamento do worker, preencher `LinkResolution__Proxy` **desliga o IP pinado** da
escada de link e transfere a decisão ao Squid. A ACL proposta deixa de fora faixas que a barreira
atual cobre e testa:

```
acl interna dst 10.0.0.0/8 172.16.0.0/12 192.168.0.0/16 169.254.0.0/16 127.0.0.0/8 ::1/128

ausentes:  100.64.0.0/10 (CGNAT)   fc00::/7 (ULA)
           fe80::/10 (link-local)  0.0.0.0/8
```

A suíte `LinkResolutionTests` descreve a cobertura atual como "faixa privada, metadados de nuvem,
CGNAT e v4 mapeado em v6" (18 testes de integração). Trocar isso por uma ACL mais estreita é
regressão, não migração.

**Correção:** acrescentar as quatro faixas ao `squid.conf` **antes** de preencher
`LinkResolution__Proxy`. Sem isso, não executar a fase de isolamento do worker.

### M1 — Cinco nomes publicados sem serviço atrás

`monitor`, `le-api` e `le-evolution` devolvem 502 — rota registrada no Traefik, contêiner morto.
`tenant` e `blob` devolvem 404 — DNS apontando para a máquina sem rota correspondente. O prefixo
`le-` sugere um ambiente inteiro abandonado. Cada nome vivo é um certificado Let's Encrypt renovado
à toa e uma pista de reconhecimento.

**Correção:** decidir por nome — ressuscitar ou apagar o registro A.

### M2 — Nenhum registro CAA nos dois domínios

Nem `couratechsafety.cloud` nem `rufino.app` declaram CAA: qualquer autoridade certificadora do
mundo pode emitir certificado para eles. Como Let's Encrypt já é usado em tudo, fixar custa um
registro.

**Correção:** `CAA 0 issue "letsencrypt.org"` no apex dos dois.

### M3 — `rufino.app` é spoofável

```
DNS_getDNSRecordsV1("rufino.app") ->
  A  @    85.31.61.241
  A  www  85.31.61.241
  (nada além disso)
```

Sem SPF, sem DMARC e sem MX. Qualquer um envia e-mail dizendo ser `@rufino.app` e nenhum receptor
tem como recusar. É o domínio que carrega o nome do produto.

**Correção:** MX nulo, `v=spf1 -all` e `v=DMARC1; p=reject`.

### M4 — `couratechsafety.cloud` com DMARC sem dentes e SPF ausente

```
_dmarc TXT "v=DMARC1; p=none; rua=mailto:rua@dmarc.brevo.com"
brevo1._domainkey / brevo2._domainkey -> CNAME (DKIM ok)
nenhum TXT com v=spf1
```

O DKIM do Brevo está configurado, mas `p=none` significa que o DMARC só observa. Sem SPF, a
autenticação depende exclusivamente do DKIM. É o domínio de onde a plataforma fala com os clientes.

**Correção:** publicar o SPF do Brevo, acompanhar os relatórios `rua` e subir para `p=quarantine` e
depois `p=reject`.

### M5 — O scanner de malware nunca varreu nada

```
VPS_getScanMetricsV1 -> {"records":0,"malicious":0,"compromised":0,
  "scanned_files":0,"scan_started_at":"2026-09-11T01:14:40Z","scan_ended_at":null}
```

Zero arquivos varridos e nenhuma varredura concluída. O Monarx está contratado e não entrega
resultado. Os zeros em `malicious` não são boa notícia — são ausência de dado.

**Correção:** verificar se o agente está instalado e rodando. Scanner que reporta zero varreduras é
pior que nenhum, porque parece cobertura.

### M6 — Nó único sem redundância e sem limites de recurso

```
VPS_getBackupsV1 -> 4 pontos: 28/08, 04/09, 09/09, 10/09 · restore_time: 1800s
application-one  -> memoryLimit: null, cpuLimit: null, memoryReservation: null
```

Os backups existem e o mais recente é do dia — isso está certo. O resto não: Swarm de nó único com
tudo dentro, sem para onde falhar, e **nenhum serviço com limite de memória ou CPU**, então um
contêiner sob ataque leva os vizinhos junto. E 30 min de RTO só valem depois de a restauração ter
sido testada ao menos uma vez.

**Correção:** restaurar um backup num VPS descartável e cronometrar de verdade; definir
`memoryLimit` e `cpuLimit` por serviço.

### B1 — Evolution anuncia o manager em `http://`

A resposta servida sobre HTTPS aponta `http://evolution.couratechsafety.cloud/manager`. A aplicação
não sabe que está atrás de TLS — `SERVER_URL` com esquema errado, ou `X-Forwarded-Proto` ignorado.

### B2 — `couratechsafety.cloud` duplicado na conta

Uma entrada `free_domain` (id 29663999, sem expiração) e uma `domain` (id 29671813, expira
2027-03-13), criadas com 5 horas de diferença. Cosmético, mas confunde na renovação.

### B3 — Recursão de diretório de build no repositório

```
server/bin/Debug/bin-agent/Debug/net10.0/bin/Debug/net10.0/
  bin-agent/Debug/net10.0/bin/Debug/... (dezenas de níveis)
```

Um `OutputPath` que se realimenta a cada build. Não é segurança, mas infla o repositório e quebra
ferramenta que percorre a árvore. Encontrado durante a varredura.

---

## Controles já corretos (não refazer)

1. **Traefik bem configurado.** O roteador `web` carrega `redirect-to-https`, o `websecure` usa
   `certResolver: letsencrypt` e `passHostHeader: true`. Não há porta de entrada em texto claro.
2. **As APIs recusam sem vazar superfície.** `/swagger` responde **401, não 404**, em `api` e
   `billing` — não dá para mapear quais rotas existem.
3. **O endpoint S3 do Garage recusa anônimo** com 403.
4. **HTTPS válido** em todos os hosts que responderam; nenhum erro de certificado ou handshake.
5. **Backups diários e recentes**, o mais novo do próprio dia da auditoria.

## O que não foi verificado

Limites reais do ferramental usado — não há shell nem port scan pela API. Estes comandos fecham as
lacunas:

```bash
# Portas escutando de fato, e em qual interface (a lacuna mais importante)
ss -tulpnH | awk '{print $1, $5, $7}' | sort -u

# O que a internet enxerga, de fora da máquina
nmap -Pn -sS --top-ports 2000 85.31.61.241

# Cabeçalhos de segurança: HSTS, CSP, X-Frame-Options
curl -sSI https://billing.couratechsafety.cloud | grep -i 'strict\|content-security\|x-frame'

# Dashboard do Traefik exposto?
docker service inspect dokploy-traefik --format '{{json .Spec.TaskTemplate.ContainerSpec.Args}}'

# SSH: senha desabilitada? root direto?
sshd -T | grep -E 'permitrootlogin|passwordauthentication|pubkeyauthentication'

# fail2ban existe?
systemctl is-active fail2ban 2>/dev/null || echo "ausente"

# O Keycloak usa o Postgres compartilhado? (decide o risco da fase F5)
docker service ls --filter label=com.docker.stack.namespace=keycloak
```

Além disso: o `garageui` é uma SPA e a sondagem não provou se exige login — precisa de conferência
manual.

## Plano de execução

Ordenado por relação ganho/risco, não por severidade. **O firewall vem antes do isolamento do
worker**, porque isolar o egresso de um contêiner numa máquina com 65.535 portas abertas resolve
pouco.

| Fase | O que | Resolve | Risco |
|---|---|---|---|
| F0 | Verificação (comandos acima + conferir `garageui` + stack do Keycloak) | — | nulo |
| F1 | Firewall na borda: 22, 80, 443, ICMP | C1 | médio |
| F2 | Allowlist nos consoles: `admin`, `garageui`, `evolution/manager`, `keycloak/admin`; 2FA no Dokploy | C2, C3, A3, A4 | médio |
| F3 | Higiene de DNS: rotas mortas, CAA, SPF/DMARC/MX | M1–M4 | baixo |
| F4 | `rateLimit` no Traefik, Cloudflare, limites de memória/CPU por serviço | A1, A2, M6 | médio |
| F5 | Isolamento de egresso do worker (ver anexo) | A5 | **alto** |
| F6 | Resiliência: restauração testada, Monarx, monitoramento | M5, M6 | baixo |

### Ordem de reversão

Saber o custo de voltar atrás, antes de começar:

- **F1, F3, F4** — reversão em minutos. Regra de firewall se apaga, registro de DNS se republica,
  middleware se remove. Se o firewall cortar o SSH, o console de recuperação da Hostinger entra
  pela porta dos fundos.
- **F2** — reversão imediata, é middleware.
- **F5, variáveis e deploy** — reversão barata: religa as flags na API, para o worker, redeploy.
- **F5, redes no Postgres e no Garage** — reversão **cara**: remover a rede exige outro reinício
  dos mesmos serviços compartilhados, ou seja, paga-se a janela duas vezes. É por isso que essa é a
  última coisa a fazer e a primeira a validar.

## Anexo — F5, o isolamento de egresso do worker

Registrado aqui porque a análise de risco dele produziu o achado A5 e porque ele depende de F1.

**Premissa (verificada):** aplicação no Dokploy é **serviço Swarm**, não contêiner comum — o
`application-one` traz `modeSwarm`, `placementSwarm`, `networkSwarm` e `replicas`. Um contêiner
ganha rota para a internet se estiver em **qualquer** rede não-`internal`; enquanto o worker
estiver na `dokploy-network`, o proxy é decoração.

**Três correções sobre o desenho original:**

1. **As redes têm de ser `overlay`, e o proxy tem de estar no Swarm.** Stack criado como "Docker
   Compose" gera redes *bridge*, e serviço Swarm não entra em rede bridge. O stack do Squid precisa
   ser do tipo **Stack**.
2. **Rede criada por stack ganha prefixo** (`<stack>_bp-egress`). Criar as redes antes, fora do
   Dokploy, e declará-las `external: true`.
3. **`bp-data` só funciona se o Postgres e o Garage entrarem nela.** Hoje estão na
   `dokploy-network`. Se o worker fica apenas em redes internal, perde o caminho para os dois — e
   os dois são compartilhados (o `server/CLAUDE.md` registra que o PeopleManagement grava no
   **mesmo balde** que o BillPayment).

**Bloqueio operacional conhecido:** com `DOKPLOY_REDACT_ENV=true` (o padrão, e o correto), o MCP
devolve `env`, `buildArgs`, `buildSecrets` e `environment` como `[REDACTED]`. Como
`application.saveEnvironment` **substitui o env inteiro** e não existe patch por chave, não há
leitura-modificação-escrita segura: qualquer escrita de env por agente apagaria todos os secrets.
**Toda alteração de variável de ambiente é manual, na UI.**

**Ordem obrigatória do corte:** API com as flags em `false` -> deploy da API -> só então deploy do
worker. Invertida, os dois processam outbox e expectativas ao mesmo tempo, que é o que o
`CLAUDE.md` do BC proíbe.

## Histórico

| Data | O que |
|---|---|
| 2026-09-10 | Levantamento inicial. 17 achados, todos abertos. |
| 2026-09-10 | **F1 executada.** Firewall `rufino-baseline` (359367) criado e ativado: 22, 80, 443, ICMP. C1 resolvido, com ressalva de IPv6 por verificar. Restam 16 abertos. |
