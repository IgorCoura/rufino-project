# ADR-004 — O pagador: não é autoritativo, mas é decisivo

**Status:** Aceito · **Data:** 2026-07-31 · **Revisado:** 2026-07-31 (requisito de fonte compartilhada + medição do corpus real)

## Contexto

O requisito pede verificar "se o pagador condiz". Duas restrições, uma técnica e uma de produto, colidem aqui:

1. **A consulta oficial não devolve pagador.** O `POST /v3/bill/simulate` do Asaas entrega `beneficiaryName`, `beneficiaryCpfCnpj`, `bank`, valores e vencimento — e nenhum campo de pagador. Não é limitação do provedor: a rede de cobrança identifica quem *recebe*; quem paga é qualquer um que apresente o código de barras. Boleto não é nominal.
2. **Uma fonte pode servir a vários tenants.** Duas pessoas compartilham uma caixa de e-mail, e o sistema precisa impedir que uma pague a conta da outra. O pagador deixa de ser um detalhe de conferência e vira **chave de roteamento**.

E a medição do corpus real ([`08-boleto-corpus-findings.md`](../08-boleto-corpus-findings.md)) acrescenta a terceira: **o CNPJ do pagador aparece em só 38% dos boletos**. As contas de concessionária e serviço continuado identificam o pagador por conta contrato, instalação ou matrícula.

## Decisão

**O pagador extraído é sinal frágil na origem e forte na consequência.** Frágil porque vem de PDF/OCR e falta em quase dois terços dos casos. Forte porque, quando existe e contradiz, é prova suficiente para **impedir** o pagamento.

Severidade do check `PayerMatch` passa a ser condicional:

| Situação | Outcome | Severidade |
|---|---|---|
| Extraído e casa com o `PayerProfile` do tenant | `Passed` | — |
| Extraído e **não** casa | `Failed` | **Blocking** |
| Não extraível | `Inconclusive` | Advisory |

A assimetria é intencional: **presença de contradição bloqueia; ausência de confirmação não libera.** Um `Passed` aqui não é prova de propriedade — é ausência de contradição em um dado que ninguém certifica.

Como o pagador não basta para roteamento, ele é um degrau de uma escada de cinco (o degrau 0, por senha derivada de PDF, vem antes dele — ver [`09-capture-channels.md`](../09-capture-channels.md)), e nenhum boleto vira `Bill` sem rota determinada. A escada está em [`07-multitenancy-and-routing.md`](../07-multitenancy-and-routing.md).

## Razões

- **Honestidade do sinal.** Tratar OCR como autoritativo cria confiança falsa exatamente onde a fraude opera. Mas ignorá-lo quando ele contradiz seria pior: é a única evidência disponível de que o boleto é de outra pessoa.
- **A assimetria é gratuita.** Bloquear com base em contradição não gera falso positivo relevante — se o PDF diz outro CNPJ, ou é de outro mesmo, ou o parser errou (e aí o usuário corrige o parser reportando, não pagando).
- **`Inconclusive` é o caso majoritário por medição, não por suposição.** 64% dos boletos reais caem nele. Marcá-lo como falha treinaria o usuário a ignorar alertas, que é como o alerta que importa passa batido.
- **O check que de fato protege contra fraude de boleto é `PayeeMatch`** (CNPJ do beneficiário contra o cadastro), que é autoritativo e bloqueante. `PayerMatch` protege contra um risco diferente — pagar a conta certa da pessoa errada.

## Consequências

- **O parser de PDF sobe de prioridade.** Ele deixa de ser conforto de UX e vira componente de isolamento entre tenants. OCR passa a ser caminho de primeira classe (18% dos boletos reais não têm camada de texto).
- **O TaxId extraído precisa de validação de dígito verificador** antes de ser tratado como identidade — o corpus mostrou 214 falsos CNPJs vindos de fonte de código de barras renderizada como texto.
- `PayerProfile` (Aggregate novo) passa a ser obrigatório: sem cadastro fiscal do tenant não há contra o que comparar. Enquanto não existir, o check é `Skipped` com motivo — melhor do que comparar contra nada.
- A UI precisa distinguir visualmente `PayerMatch=Passed` de `PayeeMatch=Passed`. Selos idênticos para um dado certificado e um dado lido de PDF seriam enganosos.
- **Esta decisão é permanente, não provisória.** A única fonte que tornaria o pagador verificável seria o DDA (Débito Direto Autorizado), que lista os boletos emitidos *contra* o CNPJ — e o DDA está **fora do desenho** por custo e complexidade de acesso ([`ADR-012`](ADR-012-portais-reduzir-residuo.md)). Não há alternativa no horizonte: a escada de roteamento de cinco degraus e o `PayerMatch` Advisory-quando-ausente ficam como estão. Só reabra este ADR se o acesso ao DDA mudar de patamar.
- **Consequência direta:** como nada garante que uma cobrança emitida contra o tenant chegou até o sistema, a defesa contra ausência silenciosa passa a ser a expectativa de boleto ([`ADR-014`](ADR-014-expectativa-e-lembretes.md)) — não um check.

## Alternativa descartada

**Atribuir o boleto ao dono da fonte quando o pagador não é extraível.** É o caminho óbvio e é exatamente o que o requisito proíbe: numa caixa compartilhada, faria um tenant herdar as contas do outro por default. A quarentena `Unrouted` com reivindicação explícita custa um clique no primeiro boleto de cada conta recorrente e elimina a classe inteira de erro.
