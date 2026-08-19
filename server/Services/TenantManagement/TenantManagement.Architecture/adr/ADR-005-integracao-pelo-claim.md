# ADR-005 — Suspensão e entitlement de produto trafegam pelo claim

**Data:** 2026-08-17 · **Status:** aceito

## Contexto

O [ADR-002](ADR-002-registro-mestre.md) fechou que nenhum produto chama este BC em runtime, e que
a única coisa que atravessa é o claim do token. O que ele **não** disse é o que fazer quando este
BC sabe algo que o produto precisa obedecer. Duas medições mostraram que faltava:

1. **Suspender um tenant não fazia nada nos produtos.** `TenantStatus.Suspended` se documenta como
   *"Cadastro preservado, acesso cortado"*, mas `TenantSuspendedDomainEvent` não tinha handler
   nenhum: o atributo do provedor ficava intacto e o tenant suspenso seguia importando boleto,
   aprovando pagamento e lendo caixa de e-mail.
2. **`ProductCode` não governava nada.** Quem tivesse o tenant no claim usava o BillPayment mesmo
   que o tenant só tivesse contratado o PeopleManagement.

## Decisão

As duas informações trafegam **pelo canal que já existe** — o provedor de identidade:

- **Suspensão** revoga o acesso de todos os vínculos ativos no provedor. O cadastro fica intacto;
  reativar reconcede.
- **Entitlement** ganha **um atributo por produto** (`bp_tenants`, `pm_tenants`) ao lado do
  `tenants` genérico. Cada produto lê o seu, e o guard que já existe passa a negar por produto sem
  uma linha de código nova nos produtos.

Nem chamada síncrona, nem mensageria.

## Por quê

**Síncrono está descartado pelo ADR-002** e o custo é composto: cada request protegida já faz uma
ida ao Keycloak (ticket UMA); um segundo serviço no caminho dobraria a superfície de
indisponibilidade justamente do fluxo que move dinheiro.

**Mensageria custa quatro peças novas para um consumidor.** Não há broker em lugar nenhum do
repositório, este BC não tem outbox (despacha in-process depois do commit), e o consumidor
precisaria de uma **réplica local do tenant** — que é o cadastro replicado que o ADR-002 declinou.
Vira a resposta certa no dia em que houver um segundo consumidor e volume que pague o outbox.

**O claim já é o barramento.** Este BC escreve, os produtos leem, e os dois lados já estavam
implementados. Faltava o conteúdo refletir o que o cadastro sabe.

## Consequências

- **A propagação leva até o TTL do access token** (5 min). O refresh reemite o token já sem o
  tenant, porque o atributo mudou; o que não morre é a sessão. Corte instantâneo exigiria logout
  backchannel — não adotado.
- **A revogação da suspensão passa pelo provisionador, nunca por `RevokeMembership`.** O método de
  domínio protege o último responsável (`TNM.TNT20`) e recusaria cortar justamente o dono, deixando
  a suspensão pela metade; e reativar exigiria recriar vínculos, perdendo papel e histórico.
- **`RequeueFailedAccessProvisioning` passou a consultar o status.** Num tenant suspenso ele emite
  revogação, não concessão — senão `POST /tenants/{id}/access/reprovision` seria a forma de burlar
  a suspensão.
- **O nome do claim de produto não é livre.** O guard casa o **tipo** do claim por `Contains`:
  `"bp_tenants".Contains("tenants")` é verdadeiro. O sentido que importa está seguro
  (`"tenants".Contains("bp_tenants")` é falso, logo o BillPayment não aceita o genérico), mas
  produto novo exige um nome que nenhum outro contenha.
- **Ordem de deploy é obrigatória**: backfill dos tenants → mappers no realm → reprovisionamento de
  todos os tenants → só então o produto passa a ler o claim novo. Fora dessa ordem o atributo nasce
  vazio e todo cliente legítimo toma 403.
- **Os workers do BillPayment continuam fora do alcance.** Varredura de captura, processamento e
  varredura de expectativas rodam sem token, então um tenant suspenso segue tendo a caixa lida.
  Não move dinheiro (o ADR-007 do BillPayment exige aprovação humana, que é HTTP). Aceito e
  registrado; reabrir quando a fase 3 daquele BC existir, porque aí o worker passa a pagar.
- **Cadastro (razão social, endereço, documento) continua sem sincronizar**, como o ADR-002 decidiu.
