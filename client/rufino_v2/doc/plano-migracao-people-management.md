# Plano de migração — People Management vira pacote

**Data:** 2026-09-03 · **Escopo:** `client/rufino_v2` · **Fase 3** da sequência do `CLAUDE.md`

Este documento executa a linha marcada como adiada na tabela "Sequência de migração":
*"3 — mover PM para pacote + costura `AppModule`"*. Ele descreve **como**, em que ordem e o que
verificar em cada passo. Não é design novo: D1, D2, D3 e D6 já estão decididas, e este plano só as
aplica ao último produto que ainda mora na casca.

---

## 1. Ponto de partida — medido, não estimado

| | |
|---|---|
| Arquivos `.dart` em `lib/` | **249** (~47.000 linhas) |
| — do **PeopleManagement** | **195 (78%)** |
| — da **casca** | 42 (17%) |
| — **mistos** (precisam ser divididos) | 6 |
| — **mortos** (sem chamador) | 6 |
| `lib/ui/features/` | 13 features, 30.941 linhas — 11 são PM |
| `lib/app.dart` | **1.103 linhas**, 29 `GoRoute` (≈22 do PM) |
| Testes na casca | 143 arquivos, **1.656 testes** |
| Fakes em `test/testing/fakes/` | 17, sendo **13 de repositórios do PM** |

**Baseline verde, conferido antes de escrever este plano:**

```
flutter analyze                    → No issues found!
flutter test (casca)               → 1656 testes, all passed
flutter test (bill_payment)        →  346 testes, all passed
flutter test (tenant_management)   →   99 testes, all passed
```

**Peso por feature** — é isto que dita o fatiamento:

| Feature | Arquivos | Linhas | |
|---|---:|---:|---|
| `employee` | 24 | **12.206** | PM |
| `batch_document` | 5 | 5.189 | PM |
| `batch_download` | 7 | 2.895 | PM |
| `document_template` | 4 | 2.200 | PM (2 arquivos mortos) |
| `department` | 8 | 1.734 | PM |
| `require_document` | 4 | 1.512 | PM |
| `document_group` | 6 | 1.171 | PM (2 arquivos mortos) |
| `document_dashboard` | 2 | 1.050 | PM |
| `workplace` | 4 | 769 | PM |
| `company` | 2 | 483 | PM (só sobrou `/company/edit/:id`) |
| `auth` | 8 | 762 | **casca** |
| `home` | 2 | 679 | **misto** |
| `debug` | 1 | 291 | casca (gated pelo recurso `debug` da audiência do PM) |

`employee` sozinho é 39% da UI do produto. Ele **não** cabe em uma fase.

---

## 2. O achado que muda o tamanho do problema

**O acoplamento é assimétrico, e aponta para o lado fácil.**

A casca depende do PM; o PM quase não depende da casca. Não existe **um único** import de
`ui/features/auth/**` ou de `domain/repositories/auth_repository.dart` a partir de código do PM — a
autenticação já chega por callback (`getAuthHeader`), exatamente como no `bill_payment`. E nenhuma
entidade do PM vaza para fora: `Company` é importada por 4 arquivos, todos do próprio produto.

Toda a dependência da casca sobre o PM cabe em quatro pontos:

| Ponto | O quê |
|---|---|
| `lib/core/tenant/tenant_session_bridge.dart:6` | importa `CompanyRepository` e chama `getCompanyDetail` / `selectCompany` / `clearSelectedCompany` — **o único consumidor real fora do PM** |
| `lib/ui/features/home/widgets/home_screen.dart:233-275` | strings de recurso e rotas do PM escritas à mão |
| `lib/app.dart` | 14 pares service/repo, ~22 rotas, 14 providers |
| `lib/core/config/app_config.dart:24,50` | `peopleManagementUrl` + `peopleManagementAudience` |

Isso significa que a migração é sobretudo **mover arquivo e reapontar import**, não desatar nós. O
único nó de verdade é o `CompanyRepository`, e ele se resolve com um contrato de uma linha
(decisão C).

## 3. O que joga contra

- **Não existe CI que rode `flutter analyze` nem `flutter test`.** Os workflows em
  `.github/workflows/` cobrem as APIs .NET e o build Android (`deploy-rufino-android.yml`, que roda
  só `flutter pub get` + `flutter build`). O `gotchas.md` já registra que **`flutter build` passa
  com import cruzado** — só o analyzer recusa. A regra que sustenta o isolamento entre produtos
  hoje **nunca é verificada automaticamente**. Pré-requisito, não melhoria.
- **O PM usa plugins de plataforma direto na UI.** `file_picker` em 5 arquivos
  (`batch_document` ×2, `document_template`, `employee` ×2) e `camera` no scanner. O `bill_payment`
  não declara plugin nenhum — recebe `DocumentPicker`/`LinkOpener` por callback. Ver decisão B.
- **Lógica de produto vazou para o roteador raiz.** As rotas `/batch-document` e `/batch-download`
  (`app.dart:951-1023`) fazem `context.read<CompanyRepository>().getSelectedCompany()` dentro de um
  `FutureBuilder` **no builder da rota**, e `DocumentScannerRepositoryImpl` é instanciado ali
  também (l. 912, 972). Isso não pode ser copiado para dentro do pacote como está — é a mesma
  família do bug "ViewModel criado no builder" do `gotchas.md`.
- **`peopleManagementUrl` é `host:porta`, não origem completa.** Os services montam a URL com
  `Uri.https(baseUrl, path)`, enquanto os BCs novos aceitam `http://host:porta`. Ao mover os
  services, **não** os "uniformize" com os do `bill_payment` sem trocar a chave de configuração
  junto — o app deixaria de subir em desenvolvimento.
- **Os shims de permissão em `lib/` estão vivos apenas nos testes.** Nenhum arquivo de `lib/` os
  importa (produção já usa `rufino_core` direto), mas `lib/domain/entities/permission.dart` sozinho
  sustenta 19 testes. Apagá-los cedo quebra a suíte por um motivo que nada tem a ver com a
  migração.

---

## 4. Seis decisões a fechar antes de começar

### A — `rufino_auth` (Fase 2 da sequência) vem antes?

**Não.** Continua adiada. O PM não importa auth — consome `getAuthHeader` por callback, e o
inventário confirma zero imports. Extrair auth antes acrescentaria uma segunda mudança estrutural
na mesma janela sem comprar nada. Se sobrar acoplamento real depois, a Fase 2 ganha motivo próprio.

### B — Plugins de plataforma: dependência do pacote ou porta na casca?

**Dividir por natureza do pacote**, seguindo o precedente do `bill_payment` — que **declara**
`syncfusion_flutter_pdfviewer` no próprio pubspec mas **não declara** `file_picker` nem
`url_launcher`. Biblioteca Dart pura entra no pacote; plugin que pede permissão nativa, canal de
plataforma ou entrada no manifesto fica com quem monta o app.

| Vai para o `people_management` | Fica na casca, atrás de porta |
|---|---|
| `syncfusion_flutter_pdf`, `syncfusion_flutter_pdfviewer`, `syncfusion_flutter_xlsio` | `file_picker` |
| `archive`, `image`, `pdf`, `pdf_combiner` | `camera`, `permission_handler` |
| `flutter_json_view`, `mask_text_input_formatter`, `intl`, `uuid` | `cunning_document_scanner`, `google_mlkit_text_recognition` |
| | `file_saver` |

**Correções que o inventário trouxe** — não mova por suposição:

- `google_fonts` e `material_symbols_icons` têm **zero** imports em `lib/`; são do `rufino_core` e
  dos outros pacotes. (De passagem: `lib/` usa `Icons` do Material, contrariando o guideline do
  `CLAUDE.md` — corrigir é trabalho separado.)
- `url_launcher` **não tem nenhum uso no PM**: é do OAuth (casca) e do `bill_payment`.
- `shimmer` e `crypto` são **dependências mortas no workspace inteiro** e `flutter_svg` só vive
  pelo `rufino_logo.dart`, que é código morto. Saem na limpeza (Fase 1), não na migração.

As portas a criar no pacote: `DocumentPicker` (o typedef já existe em `bill_payment` e **sobe para
`rufino_core`** aqui, porque passa a ter dois consumidores — literalmente o critério D3),
`FileSaver` e `DocumentScanner`.

### C — Onde fica a ponte tenant → empresa?

**`CompanyRepository` e a chave `selected_company` vão para o pacote; o `TenantSessionBridge`
continua na casca** e passa a depender do que o barril exporta.

O bridge costura três coisas (tenant, PM e as três audiências de permissão), e costura é trabalho
da casca — movê-lo para dentro do PM faria o pacote conhecer `bill_payment`. O contrato mínimo é
uma função, não o repositório inteiro:

```dart
// exportado por package:people_management/people_management.dart
Future<bool> selectPeopleManagementCompany(String tenantId);
Future<void> clearPeopleManagementCompany();
```

É a mesma forma do `getTenantId`, invertida. Uma classe da casca muda.

### D — A costura `AppModule` (D6) entra nesta migração?

**Não. Fase separada, depois.** A D6 resolve um problema real (o menu do Home tem os destinos
escritos à mão, misturando strings cruas do PM com constantes do `bill_payment`), mas fazê-la junto
significa mover 195 arquivos **e** trocar o contrato dos três pacotes no mesmo intervalo. Migre o PM
reproduzindo o padrão que `bill_payment` usa hoje; faça a D6 depois, de uma vez, para os três. O
estado final é o mesmo e cada passo é reversível sozinho.

### E — Os shims de reexport em `lib/` morrem?

**No fim, não durante.** Enquanto os arquivos se movem, os shims evitam mexer no import de dezenas
de arquivos no mesmo commit em que eles trocam de pasta. Com o pacote fechado, cada `export`
remanescente que só servia ao PM é apagado — junto com a atualização dos 19 testes que hoje
dependem de `lib/domain/entities/permission.dart`.

### F — O que fazer com o `debug`?

`lib/ui/features/debug/` é casca por conteúdo (diagnóstico sobre `AppConfig`) mas está protegido
pelo recurso `debug` da audiência **do PM**. **Fica na casca**, e o guard passa a usar o notifier do
pacote — é o mesmo tipo de dependência que o Home tem, e não justifica mover a tela.

---

## 5. O padrão a reproduzir (extraído de `bill_payment`)

```
packages/people_management/
├── pubspec.yaml              name, publish_to:'none', resolution: workspace, sdk ^3.6.0
├── analysis_options.yaml     cópia textual: include ../../ + depend_on_referenced_packages: error
├── lib/
│   ├── people_management.dart          barril: doc de biblioteca + `library;`
│   └── src/
│       ├── people_management_permissions.dart   Resources, Scopes, Notifier, 2 typedefs de guard
│       ├── data/       *_api_service.dart · *_repository_impl.dart · *_api_models.dart
│       ├── domain/     entidades ricas · *_repository.dart (interfaces) · exceções seladas
│       └── ui/
│           ├── people_management_routes.dart    peopleManagementRoutes(...) + PeopleManagementRoutes
│           ├── people_management_pages.dart     as *Page donas do ViewModel
│           ├── people_management_back_button.dart
│           ├── <feature>/                       tela + viewmodel LADO A LADO
│           └── shared/
└── test/{data,domain,ui,fakes}/   espelha lib/src/; fakes.dart central
```

Regras do padrão que não são negociáveis, porque cada uma corrige um bug já vivido:

1. **O barril exporta `data/` + `domain/` + o arquivo de rotas + os typedefs de callback. Nada de
   tela, ViewModel, `*_pages.dart` ou DTO mapper.** É o critério do `bill_payment`, o mais recente
   e o mais apertado. (`tenant_management` exporta telas e viewmodels — herança da fase 5; não é o
   modelo a copiar.)
2. **A `Page` é dona do ViewModel, nunca o builder da rota** — é para isso que `*_pages.dart`
   existe. Criar no builder faz a tela voltar e girar para sempre (`gotchas.md`).
3. **Rota literal declarada antes da `:id` irmã**, com teste que verifica a ordem.
4. **O guard de rota libera enquanto `status != PermissionStatus.loaded`**, senão um F5 na web
   expulsa o usuário.
5. **Navegação sai do módulo por callback**, nunca por string escrita na tela.
6. **Services recebem `http.Client`, `baseUrl` e `getAuthHeader` por construtor** — o pacote nunca
   constrói cliente HTTP.
7. **Repositório reporta no catch** (`reporter.failure(e, st)`); ViewModel não reporta.

---

## 6. As fases

Cada fase é um commit próprio e termina com `flutter analyze` limpo e as quatro suítes verdes.

### Fase 0 — rede de segurança (pré-requisito)

1. `.github/workflows/client-flutter.yml`: `flutter pub get` → `flutter analyze` → `flutter test`
   na casca **e em cada pacote**. Sem isso a regra `depend_on_referenced_packages: error` continua
   sendo documentação, e a migração inteira se apoia nela.
2. Registrar o baseline da seção 1 no PR, para comparar no fim.

**Pronto quando:** o workflow roda verde no commit atual **e** falha ao receber um import cruzado
deliberado.

### Fase 1 — limpeza (commit separado, antes de mover qualquer coisa)

Regra do `CLAUDE.md`: *"Before ANY structural refactor: remove all dead props, unused exports,
unused imports. Commit cleanup separately."* O inventário já achou o que remover:

| Remover | Motivo |
|---|---|
| `ui/features/document_group/widgets/document_group_list_screen.dart` + seu viewmodel | nenhuma rota os constrói |
| `ui/features/document_template/widgets/document_template_list_screen.dart` + seu viewmodel | não existe rota de listagem |
| `ui/core/widgets/rufino_logo.dart` | `RufinoLogo` não é instanciado em lugar nenhum |
| `data/repositories/permission_repository_impl.dart` | shim sem nenhum importador, nem em teste |
| deps `shimmer`, `crypto` | zero imports no workspace |
| dep `flutter_svg` | só o `rufino_logo` morto a usava |

São ~1.500 linhas que não precisam ser migradas nem revisadas. Os testes que cobrem as telas mortas
saem junto.

### Fase 2 — pacote vazio, fronteira provada

1. `packages/people_management/` com `pubspec.yaml`, `analysis_options.yaml` (cópia textual) e um
   barril mínimo.
2. Acrescentar a `workspace:` e a `dependencies:` do `pubspec.yaml` raiz — onde o comentário **já
   nomeia `people_management`** como o pacote que vai existir.
3. `git status --untracked-files=all packages/people_management` e conferir a contagem contra o
   disco. (A exceção `!packages/*` já existe em `client/rufino_v2/.gitignore` e cobre pacote novo —
   conferido —, mas a lição que entregou dois pacotes pela metade custou caro o bastante para se
   conferir de novo.)
4. **Escrever a violação de propósito**: um arquivo no pacote com `import 'package:rufino_v2/...'`,
   rodar `flutter analyze`, ver falhar, apagar. *Uma fronteira que nada recusa não existe ainda.*

### Fase 3 — domínio (sem UI, sem plugin)

`lib/domain/entities/` (34 do PM) + `lib/domain/repositories/` (12 do PM) + as 10 exceções seladas
do produto em `lib/core/errors/`. Ficam: `auth_repository`, e os shims `permission*`, `auth_*`,
`cep_*`.

- Ordem correta porque **imports apontam para baixo**: entidade não importa repositório, nem UI.
- `git mv` por lote, com sub-agentes de 5–8 arquivos (regra do `CLAUDE.md` para >5 arquivos).
- Na reimportação em massa, **ancore o padrão no separador de caminho**, nunca no nome do arquivo:
  o `gotchas.md` registra 16 arquivos quebrados porque `error_reporter.dart` casou também em
  `sentry_error_reporter.dart` e `fake_error_reporter.dart`.
- Os arquivos antigos viram shims de reexport (decisão E) — nesta fase nenhum import de UI muda.

### Fase 4 — dados

`lib/data/models/` (27), `lib/data/services/` (16 do PM), `lib/data/repositories/` (12 do PM). Os
cross-cutting vão junto porque só o PM os usa: `http_status_helper`, `http_exception`,
`multipart_upload_helper`, `request_id_helper`, `file_save_service`, `spreadsheet_service`.

**Dois pontos de atenção:**

- `checkHttpStatus` (shape `{errors:{…}}`) é do PM e vai com ele; `checkApiStatus`
  (shape `{id,message}`) é do `rufino_core` e fica. Não unifique os dois.
- Preserve `Uri.https(baseUrl, path)` e a semântica `host:porta` de `peopleManagementUrl`.

### Fase 5 — utilitários e portas de plataforma

Aplica a decisão B. Move os 19 utilitários de `lib/core/utils/` que são do PM; para o que é plugin
nativo, cria a porta no pacote e deixa o adapter em `lib/`.

- **`DocumentPicker` sobe de `bill_payment` para `rufino_core`** — a única mudança que este plano
  faz no outro produto.
- `error_messages.dart` é **misto**: `sessionExpiredMessage`/`accessDeniedMessage` são vocabulário
  de sessão (casca); `extractServerMessages` é usado por ~14 ViewModels do PM. Divida — a função
  vai para o pacote, as constantes ficam.
- `concurrency.dart` é candidato a `rufino_core`, mas **só sobe com um segundo consumidor real**
  (critério D3). Hoje não tem: vai para o pacote.

### Fase 6 — features pequenas

Uma por commit, em ordem crescente de tamanho, para as primeiras calibrarem o processo com o menor
risco: `workplace` (769) → `document_dashboard` (1.050) → `document_group` (1.171) →
`require_document` (1.512) → `department` (1.734) → `document_template` (2.200) → `company` (483,
por último porque depende da decisão C).

Cada feature vira `src/ui/<feature>/` com tela e ViewModel lado a lado, mais a entrada em
`people_management_pages.dart`. Os três widgets de `lib/ui/core/widgets/` cujos consumidores são
100% PM (`error_dialog`, `filter_sheet`, `scanner_error_handler`) migram junto da primeira feature
que os usa.

### Fase 7 — features grandes

`batch_download` (2.895) → `batch_document` (5.189) → `employee` (12.206).

- `employee` é fatiado por componente, não movido de uma vez: `widgets/components/` tem seções
  independentes (documentos, contratos, assinatura, dados pessoais) e 3 builders de export.
- **`batch_document` e `batch_download` carregam a dívida do roteador**: hoje leem a empresa
  selecionada num `FutureBuilder` dentro do builder da rota. Ao migrar, isso vira estado da `Page`,
  como manda o padrão. Não copie a forma atual.

### Fase 8 — rotas, permissões e barril

1. `peopleManagementRoutes({required String homeRoute, required DocumentPicker onPickDocument, …})`
   com as ~22 rotas hoje em `app.dart`, na ordem literal-antes-de-`:id`.
2. `abstract final class PeopleManagementRoutes` com as constantes — hoje o `home_screen.dart` usa
   strings cruas (`'/employee'`, `'/workplace'`) enquanto o `bill_payment` já usa constantes.
3. `PeopleManagementPermissionNotifier extends PermissionNotifier` + `PeopleManagementResources` e
   `Scopes` com os 11 recursos da tabela canônica do `CLAUDE.md`.

   > ⚠️ **Maior fonte de quebra silenciosa desta fase.** A audiência `people-management-api` é a que
   > o `PermissionNotifier` **base** atende hoje — `PermissionGuard` sem parâmetro de tipo resolve
   > para ela. Ao criar a subclasse, todo guard sem tipo deixa de resolver. Troque os dois no mesmo
   > commit e confie no analyze. Cache com chave própria:
   > `cached_permissions_people_management`.
4. Fechar o barril no critério do `bill_payment`.

### Fase 9 — a casca encolhe

`app.dart` passa a instanciar services/repos do PM e a chamar `peopleManagementRoutes(...)`;
`home_screen.dart` passa a usar as constantes; `TenantSessionBridge` passa a usar o contrato da
decisão C; os shims de `lib/` morrem e os 19 testes que dependem deles são reapontados.

**Pronto quando:** `lib/` contém apenas casca — `main*.dart`, `app.dart`, `core/config`,
`core/monitoring/sentry_error_reporter.dart`, `core/tenant/`, `ui/features/{auth,home,debug}`, os
13 services de auth e os adapters de plugin da decisão B. Ou seja, os **42 arquivos** que o
inventário classificou como casca, mais os adapters.

### Fase 10 — testes

Os testes seguem o código: `unit/domain/entities` (31), `unit/data/models` (19),
`unit/data/services` (9), `unit/data/repositories` (7) e os de features PM vão para
`packages/people_management/test/`, junto com 13 dos 17 fakes. Ficam na casca só os que cobrem a
**costura** — splash, home, auth —, que é exatamente o que sobrou para `bill_payment` e
`tenant_management` (5 arquivos cada).

> Melhor feita incrementalmente, junto de cada fase. Está separada aqui só para deixar explícito
> que **nenhuma fase é dada por pronta com o teste ainda apontando para o lugar antigo**.

### Fase 11 — `AppModule` (D6)

Fora do escopo deste plano, por decisão D.

---

## 7. Armadilhas — todas já custaram caro uma vez

| Armadilha | Onde está registrada | O que fazer |
|---|---|---|
| Pacote separado não separa nada | `gotchas.md` | Promover o lint a erro **e escrever a violação de propósito** |
| `flutter build` passa com import cruzado | `gotchas.md` / D2 | A regra só vale rodando em CI (Fase 0) |
| `.gitignore` da raiz engole `packages/` | `gotchas.md` | `git status --untracked-files=all` e conferir a contagem |
| Reimport em massa casa por prefixo | `gotchas.md` | Ancorar no separador; `flutter analyze` **antes** dos testes |
| ViewModel criado no builder da rota | `gotchas.md` | A `Page` é dona — e as rotas de batch já violam isso hoje |
| Tela alcançada por `go` não tem `pop` | `gotchas.md` | `PeopleManagementBackButton` no molde dos outros dois |
| Guard recusando antes de carregar permissão | `CLAUDE.md` | Liberar enquanto `status != loaded` |
| Chave de cache de permissão compartilhada | `CLAUDE.md` | `cached_permissions_people_management`, própria |
| `PermissionGuard` sem tipo deixa de resolver | este documento, Fase 8 | Criar a subclasse e trocar os guards no mesmo commit |

---

## 8. Como verificar, sempre na mesma ordem

```bash
cd client/rufino_v2
flutter analyze                                    # localiza a quebra em segundos
flutter test                                       # casca
(cd packages/people_management  && flutter test)
(cd packages/bill_payment       && flutter test)   # prova que o outro produto não se mexeu
(cd packages/tenant_management  && flutter test)
```

O `analyze` vem **antes** dos testes de propósito: depois de um `git mv` em lote ele aponta arquivo
e linha, enquanto a suíte só informa que algo quebrou.

**Ao final, os números da seção 1 devem se conservar**: 1.656 + 346 + 99 testes continuam passando
(menos os das telas mortas removidas na Fase 1), e a soma de arquivos `.dart` entre `lib/` e
`packages/` não cresce — migração não é reescrita.

---

## 9. O que este plano deliberadamente não faz

- **Não reescreve o PM.** Nenhuma tela muda de comportamento, nenhum endpoint muda de contrato. Com
  duas exceções nomeadas, ambas obrigatórias porque o padrão do pacote as exige: as rotas de batch
  param de ler a empresa no builder, e os 5 pontos que chamam `file_picker` passam a receber a
  capacidade por injeção.
- **Não mexe no `bill_payment`**, exceto por `DocumentPicker` subir para `rufino_core` na Fase 5 —
  que é literalmente o critério D3 sendo aplicado.
- **Não implementa a D6** (decisão D) nem **extrai `rufino_auth`** (decisão A).
- **Não sobe nada para `rufino_core` "porque parece fundação".** O critério continua sendo *"se o
  outro produto mudar isto, este deveria se importar?"*. Um segundo consumidor real, ou não sobe.
- **Não conserta o uso de `Icons` em vez de `material_symbols_icons`** em `lib/`, nem a ausência de
  testes em `rufino_core`. São dívidas reais, anotadas aqui para não se perderem, e trabalho de
  outro commit.
