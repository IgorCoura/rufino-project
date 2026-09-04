# ADR-012 — Portais: reduzir o resíduo antes de automatizá-lo

**Status:** Aceito · **Data:** 2026-07-31 · **Revisado:** 2026-07-31 (DDA indisponível)

## Contexto

Boa parte das contas recorrentes do corpus real só existe no portal do fornecedor: EDP, SABESP, ENEL, CPFL, DAE, VIVO. Esses portais exigem login e vários usam proteção anti-bot.

**O DDA está fora.** A versão anterior deste ADR apostava no Débito Direto Autorizado como degrau que eliminaria o problema. O acesso é caro ou complicado de obter para este projeto, então ele sai do desenho — não como "adiado", como indisponível. Duas consequências pesam:

1. **O pagador nunca vira verificável por fonte autoritativa.** A ressalva do [`ADR-004`](ADR-004-pagador-nao-autoritativo.md) deixa de ser temporária: a escada de roteamento de cinco degraus é permanente, e o roteamento aprendido (degrau 2) é o mecanismo principal, não um paliativo.
2. **Vai sobrar portal.** Mesmo migrando o máximo possível para e-mail, o usuário avalia que restarão sites a acessar. A fase 5 não encolhe até desaparecer como a versão anterior supunha.

## Decisão

A escada continua, sem o degrau que sumiu. Cada degrau **reduz** o resíduo do próximo; nenhum deles o elimina sozinho.

### Degrau 1 — fatura digital por e-mail

EDP, SABESP, ENEL, CPFL, VIVO e praticamente toda concessionária oferecem "conta por e-mail" / "fatura digital", gratuito, ativado no próprio portal.

**Converte um problema de scraping num problema de e-mail — que o sistema já resolve.** Melhor relação resultado/esforço da lista: uma tarde de cadastros, custo zero, sem código, sem manutenção. É agora o degrau mais importante, porque é o único que retira volume da fase 5 sem escrever conector.

Ressalva que o degrau 1 **não** resolve: muitas dessas faturas chegam como **link** e não como anexo, e vários links exigem navegação dentro da página até chegar ao PDF. Isso está tratado em [`09-capture-channels.md`](../09-capture-channels.md) — é um problema menor que o portal (sem login, sem anti-bot), mas não é trivial.

### Degrau 2 — débito automático

Para concessionárias de valor previsível, remove a conta do fluxo inteiro. Perde-se a aprovação prévia por item — decisão de negócio do cliente, não técnica. Vale para energia e água de imóvel residencial; não vale para fornecedor.

### Degrau 3 — integração oficial

Alguns fornecedores têm canal B2B (API, EDI, portal de grandes clientes) para cliente com volume. Vale perguntar antes de assumir que não existe.

### Degrau 4 — automação assistida

Para o resíduo — que existirá. Um conector por portal com **Playwright em modo assistido**:

- **Perfil de navegador persistente por `CaptureSource`.** Cookies e sessão sobrevivem entre execuções.
- **Humano no laço para autenticar.** O usuário faz login, resolve CAPTCHA e segundo fator **uma vez**; a sessão persistida cobre as execuções seguintes. Quando expira, o sistema **não tenta contornar** — marca a fonte como `RequiresReauth` e notifica.
- **Cadência humana.** Uma execução por dia por fonte, em horário disperso. Sem paralelismo por fornecedor, sem retentativa agressiva.
- **Navegador real.** Sem spoofing de fingerprint, sem proxies residenciais rotativos, sem serviços de resolução de CAPTCHA.
- **Receita declarativa por portal** (passos + seletores) antes de agente de navegação — determinística, versionável, diagnosticável. O agente é fallback para descobrir a receita quando ela quebra, nunca o caminho normal.
- **Detecção de quebra explícita**: zero resultados por N execuções consecutivas é **falha**, não silêncio. Desativa a fonte e alerta.
- Credenciais cifradas na camada por tenant do [`ADR-009`](ADR-009-cofre-de-segredos.md), nunca em claro.

### Rede de segurança — obrigatória, não opcional

Com o DDA fora e portal no desenho, **nenhum degrau garante que a conta chegou**. A defesa contra "a conta não veio e ninguém percebeu" deixa de ser conveniência e vira requisito: o sistema precisa saber o que **espera** receber e avisar quando não recebeu. Ver [`ADR-014`](ADR-014-expectativa-e-lembretes.md) e [`11-bill-expectations.md`](../11-bill-expectations.md).

## Razões

**Por que não construir evasão de anti-bot**, dito uma vez e sem rodeio:

- **É uma esteira que não termina.** Fingerprint spoofing, proxy rotativo e resolução automática de CAPTCHA são contramedidas contra sistemas que se atualizam. Cada atualização do fornecedor quebra o conector, sempre em produção e sempre sem aviso. O custo não é construir, é manter.
- **Viola os termos de uso** de praticamente todo portal. Num sistema que movimenta dinheiro de terceiros, isso é exposição contratual agregada a todos os clientes de uma vez.
- **Não é o gargalo.** O login se resolve com sessão persistida; o que quebra conector é mudança de layout, e nenhuma técnica de evasão ajuda nisso.
- **A automação assistida entrega o mesmo resultado com mais confiabilidade.** O usuário é o titular da conta e tem o direito de acessá-la — automatizar o acesso *dele*, com a sessão *dele*, é diferente de se passar por outro cliente.

## Consequências

- **A fase 5 é real e tem peso.** Escopo: framework de conector + receitas por portal + reautenticação assistida + detecção de quebra. Priorizar por volume: os portais que mais aparecem no corpus primeiro.
- **Sem DDA, a escada de roteamento de cinco degraus é permanente**, e `PayerMatch` segue Advisory-quando-ausente para sempre. Isso eleva a importância do roteamento aprendido e da reivindicação manual.
- **Uma tarefa entra no onboarding de todo tenant**, não no código: cadastrar fatura digital em cada concessionária. Continua sendo a ação de maior impacto por unidade de esforço do projeto.
- **`CaptureSource` ganha `RequiresReauth`** e o fluxo de reautenticação.
- Conector roda **isolado** (processo/container próprio, sem acesso ao banco), entrega o artefato pela porta de ingestão comum, e o artefato passa pelo mesmo funil de validação.
- **A expectativa de boleto vira dependência de arquitetura, não feature secundária** — é o que impede um portal quebrado de virar conta vencida em silêncio.
