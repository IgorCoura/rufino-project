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

## Máscara de tamanho fixo não pode decidir se cresce

**What happened.** No formulário de novo beneficiário, o campo "CPF ou CNPJ"
nascia com a máscara de CPF (`###.###.###-##`, 11 posições) e um `onChanged`
que trocaria para a de CNPJ ao ver o 12º dígito. O `onChanged` nunca rodava:
o `MaskTextInputFormatter` **descarta a tecla que passa do tamanho da máscara**
antes de qualquer callback do campo. Digitando um CNPJ de 14 dígitos, o campo
parava em `112.223.330-00` — e cadastrar empresa era impossível pela tela.
Provado com o formatador cru: 14 teclas entram, 11 dígitos saem.

**The rule.** Quando a máscara depende do que está sendo digitado, quem decide
tem que ver a tecla **antes** de mascarar — ou seja, dentro de um
`TextInputFormatter`, nunca no `onChanged` do campo. `TaxIdInputFormatter`
(`bill_payment/lib/src/ui/shared/`) é o formato: escolhe a máscara pelo número
de dígitos do valor entrando e delega o resto ao `MaskTextInputFormatter`, que
continua sendo o motor de máscara do app.

**Where it does NOT apply.** Máscara que troca por *outro* controle — o
`PayerProfileScreen`, onde o segmento PF/PJ dispara `updateMask` — está
correta: ali a decisão não depende da tecla, e o `updateMask` roda fora do
caminho de digitação.

**How to apply.** Teste que digita **tecla a tecla**, não `enterText` de uma
vez: o bug só aparece na 12ª, e um `enterText` com o documento inteiro passa
pelos dois caminhos e não distingue um do outro.

## A validação de uma tela também mede o que ela deixa de mandar

**O que aconteceu.** A importação manual de boleto exigia "a linha digitável ou o código Pix". O
campo de anexo entrou depois, e o validador continuou olhando só os dois campos de texto — então
anexar o arquivo e clicar em Importar reprovava o formulário sobre um campo que a pessoa
deliberadamente deixou vazio.

**Por que é traiçoeiro.** O validador do `TextFormField` roda no campo, e é natural escrevê-lo
lendo só os controllers que estão ali. Mas a pergunta que ele responde — "há o suficiente para
enviar?" — é sobre o **formulário inteiro**, e o anexo é estado do ViewModel, não do campo.

**A regra.** Validador de "informe ao menos um" lê **todas** as fontes que satisfazem a exigência,
inclusive as que não são `TextFormField`. E depois de mexer numa fonte que não é campo (anexar,
remover), chame `_formKey.currentState?.validate()`: sem isso a mensagem de erro anterior fica na
tela contradizendo o que a pessoa acabou de fazer.

## Mover arquivo é reescrever import, e casar por sufixo não basta

**What happened.** Migrando o domínio do PeopleManagement para um pacote, a
reescrita de imports casava o caminho por sufixo (`domain/entities/x.dart`).
Quatro imports escaparam: `'multipart_upload_helper.dart'` (mesmo diretório),
`'../errors/...'` e `'../models/...'` — todos apontando para o mesmo arquivo por
um caminho relativo mais curto do que o padrão previa.

**Why it is treacherous.** O padrão por sufixo funciona para a maioria e falha
em silêncio para o resto; o que sobra parece um erro de outra natureza
("Target of URI doesn't exist" num arquivo que ninguém tocou).

**The rule.** Não case texto: **resolva o caminho**. Normalize o import contra o
diretório do arquivo e compare com a lista de arquivos que de fato se moveram —
que o próprio git fornece (`git diff --cached --name-status -M`).

**How to apply.** Depois de qualquer `git mv` em lote, rode `flutter analyze`
antes dos testes: ele aponta arquivo e linha em segundos, a suíte só diz que
algo quebrou.

## O plugin exporta uma função com o nome do seu método

**What happened.** A porta do scanner ganhou `openAppSettings()`, e a
implementação virou `Future<void> openAppSettings() => openAppSettings();`. O
`permission_handler` exporta uma função top-level com exatamente esse nome — o
método passou a chamar a si mesmo. Recursão infinita, e o analyzer não reclama:
a chamada é válida.

**Why it is treacherous.** O código lê como delegação. Só a execução mostra o
stack overflow, e a implementação de plataforma não costuma ter teste.

**The rule.** Ao adotar um plugin cujo símbolo tem o mesmo nome do membro que o
envolve, importe com prefixo (`as ph`) e use o prefixo em **todos** os usos —
não só no que colidiu.

## Barril não é para consumo interno

**What happened.** Ao mover arquivos para dentro de um pacote, os imports dos
consumidores tinham sido reapontados para o barril. Os arquivos migraram
levando esse import junto — e 41 arquivos do pacote passaram a importar o
próprio barril. Funciona, compila, e faz o analyzer marcar todo import direto
como `unnecessary_import`.

**Why it is treacherous.** O sinal aparece como sugestão de estilo, quando o que
está acontecendo é que o pacote perdeu a noção de quem depende de quem: todo
arquivo passa a "depender de tudo".

**The rule.** Dentro do pacote, import relativo. O barril é a fachada para quem
está **fora**.

**How to apply.** Remover os self-imports quebra centenas de símbolos de uma vez
— não tente resolver a mão. Construa um índice `símbolo → arquivo` varrendo as
declarações do pacote, cruze com os `undefined_*` do analyze e insira os imports
relativos; repita até estabilizar. Note que os erros que **não nomeiam o
símbolo** (`implements_non_class`) escapam desse laço e pedem uma regra própria
— `X_repository_impl.dart` importa `X_repository.dart`.
