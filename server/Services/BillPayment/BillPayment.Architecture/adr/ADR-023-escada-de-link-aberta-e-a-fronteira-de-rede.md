# ADR-023 — A escada de link aberta, e a fronteira de segurança que desce para a rede

**Status:** Aceito · **Data:** 2026-09-10 · **Substitui a trava de host de**
[ADR-012](ADR-012-portais-reduzir-residuo.md) no que diz respeito à allowlist; **não** revoga a
proibição de evasão de anti-bot.

## Contexto

Em 2026-09-09 um boleto real não foi capturado em produção. O link era
`https://app.acessorias.com/getguia.php?ko=…`, e o usuário perguntou se a feature de "abrir link e
baixar o boleto" existia mesmo.

Existia, e funcionava como projetada. O que a sondagem mostrou foi **três causas em série**, todas
de desenho e nenhuma de defeito:

1. **`app.acessorias.com` não tinha receita.** A allowlist tinha três hosts, cadastrados à mão em
   2026-08-11. `Matches` não devolvia nada, `ResolveAsync` devolvia `null`, e o item ia para a
   quarentena. **Nenhuma requisição chegou a sair.**
2. **Mesmo com receita, o segundo salto não acharia o documento.** A página da Acessórias entrega o
   PDF por `document.write("<iframe src='…'>")` e **não tem uma única tag `<a>`**. O colhedor lia
   apenas âncoras.
3. **O PDF está em outro host** (`acessorias.s3.us-east-2.amazonaws.com`), o que exigia
   `FollowHosts`.

Sondado no mesmo dia: `getguia.php` responde `200 text/html` 3,7 KB sem autenticação, e o `iframe`
aponta para um S3 presignado que responde `200 application/pdf` 160 KB. **O documento é alcançável;
o código é que não chegava nele.**

Dois problemas menores apareceram junto, e o segundo explica por que a falta da receita ficou
invisível por semanas:

- A procedência gravada era a URL do **último salto**. Para a Acessórias ela é presignada com
  `X-Amz-Expires=120` — dois minutos. A "evidência de onde veio o documento" estaria morta antes de
  qualquer pessoa abrir a quarentena.
- `RecordAttemptedLinkAsync` guardava o **primeiro `<a>`** do e-mail. Em mensagem de campanha isso é
  o logotipo ou o "ver no navegador". O mecanismo que existia para transformar a quarentena em fila
  de emissores a cadastrar apontava para o host errado.

## Decisão

**A allowlist deixa de ser a fronteira de segurança e vira filtro de relevância. A fronteira desce
para a camada de rede e para um buscador blindado.**

Concretamente:

### 1. Dois regimes, e o fechado continua sendo o padrão

`LinkResolution:Mode` = `Allowlist` (padrão) ou `Open`. No aberto, o destino da requisição passa a
ser escolhido por **quem manda o e-mail** — que é a definição de SSRF, e por isso ele só é
defensável junto de tudo que vem abaixo. Uma instalação nova nunca nasce aberta.

**A allowlist não morre.** Link que casa receita é buscado direto, sem descer a escada; o que muda
é o destino do que **não** casa — recusa no regime fechado, escada no aberto.

### 2. O IP é pinado no `connect`, e é isto que fecha o rebinding

`SafeUrlPolicy` deixou de responder `bool` e passou a devolver o `IPAddress` conferido, discado
pelo `SocketsHttpHandler.ConnectCallback`. A versão anterior conferia o **nome** e devolvia o nome
para o `HttpClient` resolver outra vez — e entre as duas resoluções o DNS pode responder outra
coisa.

> Qualquer defesa que resolve o nome, confere o resultado e devolve o nome para a biblioteca HTTP
> resolver de novo é vulnerável a rebinding. O conserto tem de acontecer na camada onde a conexão é
> feita, não onde a URL é lida.

Antes, explorar isso exigia controlar o DNS da SABESP, do Asaas ou da BRCondos. No regime aberto
exigiria apenas registrar um domínio — a severidade sairia de teórica para trivial. O TLS continua
sendo validado contra o **nome**: o handshake acontece depois que o retorno entrega o stream.

### 3. As faixas reservadas que faltavam, e as que só a instalação conhece

Três endereços passavam como públicos e alcançam loopback:

| Endereço | Por que escapava |
|---|---|
| `::` | `IsLoopback` compara com `::1` exato; `fc00::/7` não casa com zeros; `IsIPv4MappedToIPv6` exige `0xFFFF` nos bytes 10-11 |
| `::127.0.0.1` | IPv4-**compatible** (RFC 4291, deprecado), primo do IPv4-**mapped**: mesmos zeros, sem o `0xFFFF` |
| `64:ff9b::/96`, `2002::/16` | NAT64 e 6to4 embutem IPv4 e não estavam listados |

Mais `198.18.0.0/15` (benchmarking), `192.0.0.0/24` e as três TEST-NET.

E o mais importante: **a conferência embutida não tem como saber que `203.0.113.10` é o IP público
da própria VPS.** `BlockedCidrs` e `AllowedCidrs` são a lista configurável para isso — é justamente
o endereço da própria instalação que um atacante quer alcançar a partir de dentro, porque a API, o
Keycloak e o balde vivem lá e frequentemente confiam em quem chega pelo IP de saída. Faixa
malformada **derruba o arranque**: uma faixa que ninguém percebeu que não vale é pior que faixa
nenhuma, porque quem a escreveu acredita estar protegido.

### 4. Profundidade limita a FORMA; o orçamento limita o VOLUME

A escada desce até `MaxDepth` (5) níveis, em **largura** — todo o nível 1 antes de qualquer link de
nível 2, porque nos casos medidos o documento está no primeiro ou no segundo salto.

Profundidade sozinha **não** segura nada. Com 60 links por página e 5 níveis, uma árvore sem
orçamento são `60⁵ ≈ 777 milhões` de requisições saindo da nossa rede por causa de um e-mail — que
é um ataque de negação de serviço contra terceiros, com o nosso IP na origem, e um problema
jurídico antes de ser técnico. Quatro tetos coexistem:

| Teto | Padrão | O que impede |
|---|---|---|
| `MaxDepth` | 5 | a forma da árvore |
| `MaxFetchesPerMessage` | 12 | **a explosão combinatória** |
| `MaxFetchesPerHost` | 3 | o orçamento inteiro gasto martelando um alvo |
| conjunto de visitados | — | duas páginas que se apontam |

Mais `TotalTimeoutSeconds` (90), porque os tetos acima limitam quantas requisições saem e não quanto
tempo elas levam; e `MaxFetchesPerSenderPerDay` (200), porque o orçamento por mensagem limita um
e-mail e este limita mil.

### 5. O colhedor passa a ler o que o navegador leria — sem ser um navegador

Duas passadas, ambas lineares:

- **Estruturada**: `<a>`, `<iframe>`, `<frame>`, `<embed>`, `<object>`, `<meta refresh>`, `<area>`.
- **Bruta**: qualquer `https?://…` no texto, inclusive dentro de `<script>`. **É esta que resolve o
  caso Acessórias.**

**A alternativa era um navegador sem cabeça, e ela foi recusada.** Renderizar a página para ver o
que ela monta significa executar JavaScript escolhido por quem manda o e-mail dentro da nossa rede
— trocar um problema de leitura por um de execução. Ler o endereço com um padrão é estritamente
mais seguro e resolveu todos os casos medidos.

Nenhum padrão tem quantificador aninhado: o regex de âncora anterior retrocedia em O(L²) sobre HTML
construído de propósito (achado M9), e um colhedor mais permissivo sobre isso seria negação de
serviço com um e-mail só.

**A ordem passou a importar mais que o conteúdo.** Enquanto havia receita, era ela que escolhia qual
link era o boleto. No regime aberto não há quem escolha, e gastar o orçamento nos oito rastreadores
de rede social que a EDP põe antes da fatura significa não achar a fatura — daí a pontuação por
sinal de documento no caminho e no rótulo. **É ordenação, jamais filtro.**

### 6. O desfecho vira dado, e "lançar erro" é recusado

O pedido original era *"para e lança erro"* ao esgotar a profundidade. **Lançar quebra o desenho da
degradação**: conta tentativa contra o item e derruba o worker por causa de um servidor de terceiro
fora do ar. O que dá o mesmo sinal sem esse efeito é o `LinkResolutionOutcome` gravado no
`CaptureItem` — `Resolved`, `NoCandidates`, `NoRecipe`, `DepthExhausted`, `BudgetExhausted`,
`Refused`, `Unreachable`, `Throttled`, `Disabled`.

Até aqui os quatro últimos produziam o mesmo item mudo na quarentena, e quem olhava a fila não tinha
como separar o que precisa de cadastro do que precisa de conserto. `Refused` repetido merece olhar
especial: emissor honesto não hospeda boleto em `127.0.0.1`.

### 7. A imagem passa a ser conferida pelos bytes

`AsDocument` aceitava imagem pelo `Content-Type` **declarado pelo servidor remoto**, enquanto o PDF
sempre foi conferido pelo `%PDF-`. A assimetria abria a porta que o PDF fechava: um servidor hostil
respondendo `image/png` com 20 MB de qualquer coisa entregava esses bytes ao decodificador de
imagem do leitor de QR, ao extrator de IA e ao aparelho de quem aprova.

## Consequências

**O que melhora:** emissor novo deixa de exigir cadastro manual; a Acessórias resolve; a quarentena
passa a dizer por que falhou; três buracos de IPv6 e o rebinding fecham **também no regime fechado**.

**O que piora:** no regime aberto, a superfície de ataque deixa de ser três hosts conhecidos e passa
a ser o que qualquer remetente escrever. O conteúdo que chega ao extrator de IA passa a vir de
qualquer página, o que amplia a superfície de injeção de prompt indireta (achado M2, aberto).

**O que continua valendo do ADR-012:** nenhum formulário é enviado, nenhuma credencial é preenchida,
e não há evasão de anti-bot. Portal com login continua sendo fase 5.

**Pré-requisito operacional, e ele não é opcional:** ligar `Mode: Open` sem egresso de rede fechado
apoia toda a segurança em o código estar certo — e o achado das faixas de IP faltantes é a prova de
que uma faixa esquecida acontece. O padrão default-deny no egresso é a segunda barreira, a única
que continua de pé quando o código tem um defeito.

## Alternativas consideradas

**Manter a allowlist e cadastrar emissores sob demanda.** É o que existia. Não escala — a
Acessórias levou semanas para ser notada, e só foi notada porque uma pessoa reclamou.

**Derivar a autorização do domínio do remetente.** Já recusada em 2026-08-11 e continua errada: a
SABESP publica em `7az.com.br` e a EDP em `montreal.com.br`. Recusaria os dois casos reais **e**
autorizaria qualquer coisa hospedada no domínio de quem mandou o e-mail.

**Navegador sem cabeça para páginas movidas por JS.** Ver o item 5: troca um problema de leitura por
um de execução de código escolhido pelo atacante.
