# ADR-008 — Fontes compartilhadas: uma conexão por tenant, isolamento por construção

**Status:** Aceito · **Data:** 2026-07-31

## Contexto

Duas pessoas (ou duas empresas) usam a mesma caixa de e-mail para receber contas. O requisito: o sistema precisa usar essa caixa para os dois, filtrar o que é de cada um, e **as fontes de um usuário não podem ser vistas por outro** — no máximo cabe um aviso de que outro usuário já usa aquela fonte.

Três desenhos possíveis:

1. Uma `CaptureSource` compartilhada entre tenants, com autorização por linha nos itens capturados.
2. Uma `CaptureSource` por tenant, cada uma com sua credencial, ingerindo a mesma caixa em paralelo.
3. Uma fonte "dona" que ingere e distribui itens para os outros tenants.

## Decisão

**Opção 2.** Cada tenant conecta a caixa como sua própria `CaptureSource`, com seu próprio `CredentialRef`, seu próprio cursor e sua própria pipeline de ingestão. As fontes não se conhecem e não se referenciam.

Cada pipeline lê todas as mensagens da caixa, roteia, promove a `Bill` só o que é do seu tenant, e manda o resto para quarentena com visibilidade reduzida.

O isolamento tem exatamente **três** furos deliberados, todos devolvendo booleano ou aviso genérico, nunca identidade nem conteúdo:

1. **Aviso de fonte já monitorada** — mostrado **depois** do OAuth concluir, para não virar oráculo de endereços cadastrados.
2. **Unicidade global da linha digitável** — um boleto tem uma `Bill` ativa no sistema inteiro.
3. **Conflito de reivindicação** — o segundo a reivindicar o mesmo item recebe aviso genérico.

## Razões

- **Autorização por linha vaza na primeira query esquecida.** A opção 1 exigiria que todo acesso a `CaptureItem` carregasse um predicado de propriedade separado do `TenantId`. Num sistema que move dinheiro, o modelo de isolamento precisa ser o mesmo em todo lugar: filtra por `TenantId`, ponto.
- **Credencial é de quem conectou.** Se o tenant A revoga o OAuth ou sai do sistema, o tenant B continua funcionando. Na opção 3, a saída do dono derrubaria todo mundo.
- **Nenhum privilégio implícito.** Quem conecta a caixa já a lê no cliente de e-mail. O sistema não amplia o acesso de ninguém — só deixa de ampliar para os outros.
- **O aviso pós-OAuth resolve a tensão do requisito.** O usuário precisa saber que não está sozinho na caixa (afeta a expectativa dele sobre o que vai aparecer), e não precisa saber quem é o outro. Exigir o OAuth antes elimina o uso do endpoint para sondagem.
- **Unicidade global não é opcional.** Pagamento duplicado é irreversível. Manter a unicidade por tenant deixaria dois tenants pagando o mesmo boleto — o pior desfecho possível, e justamente o que uma caixa compartilhada torna provável.

## Consequências

- **A mesma mensagem é lida N vezes**, uma por tenant conectado. Custo de API e de processamento multiplicado por N. Aceito: caixas de contas a pagar têm volume baixo, e N na prática é 2 ou 3.
- **Existe um índice global de endereços de fonte**, fora do filtro de tenant. Ele tem **um** caminho de código, devolve `bool`, e qualquer outro uso é violação — anotar no `CLAUDE.md` para não erodir com o tempo.
- **A unicidade global da linha digitável é um índice único sem `TenantId` na chave.** Contraintuitivo num sistema multi-tenant e por isso precisa de comentário no mapping EF explicando o porquê, senão alguém "conserta" isso depois.
- **Quarentena com dois níveis de visibilidade** (`ForeignPayer` sem dados financeiros, `Unrouted` com dados suficientes para reivindicar) é regra de apresentação com efeito de segurança — precisa estar no read model, não só na UI. Query que devolve `CaptureItem` tem que projetar campos diferentes por status.
- Reprocessamento e reconciliação precisam ser idempotentes por `(TenantId, SourceId, ExternalMessageId)` — a mesma mensagem gera itens distintos em tenants distintos, e isso é correto.
