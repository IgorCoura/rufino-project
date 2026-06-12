# Gotchas — lições de correções do usuário

Registro de padrões corrigidos pelo usuário (regra "Self-Correction" do CLAUDE.md). Leia no início da sessão.

## 2026-06-11 — Queries são a exceção CQRS: Application → Infra é intencional, e queries não passam pelo mediator

**O que eu propus errado**: diagnosticar `Application → Infra` (csproj) como violação da Dependency Rule e planejar a inversão (porta na Application, impl na Infra, flip das ProjectReferences).

**Correção do usuário**: neste BC o query side (CQRS) é a **única exceção autorizada** a referenciar a Infra diretamente — não inverter, não "corrigir". O problema real era outro: queries estavam sendo despachadas via **mediator**; devem ser interface direta no controller, padrão eShop (`Ordering.API/Application/Queries` → `IOrderQueries`).

**Regra a aplicar**: commands = 100% mediator + `IdentifiedCommand`; queries = `IXxxQueries` + impl com `DbContext`/`AsNoTracking` em `Application/Queries/`, injetadas direto no controller, sem mediator. Ver seções "Query side (CQRS)" e "Regras invioláveis de Handler" no CLAUDE.md.
