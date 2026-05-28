# EconomicCore.Architecture — índice de referência

A pasta `EconomicCore.Architecture/` contém o design rationale e a base teórica do BC. **Leia esses documentos antes de modelar — eles são a fonte de verdade, não o código.** Os três primeiros são documentos do projeto; os quatro últimos são as fontes acadêmicas/normativas convertidas de PDF.

## Documentos do projeto

| Arquivo | O que contém | Quando consultar |
|---|---|---|
| [`Modelo-REA-Conceitual.md`](Modelo-REA-Conceitual.md) | Modelagem conceitual REA pura do domínio. Linguagem ubíqua (§3), subdomínios e bounded contexts (§4–5), design conceitual por contexto (§6), as três camadas REA — operacional, conhecimento, valor (§7), padrões temporais pré/pós-pago e DRE por competência (§7-bis), axiomas REA (§8), decisões com justificativa (§10), roadmap faseado (§12). | Antes de qualquer modelagem ou decisão de fronteira de aggregate. É a "profundidade B1" — o que um pesquisador REA validaria. |
| [`Modelo-REA-Tatico.md`](Modelo-REA-Tatico.md) | Aterrissagem tática DDD do modelo conceitual. Corte do grafo REA em aggregates (§1–2), mapa de aggregates e decisão de BC único (§3), invariante de anti-orfandade (§4), design tático completo — registro operacional (§5) e contratos/commitments (§6), IDs/VOs/Smart Enums compartilhados (§7), domain events e fluxo de dualidade (§8), recorte da Fase 1 (§9), SeedWork (§13). | Fonte de verdade para o codegen. Todo aggregate, invariante, erro e VO está especificado aqui com código de erro `ECC.<AGG>##`. |
| [`Instrucoes-Claude-Code.md`](Instrucoes-Claude-Code.md) | Guia prático para gerar o código do domínio via skills do Claude Code. Pré-requisitos do projeto (§0), princípios de conversa com as skills (§1), decisões já tomadas (§2), ordem dos 7 prompts da Fase 1 (§4), checklist pós-geração (§5). | Ao iniciar uma sessão de codegen ou ao onboard de novo agente. |

## Fontes acadêmicas e normativas (REA)

> **Nota de leitura:** linhas referem o arquivo `.md` (use `offset`+`limit` no Read tool). Arquivos convertidos via OCR (McCarthy 1982, Hruby 2006) podem ter artefatos em diagramas.

---

### 1. McCarthy 1982 — [`The_REA_Accounting_Model_A_Generalized_Framework.md`](The_REA_Accounting_Model_A_Generalized_Framework.md)

> McCarthy, W. E. (1982). *The REA Accounting Model: A Generalized Framework for Accounting Systems in a Shared Data Environment.* The Accounting Review, LVII(3), 554–578.

**Referência primária** — define os conceitos que nomeiam os aggregates (`EconomicResource`, `EconomicEvent`, `EconomicAgent`). A crítica a débito/crédito como artefatos de armazenamento fundamenta a decisão de não modelar contas contábeis. Convertido via OCR.

| Linha | Seção | Conteúdo |
|---|---|---|
| 37 | **§I. Database Design and the Conceptual Schema** | Design de banco de dados em ambiente multi-usuário; importância do schema conceitual |
| 43 | §I — Data Model Development | Requirements analysis, view modeling, view integration como fases de design |
| 61 | §I — Conceptual Schema and Semantic Descriptions | Modelos semânticos de 2ª geração; tipos de relacionamento (associação e generalização) |
| 97 | **§II. The REA Accounting Model** | Argumento contra débito/crédito no schema conceitual; proposta de modelar fenômenos econômicos diretamente |
| 105 | §II — Economic Resources and Events | Define resources (stocks) e events (flows); relações stock-flow e duality |
| 134 | §II — Economic Agents and Units | Agents (externos) vs units (internos); control relationships e hierarquias de responsabilidade |
| 164 | §II — Generalized Framework Summary | Combina todos os elementos REA na Figure 5 (entities, associations, generalization, role declarations) |
| 189 | **§III. Database Design with the REA Framework** | Uso operacional do modelo REA para view modeling e view integration |
| 195 | §III — Initial REA Example | Cenário purchase/cash-disbursement demonstrando view modeling e integration passo a passo |
| 228 | §III — Conclusion Materialization | Produção de snapshots de informação a partir de registros de eventos |
| 232 | §III — Resources and Claims | Distingue resource stocks (imbalances de stock-flow) de claims (imbalances de duality) |
| 251 | §III — Procedures | Classifica procedures: triggered, adjustment, view, derivation (eixos timing × base-object-effect) |
| 286 | §III — Design Decisions Based on Existing Practice | 5 convenções contábeis examinadas: claims, temporal summation, event partitioning, macro-duality, equity |
| 300 | §III — Claims as Base Objects | Quando claims (ex.: accounts receivable) devem ser objetos independentes vs atributos de agente |
| 320 | §III — Event Partitioning and Combination | Adjusting entries, combinação de eventos conceitualmente distintos |
| 336 | §III — Macro-level Duality | Matching despesas a receita em nível sumarizado; gains/losses sem duality direta |
| 348 | §III — Equity Transactions | Equity como imbalance de duality entre investment inflows e distribution outflows |

---

### 2. Geerts & McCarthy 2000 — [`The_Ontological_Foundations_of_REA_Enterprise_Information_Systems_2000.md`](The_Ontological_Foundations_of_REA_Enterprise_Information_Systems_2000.md)

> Geerts, G. & McCarthy, W. E. (2000). *The Ontological Foundation of REA Enterprise Information Systems.*

**Referência para a camada de conhecimento e commitments.** Os três axiomas (§II) fundamentam as invariantes de dualidade e stock-flow. A extensão horizontal (type/commitment images) mapeia diretamente para `EconomicContract`/`Commitment` e o futuro `Knowledge.Domain`.

| Linha | Seção | Conteúdo |
|---|---|---|
| 1 | Título + Autores | Geerts (U. Delaware) e McCarthy (Michigan State); acknowledgments |
| 28 | Abstract | Propõe REA como ontologia de domínio empresarial; extensão vertical e horizontal |
| 34 | **§I. Introduction** | Enquadra ontologias empresariais; introduz REA como framework de contabilidade conceitual |
| 38 | §I — What is an ontology? | Definição de Gruber (1993); contraste com database schemas (objetivo, escopo, conteúdo) |
| 56 | §I — How to construct an ontology? | 3 abordagens: pragmática (competency questions), teórica, empírica |
| 62 | §I — What is a good ontology? | Critérios de Gruber: clarity, coherence, extendibility, minimal encoding bias |
| 72 | **§II. Ontological Foundation** | Separa ontologia em infraestrutura operacional (fenômenos reais) e de conhecimento (type-level) |
| 80 | §II — Exchange | Padrão de troca REA: eventos requitados, resources, agents participantes (UML) |
| 86 | §II — Economic Events and Duality | Events como mudanças em meios escassos; duality liga inflow/outflow; transfer vs transformation |
| 92 | §II — Economic Resources and Stock-Flow | 5 tipos de stock-flow: use, consumption, give, take, production |
| 98 | §II — Economic Agents and Participation | Inside vs outside agents; participation e accountability |
| 104 | §II — Congruent Exchanges | Trocas com eventos simultâneos (ex.: cash-sales); duality capturada só via stock-flow |
| 110 | §II — Association, Linkage and Custody | Dependências agent-agent (responsibility), resource-resource (composition), resource-agent (custody) |
| 122 | §II — Commitment | Commitments como acordos de eventos futuros; reciprocal; reificados como contracts/schedules |
| 130 | §II — **Axioms** | **3 axiomas fundamentais REA:** (1) resource flow completeness, (2) duality pairing, (3) inside/outside agent |
| 140 | §II — Knowledge Infrastructure | Camada abstrata de type images e seus relacionamentos |
| 142 | §II — Type Images | Typification: Resource Type, Event Type, Agent Type, Commitment Type |
| 156 | §II — Type Image Relationships | Policies (restrições), prototypes (blueprints), characterizations (informativas) |
| 164 | **§III. Three-Layer Architecture** | Decomposição vertical: enterprise script (topo), process/exchange (meio), task/recipe (base) |
| 170 | §III — Tasks | Compromisso representacional de exchange; critérios para simplificação |
| 184 | §III — Recipes and Orderings | Recipe = sequência ordenada de tasks; ordering = dependência entre tasks |
| 188 | §III — Enterprise Scripts and Processes | Process = exchange + tasks; enterprise script = configuração completa de processos |
| 194 | §III — Knowledge Infrastructure (3-layer) | Type images para arquitetura de 3 camadas: process type, exchange type, task type |
| 216 | **§IV. Applications** | 3 categorias de aplicação: corporate memories, conceptual design support, intensional reasoning |
| 220 | §IV — Corporate Memories | Knowledge management via REA: best practices, resource descriptions, task expertise |
| 224 | §IV — Conceptual Design Support | REA como analysis patterns reutilizáveis; axiomas forçam raciocínio causal explícito |
| 235 | §IV — Intensional Reasoning | Uso operacional com sistema CREASY (Prolog); detecção automatizada de claims |
| 245 | **§V. Conclusions** | Resume primitivos REA aumentados; delineia pesquisa futura |
| 268 | **References** | 40+ obras (ontologia, economia, contabilidade, AI, knowledge engineering) |
| 376 | **Figures** | Descrições textuais detalhadas das 8 figuras (exchange, congruent exchange, association, commitments, type images, policy, 3-layer architecture, knowledge infrastructure) |

---

### 3. ISO/IEC 15944-4:2015 — [`ISO_IEC_15944-4_2015(en).md`](ISO_IEC_15944-4_2015(en).md)

> ISO/IEC 15944-4:2015. *Information technology — Business Operational View — Part 4: Business transaction scenarios — Accounting and economic ontology.* 2nd ed.

**Referência normativa para terminologia.** As 67 definições de §3 (economic resource, economic event, economic agent, duality, participation, stock-flow, commitment, economic contract) são a fonte canônica da linguagem ubíqua do `Modelo-REA-Conceitual.md`.

| Linha | Seção | Conteúdo |
|---|---|---|
| 1 | Título + Metadados | Business Operational View Part 4, 2ª edição 2015-04-01 |
| 9 | Foreword | Contexto ISO/IEC, comitê responsável, histórico de edição |
| 54 | **§0 Introduction** | |
| 54 | §0.1 Purpose and overview | Motivação Open-edi; introduz ontologia REA como framework OeBTO |
| 110 | §0.2 Definition of OeBTO | Definição de Gruber aplicada ao REA; ontologia formal, explícita, compartilhada |
| 128 | §0.3 Independent vs trading partner perspective | Perspectiva independente vs parceiro; por que redundância é inaceitável |
| 173 | §0.4 The OeBTO | Definição formal e 5 princípios governantes derivados da ISO/IEC 15944-1 |
| 200 | §0.5 Organization of this standard | Estrutura do documento: Cláusulas 1–7, anexos normativos/informativos |
| 221 | **§1 Scope** | Escopo: diagramas UML, máquinas de estado, constraints para colaborações OeBTO |
| 233 | **§2 Normative references** | Referências indispensáveis: ISO/IEC 6523-1, 11179-3, 14662, 15944-1, 15944-5 |
| 251 | **§3 Terms and definitions (67 termos)** | Definições formais de agent até undefined market model, com notas e fontes |
| 691 | **§4 Symbols and abbreviations** | BOV, BTE, OeBTO, REA, UML, UN/CEFACT etc. |
| 743 | **§5 Declarative component — data classes** | Componente declarativo da ontologia: classes primitivas e derivadas |
| 745 | §5.1 Person and economic resources | Sub-tipos de Person; taxonomia de economic resource; Rules 1–4 |
| 816 | §5.2 Normative data categories for economic exchange | Padrão REA core: resources, events, Persons; 4 perguntas fundamentais (who/what/when/why); Rules 5–6 |
| 888 | §5.3 Addition of business event | Business event adicionado ao padrão OeBTO; estrutura recursiva de workflow; Rule 7 |
| 904 | §5.4 Extension into types | Typification: type images para resources, events, Persons; policy specification; Rule 8 |
| 938 | §5.5 Locations and claims | Adições não-normativas: business location e economic claim (temporal duality imbalance) |
| 952 | §5.6 Adding commitments | Commitments: economic contracts como bundles de compromissos recíprocos; Rule 9 |
| 968 | §5.7 Business transactions with contracts | Modelo completo: fulfillment, reciprocal, economic contract, bundle; Rules 10–12 |
| 1004 | §5.8 Typifying agreements and transactions | Typification de acordos: markets, scenarios, pricing methods (bid, auction, matching) |
| 1027 | **§6 Procedural component — state machines** | Componente procedural: entity types, lifecycles, máquinas de estado; Rule 13 |
| 1044 | §6.1 Ontological components → Open-edi phases | 5 fases Open-edi (planning, identification, negotiation, actualization, post-actualization); Rules 14–17 |
| 1234 | **§7 Constraint component — business rules** | 3º componente OeBTO: business rules como constraints (dados + procedures) |
| 1238 | §7.1 Business rules and constraints | Internal vs external constraints; derivation rules vs structural/behavioral |
| 1312 | §7.2 Constraint examples | Exemplos concretos: separation of duties (OCL), Michigan sales tax, pre/post-conditions |
| 1330 | **Annex A (normativo) — Glossário bilíngue** | Lista consolidada de termos EN/FR para adaptabilidade cultural |
| 1379 | Annex A.4 — Terms in French order | Tabela 3 colunas: nº da definição, termo FR, termo EN |
| 1649 | Annex A.6 — Consolidated matrix | Matriz bilíngue completa: 67 termos com definições, notas e exemplos em EN e FR |
| 3115 | **Annex B (informativo) — REA Model Background** | Histórico detalhado da ontologia REA |
| 3119 | Annex B.1 — REA introduction | Origens na teoria contábil; uso em UN/CEFACT |
| 3135 | Annex B.2 — Basic REA ontology | McCarthy 1982, AAA awards; estrutura básica Resource-Event-Agent e duality |
| 3164 | Annex B.3 — Adding commitments | Estende REA com Commitments, Contracts, Agreements, Claims, Locations |
| 3195 | Annex B.4 — Adding types | Type images (Resource Type, Event Type, Partner Type) para policies prescritivas |
| 3232 | Annex B.5 — Suitability of REA in Open-edi | Avaliação de qualidade REA (critérios Gomez-Perez); correspondência ISO/REA/UN-CEFACT |
| 3248 | **Annex C (normativo) — BTM constraints** | Business Transaction Model: 2 classes de constraints (internal e external) com UML |

---

### 4. Hruby 2006 — [`Model-Driven_Design_Using_Business_Patterns-Pavel_Hruby.md`](Model-Driven_Design_Using_Business_Patterns-Pavel_Hruby.md)

> Hruby, P. (2006). *Model-Driven Design Using Business Patterns.* Springer. 368 págs.

**Referência para padrões de implementação.** Quando um padrão REA precisar ser mapeado para código (exchange patterns, commitment fulfillment, type-images), este livro tem diagramas UML e discussão de trade-offs. Convertido via OCR — diagramas aparecem como texto descritivo.

**Part I — Structural Patterns** (padrões fundamentais REA)

| Linha | Seção | Conteúdo |
|---|---|---|
| 391 | **Part I opener** | Capa da Parte I: padrões estruturais REA |
| 395 | §1.1 What Is REA? | Introdução a Resources, Events, Agents e regras de domínio |
| 398 | §1.2 Joe's Pizzeria | Exemplo introdutório: modelagem de processos de troca |
| 401 | §1.2.1 Sales Process | Modelo REA para vendas: troca de pizza por caixa |
| 404 | §1.2.2 Purchase Process | Modelo REA para compra de matérias-primas |
| 412 | §1.4 REA Exchange Process In Detail | Semântica detalhada: resources, events, agents, duality em exchange |
| 415 | §1.4.1 Economic Resources | Resources como portfólios de direitos (ownership, usage, copy) |
| 418 | §1.4.2 Inflow and Outflow | Relações resource↔increment/decrement events com cardinalidades |
| 421 | §1.4.3 Economic Events | Events transferem direitos entre agents; momento ou intervalo |
| 424 | §1.4.4 Exchange Duality | Liga increment e decrement events; rastreia quais recursos trocados |
| 427 | §1.4.5 Economic Agent | Indivíduos ou organizações com capacidade de deter/transferir direitos |
| 430 | §1.4.6 Provide and Receive | Quem perde e quem ganha direitos durante eventos |
| 433 | §1.5 How Joe's Pizzeria Obtains Pizza | Introduz conversion processes: produzir pizza a partir de matérias-primas |
| 442 | §1.6 REA Conversion Process Pattern | Padrão para processos que criam/modificam resources via conversão |
| 449 | §1.7.2 Produce, Use and Consume | 3 relações entre resources e events em conversão |
| 464 | §1.8 Value Chain | Como exchange e conversion se conectam via shared resources |
| 467 | §1.9 REA Value Chain Pattern | Padrão para modelar empresa como cadeia de valor |
| 475 | §1.10 REA Value Chain in Detail | Semântica de resource value flows, cost, unit price |
| 488 | §2.1 Group Pattern | Coleções heterogêneas para aplicar business rules |
| 492 | §2.2 Type Pattern | Coleções homogêneas com definições/descrições de resources, events, agents |
| 494 | §2.3 Difference Between Types and Groups | Types definem características de membros; groups são coleções arbitrárias |
| 497 | §2.4 Commitment Pattern | Promessas de eventos futuros com fulfillment e reservation |
| 501 | §2.5 Contract Pattern | Coleção de commitments + termos sobre descumprimento |
| 504 | §2.6 Schedule Pattern | Coleção de commitments para conversion processes com planos de mitigação |
| 507 | §2.7 Policy Pattern | Constraints sobre exchanges e conversions via relações de grupo |
| 510 | §2.8 Linkage Pattern | Estrutura hierárquica (bill of materials) de economic resources |
| 513 | §2.9 Responsibility Pattern | Dependência/reporte entre economic agents |
| 516 | §2.10 Custody Pattern | Responsabilidade de agents por resources específicos |
| 520 | §3.1 Representing the Metamodel | Escolha entre implementar application model vs metamodel |
| 523 | §3.2 Component Model | Arquitetura: REA Model, Domain Model, Data Access, Database, Web, OLAP |
| 527 | §3.4 The Domain Model Component | Classes Customer, Pizza, Order herdando de REA base (28 linhas de código) |
| 530 | §3.5 The Database | Tabelas mimicking domain model com foreign keys para O/R mapping |
| 536 | §3.6 The Data Access Layer | Classe estática com métodos CRUD e wiring de event handlers |
| 539 | §3.8 The Fulfillment Page | Web page para tracking de fulfillment e status de pagamento |
| 542 | §3.9 The OLAP Cube | Pivot tables e gráficos para estatísticas de vendas |

**Part II — Behavioral Patterns** (extensões funcionais)

| Linha | Seção | Conteúdo |
|---|---|---|
| 7070 | **Part II opener** | Estender o esqueleto estrutural REA com funcionalidade específica do usuário |
| 7114 | §4.1 Behavior May Not Be Localizable | Cross-cutting concerns que perpassam múltiplas entidades |
| 7184 | §4.2 Framework-Based Approach | Framework de aspectos em 2 níveis (type + application model) |
| 7395 | §4.3 No Complete List of Behavioral Patterns | Padrões comportamentais são abertos; novos emergem de requisitos |
| 7419 | §5.1 Identification Pattern | Nomeação/numeração de entidades REA (serial numbers, auto-numbers) |
| 7688 | §5.2 Classification Pattern | Categorização hierárquica com auto-classificação |
| 8000 | §5.3 Location Pattern | Position e Route para rastreamento espacial de eventos |
| 8227 | §5.4 Posting Pattern | Histórico imutável de eventos/commitments com dimensões para analytics |
| 8451 | §5.5 Account Pattern | Saldos agregados com addition/subtraction e drill-down |
| 8809 | §5.6 Materialized Claim Pattern | Representação física de duality desbalanceada (invoices, credit memos) |
| 9066 | §5.7 Reconciliation Pattern | Matching many-to-many de increment/decrement events |
| 9308 | §5.8 Due Date Pattern | Deadlines com activation rules, state machine, dependências |
| 9558 | §5.9 Description Pattern | Informação não-estruturada (texto, imagens, URLs) sobre entidades REA |
| 9715 | §5.10 Notification Pattern | Notificação de agents via email, SMS, postal, voz |
| 9898 | §5.11 Note Pattern | Comentários internos com autor e data |
| 10042 | §5.12 Value Pattern | Valores quantitativos (preços, custos, quantidades, impostos, descontos) |
| 10230 | §5.13 Inventor's Paradox Pattern | Paradoxo de Polya aplicado à descoberta de novos padrões |

**Part III — Modeling Handbook** (exemplos aplicados)

| Linha | Seção | Conteúdo |
|---|---|---|
| 11590 | **Part III opener** | Handbook com exemplos menos triviais de modelagem REA |
| 11637 | §7.1 Cash Sale | Troca mais simples: produto por caixa, com claim e cash return |
| 11743 | §7.2 Product Return | Duality 4-ária: venda, recebimento, devolução de produto, devolução de caixa |
| 11852 | §7.3 Loan and Rent | Aluguel como evento time-interval de troca de direitos de uso |
| 11937 | §7.4 Financial Loan | 3 eventos separados (recebimento, devolução, juros) para recursos não-identificáveis |
| 12086 | §8.1 Creating a New Product | Conversão: consumo de materiais, uso de ferramentas, consumo de trabalho → produto |
| 12256 | §8.2 Chain of Conversion Processes | Fases sequenciais de produção com produtos intermediários |
| 12365 | §8.3 Modifying a Product | Conversão que altera features sem destruir o resource |
| 12519 | §8.4 Creating and Consuming Services | Recursos transientes de serviço; útil para outsourcing |
| 12656 | §9.1 Sale and Shipment | Venda (exchange) + expedição (conversion) alterando localização |
| 12752 | §9.2 Resources Consumed During Sales | Trabalho de vendedores consumido para viabilizar vendas |
| 12867 | §9.3 People Management | Trabalho de gerente consumido para aumentar valor do trabalho de subordinado |
| 12959 | §9.4 Education | Educação como resource que aumenta valor do trabalho |
| 13056 | §9.5 Taxes | VAT/sales tax como troca de caixa por serviços governamentais |
| 13489 | §9.7 Waste | Disposição de resources com valor negativo |
| 13591 | §9.8 Purchasing and Selling Services | Outsourcing via troca de resources transientes de serviço |
| 13686 | §9.9 Transient Resources | Eletricidade, serviços consumidos na criação |
| 14017 | §10.1 Purchase Order | Contrato com linhas de compra, linhas de pagamento, types, fulfillment, claims |
| 14099 | §10.2 Labor Acquisition | Contrato de trabalho com labor type, salary commitment, bonus |
| 14201 | §10.3 Guarantee | Termo de contrato instanciando commitments de devolução condicionais |
| 14275 | §10.4 Insurance | Contrato com commitment condicional de reembolso |
| 14396 | §10.5 Penalty for Violated Commitment | Termo criando commitment de penalidade por descumprimento |
| 14520 | §10.6 Schedule | Schedule de produção com requisições de materiais, ferramentas, trabalho |
| 14641 | §10.7 Transport | Viagem de negócios: compra de assento (exchange) + transporte de trabalho (conversion) |

**Appendices**

| Linha | Seção | Conteúdo |
|---|---|---|
| 14641 | **Appendix A — REA Ontology** | Categorias ontológicas REA e seus significados intuitivos |
| 14769 | **Appendix B — Notes on Modeling** | Princípios fundamentais de modelagem REA |
| 14784 | B.1 No Top-Level Business Process | Sistema como value chain de processos independentes, não decomposição top-down |
| 14796 | B.2 No Premature Sequential Ordering | Expressar constraints logicamente, não temporalmente |
| 14832 | B.3 Bottom-Up Approach | Bottom-up para desenvolvimento (componentes gerais), top-down para explicação |
| 14854 | B.4 Trading Partner vs Independent View | Perspectiva da empresa vs observador neutro |
| 14930 | B.5 Levels of Granularity | 3 níveis: value chain, REA entities, task-level |
| 14970 | B.6 Models, Metamodels and UML | 3 níveis de abstração: runtime, application model, metamodel |
| 15070 | **Appendix C — Patterns and Pattern Form** | Forma de pattern modificada de Coplien usada no livro |
| 15124 | **References** | 40+ fontes: McCarthy, Geerts, Evans, Fowler, Polya |
| 15290 | **Index** | Índice alfabético de account a XML stylesheet |

## Como usar o índice

1. **Modelagem nova ou dúvida conceitual** → comece pelo `Modelo-REA-Conceitual.md`, depois consulte McCarthy (1982) ou Geerts & McCarthy (2000) para a fundamentação.
2. **Codegen ou implementação** → `Modelo-REA-Tatico.md` é a fonte de verdade; `Instrucoes-Claude-Code.md` dá o roteiro de execução.
3. **Terminologia ou definição formal** → ISO/IEC 15944-4 (§3, Annex A).
4. **Padrão de design REA → código** → Hruby (2006), capítulos por padrão (exchange, commitment, contract, etc.).
