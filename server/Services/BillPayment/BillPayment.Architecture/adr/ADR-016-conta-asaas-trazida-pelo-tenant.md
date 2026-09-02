# ADR-016 — A conta Asaas é trazida pelo tenant, não criada pela plataforma

**Status:** Aceito · **Data:** 2026-09-02 (formaliza a decisão executada em 2026-08-31)

## Contexto

O desenho original da fase 3 (doc 07 → "Subcontas Asaas", [`ADR-001`](ADR-001-asaas-como-provedor.md))
previa **subcontas criadas pela plataforma**: `POST /v3/accounts` com uma chave-plataforma,
onboarding/KYC acompanhado por nós, e a chave de cada subconta no cofre. Em 2026-08-31 a dívida
"chave Asaas por tenant" foi paga por outro caminho, decidido com o usuário: **cada tenant cola a
própria chave de API** (`PUT /payer-profile/asaas-account`), provada no provedor
(`GET /v3/myAccount`, porta `IPaymentAccountVerifier`), cifrada no cofre e referenciada por
`PayerProfile.AsaasAccountRef` (`CredentialRef?`). A chave da instalação saiu de cena — é a chave
que paga, e um fallback global pagaria a conta de um tenant com o dinheiro de outro engano de
configuração.

## Decisão

**A conta Asaas pertence ao tenant e é trazida por ele.** A plataforma não cria subcontas, não
guarda chave-plataforma e não acompanha KYC. O vínculo é: chave em claro → prova no provedor →
cofre → ponteiro no `PayerProfile`. `CanSchedulePayments` deriva do ponteiro; sem ele o tenant usa
o sistema até `Approved` e não agenda — estado do tenant, não erro.

## Consequências para a fase 3

- **Webhook por conta.** Não existe mais um webhook da plataforma: cada conta de tenant precisa do
  seu, apontando para o nosso endpoint. O provisionamento é **programático, com a chave do
  tenant** (`POST /v3/webhooks`), disparado no vínculo da chave (`LinkAsaasAccount`) e conferido
  pela conciliação. O token de autenticação do webhook é **gerado por nós, por tenant, e guardado
  no cofre** (`SecretKind` próprio); a validação na borda é constant-time e a resolução do tenant
  é pela `externalReference` da ordem — o Asaas não conhece o nosso `tenantId`.
- **Saldo por conta.** `GET /v3/finance/balance` com a chave do tenant; a verificação de saldo da
  sprint 3.2 é por tenant, e o aporte é operação do cliente na conta dele.
- **Whitelist de IP por conta.** O item do checklist pré-produção vale por tenant: cada conta
  precisa da whitelist configurada pelo próprio dono. Vira passo de onboarding documentado, não
  configuração nossa.
- **KYC saiu do nosso caminho crítico.** O risco de cronograma da 3.0 original ("KYC das
  subcontas pode atrasar o piloto") deixa de existir; o resíduo é "tenant sem conta", que é
  visível (`CanSchedulePayments`) e destravável a qualquer momento.
- **Suspensão de tenant — mitigação operacional registrada.** Os workers não enxergam o claim
  (decisão do CLAUDE.md), e a fase 3 acrescenta um worker que move dinheiro. Enquanto não houver
  réplica local de estado do tenant (ADR a abrir no TenantManagement quando o volume justificar),
  a suspensão de um tenant **inclui operacionalmente** remover o vínculo da chave
  (`DELETE /payer-profile/asaas-account`): sem chave, a fila de submissão estaciona as ordens em
  estado visível e nada mais é submetido. Ordens já agendadas no provedor são canceladas pelo
  operador na conta do tenant.

## Alternativas descartadas

- **Subconta criada pela plataforma** (o desenho original) — mantém chave-plataforma capaz de
  criar contas, põe o KYC no nosso caminho crítico e nos torna operadores da conta alheia. Pode
  ser reaberto como *conveniência de onboarding* quando houver volume, sem mudar o modelo: a
  subconta criada continuaria sendo do tenant, com a chave dela entrando pelo mesmo
  `LinkAsaasAccount`.
- **Fallback para uma chave global quando o tenant não tem a sua** — rejeitado explicitamente em
  2026-08-31; misturaria dinheiro de tenants na mesma conta, que é o que a segregação por conta
  existe para impedir.
