# 01 — Visão e escopo do Bounded Context BillPayment

## Problema

Contas a pagar chegam espalhadas: anexos de PDF em uma ou mais caixas de e-mail, portais de fornecedores, mensagens avulsas. Alguém abre cada uma, confere manualmente se o boleto é legítimo, digita a linha no banco e paga. Esse processo é lento, não deixa rastro auditável e é o vetor clássico de fraude de boleto (linha digitável trocada, beneficiário sósia, valor inflado).

## Missão do BC

**Capturar boletos automaticamente, provar que são legítimos antes de qualquer autorização humana, agendar e executar o pagamento na data, e manter o histórico auditável de tudo isso.**

O BC é responsável pelo ciclo completo:

```
capturar → normalizar → validar → aprovar (humano) → agendar → pagar → conciliar → reportar
```

## Escopo

### Dentro

- Captura de boletos por **e-mail** (Microsoft Graph e Gmail API) e por **upload manual**.
- Captura por **portais de fornecedor** (automação headless) — fase tardia, ver [`06-roadmap.md`](06-roadmap.md).
- Normalização: linha digitável / código de barras, validação de dígitos verificadores, classificação (cobrança bancária × arrecadação/concessionária).
- **Consulta oficial do título** para obter beneficiário, banco recebedor, valor atualizado, juros/multa/desconto e vencimento.
- **Bateria de verificações** com resultado materializado e auditável (ver [`03-bill-validation.md`](03-bill-validation.md)).
- Cadastro de **beneficiários esperados** (`Payee`) e de **origens confiáveis** (`TrustedOrigin`) — o "condiz com o quê" das verificações.
- **Autorização humana** obrigatória antes de qualquer movimentação.
- **Agendamento** respeitando dias úteis, horário de corte e data mínima do provedor.
- **Execução do pagamento** via Asaas, com conciliação por webhook.
- **Histórico e relatórios** de pagamentos.

### Fora

- Contas a **receber** / emissão de cobrança (é outro BC).
- Contabilidade, rateio de centro de custo, conciliação contábil.
- Gestão de fluxo de caixa e projeção financeira.
- Cadastro de empresas/usuários (vem do BC de identidade; aqui só `TenantId`/`UserId`).
- Aporte de saldo na conta do provedor de pagamento (operação manual do cliente na fase 3).

## Premissas de negócio

1. **Nada é pago sem aprovação humana.** Auto-aprovação por política é possível como evolução, mas exige ADR próprio e teto de valor — ver [`adr/ADR-007-aprovacao-humana-obrigatoria.md`](adr/ADR-007-aprovacao-humana-obrigatoria.md).
2. **Verificação nunca bloqueia silenciosamente.** Um boleto que falha em uma checagem não some: ele fica visível com o motivo, e o aprovador decide com a evidência na tela.
3. **Origem desconhecida não é origem confiável, mas também não é origem hostil.** Primeiro boleto de um remetente novo cai como inconclusivo e o usuário promove a origem depois de conferir.
4. **Multi-tenant desde o dia zero.** Todo Aggregate Root carrega `TenantId`; toda query e todo `ExistsAsync` filtram por ele.
5. **O provedor de pagamento é Asaas**, mas o Domain fala com portas (`IBillLookupService`, `IBillPaymentGateway`) — trocar de provedor é trocar adapter. Ver [`adr/ADR-001-asaas-como-provedor.md`](adr/ADR-001-asaas-como-provedor.md).

## Linguagem ubíqua

| Termo | Significado neste BC |
|---|---|
| **Bill** | O boleto capturado, com toda a sua história: origem, consulta oficial, checagens, decisão humana. É o Aggregate central. |
| **Digitable line** (linha digitável) | Representação numérica impressa do boleto (47 dígitos em cobrança, 48 em arrecadação). Chave natural de deduplicação. |
| **Barcode** (código de barras) | Os 44 dígitos codificados. Derivável da linha digitável e vice-versa. |
| **Lookup** / consulta oficial | Chamada ao provedor que devolve os dados autoritativos do título a partir da linha digitável. Nunca confiar no PDF quando existe lookup. |
| **Lookup snapshot** | Retrato imutável do que a consulta devolveu, com o instante da consulta. É a evidência que sustenta as checagens. |
| **Check** | Uma verificação nomeada sobre o Bill (beneficiário, valor, banco, pagador, origem…), com resultado, severidade e evidência. |
| **Payee** (beneficiário) | Cadastro do fornecedor esperado: razão social, CNPJ/CPF, bancos recebedores aceitos e política de valor. |
| **Trusted origin** (origem confiável) | Endereço/domínio de e-mail ou domínio de site marcado como confiável, bloqueado ou desconhecido. |
| **Capture source** (fonte de captura) | Caixa de e-mail ou portal monitorado, com seu cursor de sincronização. |
| **Capture item** | Registro bruto de um item ingerido (mensagem + anexo), mesmo quando não vira Bill. Trilha de auditoria e idempotência da ingestão. |
| **Payment order** (ordem de pagamento) | A ordem registrada no provedor. Fonte de verdade da execução financeira. |
| **Approval** | Ato humano de autorizar (ou recusar) o pagamento de um Bill. Sempre atribuído a um `UserId`. |
| **Alçada** | Teto de valor que um aprovador pode autorizar. |
| **Arrecadação** / concessionária | Boleto iniciado por `8` (água, luz, telefone, tributos). Regras de dígito verificador e de estrutura diferentes da cobrança bancária. |

## Documentos relacionados

- [`02-domain-model.md`](02-domain-model.md) — Aggregates, VOs, eventos, invariantes.
- [`03-bill-validation.md`](03-bill-validation.md) — as checagens em detalhe.
- [`04-integrations.md`](04-integrations.md) — Asaas, Microsoft Graph, Gmail, portais.
- [`05-use-cases.md`](05-use-cases.md) — casos de uso e contratos de API.
- [`06-roadmap.md`](06-roadmap.md) — fases e sprints.
