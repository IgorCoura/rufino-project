# ADR-007 — Aprovação humana obrigatória antes de qualquer pagamento

**Status:** Aceito · **Data:** 2026-07-31

## Contexto

Com dez verificações automáticas, é tentador deixar o sistema pagar sozinho quando tudo passa. Seria o auge da automação prometida pelo produto.

## Decisão

**Nenhum pagamento ocorre sem um `UserId` autorizando.** Não existe transição automática para `Approved`. O `ApprovalRecord` (usuário, instante, decisão, observação) é obrigatório e imutável.

Auto-aprovação por política fica **adiada**, e só entra em pauta na fase 6 se todas estas condições forem satisfeitas:

1. Todos os checks `Blocking` e `Advisory` em `Passed` — nenhum `Inconclusive` conta como aprovação.
2. `Payee` cadastrado, ativo e com histórico mínimo de N pagamentos já aprovados manualmente.
3. `AmountPolicy` do `Payee` do tipo `Fixed` ou `Range` — `Unbounded` nunca auto-aprova.
4. Valor abaixo de um teto configurado por tenant, independente da alçada de usuário.
5. `OriginTrust=Passed` com origem explicitamente marcada como confiável.
6. Ativação opt-in por tenant, desligada por padrão, com registro de quem ligou.
7. Toda auto-aprovação notifica um humano e é reversível enquanto a ordem não foi submetida.

Mesmo satisfeitas, isso é um ADR novo e uma decisão de negócio do cliente — não uma evolução técnica.

## Razões

- **A ação é irreversível.** Pagamento errado não tem `undo`; recuperar dinheiro é processo jurídico. A assimetria entre o ganho (poupar um clique) e o risco (pagar fraude) não fecha.
- **As verificações provam consistência, não intenção.** `PayeeMatch=Passed` prova que o CNPJ bate com o cadastro — não prova que o serviço foi prestado, que o valor está correto contratualmente, ou que o boleto não é uma cobrança em duplicidade legítima do ponto de vista do emissor.
- **A responsabilidade precisa ter dono.** Numa disputa, "o sistema aprovou" não é resposta aceitável para uma auditoria.
- **`Inconclusive` é frequente** por desenho (ADR-003, ADR-004). Um critério de auto-aprovação que tolerasse inconclusivos aprovaria justamente os casos menos verificados.

## Consequências

- A tela de aprovação é o gargalo do fluxo e precisa ser rápida: lista ordenada por atenção, evidência completa no detalhe, ações acessórias (cadastrar payee, aprender banco, confiar na origem) sem sair da tela.
- **Aprovação em lote** é aceitável e desejável — desde que cada item gere seu próprio `ApprovalRecord` com o mesmo `UserId`. Lote é economia de clique, não delegação de responsabilidade.
- Alçada por usuário entra na fase 1 (`above_approval_limit` como check bloqueante). Segregação de funções — exigir aprovador diferente de quem importou — fica para a fase 6, junto do Keycloak.
- Aprovação vence: snapshot com mais de N horas invalida a aprovação (`BLP.BIL06`), e revalidação que mude o valor de um Bill `Approved` ainda não agendado o devolve para `AwaitingApproval`. Consentimento foi dado sobre um valor específico.
