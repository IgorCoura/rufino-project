# ADR-001 — TenantManagement é um Bounded Context próprio

**Data:** 2026-08-13 · **Status:** aceito

## Contexto

O cadastro de clientes da plataforma existia só dentro do PeopleManagement, como `Company`, e é
CNPJ-only. O BillPayment atende pessoa física, então não tinha por onde cadastrar um cliente.

As opções eram: (a) acrescentar pessoa física ao `Company` do PeopleManagement; (b) criar o
cadastro dentro do BillPayment; (c) um BC próprio.

## Decisão

**BC próprio**, com `.sln`, banco, schema e deploy separados, espelhando a estrutura do
BillPayment.

## Por quê

- **(a) faria o produto de contas a pagar depender do produto de RH.** O cliente que só usa
  contas a pagar passaria a ter seu cadastro num serviço de gestão de pessoas, e o `Company` está
  amarrado a `Employee`, `Document`, `Department` — arrastaria tudo junto.
- **(b) inverteria o problema**: o PeopleManagement passaria a depender do BillPayment.
- A independência entre os dois produtos é decisão declarada, e o cliente já a materializou em
  pacotes separados (D1–D6 do `client/rufino_v2/CLAUDE.md`). Um cadastro compartilhado dentro de
  um dos produtos desfaria isso do lado do servidor.

## Consequências

- Um terceiro serviço para subir e operar. Custo aceito conscientemente; compose e Dockerfile
  clonados do BillPayment mantêm o custo baixo.
- `TaxId`, `ValueObject`, `Enumeration` e o mediator são **copiados**, não compartilhados — BCs
  não dividem código. Vão divergir, e isso é correto.
- Portas: API `8110`, banco `8112` (PeopleManagement ocupa 8040–8042, BillPayment 8100–8104).
