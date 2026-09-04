# ADR-005 — Confiança de origem é allowlist por tenant, com promoção pelo usuário

**Status:** Aceito · **Data:** 2026-07-31

## Contexto

O requisito pede verificar se o boleto "veio de um site e e-mail confiável". Duas leituras possíveis: a **caixa monitorada** é confiável (todo boleto que chega nela vale), ou o **remetente** é confiável (cada mensagem é avaliada pela sua origem).

## Decisão

Confiança é do **remetente/domínio**, não da caixa. Aggregate `TrustedOrigin` por tenant, com `Kind` (`EmailAddress` \| `EmailDomain` \| `WebDomain`), `Value` normalizado e `Decision` (`Trusted` \| `Blocked`). A resolução casa endereço exato antes de domínio; sem registro, a origem é **desconhecida** — ausência de linha, não um terceiro valor de `Decision`.

O check `OriginTrust`:

| Situação | Outcome | Severidade |
|---|---|---|
| `Trusted` | `Passed` | Advisory |
| `Blocked` | `Failed` | **Blocking** |
| Desconhecida | `Inconclusive` (`origin_unknown`) | Advisory |
| Upload manual autenticado | `Passed` (evidência = `UserId`) | Advisory |

A promoção acontece na tela de aprovação: "confiar nesta origem" é um comando próprio, não efeito colateral do approve.

## Razões

- **A caixa recebe de qualquer um.** Confiar na caixa é confiar em qualquer remetente do mundo — a verificação viraria decorativa.
- **Desconhecido não é hostil.** Fornecedor novo é rotina. Bloquear o primeiro boleto de todo remetente novo tornaria o sistema inutilizável e treinaria o usuário a clicar "aprovar mesmo assim" por reflexo.
- **`Blocked` bloqueia.** Se o usuário marcou uma origem como hostil, o sistema honra isso sem discussão.
- **A confiança se constrói pelo uso.** Cada aprovação é uma oportunidade barata de ensinar o sistema — mesmo mecanismo do "cadastrar como beneficiário" e "aprender o banco".

## Consequências

- **`OriginTrust` é Advisory por decisão explícita.** Remetente confiável **não** torna o boleto confiável: envelope de e-mail é trivialmente falsificável e contas legítimas são comprometidas — comprometer a conta do fornecedor e mandar um boleto com outro CNPJ é o roteiro clássico da fraude. `OriginTrust=Passed` jamais compensa `PayeeMatch=Failed`.
- **Ordem de leitura da tela é requisito, não estética**: identidade do beneficiário primeiro, origem por último. Origem no topo com selo verde induz o aprovador ao erro.
- Domínio precisa ser normalizado (lowercase, sem espaço) e comparado só no domínio real do remetente. Sem SPF/DKIM validado, o remetente é dado declarado — anotar isso na evidência.
- Avaliar na fase 2 se o adapter de e-mail consegue expor o resultado de autenticação da mensagem (SPF/DKIM/DMARC). Se sim, vira insumo do check e eleva o valor de `Trusted`.
