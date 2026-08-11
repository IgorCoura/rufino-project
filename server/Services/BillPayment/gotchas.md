# gotchas.md — BillPayment

Registro de correções e lições aprendidas neste BC (ver regra em CLAUDE.md → Self-Correction).

## Object Mother com `??` engole a invariante que o teste queria provar

**Quando:** 2026-07-31, sprint 1.1 (`TrustedOrigin`).

**O que aconteceu:** o Mother expunha `Register(OriginKind? kind = null, ...)` e resolvia com `kind ?? OriginKind.EmailAddress`. Os testes que provavam "tipo nulo é recusado" chamavam `Register(kind: null!)` — e o `??` substituía o nulo pelo default, então o agregado era criado com sucesso e o `Assert.Throws` falhava.

**Por que é traiçoeiro:** se o agregado *não* tivesse a validação, esses dois testes falhariam exatamente do mesmo jeito. Eles nunca teriam pegado a ausência da regra — só apareceram porque falharam por sorte.

**Regra:** todo Mother tem dois caminhos.

- `Register(...)` com parâmetros opcionais e defaults — caminho feliz, onde o setup não é o assunto do teste.
- `RegisterVerbatim(...)` com parâmetros **obrigatórios, repassados sem coalescer** — o único caminho usado por teste de invariante que rejeita argumento nulo ou inválido.

`Register` delega para `RegisterVerbatim`, então a construção real fica num lugar só. Aplicado em `TrustedOriginMother`, `PayeeMother` e `PayerProfileMother`.

**Como pegar de novo:** ao escrever um teste `*_ShouldThrow_*` que passa argumento nulo ou inválido, confira se ele atravessa o Mother sem passar por `??`. Se passar, o teste está mentindo.

## Índice do EF não alcança propriedade de owned type

**Quando:** 2026-07-31, sprint 1.1 (`Payee`).

**O que aconteceu:** `TaxId` foi mapeado como owned type (`OwnsOne`) achatado em duas colunas, e o índice único de unicidade por tenant foi declarado como `builder.HasIndex(e => new { e.TenantId, e.TaxId!.Value })`. **Compila sem reclamar.** Em runtime, na construção do modelo, o EF lança `ArgumentException: The expression ... is not a valid member access expression`.

**Por que é traiçoeiro:** a falha derruba o `OnModelCreating` inteiro, então **todos** os testes de integração quebram de uma vez — inclusive o `HealthCheckTests`, que não tem nada a ver com `Payee`. A mensagem aponta o mapping certo, mas o sintoma sugere que a infra de teste quebrou.

**Regra:** o construtor de índice só enxerga propriedades do **próprio** tipo. Se um valor precisa entrar num índice composto com a raiz, ele não pode ser owned. Quando o resto do VO é dedutível do valor guardado — `TaxId` é: 11 dígitos é CPF, 14 é CNPJ — mapeie como **uma coluna escalar com `HasConversion` + `ValueComparer`** e reidrate pela factory do domínio (`TaxId.Parse`). Ganha-se o índice, a comparação direta em LINQ (`p.TaxId == taxId`) e a revalidação do DV na leitura.

**Como pegar de novo:** ao escrever `HasIndex` com mais de uma propriedade, confira se alguma vem de `OwnsOne`. Se vier, o índice não existe — decida entre coluna escalar convertida ou abrir mão do índice.

## Valor com barra em segmento de rota morre em 404 antes do controller

**Quando:** 2026-07-31, sprint 1.1 (`Payee`, `PayerProfile`).

**O que aconteceu:** `GET api/v1/{tenantId}/payees/by-tax-id/{taxId}` funcionava com `11222333000181` e devolvia 404 com `11.222.333/0001-81`. A barra do CNPJ formatado vira separador de segmento e nenhuma rota casa — o request nem chega ao controller, então não há erro de domínio para diagnosticar.

**Regra:** valor livre (documento formatado, apelido, razão social) vai em **query string**; só fica no path o que é sabidamente seguro — `{id:guid}`, código de banco de três dígitos. Vale também para `DELETE`, onde a tentação de pôr a identidade do item no path é maior.

**Como pegar de novo:** todo endpoint com `{algumaCoisa}` que não seja `:guid` ou `:int` precisa de um teste com o valor na forma que o usuário realmente digita — formatada, com espaço, com acento.
