# ADR-004 — A camada de autorização é copiada do PeopleManagement, não compartilhada

**Data:** 2026-08-13 · **Status:** aceito

## Contexto

A autorização deste BC é a mesma do PeopleManagement, por decisão do usuário: endpoints
declarados com `[ProtectedResource(recurso, escopo)]`, recursos e escopos definidos no Keycloak,
papéis amarrados por policy no realm. São ~12 arquivos entre `Authentication/` e `Authorization/`.

Esta é a **terceira** cópia potencial desse código (PeopleManagement, TenantManagement, e o
BillPayment quando chegar à fase 6).

## Decisão

**Copiar**, seguindo o precedente do BC (o BillPayment clonou o esqueleto do EconomicCore).

## Por quê

- O código é pequeno, estável e sem regra de negócio.
- Um pacote compartilhado amarraria o ciclo de release dos três serviços: subir a versão para
  corrigir um detalhe de um obrigaria a revalidar os outros dois.
- Os BCs já não compartilham `TaxId`, `ValueObject` nem mediator. Uma exceção só para
  autorização criaria uma dependência transversal onde não há nenhuma.

## Consequências

- **Correção feita num, não chega nos outros.** É o custo real, e vale registrar quais correções
  a cópia já traz: o `AuthorizationResultHandler`, que impede o 401 do servidor de autorização
  colapsar em 403 e o servidor fora do ar virar 403 em vez de 503.
- A cópia foi normalizada para o estilo deste BC (`namespace` file-scoped, `using` interno) por
  `dotnet format` — o conteúdo é o mesmo.
- Se um dia houver um quarto consumidor, este ADR é o lugar de reabrir a decisão.
