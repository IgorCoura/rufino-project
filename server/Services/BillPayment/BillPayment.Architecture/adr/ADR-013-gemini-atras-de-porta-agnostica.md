# ADR-013 — Gemini como provedor de IA, atrás de porta agnóstica

**Status:** Aceito · **Data:** 2026-07-31

## Contexto

[`ADR-011`](ADR-011-llm-propoe-codigo-dispoe.md) fixou **o papel** do modelo — extrai candidatos, nunca decide. Falta fixar **qual provedor** e, mais importante, **quanto o resto do sistema pode saber sobre ele**.

O requisito é explícito nos dois pontos: usar Gemini, e manter o acoplamento baixo o suficiente para trocar de IA depois sem reescrever o BC.

## Decisão

**Provedor: Google Gemini.** Modelo padrão `gemini-3.6-flash`, configurável.

**Acoplamento: nenhum fora do adapter.** A regra é literal — se um `grep` por `gemini` retornar qualquer coisa fora de `Infra/DocumentIntelligence/Gemini/` e de `appsettings`, é violação.

### O que o Domain conhece

```csharp
// Domain/Ports/IDocumentIntelligence.cs
public interface IDocumentIntelligence
{
    Task<ExtractedDocument> ExtractAsync(DocumentPayload payload, ExtractionHints hints, CancellationToken ct);
    Task<DocumentTriage>    TriageAsync(MessageSummary message, CancellationToken ct);
}
```

Tipos que atravessam a fronteira: `DocumentPayload` (bytes + mime), `ExtractionHints`, `ExtractedDocument`, `DocumentTriage`. **Todos definidos no Domain.** Nenhum nome de modelo, nenhum conceito de "token", "prompt", "schema", "temperature" ou "candidate" cruza.

### O que o adapter encapsula

Tudo o mais: endpoint, autenticação, montagem do request, `responseSchema`, `responseMimeType`, retries, rate limit, Batch API, contagem de tokens, telemetria de custo, e o **prompt**. O prompt é detalhe de implementação do adapter, não configuração do BC — provedores diferentes pedem prompts diferentes.

### Como a troca acontece

Um `IDocumentIntelligence` novo em `Infra/DocumentIntelligence/<Provedor>/`, registrado por configuração:

```json
"DocumentIntelligence": { "Provider": "Gemini", "Model": "gemini-3.6-flash" }
```

Nenhuma linha de Domain, Application ou API muda. O `NullDocumentIntelligence` (devolve zero candidatos) é o adapter que desliga a IA inteira — a cascata degrada para o parser determinístico e a quarentena manual, sem quebrar.

### Implementação: HTTP direto, sem SDK

O adapter fala REST puro com `HttpClient` + `System.Text.Json` contra `https://generativelanguage.googleapis.com/v1beta/`. **Não adotar SDK de terceiro.**

Motivos: não existe SDK .NET oficial do Google para a Gemini API (as opções são bibliotecas de comunidade); a superfície que usamos é minúscula (um endpoint, um schema, upload de arquivo); e um SDK de provedor no `.csproj` da Infra é exatamente o acoplamento que este ADR existe para evitar — trocar de IA passaria a incluir trocar dependência de pacote, não só de classe.

## Razões

- **Custo.** Gemini fatura página de PDF a ~258 tokens; Flash-Lite custa $0,30/$2,50 por milhão. Isso põe a extração de um boleto na casa de **US$ 0,001** — uma ordem de grandeza abaixo da alternativa avaliada antes. Números completos em [`10-llm-extraction.md`](../10-llm-extraction.md).
- **`responseSchema` nativo.** Structured output com JSON Schema imposto pelo provedor, que é o mecanismo do qual o [`ADR-011`](ADR-011-llm-propoe-codigo-dispoe.md) depende para eliminar erro de parsing.
- **PDF nativo até 1000 páginas e 50 MB**, com texto extraído nativamente **não cobrado** nos modelos Gemini 3 — só o processamento de imagem da página conta. Casa exatamente com a cascata: quando há camada de texto o custo cai sozinho.
- **Batch API com 50% de desconto**, alvo de 24h — serve a ingestão agendada, que é o grosso do volume.
- **A porta agnóstica é barata agora e cara depois.** O contrato tem quatro tipos e dois métodos; escrevê-lo agora custa uma tarde. Descobrir depois que `GenerateContentRequest` vazou para a Application custa uma refatoração.

## Consequências

- **Duas chamadas por documento no pior caso** (triagem + extração) — a triagem usa o modelo mais barato e pode ser desligada por configuração enquanto o volume não justificar.
- **Não determinismo permanece** (ADR-011): o artefato persistido é o resultado *validado*, não a resposta do modelo. Reextração parte sempre do PDF no storage.
- **Dado financeiro sai do perímetro** para o Google. É a única dependência externa paga da stack, contra a premissa de só software open source. Decisão consciente do usuário; o sistema garante que **só o documento** sai — a página do boleto, nunca a caixa de e-mail ou o cadastro.
- **A chave da API é segredo de infraestrutura**: variável de ambiente em produção, `dotnet user-secrets` em dev ([`ADR-009`](ADR-009-cofre-de-segredos.md)).
- **Teste de contrato do adapter**, separado dos testes de domínio: um conjunto de respostas gravadas do Gemini (fixtures JSON) prova que o mapeamento provedor → `ExtractedDocument` está correto. A suíte normal usa `FakeDocumentIntelligence` e nunca chama a rede.
- **Modelo é configuração, não constante.** `gemini-3.6-flash` é o padrão; trocar para `gemini-3.5-flash-lite` (mais barato) ou para um Pro (mais capaz) é mudança de `appsettings`, e a métrica de extração sobre o corpus real diz se compensou.

## Alternativa descartada

**Camada de abstração multi-provedor de terceiro** (LangChain, Semantic Kernel e afins). Traria os dois provedores por trás de uma interface pronta, ao custo de uma dependência grande, com seu próprio modelo conceitual, sua própria cadência de breaking changes, e superfície muitíssimo maior que os dois métodos que este BC precisa. A porta própria é menor que a configuração da alternativa.
