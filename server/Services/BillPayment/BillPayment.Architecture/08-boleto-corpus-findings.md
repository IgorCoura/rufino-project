# 08 — Achados do corpus real de boletos

Análise empírica de **39 boletos reais** que vão passar por este sistema, feita em 2026-07-31.

**Corpus:** `D:\OneDrive\OneDrive - RUFINO EMPREITEIRA\DOC EMPRESA\2 - CONTROLE DE CUSTOS\2026-06\PAGO` (competência 2026-06, todos já pagos).

**Método:** extração de texto com `pdftotext -layout`, geração de todas as janelas de 47/48 dígitos e validação dos dígitos verificadores (mod 10 para campos de cobrança e blocos de arrecadação com identificador 6/7; mod 11 para DV geral de cobrança e blocos com identificador 8/9), mais validação de DV de CPF/CNPJ.

**Reprodução:** [`tools/analyze-boleto-corpus.js`](tools/analyze-boleto-corpus.js).

```bash
for f in "$CORPUS"/*.pdf; do pdftotext -layout -enc UTF-8 "$f" "txt/$(basename "$f" .pdf).txt"; done
node tools/analyze-boleto-corpus.js txt <cnpj-tenant-1> <cnpj-tenant-2>
```

> **Ressalva metodológica:** `pdftotext` é um piso, não um teto. Um parser de verdade (PdfPig com posicionamento, ou leitura por região) vai acertar mais. Os números são **limite inferior**; as conclusões qualitativas é que são robustas.

## Números

| Resultado | Qtd | % |
|---|---|---|
| Linha digitável extraída e com DV válido | 22 | 56% |
| — das quais **falso positivo** (lixo que passou no DV) | 1 | 3% |
| Texto extraível, mas sem linha digitável | 10 | 26% |
| **PDF sem nenhuma camada de texto** (imagem pura) | 7 | **18%** |
| Pagador identificado (CPF/CNPJ com DV válido) | 15 | **38%** |

Entre as 22 linhas extraídas: **12 cobrança bancária, 10 arrecadação**.

## Achado 1 — OCR não é opcional

**7 de 39 PDFs (18%) não têm camada de texto alguma.** São scans ou imagens: seguro de vida (2), despachante, IMT, container, cartão de crédito, fornecedor de material.

Consequência direta: a estratégia "texto embutido → regex → OCR se necessário" descrita em [`04-integrations.md`](04-integrations.md) precisa tratar OCR como **caminho de primeira classe na sprint 2.4**, não como fallback exótico. Quase um em cada cinco boletos depende dele.

## Achado 2 — DV é necessário, e não é suficiente

O boleto da VIVO renderiza a fonte do código de barras como texto. Sem validação de DV, o extrator devolveu **214 falsos CNPJs** e vários falsos candidatos de 48 dígitos — todos sequências de `0` e `1`.

Com validação de DV, os 214 falsos CNPJs sumiram. Mas **um falso positivo sobreviveu**: uma janela de 47 dígitos daquele mesmo lixo passou nos três mod 10 e no mod 11, e o parser reportou com toda a confiança `banco=000 valor=4.411.000,00 venc=2025-10-10`.

Isso não é azar: os DVs de uma linha de cobrança somam ~4 dígitos de verificação, ou seja, **1 em ~10.000 de chance por candidato**. Uma string longa de lixo gera milhares de janelas. O falso positivo é estatisticamente esperado.

Regras que saem daí, todas obrigatórias no `DigitableLine`:

1. **Todo candidato é hipótese até o DV fechar.** Gere todas as janelas de 47 e 48 dígitos, valide cada uma, nunca "pegue o primeiro match".
2. **DV não basta — aplique filtros de plausibilidade** antes de aceitar:
   - código do banco tem que existir na tabela COMPE (`000` não é banco);
   - valor tem que ser `> 0` e abaixo de um teto sanitário;
   - vencimento tem que cair numa janela plausível em torno de hoje (o exemplo acima já falharia por banco `000` e por valor de R$ 4,4 milhões);
   - preferir candidatos próximos de rótulos conhecidos no texto (`linha digitável`, `código de barras`, `pagável em qualquer banco`) a candidatos soltos.
3. **Ambiguidade só se resolve com a consulta oficial.** Se sobrar mais de um candidato plausível, consulte todos e fique com o que o provedor reconhecer — em vez de escolher por heurística.
4. **Validar CPF/CNPJ pelo dígito verificador** antes de tratar como identidade. Regex de formato não basta, e aqui o dado alimenta o roteamento entre tenants.

Este é o caso de teste mais valioso do corpus: um arquivo que faz o parser ingênuo **e** o parser só-com-DV errarem com confiança.

## Achado 3 — arrecadação é 45% do volume, não caso de borda

Dos 22 boletos com linha extraída: **12 cobrança bancária, 10 arrecadação**.

| Tipo | Exemplos no corpus | Segmento / banco |
|---|---|---|
| Arrecadação seg. 5, id 8 (valor referência, mod 11) | DARF, DAS | tributos federais |
| Arrecadação seg. 2, id 6 (valor efetivo, mod 10) | SABESP, DAE | saneamento |
| Arrecadação seg. 3, id 6 | EDP | energia |
| Cobrança | SECONCI (033), condomínio (341), plano de saúde (237), ENEL (237), fornecedor (341) | Santander, Itaú, Bradesco |

Isso **eleva a criticidade** da pergunta em aberto do [`ADR-001`](adr/ADR-001-asaas-como-provedor.md): se o `POST /v3/bill/simulate` do Asaas não cobrir arrecadação, quase metade do volume real fica sem consulta oficial — e sem consulta oficial não há check de beneficiário, que é o check que sustenta o produto. **Validar isso em sandbox é a primeira tarefa da sprint 1.3**, antes de qualquer código de adapter. Plano B, se a cobertura for parcial: para `BillKind.Utility`, cair para verificação offline (DV + segmento + valor + identificador do cedente no campo livre) e marcar `LookupAvailability` como `Skipped` com severidade `Advisory` **apenas para arrecadação**, nunca para cobrança.

Ambas as variantes de mod 11 de arrecadação precisam ser aceitas — a especificação FEBRABAN tem implementações divergentes em campo, e o corpus tem exemplos com identificador 8.

## Achado 4 — o fator de vencimento é ambíguo, e a desambiguação certa não é fixar a base

**Todo boleto de cobrança do corpus cai na faixa reiniciada.** Com a base antiga (07/10/1997) as datas saem em 2001; com o rollover (fator 1000 = 22/02/2025) saem corretamente em 2026.

Exemplos verificados: fator 1493 → **2026-06-30** (base antiga daria 2001-11-08); fator 1337 → **2026-01-25**, coerente com o arquivo nomeado "SECONCI ATRASADO".

O detalhe que importa: o fator tem 4 dígitos e **já deu a volta**, então qualquer valor entre 1000 e 9999 tem duas leituras válidas. Trocar a constante da base resolve hoje e quebra no próximo rollover.

**Regra adotada:** gerar os candidatos das duas épocas e escolher o **mais próximo de hoje**. Vencimento de boleto está sempre a poucos anos do presente, então o critério é estável e se autocorrige nos ciclos futuros. Implementação em [`tools/analyze-boleto-corpus.js`](tools/analyze-boleto-corpus.js) (`dueDateFromFactor`).

Fator `0000` significa "sem vencimento" e não vira data. Casos de teste obrigatórios no `DigitableLine`: os fatores 1493 e 1337 acima, o `0000`, e um fator ambíguo verificando que a escolha cai na época correta.

## Achado 5 — o CNPJ do pagador só aparece em 38% dos boletos

Este é o achado que muda o desenho de roteamento multi-tenant.

O corpus tem **dois pagadores distintos** convivendo na mesma pasta (prefixos `RBC` e `RUF`), que é exatamente o cenário de fonte compartilhada. O CNPJ do pagador aparece em **15 de 39** arquivos (38%).

| Tem CNPJ do pagador | Não tem |
|---|---|
| DARF, DAS (tributos) | FGTS (5 arquivos) |
| SECONCI (6 arquivos) | Sindicatos (4 arquivos) |
| Plano de saúde | Energia EDP (4 de 5) |
| Condomínio | Água SABESP / DAE |
| Fornecedor de material | Telefonia VIVO |
| | Cartão de crédito, seguro (PDF-imagem) |

O padrão é nítido: **as contas que não trazem o CNPJ são justamente as recorrentes de concessionária e de serviço continuado.** Elas identificam o pagador por *conta contrato*, *instalação*, *matrícula* ou *inscrição* — não por documento fiscal.

Duas consequências, ambas boas:

1. **Roteamento por CNPJ extraído não basta** — cobriria pouco mais de um terço. Precisa da escada de roteamento descrita em [`07-multitenancy-and-routing.md`](07-multitenancy-and-routing.md).
2. **Mas o que não tem CNPJ é estável e recorrente.** A mesma instalação da EDP todo mês, a mesma matrícula da SABESP, o mesmo condomínio. Isso torna o roteamento aprendido (o usuário reivindica uma vez, o sistema acerta para sempre) não só viável como o mecanismo principal. Convergência esperada: primeiro mês com trabalho manual, meses seguintes quase todos automáticos.

## Achado 6 — há uma duplicata real no corpus

Dois arquivos ("LUZ EDP CASA FLORENTINO" e "LUZ EDP CASA FLORENTINO 407") resolvem para a **mesma linha digitável**, mesmo segmento e mesmo valor (R$ 86,63). É a segunda via do mesmo boleto salva duas vezes.

Numa pasta organizada à mão isso é ruído inofensivo; num sistema que paga, é pagamento duplicado. Confirma que o check `Duplicate` não é hipotético — o cenário está presente em 39 arquivos de um único mês. E confirma o comportamento desenhado: a segunda via de um boleto não pago tem a mesma linha, então o item vira `Discarded` apontando para a Bill existente, sem gerar ruído para o usuário.

## Achado 7 — a mistura PF/PJ é real

O corpus tem contas claramente de pessoa jurídica (DARF, DAS, FGTS, sindicato patronal, SECONCI) convivendo com contas de natureza pessoal (IMT, cartão de crédito, energia e condomínio de residências e apartamentos identificados por nome de casa). Confirma o requisito de o mesmo sistema tratar PF e PJ com o mesmo modelo — muda o `TaxIdKind` e o tipo de subconta Asaas, não o Aggregate.

## Distribuição por natureza do gasto

Útil para calibrar o `Payee` e a `AmountPolicy`:

| Natureza | Qtd | Política de valor sugerida |
|---|---|---|
| Tributos e encargos (DARF, DAS, FGTS) | 9 | `Unbounded` — varia com folha e faturamento |
| Contribuições de classe (SECONCI, sindicatos) | 10 | `Range` estreito |
| Concessionárias (energia, água, telefonia) | 9 | `Range` largo — sazonal |
| Condomínio, plano de saúde, seguro | 5 | `Fixed` com tolerância |
| Fornecedores e serviços | 6 | `Unbounded` |

Metade do volume é recorrente e previsível — o que torna o check `AmountMatch` genuinamente útil depois de dois ou três ciclos de histórico.

## O que fazer com este corpus

1. **Fixture de teste.** Copiar uma amostra anonimizada para `BillPayment.UnitTests/DataForTests/boletos/`, cobrindo os seis cenários que quebram parser ingênuo: arrecadação seg. 5 id 8 (mod 11), arrecadação seg. 2 id 6 (mod 10), cobrança de três bancos diferentes, um PDF-imagem, a duplicata da EDP, e — o mais importante — **o caso VIVO**, que produz um falso positivo aprovado pelo DV e só é barrado pelos filtros de plausibilidade.
2. **Métrica de regressão do parser.** A taxa de extração sobre o corpus é o indicador da sprint 2.4. Meta: ≥ 90% com OCR ligado. Qualquer queda entre versões é regressão.
3. **Semente do cadastro.** Os beneficiários recorrentes (SECONCI, sindicatos, EDP, SABESP, VIVO, condomínios, INTERMEDICA) são a carga inicial de `Payee` dos dois tenants, com os bancos recebedores observados aqui (033, 237, 341).
4. **Reexecutar a análise** quando o parser real existir, e substituir os números deste documento pelos dele. Este documento é uma medição datada, não uma verdade permanente.
