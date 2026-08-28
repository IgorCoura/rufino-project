# 10 — Extração por IA

Como o modelo de visão entra na captura, o que ele pode produzir, e o que custa.

Duas regras governam este documento:

- **O modelo propõe candidatos; o código determinístico decide** — [`adr/ADR-011`](adr/ADR-011-llm-propoe-codigo-dispoe.md).
- **Nada fora do adapter sabe qual é o provedor** — [`adr/ADR-013`](adr/ADR-013-gemini-atras-de-porta-agnostica.md).

## Onde ele entra

Dois papéis desde 2026-08-27 (decisão do usuário: **todo candidato a boleto passa pela IA**):

| Uso | Quando roda |
|---|---|
| **Degrau 3 da cascata** ([`09-capture-channels.md`](09-capture-channels.md)) | quando o parser determinístico não resolveu e há sinal de cobrança |
| **Retrato de enriquecimento** (`Bill.Reading` — competência, descrição, pagador, referência de conta) | **sempre** que o artefato é candidato a boleto — inclusive quando o determinístico já resolveu. Uma chamada só cobre os dois papéis |
| **Triagem de mensagem** — prevista no desenho original | **não implementada de propósito**: o filtro determinístico gratuito decide se vale gastar |

O **corpo do e-mail** viaja junto do documento como parte de texto (`DocumentPayload.SupplementalText`, HTML convertido no adapter) — é dele que saem a competência e a descrição quando o boleto não as traz. O que volta vira `DocumentReading`: campos tipados e aparados, documentos fiscais só com DV provado, competência normalizada (`CompetencePeriod`). O retrato alimenta a tela (Resumo), o roteamento (o CNPJ do pagador lido sobe o degrau 1 para documento escaneado), o check 13 (`DocumentConsistency`, doc 03) e — única exceção deliberada ao "só conferência" — o **vencimento consolidado**, como última reserva atrás da consulta oficial e da linha digitável. `POST /bills/{id}/enrich` relê um boleto do acervo (backfill), um por chamada.

## O contrato — o que o BC conhece

```csharp
// Domain/Ports/IDocumentIntelligence.cs
public interface IDocumentIntelligence
{
    Task<ExtractedDocument> ExtractAsync(DocumentPayload payload, ExtractionHints hints, CancellationToken ct);
    Task<DocumentTriage>    TriageAsync(MessageSummary message, CancellationToken ct);
}
```

`ExtractedDocument` é **um saco de candidatos**, não uma resposta:

| Campo | Nota |
|---|---|
| `DigitableLineCandidates` | `IReadOnlyList<string>` — todos os que o modelo viu |
| `PixPayloadCandidates` | `IReadOnlyList<string>` — BR Code do QR ou do "copia e cola" |
| `PayerName` / `PayerTaxId` | validados por DV **depois**, nunca aqui |
| `PayeeName` / `PayeeTaxId` | informativo; quem manda é a consulta oficial |
| `AccountReference` / `AccountReferenceKind` | alimenta o roteamento e a expectativa ([`11-bill-expectations.md`](11-bill-expectations.md)) |
| `Amount` / `DueDate` | **só para conferência cruzada**, jamais para pagar |
| `DocumentKind` | `BankSlip` \| `Utility` \| `TaxGuide` \| `NotABill` |
| `Notes` | texto curto para a evidência do check |

`ExtractionHints` leva o que o sistema já sabe: TaxIds dos tenants que monitoram a fonte, `Payee` conhecidos, remetente. Reduz alucinação e melhora leitura de campo cortado.

**Nenhum termo de IA cruza esta fronteira.** Sem `model`, sem `prompt`, sem `token`, sem `schema`, sem `temperature`. Se um deles aparecer em `Domain/`, `Application/` ou `API/`, o acoplamento vazou.

## O adapter — o que fica escondido

Tudo o resto: endpoint, autenticação, montagem do request, `responseSchema`, retries, rate limit, Batch, contagem de tokens, telemetria de custo e **o prompt** (que é detalhe de implementação — provedores diferentes pedem prompts diferentes).

```json
"DocumentIntelligence": {
  "Provider": "Gemini",
  "Model": "gemini-3.6-flash",
  "TriageModel": "gemini-3.5-flash-lite",
  "TriageEnabled": true,
  "DailyBudgetUsdPerTenant": 1.00
}
```

Trocar de IA = novo adapter em `Infra/DocumentIntelligence/<Provedor>/` + uma linha de configuração. `Provider: "None"` resolve para `NullDocumentIntelligence`, que devolve zero candidatos: a cascata degrada para o parser determinístico e a quarentena manual, sem quebrar nada.

### Implementação: HTTP direto

`HttpClient` + `System.Text.Json` contra `https://generativelanguage.googleapis.com/v1beta/`. **Sem SDK de terceiro** — a superfície usada é minúscula e um pacote de provedor no `.csproj` é o acoplamento que o ADR-013 existe para evitar.

Elementos do request que importam:

- **`responseMimeType: "application/json"` + `responseSchema`** — structured output imposto pelo provedor. É o que elimina erro de parsing; sem isso, o ADR-011 perde uma perna.
- **PDF inline** em base64 com `mime_type: "application/pdf"` para boleto de uma página; **Files API** para arquivo grande ou reutilizado.
- Limites: **50 MB**, até **1000 páginas**, páginas redimensionadas para no máximo 3072×3072.

## Custo

Preços por milhão de tokens (verificados em 2026-07-31; confirme antes de fechar orçamento):

| Modelo | Input | Output | Batch (−50%) |
|---|---|---|---|
| `gemini-3.6-flash` | $1,50 | $7,50 | $0,75 / $3,75 |
| `gemini-3.5-flash-lite` | $0,30 | $2,50 | $0,15 / $1,25 |
| `gemini-3.1-flash-lite` | $0,25 | $1,50 | $0,125 / $0,75 |
| `gemini-2.5-flash-lite` | $0,10 | $0,40 | $0,05 / $0,20 |

> **Estar em `GET /models` não significa que aceita `generateContent`.** Medido em 2026-08-11
> contra a conta real: a linha `gemini-2.5-*` **aparece na listagem** e devolve **404** na
> geração. Responderam: `gemini-3.1-flash-lite`, `gemini-3.5-flash-lite` e o alias
> `gemini-flash-lite-latest`. Ao trocar de modelo, **prove com uma chamada real** — a listagem
> mente. E prefira nome fixo a alias `-latest`: alias flutua, e modelo trocando por baixo faria a
> qualidade da extração mudar sem nenhuma alteração no repositório.

### O número que muda a decisão

**Uma página de PDF custa ~258 tokens de entrada.** Somando o prompt e o schema, a extração de um boleto gira em torno de **~800 tokens de entrada** e **~300 de saída**.

| Modelo | Por boleto | Com Batch |
|---|---|---|
| `gemini-3.6-flash` | ~US$ 0,0035 | ~US$ 0,0017 |
| `gemini-3.5-flash-lite` | ~US$ 0,0010 | ~US$ 0,0005 |

E nos modelos Gemini 3 o **texto extraído nativamente do PDF não é cobrado** — só o processamento da imagem da página. Isso casa com a cascata: quanto melhor a camada de texto, menor o custo.

### Mensal

Só ~45% dos documentos chegam ao passo 3 — o resto resolve no texto embutido, de graça.

| Volume mensal | Passam pela IA | `gemini-3.6-flash` | Com Batch |
|---|---|---|---|
| 40 documentos (volume real medido) | ~18 | **~US$ 0,06** | ~US$ 0,03 |
| 200 documentos | ~90 | ~US$ 0,32 | ~US$ 0,16 |
| 1.000 documentos | ~450 | ~US$ 1,58 | ~US$ 0,79 |

> **Correção da estimativa anterior.** Uma versão anterior deste documento projetava ~US$ 0,80/mês no volume real, com outro provedor. O número correto com Gemini é **cerca de US$ 0,06/mês** — uma ordem de grandeza abaixo. A diferença vem de como o provedor tarifa página de documento (~258 tokens contra alguns milhares) somada ao preço da linha Flash.

No volume real deste cliente, a extração por IA custa **centavos por mês**. Escrever e manter um extrator OCR por layout de concessionária custa semanas e quebra a cada mudança de template. A comparação não é próxima.

São estimativas — re-baseline com a contagem de tokens do provedor sobre uma amostra do corpus real antes de tratar como orçamento.

## Batch API

Metade do preço, alvo de 24 h (na prática costuma ser bem menos). Entrada por arquivo JSONL, uma `GenerateContentRequest` por linha.

- **Use para**: ingestão agendada de caixa de e-mail, reprocessamento do corpus quando o prompt mudar, backfill.
- **Não use para**: upload manual e reivindicação na tela — o usuário está esperando.
- **Resultados voltam fora de ordem** — indexar por chave própria (`CaptureItemId`), nunca por posição.

## Guardrails

1. **Toda saída é candidato.** DV → plausibilidade → consulta oficial. Sempre, sem atalho.
2. **Persistir o resultado validado**, não a resposta bruta. Reextração parte do PDF no storage.
3. **Só o documento sai do perímetro** — a página do boleto, nunca a caixa de e-mail ou o cadastro. Os `ExtractionHints` levam TaxIds e nomes de beneficiários porque melhoram a extração; é escolha consciente e deve constar do aviso de privacidade.
4. **A chave da API é segredo de infraestrutura** — variável de ambiente em produção, `dotnet user-secrets` em dev ([`ADR-009`](adr/ADR-009-cofre-de-segredos.md)).
5. **Falha do provedor não trava a ingestão.** Timeout, 429 ou erro → item vira `Unrecognized` com motivo `extraction_unavailable` e entra na fila de reprocessamento. Nunca "aprova sem extrair".
6. **Teto de gasto por tenant por dia**, configurável. PDF malformado em laço de retentativa não pode virar conta surpresa.
7. **A suíte de testes nunca chama a rede.** `FakeDocumentIntelligence` devolve respostas fixas — incluindo **respostas erradas de propósito**. O teste mais valioso do conjunto é o que prova que uma linha digitável alucinada é barrada pelo DV e pela consulta oficial.
8. **Teste de contrato do adapter**, separado: fixtures JSON de respostas reais do provedor provam que o mapeamento provedor → `ExtractedDocument` está correto. É o único teste que precisa mudar quando a IA trocar.

## Métricas

Sobre o corpus real ([`tools/analyze-boleto-corpus.js`](tools/analyze-boleto-corpus.js)), a cada mudança de prompt, de modelo ou de provedor:

| Métrica | Meta |
|---|---|
| Taxa de extração da cascata completa | ≥ 90% |
| Fatia resolvida no passo 2 (texto embutido, custo zero) | maximizar |
| **Taxa de rejeição pós-validação** (candidato que falhou no DV ou na consulta) | monitorar — subida súbita indica regressão |
| Custo por documento extraído | acompanhar contra a tabela acima |

A terceira é a que importa para a segurança: mede quantas vezes o funil determinístico salvou o sistema de uma extração errada. **Não deve ser zero** — zero significa que o funil não está sendo exercitado, e provavelmente que a métrica está errada.
