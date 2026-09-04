# ADR-011 — LLM propõe, código determinístico dispõe

**Status:** Aceito · **Data:** 2026-07-31

## Contexto

A medição do corpus real ([`08-boleto-corpus-findings.md`](../08-boleto-corpus-findings.md)) mostrou os limites do parser determinístico: 18% dos PDFs não têm camada de texto, 26% têm texto mas nenhuma linha digitável extraível por regra, o CNPJ do pagador só aparece em 38%, e um caso passou até na validação de DV. Somando os canais novos — portais, PDFs com senha, links em e-mail — o número de formas de um boleto chegar cresceu muito além do que dá para cobrir com heurísticas por layout.

Um modelo de visão lê um boleto escaneado, torto e com ruído sem precisar de uma regra por concessionária. A pergunta não é *se* usar LLM, é **onde ele pode decidir e onde não pode**.

## Decisão

**O LLM extrai e classifica; ele nunca decide nada que toque dinheiro.**

Toda saída de LLM entra no sistema como **candidato**, sujeita ao mesmo funil que qualquer outro candidato:

```
LLM → structured output (schema forçado)
    → validação de DV (linha digitável, CPF/CNPJ, CRC do payload Pix)
    → filtros de plausibilidade (banco existe, valor sensato, data plausível)
    → CONSULTA OFICIAL no provedor  ← a fonte de verdade continua sendo esta
    → checks do catálogo
    → aprovação humana
```

**Usos autorizados:**

| Uso | Por quê |
|---|---|
| Extrair linha digitável / payload Pix de PDF ou imagem | Resolve os 18% sem camada de texto e os 26% com layout hostil. A saída passa por DV + consulta oficial |
| Extrair pagador (nome + CPF/CNPJ) | Único caminho para o dado que sustenta o isolamento entre tenants; a saída passa por DV de CPF/CNPJ |
| Extrair referência de conta (instalação, matrícula, conta contrato) | Alimenta o degrau 2 do roteamento; é string opaca, sem risco de decisão |
| Classificar anexo/e-mail ("isto é um boleto?", "qual anexo é o boleto?", "há link de fatura?") | Triagem barata; erro custa um item mal classificado na quarentena, não dinheiro |
| Redigir a evidência de um check em linguagem de negócio | Texto para humano ler, nenhuma decisão |

**Usos proibidos, sem exceção:**

- Calcular ou "corrigir" linha digitável, código de barras ou payload Pix.
- Validar dígito verificador. Isso é aritmética; um modelo probabilístico é a ferramenta errada.
- Decidir o resultado de qualquer `BillCheck`.
- Escolher entre candidatos ambíguos — quem desempata é a consulta oficial.
- Aprovar, agendar ou disparar pagamento.
- Decidir de qual tenant é um boleto. O `BillRoutingService` consome **valores** que o LLM extraiu; a escada de roteamento em si é código.

## Razões

- **Verificabilidade é a razão de existir do produto.** Um sistema que impede fraude de boleto não pode ter, no caminho crítico, um componente cuja saída não dá para provar. Restringir o LLM à extração mantém 100% das decisões auditáveis.
- **A saída é barata de verificar.** Linha digitável tem dígito verificador; CPF/CNPJ tem dígito verificador; o beneficiário é confirmado pela consulta oficial. O LLM não precisa estar certo — precisa estar *checável*, e está.
- **Alucinação vira `Unrecognized`, não pagamento errado.** Um número inventado falha no DV ou na consulta e o item cai na fila de exceção. É exatamente o mesmo desfecho de um PDF ilegível.
- **Structured outputs eliminam a classe de erro de parsing.** Com o schema imposto pelo provedor (`responseSchema` no Gemini — ver [`ADR-013`](ADR-013-gemini-atras-de-porta-agnostica.md)), a resposta é garantidamente parseável — sobra só o problema de *conteúdo*, que o funil de validação já trata.
- **Economia esmagadora contra o alternativo.** Um conector de OCR por layout de concessionária é semanas de trabalho e quebra quando o fornecedor muda o template. Ver os números em [`10-llm-extraction.md`](../10-llm-extraction.md) — o custo por boleto é de centavos, e cai pela metade na Batch API.

## Consequências

- **Porta `IDocumentIntelligence` no Domain**, ao lado das outras. Trafega tipos do Domain (`ExtractedDocument` com candidatos), nunca DTO de provedor. Trocar de modelo — ou desligar o LLM e cair só no parser determinístico — é trocar adapter.
- **O parser determinístico não é descartado; ele vem primeiro.** Texto embutido + DV resolve a maioria por alguns milissegundos e custo zero. O LLM entra quando o determinístico não resolve — cascata, não substituição. Isso mantém o custo baixo e a latência boa no caso comum.
- **Dado financeiro sai do perímetro.** Boletos carregam CNPJ, valores e nomes de fornecedores. É a única dependência externa paga da stack, contra a premissa de "só open source". A decisão é do usuário e está tomada; o que o sistema deve garantir é: só o necessário sai (a página do boleto, não a caixa de e-mail inteira), e a chave da API é segredo de infraestrutura ([`ADR-009`](ADR-009-cofre-de-segredos.md)).
- **Determinismo não existe.** O mesmo PDF pode extrair diferente entre execuções. Por isso o **artefato persistido é o resultado validado**, não a resposta bruta do modelo — e a reextração é sempre possível a partir do PDF guardado no storage.
- **Métrica obrigatória**: taxa de extração e taxa de rejeição pós-validação, medidas sobre o corpus real ([`tools/analyze-boleto-corpus.js`](../tools/analyze-boleto-corpus.js)). Sem elas não há como saber se uma troca de modelo ou de prompt melhorou ou piorou.
- **Teste de integração com adapter falso determinístico.** A suíte nunca chama a API real; o `IDocumentIntelligence` de teste devolve respostas fixas, inclusive respostas *erradas de propósito* — o teste mais importante é o que prova que uma extração alucinada é barrada pelo DV e pela consulta.

## Alternativa descartada

**Agente de navegação (computer use) operando portais e decidindo o que baixar.** Tecnicamente viável e tentador para a fase 5. Descartado como caminho principal por três motivos que se somam: é lento e caro por execução, é não determinístico num lugar onde o resultado é uma credencial usada em sessão autenticada, e falha de forma difícil de diagnosticar. Fica como último recurso para portais sem conector estável, sempre com o artefato baixado passando pelo mesmo funil de validação — ver [`ADR-012`](ADR-012-portais-reduzir-residuo.md).
