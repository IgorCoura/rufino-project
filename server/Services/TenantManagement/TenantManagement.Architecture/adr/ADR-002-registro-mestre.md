# ADR-002 — O Tenant é registro-mestre; os cadastros locais continuam

**Data:** 2026-08-13 · **Status:** aceito

## Contexto

Com o BC novo, três lugares passariam a saber o nome e o documento do cliente: o `Tenant` daqui,
o `Company` do PeopleManagement e o `PayerProfile` do BillPayment.

## Decisão

O `Tenant` é dono do **`TenantId`, do tipo, do documento fiscal, do endereço e de quem acessa
o quê**. `Company` e `PayerProfile` **continuam existindo** como cadastro local de cada produto.

Nenhum produto chama este BC em tempo de execução. A única coisa que atravessa é o **claim do
token**.

## Por quê

- Um cadastro central chamado em runtime tornaria este serviço uma dependência de disponibilidade
  para pagar boleto e emitir documento. Não é o papel dele.
- Sincronizar por evento faria os produtos perderem a edição local do próprio cadastro, e criaria
  replicação assíncrona para resolver um problema que ninguém tinha.
- O `PayerProfile` não é redundância: ele guarda o que **contas a pagar** precisa (documentos
  adicionais, casamento por raiz de CNPJ, referência da subconta) e que não é identidade.

## Consequências

- **Divergência de cadastro é possível e aceita.** Razão social alterada aqui não se propaga
  sozinha. Se incomodar, vira evento de sincronização depois — não agora.
- **O que atravessa pelo claim ficou definido no [ADR-005](ADR-005-integracao-pelo-claim.md)
  (2026-08-17).** Este ADR dizia o que **não** atravessa e deixava em aberto o que fazer quando
  este BC sabe algo que o produto precisa obedecer. A resposta: suspensão e entitlement de produto
  viajam pelo provedor de identidade — a suspensão revogando o acesso de todos os vínculos, o
  produto ganhando um atributo próprio (`bp_tenants`, `pm_tenants`). Continua valendo que nenhum
  produto chama este BC em runtime.
- O `PayerProfile` continua sendo criado **dentro do BillPayment**, pelo próprio usuário. Este BC
  **não** chama o BillPayment para semeá-lo: seria exatamente o acoplamento que o ADR-001 evita.
- O backfill preserva o Guid dos cadastros existentes; sem isso, todo acesso teria de ser
  reemitido.
