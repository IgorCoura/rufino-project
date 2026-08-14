# Gotchas

Patterns turned into rules after a correction. Review at session start.

## Invariants belong in the constructor, not in the caller

**What happened.** A rule (`ExpirationPolicy`) could not exist with a zeroed
duration. I proposed guarding the caller that composed it. I was asked: "não
teria como deixar dentro da própria policy?" — and that was right.

**Why the caller guard was wrong.** A guard protects *one path*. The bug being
fixed had exactly that shape: a guard on the derivation path left the
explicit-construction path open, which was the path the app was about to use. A
caller guard would have repeated the same mistake somewhere else.

**The rule.** When a value is invalid *by definition*, reject it in the
constructor. Then there is no path to protect — the invalid state cannot be
built, including through deserialisation, which no caller guard would have
covered.

**How to apply.** Before adding `if (x <= 0) skip` in a caller, ask whether the
type should refuse `x <= 0` at all. If yes, the check goes in the type. A caller
may still *skip* (rather than throw) when legacy data legitimately holds the
invalid value — those two are complementary, not alternatives.

## Absence and zero are different values

**The rule.** When a model reads presence as meaning, never collapse `null` into
`0` (or `''`). `(value ?? 0)` in a repository silently turned "no expiration
rule" into "expiration rule of zero days" all the way to the server.

**How to apply.** If the API distinguishes absent from zero, the DTO field must
be nullable end to end. Assert on the **raw request body** in tests — a test
against the returned `Result` cannot see the difference.

## Pacote separado não separa nada por si só

**What happened.** Two products were split into packages so that changes in one
could not break the other. The claim on the table — mine — was that importing
across packages would be a compile error. It is not. In a pub workspace every
package shares one `package_config`, so `import 'package:rufino_v2/...'` from
inside `bill_payment` resolves and compiles; the analyzer only emits an `info`
for `depend_on_referenced_packages`.

**Why it is treacherous.** The structure *looks* isolated — separate folder,
separate `pubspec.yaml`, dependency not declared. Everything about it suggests
a wall, and the build stays green while code crosses it.

**The rule.** A boundary that nothing enforces is documentation. Promote
`depend_on_referenced_packages` to `error` in each package's
`analysis_options.yaml`, and know what that buys: `flutter analyze` breaks,
`flutter build` alone still passes. If it must hold, it runs in CI.

**How to apply.** After declaring any boundary, write the violation on purpose
and watch it fail. If it does not fail, the boundary does not exist yet.

## Reimportação em massa: ancore o padrão no separador

**What happened.** Moving `error_reporter.dart` into a package, a regex rewrote
every import ending in that name — and silently caught
`sentry_error_reporter.dart` and `fake_error_reporter.dart` too. Sixteen files
lost the import they actually needed.

**Why it is treacherous.** The mechanical rewrite reports success; the damage
surfaces as unrelated-looking errors ("The function 'FakeErrorReporter' isn't
defined") far from the file that was moved.

**The rule.** In a filename pattern, anchor on the path separator or the start
of the path — never on the bare name, because Dart filenames compose by prefix.

**How to apply.** Run `flutter analyze` after *every* bulk rewrite, before the
tests. It localizes the breakage in seconds; the test suite only tells you that
something is wrong.

## O `.gitignore` da raiz engoliu o pub workspace inteiro

**What happened.** `packages/rufino_core` e `packages/bill_payment` foram criados
num commit que dizia extrair a fundação — e chegaram ao repositório **pela
metade**: 13 arquivos rastreados, 5 faltando, entre eles os dois `pubspec.yaml`,
os dois `analysis_options.yaml` e os dois barris. Um clone limpo não resolvia
dependência nenhuma.

**Why it is treacherous.** O `.gitignore` da raiz é o template do Visual Studio e
traz `**/packages/*`, que existe para a pasta `packages/` do NuGet. Ele exclui os
**diretórios** dos pacotes, então o Git nem desce neles. O que salvou os 13
arquivos foi acidente: eles vieram de `git mv`, que estagia o destino
**ignorando o .gitignore**. Arquivo criado do zero era descartado em silêncio —
`git status` limpo, `git commit` bem-sucedido, e o pacote incompleto no remoto.

**The rule.** Depois de criar pasta nova que o template do repositório possa
cobrir (`packages/`, `debug/`, `build/`), rode
`git status --untracked-files=all <pasta>` e confira a contagem contra o disco.
`git status` sozinho mente por omissão: ele não lista o que está ignorado.

**How to apply.** Antes de commitar estrutura nova:
`git check-ignore -v --no-index <um arquivo dentro dela>`. Se responder alguma
regra, escreva a exceção no `.gitignore` mais próximo — regra de arquivo mais
fundo vence a da raiz, e é assim que `lib/ui/features/debug/` já era tratado.

## ViewModel criado no builder da rota: a tela volta e gira para sempre

**What happened.** Voltar do cadastro para o seletor deixava o seletor com o
spinner eterno. O mesmo do detalhe para a listagem. Nada no log, nada de erro —
só a tela girando.

**Why it is treacherous.** O `go_router` reexecuta o builder da rota a cada
mudança na pilha. O builder criava o ViewModel, então cada `pop` produzia uma
instância **nova**, em `loading`. Mas o `State` da tela sobrevive ao rebuild
(mesmo tipo, mesma posição), então o `initState` — o único lugar que chamava
`load()` — **não roda de novo**. A tela fica ligada a um ViewModel que ninguém
mandou carregar. E o teste de widget comum não pega: ele monta a tela uma vez.

**The rule.** Página `StatefulWidget` dona do ViewModel (`late final` no
`initState`, `dispose` no fim); o builder da rota só constrói a página. Já
estava escrito no `CLAUDE.md` para o `DocumentDashboardPage` — e mesmo assim se
repetiu, porque o sintoma não parece um problema de ciclo de vida.

**How to apply.** Teste de navegação com `GoRouter` de verdade: entre na tela,
navegue para a seguinte, volte, e afirme que os **dados** ainda estão lá e que
não há `CircularProgressIndicator`. É o único formato que reproduz o bug.

## Tela alcançada por `go` não tem para onde dar `pop`

**What happened.** As telas novas ficaram sem botão de voltar, e o back do
Android não tinha para onde ir: o menu do Home chega nelas com `context.go`,
que **substitui** a pilha.

**The rule.** Botão de voltar sempre `context.canPop() ? context.pop() :
context.go(fallback)`. E ele precisa existir em **todos** os estados da tela —
uma tela que só desenha a `AppBar` depois de carregar tranca o usuário enquanto
a rede não responde.
