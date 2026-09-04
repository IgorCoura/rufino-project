# 09 — Canais de entrada: anexo, senha, link e portal

Boleto chega de muitas formas, e cada uma tem um modo de falhar próprio. Este documento cobre os quatro casos que o desenho original não tratava: **PDF protegido por senha**, **link em vez de anexo**, **portal com login** e a **cascata de extração** que unifica todos eles.

Todos convergem para a mesma porta de ingestão e o mesmo funil de validação — o canal muda como o artefato chega, nunca o que acontece com ele depois.

## Matriz de canais

| Canal | Artefato | Dificuldade | Fase |
|---|---|---|---|
| Upload manual (PDF ou linha digitável) | PDF ou string | trivial | 1 |
| E-mail com PDF anexo | PDF | baixa | 2 |
| E-mail com **PDF protegido por senha** | PDF cifrado | média — resolvida por derivação de senha | 2 |
| E-mail com **link para baixar** | URL → PDF | média a alta — muitos exigem navegar até o PDF; maior superfície de ataque | 2 |
| E-mail com boleto **no corpo** (HTML/imagem) | HTML ou imagem | média — resolvida pelo extrator de visão | 2 |
| **Portal** com login | sessão + download | alta — ver [`adr/ADR-012`](adr/ADR-012-portais-reduzir-residuo.md) | 5 |

> **DDA está fora do desenho** — acesso caro ou complicado de obter. Consequência: não existe fonte que diga o que foi emitido contra o CNPJ, então **nenhum canal garante que a conta chegou**. A defesa contra falha silenciosa é a expectativa de boleto ([`11-bill-expectations.md`](11-bill-expectations.md)), que deixa de ser conveniência e vira requisito de arquitetura.

---

## PDF protegido por senha

Muita fatura chega cifrada, e a senha é quase sempre **derivada do documento fiscal do pagador**: os primeiros N dígitos do CNPJ, o CPF completo, a data de nascimento do titular.

### O achado que muda o desenho

**A senha é uma prova de propriedade.** O emissor derivou aquela senha do documento do pagador. Se o PDF abre com um documento do `PayerProfile` do tenant, isso é evidência forte — em muitos casos mais forte que OCR — de que o boleto é **daquele tenant**.

Isso resolve, de graça, parte do problema de roteamento em caixa compartilhada. A escada de [`07-multitenancy-and-routing.md`](07-multitenancy-and-routing.md) ganha um degrau antes de todos:

### Degrau 0 — derivação de senha (novo)

| Situação | Confiança |
|---|---|
| Abriu com o **documento completo** (CPF ou CNPJ inteiro) de um `PayerProfile` | `Strong` — conclusivo |
| Abriu com um **prefixo** (5 ou 8 dígitos) de um `PayerProfile` | `Strong`, mas registrar o prefixo na evidência: colisão é improvável entre poucos tenants, não impossível |
| Abriu com senha **aprendida** para aquele `Payee` | `Learned` |
| Não abriu com nenhum candidato | segue para os degraus normais; item vai para `Unrouted` com motivo `pdf_locked` |

### Ordem dos candidatos

Gerados a partir do `PayerProfile` do tenant da fonte **e** dos demais tenants que monitoram a mesma caixa — testar os dois é o que permite classificar corretamente como `ForeignPayer` em vez de simplesmente falhar.

1. **Senha vazia** — cobre PDF com apenas *owner password* (bloqueia edição, não leitura). Sempre primeiro: é o caso mais comum e o mais barato.
2. Senha aprendida para aquele `Payee` (ver abaixo).
3. CNPJ: 5 primeiros dígitos → 8 primeiros (raiz) → 14 completos.
4. CPF: 3 primeiros → 5 primeiros → 6 primeiros → 11 completos.
5. Data de nascimento do titular, quando cadastrada: `ddmmaaaa` → `ddmmaa` → `ddmm`.

**Teto rígido de candidatos por documento** (config, default 40) e parada no primeiro acerto. Isso é *derivação*, não força bruta: os candidatos vêm de dados que o tenant já cadastrou, e o teto existe para que um PDF hostil não vire um laço caro.

### Regras

- **A senha nunca é logada, nunca aparece em evidência de check, e nunca sai em resposta de API.** A evidência registra *qual campo* a originou ("primeiros 5 dígitos do CNPJ do perfil"), nunca o valor.
- **Senha informada à mão** (`POST /capture-items/{id}/unlock`) é aceita, guardada cifrada na camada 2 do [`ADR-009`](adr/ADR-009-cofre-de-segredos.md) associada ao `Payee`, e reutilizada nas próximas faturas do mesmo beneficiário. É o mesmo padrão de aprendizado da reivindicação: manual uma vez, automático depois.
- **O PDF é armazenado como recebido**, cifrado. A versão decifrada só existe em memória — durante o processamento, e durante a leitura que serve o documento para uma pessoa.
- **O documento é servido DESTRAVADO, e a senha continua sem sair daqui** (2026-08-28). Guardar cifrado e entregar cifrado obrigava quem confere o boleto a digitar uma senha que o cadastro do tenant já tinha — o sistema sabia e pedia assim mesmo. `GET /capture-items/{id}/artifact` e `GET /bills/{id}/artifact` passam pelo `UnlockedArtifactReader`, que produz a cópia legível a cada leitura pelo mesmo `IBoletoDocumentParser.UnlockAsync` da captura. **A alternativa curta — mandar a senha junto para o app abrir — é exatamente o que o [`ADR-009`](adr/ADR-009-cofre-de-segredos.md) proíbe.** Não abrindo nenhuma candidata, o original vai como está e o leitor do app volta a pedir a senha: ali quem sabe algo que o sistema não sabe é a pessoa.
- Biblioteca: **PdfPig** (Apache-2.0) aceita lista de senhas candidatas na abertura, o que evita reabrir o arquivo por tentativa.

### Testes obrigatórios

PDF com owner password apenas (abre vazio); PDF com senha = 5 dígitos do CNPJ do tenant certo; PDF com senha derivada do **outro** tenant da mesma caixa (deve classificar `ForeignPayer`, não falhar); PDF que não abre com nenhum candidato (deve virar `Unrouted` com `pdf_locked`, sem estourar o teto em tempo).

O **fixture cifrado versionado** (`IntegrationTests/Extraction/EncryptedPdfFixture.cs`, RC4 de 40 bits como os emissores usam) existe desde 2026-08-28 e sustenta os quatro casos: nenhuma biblioteca do BC escreve PDF cifrado, então antes dele o caminho da senha só era conferido à mão contra o acervo real.

---

## Link em vez de anexo

Boa parte das faturas digitais chega como *"clique aqui para visualizar sua conta"*. O anexo não existe; existe uma URL.

**Este é o canal com a maior superfície de ataque do sistema**, porque seguir uma URL que veio de fora é exatamente o que um atacante quer que o servidor faça. Toda a seção abaixo é sobre isso.

### Classificação da URL

Após extrair as URLs do corpo (HTML e texto), classificar:

| Tipo | Como detectar | Tratamento |
|---|---|---|
| **PDF direto** | `HEAD` devolve `application/pdf` | baixar |
| **Página com o boleto a um salto** | `text/html` com âncora para PDF | seguir a âncora |
| **Fluxo de navegação** | `text/html` sem PDF visível | escada de resolução, abaixo |
| **Link tokenizado de uso único** | token longo no path/query | resolver **uma vez**, guardar o artefato; nunca retentar |
| **Exige login** | redireciona para autenticação | vira caso de portal (fase 5) |
| **Não é boleto** | rastreamento, descadastro, redes sociais | ignorar |

### Quando o link não entrega o PDF direto

O caso comum não é o simples. A fatura digital costuma cair numa página onde ainda é preciso escolher a competência, clicar em "2ª via", aceitar um aviso, ou passar por um intersticial. **Isso não é portal** — não tem login e não tem anti-bot — mas também não é um `GET` e pronto.

Escada de resolução, do determinístico ao caro. Cada degrau só roda se o anterior falhou:

| # | Estratégia | Custo | Quando resolve |
|---|---|---|---|
| 1 | **`GET` direto** — o link já é o PDF | irrisório | fatura simples |
| 2 | **Um salto** — buscar no HTML âncora para `.pdf` ou `Content-Disposition: attachment` | irrisório | maioria das páginas de 2ª via |
| 3 | **Receita por domínio** — passos declarativos versionados (`click`, `waitFor`, `select`, `download`) executados em Playwright | baixo | fluxo estável do fornecedor |
| 4 | **Agente de navegação** — modelo de visão lê a página e propõe o próximo passo, em laço curto e limitado | alto | receita quebrou, ou domínio ainda sem receita |
| 5 | **Quarentena `LinkPending`** com o link na tela | zero | nada resolveu; humano resolve e o sistema aprende |

**A receita é o degrau que importa.** É um arquivo por domínio, versionado no repositório, legível e diagnosticável:

```yaml
domain: exemplo-concessionaria.com.br
steps:
  - waitFor: "#segunda-via"
  - click:   "#segunda-via"
  - select:  { selector: "#competencia", strategy: latest }
  - download: "a.btn-pdf"
expect: { mimeType: application/pdf, minBytes: 10000 }
```

Determinística, barata, e quando quebra o erro aponta o passo exato. O **agente de navegação é fallback e ferramenta de manutenção**, não o caminho normal: quando a receita falha, ele resolve aquele download **e propõe a receita nova**, que um humano revisa e versiona. Assim o custo do modelo é pago uma vez por mudança de layout, não uma vez por fatura.

Guardas do degrau 4, sem exceção: teto de passos (default 8), teto de tempo, teto de gasto por tenant por dia, e **o agente só navega e baixa — nunca preenche formulário, nunca autentica, nunca confirma nada**. O artefato que ele traz passa pelo mesmo funil de validação de qualquer outro ([`adr/ADR-011`](adr/ADR-011-llm-propoe-codigo-dispoe.md)).

Falha em qualquer degrau → `LinkFailed`, e o ciclo de expectativa correspondente gera alerta de **falha de captura** — que é diferente de "não chegou" ([`11-bill-expectations.md`](11-bill-expectations.md)).

### Controles de segurança — todos obrigatórios

- **Allowlist de domínio.** Só segue link cujo domínio registrável esteja em `TrustedOrigin` do tenant **ou** case com o domínio de um `Payee` cadastrado. Link de origem desconhecida **não é seguido**: o item vai para `Unrouted` com a URL visível para o humano decidir. Isso é o mesmo princípio do [`ADR-005`](adr/ADR-005-confianca-de-origem.md), aplicado a um lugar onde o custo do erro é maior.
- **Anti-SSRF.** Resolver o DNS **antes** de conectar e rejeitar IP privado, loopback, link-local e metadata de nuvem. Revalidar após cada redirecionamento (rebinding). Máximo de 3 redirecionamentos, todos em `https`, sem troca de esquema.
- **Egresso isolado.** O componente que busca links roda com saída de rede própria, sem rota para o Postgres, o Asaas ou qualquer serviço interno. Comprometer o fetcher não pode dar acesso ao resto.
- **Só `GET`.** Nunca `POST`, nunca submeter formulário, nunca executar ação. Um link de "cancelar assinatura" ou "confirmar" jamais deve ser seguido — a allowlist ajuda, mas o método é a garantia.
- **Validar conteúdo, não a promessa.** Conferir `Content-Type` **e** os bytes mágicos (`%PDF-`). Teto de tamanho (config, default 20 MB) e de tempo.
- **Renderização isolada.** Página HTML é renderizada em navegador sem sessão, sem cookies persistidos, sem JavaScript de terceiros quando possível, em container descartável.
- **Uso único é uso único.** Detectado o padrão de token, baixar uma vez e persistir. Retentar costuma invalidar o link e deixa o usuário sem a fatura.

### Boleto no corpo do e-mail

Alguns fornecedores mandam a linha digitável e o QR direto no HTML, sem anexo nenhum. O corpo em texto passa pelo mesmo funil de candidatos; quando o boleto vem como **imagem embutida**, entra o extrator de visão ([`10-llm-extraction.md`](10-llm-extraction.md)).

---

## Portais com login

Tratado integralmente em [`adr/ADR-012`](adr/ADR-012-portais-reduzir-residuo.md). Resumo da decisão: **eliminar a necessidade antes de automatizar** — DDA, depois fatura digital por e-mail, depois débito automático, depois integração oficial, e só então automação assistida com sessão persistida e humano resolvendo autenticação. Sem evasão de anti-bot.

As duas ações de maior impacto do projeto inteiro estão nesse ADR e **não dependem de nenhuma sprint**: aderir ao DDA e cadastrar fatura digital em cada concessionária.

---

## A cascata de extração

Todo artefato — venha de anexo, link, corpo de e-mail ou portal — passa pela mesma cascata. Ela é **ordenada por custo**: o caminho barato resolve a maioria, e o caro só roda quando precisa.

```
1. PDF cifrado?        → derivar senha (degrau 0 do roteamento)
2. Texto embutido      → gerar candidatos → validar DV → filtros de plausibilidade
2b. LEITURA DE QR      → rasterizar a página → decodificar QR → validar CRC-16
   ├─ resolveu?        → segue para a consulta oficial (os dois trilhos, quando há os dois)
   └─ não resolveu     ↓
3. Extrator de visão   → structured output (linha digitável, Pix, pagador, referência de conta)
   ├─ candidato válido? (DV + CRC + plausibilidade) → segue para a consulta oficial
   └─ não               ↓
4. Quarentena `Unrecognized` — humano informa a linha digitável à mão
```

Três propriedades que a cascata precisa preservar:

- **O passo 2 nunca é pulado.** Ele é gratuito, instantâneo, e resolve o caso comum. O LLM é degrau de fallback, não substituto.
- **O passo 2b não é opcional** — ver abaixo. Sem ele o trilho preferencial do sistema não existe na prática.
- **O passo 3 não é fonte de verdade.** Sua saída é candidato, e o que decide é o DV seguido da consulta oficial — [`adr/ADR-011`](adr/ADR-011-llm-propoe-codigo-dispoe.md).

### Leitura de QR Code — por que é degrau obrigatório

**Medido em 2026-08-06:** nos boletos reais o BR Code **não está como texto no PDF**. Ele existe só como **imagem de QR**. Os dois documentos de agosto do corpus confirmaram: `pdftotext` não devolveu nenhuma ocorrência de `br.gov.bcb.pix` nem de payload EMV, embora o QR esteja visivelmente impresso.

**Consequência para o produto:** sem leitor de QR, o trilho Pix — que o [`ADR-010`](adr/ADR-010-pix-preferido-sobre-boleto.md) elege como **preferencial** — só funcionaria pedindo ao usuário escanear o QR com o celular e colar o "Copia e Cola" no app. **Isso é contraproducente**: o sistema existe para tirar trabalho manual do meio, e essa etapa o devolveria justamente no caminho preferido. Pior, um documento híbrido cujo QR não é lido perde o check `PixBarcodeConsistency` — a defesa contra QR adulterado colado sobre boleto verdadeiro.

Como implementar (sprint 2.3):

1. **Rasterizar a página** do PDF — a mesma dependência que o degrau 3 já vai exigir.
2. **Decodificar o QR** com biblioteca local. **ZXing.NET** é a escolha natural: MIT, sem serviço externo, alinhada à premissa de software open source auto-hospedado.
3. **Validar o CRC-16** antes de aceitar. `PixPayload.Parse` já faz isso desde a sprint 1.2 — QR lido pela metade ou com ruído de digitalização morre aqui, não vira pagamento.
4. Uma página pode ter **mais de um QR** (logotipo, QR de outra finalidade). Mesma regra da linha digitável: gerar **todos** os candidatos, validar CRC em cada um, aceitar só os que passam, e deixar a consulta oficial desempatar se sobrar mais de um.

> A saída do leitor é **candidato**, não verdade — como a do modelo de visão. O funil determinístico é o mesmo: CRC → consulta oficial → checks.

## Impacto no modelo

`CaptureItem` ganha estados e campos para cobrir estes canais:

| Adição | Para quê |
|---|---|
| `CaptureItemStatus.Locked` | PDF cifrado que nenhum candidato abriu; aguarda senha do usuário |
| `CaptureItemStatus.LinkPending` / `LinkFailed` | link identificado, download pendente ou falho |
| `SourceUrl` | URL de origem quando o artefato veio de link (evidência e reprocesso) |
| `UnlockedBy` | qual campo do `PayerProfile` derivou a senha — **nunca a senha** |
| `ExtractionMethod` | `EmbeddedText` \| `Vision` \| `Manual` — alimenta a métrica da cascata |

`RoutingConfidence` ganha `PasswordDerived`, posicionado acima de `Strong` na ordem de precedência da escada.
