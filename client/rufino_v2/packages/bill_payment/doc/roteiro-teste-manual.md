# Roteiro de teste manual — módulo BillPayment no app RUFINO_v2

Escopo: os fluxos do pacote `packages/bill_payment` rodando dentro do app
`client/rufino_v2`, contra a API `BillPayment.API`.

Como usar: percorra os blocos **na ordem** — os cadastros dos blocos 2 a 5 são
pré-requisito funcional dos boletos do bloco 6 em diante. Cada caso tem
`Passos` e `Esperado`; marque `[x]` no que passou e registre o que falhou na
tabela do fim.

---

## 0. Preparação do ambiente

### 0.1 Serviços no ar

| Item | Como confere | Observação |
|---|---|---|
| BillPayment.API | `GET http://<host>:8100/api/health` responde 200 | porta padrão do compose |
| Postgres do BC | porta 8102, schema `bill_payment` | migrations aplicadas |
| Keycloak | realm `rufino`, client `bill-payment-api` | |
| TenantManagement | `GET /api/v1/me/tenants` responde | emite o claim `bp_tenants` |

### 0.2 Autorização (o passo que mais derruba teste)

1. Importe `utils/KeyCloakConfig/bill-payment-authz-config.json` no client
   `bill-payment-api`. Ele cria os recursos (`bill`, `payee`, `origin`,
   `capture-source`, `capture-item`, `expectation`, `payer-profile`), os
   escopos e os 4 papéis: `bill-admin`, `bill-approver`, `bill-operator`,
   `bill-viewer`.
2. Crie **quatro usuários de teste**, um por papel. A matriz de permissão do
   bloco 11 depende disso.
3. Confirme que o token do usuário traz o tenant de teste no claim
   **`bp_tenants`** (não é o `tenants` genérico). Sem isso **todo** endpoint
   devolve 403 e o app mostra "Você não tem permissão para esta ação" em
   todas as telas — sintoma que parece bug de UI e é configuração.

### 0.3 App

```bash
cd client/rufino_v2
flutter run --dart-define-from-file=secrets/local_config.json
```

`secrets/local_config.json` precisa de `bill_payment_url` (aceita
`host:porta` ou origem completa `http://host:8100`).

### 0.4 Flags do servidor que MUDAM o resultado esperado

Leia esta tabela antes de abrir chamado — o padrão do `appsettings.json` é
"desligado", e desligado é comportamento correto, não defeito.

| Flag (padrão) | Efeito no teste |
|---|---|
| `Asaas.ApiKey` **vazio** | A consulta oficial devolve *Unavailable*. O check **Consulta oficial** sai **Reprovado** (é *blocking*) e todo boleto importado termina em **Rejeitado**, sem botão Aprovar. **Para testar aprovação, configure a chave sandbox.** |
| `Capture.Enabled = false` | Os workers de varredura e processamento não sobem: a quarentena nunca recebe item novo. |
| `Graph.Enabled = false` | Conectar caixa e Sincronizar não falam com o Microsoft Graph. |
| `Secrets.MasterKey` vazio | O cofre está desligado: conectar caixa / substituir credencial falha. |
| `DocumentIntelligence.Provider = "None"` | Reprocessar termina no parser determinístico; o método "Leitura por IA" nunca aparece. |
| `Storage.*` vazio | Anexos não são guardados; a captura falha na primeira gravação. |
| `Expectations.Enabled = true`, `Interval 06:00:00` | A varredura de expectativas roda a cada 6 h — ciclo "Não chegou" **não** aparece na hora. Reinicie a API para forçar uma passada. |
| `Approval.MaxSnapshotAgeHours = 12` | Consulta com mais de 12 h bloqueia o Aprovar. |
| `Capture.PollingInterval 1 min` / `ProcessingInterval 15 s` | Com a captura ligada, item novo leva ~1 min para aparecer e ~15 s para ser processado. |

### 0.5 Massa de teste a separar antes de começar

- 2 boletos bancários reais (linha digitável de **47 dígitos**).
- 1 conta de consumo / arrecadação (**48 dígitos**).
- 1 código Pix copia-e-cola (BR Code) de cobrança.
- 1 linha digitável com **um dígito alterado** (para reprovar a integridade).
- CNPJ da empresa de teste e CNPJ de uma filial da mesma raiz.

---

## 1. Entrada no módulo

- [ ] **NAV-01 — Menu do BillPayment aparece**
  Passos: login → tela inicial.
  Esperado: a seção do BillPayment lista **Painel de Contas, Boletos,
  Quarentena, Beneficiários, Expectativas, Fontes de Captura, Origens
  Confiáveis, Perfil do Pagador**. Cada item só aparece se o usuário tiver
  ao menos um escopo no recurso correspondente.

- [ ] **NAV-02 — Tenant obrigatório**
  Passos: com mais de um cliente vinculado, troque de cliente e reabra
  qualquer tela do módulo.
  Esperado: os dados trocam junto; nenhum registro do cliente anterior
  permanece na lista.

- [ ] **NAV-03 — Voltar sem pilha (web)**
  Passos: cole `/bill-payment/bills/<id>` direto na barra de endereço e
  clique na seta de voltar do app.
  Esperado: vai para a lista de Boletos (fallback), não para uma tela em
  branco.

---

## 2. Perfil do Pagador — `/bill-payment/payer-profile`

É pré-requisito de tudo: sem ele não há senha derivada para abrir PDF
protegido nem verificação de pagador.

- [ ] **PP-01 — Onboarding aparece quando não há perfil**
  Esperado: card "Antes de tudo, quem paga?" + explicação de que o que a
  captura não reconhecer é descartado, e o botão **Cadastrar perfil**.

- [ ] **PP-02 — Máscara troca com o tipo**
  Passos: alterne **Pessoa jurídica** ↔ **Pessoa física**.
  Esperado: o rótulo vira CNPJ/CPF e a máscara muda para
  `##.###.###/####-##` / `###.###.###-##`.

- [ ] **PP-03 — Validação de documento incompleto**
  Passos: PJ + CNPJ com 10 dígitos → Cadastrar.
  Esperado: "Informe o documento completo." Nome vazio → "Informe o nome."

- [ ] **PP-04 — Cadastro válido**
  Passos: PJ, razão social e CNPJ completos → Cadastrar perfil.
  Esperado: a tela troca do onboarding para o perfil, com Identificação,
  Documentos adicionais, Casar por raiz de CNPJ e Conta de pagamento.

- [ ] **PP-05 — Documento adicional**
  Passos: adicione um CPF/CNPJ extra pelo campo + botão `+`.
  Esperado: vira chip; o campo limpa. Remova pelo `x` do chip → some.

- [ ] **PP-06 — Casar por raiz de CNPJ**
  Passos: ligue o switch "Filiais contam como o mesmo pagador".
  Esperado: persiste após recarregar a tela. **A seção só existe para
  Pessoa jurídica** — em PF ela não deve aparecer.

- [ ] **PP-07 — Conta Asaas do tenant**
  Passos: cole a chave de API do Asaas (campo oculto) → Vincular.
  Esperado: o servidor prova a chave no provedor; o badge sai de "Não
  configurada" para configurada e o campo é **limpo** (a chave nunca volta
  pela API — não existe "editar", só remover com confirmação). Chave
  recusada → mensagem do domínio (`BLP.PRF12`/`PRF13`).

- [ ] **PP-08 — Erro do domínio é exibido**
  Passos: tente cadastrar um documento adicional inválido.
  Esperado: a mensagem que aparece é a **do servidor**, em português, não
  um texto genérico.

---

## 3. Beneficiários — `/bill-payment/payees`

- [ ] **BEN-01 — Lista vazia**
  Esperado: estado vazio; FAB **Cadastrar** visível para quem tem
  `payee:manage`.

- [ ] **BEN-02 — Validações do formulário**
  Passos: salvar em branco.
  Esperado: "Informe a razão social." e "Informe um CPF (11) ou CNPJ (14
  dígitos)."

- [ ] **BEN-03 — Máscara dinâmica de documento**
  Passos: digite 11 dígitos, depois um 12º.
  Esperado: a máscara vira de CPF para CNPJ sozinha.

- [ ] **BEN-04 — Política Fixo**
  Passos: selecione **Fixo**, valor esperado vazio → Cadastrar.
  Esperado: "Informe o valor." Preencha valor + tolerância (%) → cadastra e
  vai direto para o detalhe.

- [ ] **BEN-05 — Política Faixa**
  Esperado: mínimo e máximo obrigatórios; valor não numérico → "Valor
  inválido."

- [ ] **BEN-06 — Política Sem limite**
  Esperado: somem os campos numéricos e aparece o aviso de que a verificação
  de valor fica **inconclusiva**.

- [ ] **BEN-07 — Apelidos**
  Passos: no detalhe, adicione um apelido e remova outro.
  Esperado: cada operação recarrega o cadastro do servidor — o chip só
  aparece depois que o servidor aceitou.

- [ ] **BEN-08 — Bancos aceitos**
  Passos: adicione `033`, depois tente `33` ou `abc`.
  Esperado: o campo aceita só dígitos; o servidor recusa código inválido
  com mensagem própria.

- [ ] **BEN-09 — Renomear inline**
  Passos: edite a razão social na seção Identificação → Salvar / Cancelar.
  Esperado: Cancelar descarta; Salvar persiste.

- [ ] **BEN-10 — Desativar / Reativar**
  Esperado: o badge alterna Ativo ↔ Desativado.

- [ ] **BEN-11 — Excluir**
  Passos: Excluir → diálogo.
  Esperado: o diálogo avisa que os boletos deixam de casar com o cadastro;
  confirmando, volta para a lista e o registro sumiu.

- [ ] **BEN-12 — Busca por documento exato**
  Passos: busque pelo CPF/CNPJ completo de um cadastro; depois por um
  documento inexistente.
  Esperado: 1 resultado no primeiro caso; **estado vazio (não erro)** no
  segundo, com botão **Limpar busca**.

- [ ] **BEN-13 — Paginação**
  Passos: com mais de uma página, role até o fim.
  Esperado: carrega a próxima página; durante a busca a paginação não roda.

---

## 4. Origens confiáveis — `/bill-payment/trusted-origins`

- [ ] **ORI-01 — Estado vazio**
  Esperado: "Nenhuma origem cadastrada. Remetente conhecido é o que
  transforma descarte em quarentena revisável."

- [ ] **ORI-02 — Cadastrar domínio confiável**
  Passos: FAB → tipo **Domínio de e-mail**, valor `fornecedor.com.br`,
  decisão Confiável, observação opcional → Cadastrar.
  Esperado: a folha fecha e a linha aparece com badge **Confiável**.

- [ ] **ORI-03 — Valor obrigatório**
  Esperado: "Informe o endereço ou domínio."

- [ ] **ORI-04 — Precedência de casamento**
  Passos: cadastre o domínio `fornecedor.com.br` como Confiável **e** o
  endereço `cobranca@fornecedor.com.br` como Bloqueada. No resolvedor do
  topo, digite `cobranca@fornecedor.com.br` e envie.
  Esperado: casa com o **Endereço de e-mail** (precedência maior) e mostra
  **Bloqueada**.

- [ ] **ORI-05 — Remetente desconhecido**
  Passos: resolva `qualquer@dominio-nao-cadastrado.com`.
  Esperado: "Origem desconhecida — nenhum cadastro casa com este
  remetente." Isso é um estado válido, não erro.

- [ ] **ORI-06 — Trocar decisão pela linha**
  Passos: menu de ações da linha → Bloquear / Marcar como confiável.
  Esperado: o ícone e o badge trocam depois do recarregamento.

- [ ] **ORI-07 — Excluir origem**
  Esperado: some da lista.

---

## 5. Expectativas — `/bill-payment/expectations`

- [ ] **EXP-01 — Beneficiário é obrigatório**
  Passos: FAB Cadastrar → salvar sem escolher.
  Esperado: "Escolha o beneficiário." O dropdown é alimentado pelos
  beneficiários do bloco 3 — se estiver vazio, cadastre um antes.

- [ ] **EXP-02 — Faixas dos campos numéricos**
  Passos: dia do vencimento `0` ou `32`; "chega quantos dias antes" `61`;
  "avisar" `0`.
  Esperado: "Entre 1 e 31.", "Entre 0 e 60." e "Entre 1 e 60."
  respectivamente.

- [ ] **EXP-03 — Cadastro válido**
  Passos: beneficiário, conta/referência (ex.: instalação da EDP), nome
  ("EDP — Casa Florentino"), recorrência Mensal, dia 10, chega 7 dias
  antes → Cadastrar.
  Esperado: vai direto para o detalhe da expectativa.

- [ ] **EXP-04 — Detalhe**
  Esperado: recorrência + dia, conta/referência, aviso em dias, origem
  (**Manual**, ou **Aprendida** com contagem de observações) e a seção
  Ciclos.

- [ ] **EXP-05 — Pausar até**
  Passos: "Pausar até…" → escolha uma data futura.
  Esperado: badge "Pausada até <data>".

- [ ] **EXP-06 — Retomar / Desativar**
  Esperado: Retomar volta para "Ativa"; Desativar leva a "Desativada".

- [ ] **EXP-07 — Dispensar ciclo**
  Passos: com um ciclo aberto, dispense informando (ou não) o motivo.
  Esperado: o ciclo vai para **Dispensado**. **Só quem tem
  `expectation:waive` (approver/admin) vê esta ação.**

- [ ] **EXP-08 — Ciclo "Não chegou"**
  Passos: cadastre uma expectativa cuja data de aviso já passou e espere a
  varredura (6 h) ou reinicie a API.
  Esperado: o ciclo vira **Não chegou** com o motivo (Nunca chegou, Falha
  na leitura, Protegido por senha, Download falhou, Sem dono definido,
  Portal indisponível) e passa a aparecer no Painel.

---

## 6. Importar boleto — `/bill-payment/bills/import`

- [ ] **BOL-01 — Pelo menos um instrumento**
  Passos: Importar com os dois campos vazios.
  Esperado: "Informe a linha digitável ou o código Pix."

- [ ] **BOL-02 — Importar por linha digitável (47 dígitos)**
  Esperado: a tela **substitui** a de importação pelo detalhe do boleto;
  tipo **Boleto bancário**, trilho **Boleto**.

- [ ] **BOL-03 — Importar arrecadação (48 dígitos)**
  Esperado: tipo **Arrecadação**; o campo Banco recebedor não aparece e o
  check de banco sai como "não se aplica".

- [ ] **BOL-04 — Importar por Pix copia-e-cola**
  Esperado: trilho **Pix**.

- [ ] **BOL-05 — Linha + Pix juntos**
  Esperado: importa uma vez só, o trilho resultante é **Pix** (o Pix ganha
  do boleto) e o check **Pix × código de barras** é executado.

- [ ] **BOL-06 — Duplicata do mesmo cliente**
  Passos: importe a mesma linha digitável duas vezes.
  Esperado: a segunda falha com a mensagem do servidor sobre duplicidade;
  no boleto existente o check **Duplicidade** mostra "Este boleto já está
  cadastrado nesta conta."

- [ ] **BOL-07 — Linha com dígito verificador errado**
  Esperado: o boleto entra, mas o check **Integridade do código** sai
  **Reprovado** e marcado **BLOQUEIA**; o status vai para **Rejeitado**.

- [ ] **BOL-08 — Botão bloqueado durante o envio**
  Esperado: o botão vira spinner e não aceita duplo clique.

---

## 7. Detalhe e decisão do boleto — `/bill-payment/bills/{id}`

- [ ] **BOL-10 — O documento não vaza**
  Esperado: **a linha digitável e o código Pix NÃO aparecem em lugar nenhum
  desta tela.** Quem tem os dígitos, paga — é decisão de projeto. Se
  aparecerem, é falha de segurança.

- [ ] **BOL-11 — Resumo**
  Esperado: beneficiário (ou "Não identificado"), documento, valor (com o
  original entre parênteses quando divergir), vencimento, banco recebedor,
  data da consulta oficial e, quando aprovado, "Agendado para".

- [ ] **BOL-12 — Lista de verificações**
  Esperado: o cabeçalho traz a contagem; cada linha tem ícone por desfecho
  (Verificado / Reprovado / Inconclusivo / Atenção / Não se aplica), o
  rótulo em português (Integridade do código, Duplicidade, Consulta
  oficial, Consistência da consulta, Beneficiário, Banco recebedor, Valor,
  Pagador, Origem, Vencimento, Roteamento, Pix × código de barras) e a
  explicação traduzida do motivo. Só falha bloqueante mostra **BLOQUEIA**.

- [ ] **BOL-13 — Beneficiário não cadastrado**
  Passos: importe um boleto de um beneficiário que não existe no cadastro.
  Esperado: o check **Beneficiário** traz "Beneficiário não cadastrado."

- [ ] **BOL-14 — Valor fora da política**
  Passos: cadastre o beneficiário com política Fixo de valor diferente do
  boleto e revalide.
  Esperado: **Valor** → "O valor está fora da política definida para este
  beneficiário."

- [ ] **BOL-15 — Banco não aceito**
  Passos: cadastre bancos aceitos que não incluam o do boleto → Revalidar.
  Esperado: **Banco recebedor** → "O banco recebedor não está entre os
  aceitos para este beneficiário."

- [ ] **BOL-16 — Pagador diverge**
  Passos: boleto impresso com outro pagador, com o Perfil do Pagador
  preenchido.
  Esperado: **Pagador** → "O pagador impresso no documento não é este
  cliente."

- [ ] **BOL-17 — Origem de importação manual**
  Esperado: seção **Origem** com entrada "Importação manual", sem
  remetente, e o check **Origem** com "Importação manual — não há remetente
  a verificar."

- [ ] **BOL-18 — Revalidar**
  Passos: cadastre o beneficiário faltante e clique **Revalidar**.
  Esperado: snackbar "Verificações reexecutadas."; a tela recarrega e o
  check que estava reprovado passa. Boleto **Rejeitado** que zera as falhas
  bloqueantes vira **Aguardando aprovação**.

- [ ] **BOL-19 — Consulta desatualizada trava o Aprovar**
  Passos: use um boleto cuja última consulta tenha mais de 12 h (ou reduza
  `Approval.MaxSnapshotAgeHours`).
  Esperado: aviso "Consulta desatualizada — revalide antes de aprovar." e o
  botão **Aprovar…** desabilitado. Depois de revalidar, habilita.

- [ ] **BOL-20 — Aprovar**
  Passos: **Aprovar…** → a folha mostra "Pagar em <data>"; abra o
  calendário.
  Esperado: não é possível escolher data anterior a hoje nem anterior à
  data mínima do provedor; o teto é 365 dias. Confirmar → snackbar
  "Pagamento autorizado.", status **Aprovado**, seção **Decisão**
  preenchida (decisão, quando, observação) e o botão Aprovar some.

- [ ] **BOL-21 — Negar exige motivo**
  Passos: **Negar** → confirmar com o campo vazio.
  Esperado: "Informe o motivo." Com motivo → "Boleto negado.", status
  **Negado**, e a partir daí **nenhum** botão de ação aparece (terminal).

- [ ] **BOL-22 — Cancelar exige motivo**
  Esperado: mesmo comportamento; status **Cancelado** e tela sem ações.

- [ ] **BOL-23 — Botões respeitam o status**
  Esperado: Revalidar some a partir de Agendado; Aprovar/Negar só em
  "Aguardando aprovação"; Cancelar some em Negado/Pago/Cancelado.

- [ ] **BOL-24 — Boleto vencido exige o aceite de execução imediata**
  Passos: aprove um boleto cuja data de vencimento já passou.
  Esperado: a folha de aprovação mostra a caixa avisando que o pagamento
  sai **imediatamente** (sem as 24h de antecedência); **Autorizar** fica
  desabilitado até marcá-la. Sem a caixa o servidor recusa (`BLP.BIL35`).

- [ ] **BOL-25 — A folha mostra quando o pagamento sai de verdade**
  Passos: abra **Aprovar…** num boleto com vencimento futuro; troque a
  data para uma véspera de feriado bancário ou fim de semana.
  Esperado: abaixo do seletor de data aparece "Pagamento será executado
  em \<data\>", com o sufixo "(deslizou do dia pedido)" quando a política
  (24h + dia útil) empurrar a execução. A linha é **informativa**: se a
  prévia falhar (rede), nada aparece e o Autorizar continua funcionando
  exatamente como antes.

- [ ] **BOL-26 — Recusa `BLP.BIL35` revela a caixa sem perder a folha**
  Passos: perto da virada do dia (após ~21h), aprove um boleto que vence
  **hoje** — o relógio da tela pode ainda não o considerar vencido.
  Esperado: se o servidor recusar com `BLP.BIL35`, a folha **não fecha**:
  aparece o aviso em vermelho "O servidor considera este boleto vencido…"
  e a caixa de aceite; marcando-a, o **Autorizar** reenvia com o aceite e
  o formulário (data, observação) permanece intacto.

---

## 7b. Execução do pagamento — a seção da ordem (fase 3)

Depois da aprovação, o detalhe ganha a seção **Execução do pagamento**.
A ordem nasce pelo outbox do servidor, então logo após aprovar pode haver
uma janela curta sem ordem.

- [ ] **EXE-01 — Janela do outbox**
  Passos: aprove e abra o detalhe imediatamente.
  Esperado: a seção diz "Agendamento em processamento…" (nunca erro);
  recarregando em seguida, a ordem aparece.

- [ ] **EXE-02 — Conteúdo da ordem**
  Esperado: status traduzido, retenção quando houver, data pedida × data
  efetiva (com "(deslizou)" quando a política de 24h/janela 9h–17h moveu o
  dia), valor, taxa e "Pago em" quando pago; falhas listadas com o último
  erro.

- [ ] **EXE-03 — Cancelar agendamento**
  Passos: em ordem Pendente/Em processamento → **Cancelar agendamento** →
  confirmar no diálogo.
  Esperado: snackbar "Agendamento cancelado."; o boleto volta a
  aprovável. O botão só existe na janela de reação (some em
  Pago/Falhou/Cancelado) e exige `bill:cancel`.

- [ ] **EXE-04 — Confirmar pagamento imediato**
  Passos: com ordem retida em "Aguardando confirmação" (boleto vencido na
  hora de agendar) → **Confirmar pagamento imediato** → confirmar.
  Esperado: snackbar dizendo que a fila retoma; a retenção some. Exige
  `bill:approve`.

- [ ] **EXE-05 — Reabrir boleto falhado**
  Passos: em boleto **Falhou** → **Reabrir para nova tentativa** →
  confirmar.
  Esperado: o boleto volta para "Aguardando aprovação" (nova aprovação,
  nova ordem). O botão só existe em Falhou.

- [ ] **EXE-06 — Comprovante**
  Passos: em boleto **Pago** → **Ver comprovante**.
  Esperado: abre a rota `/bills/{id}/receipt` em tela cheia com o arquivo
  vindo do storage. Antes de o comprovante existir, a mensagem é de regra
  ("ainda não tem comprovante"), não de erro de rede.

---

## 8. Lista de boletos — `/bill-payment/bills`

- [ ] **BOL-30 — Abre filtrada**
  Esperado: entra já em **Aguardando aprovação** — é a fila de trabalho.

- [ ] **BOL-31 — Filtros**
  Passos: percorra Aguardando aprovação, Rejeitados, Aprovados,
  **Agendados, Pagos, Falhou**, Negados, Cancelados, Todos.
  Esperado: a lista recarrega a cada troca; filtro sem resultado mostra
  "Nenhum boleto neste estado."

- [ ] **BOL-32 — Conteúdo da linha**
  Esperado: valor formatado, "Vence em <data>", banco quando houver, e três
  badges: status, trilho e tipo. Ícone de QR para Pix, de recibo para
  boleto. Boleto com data efetiva de pagamento acrescenta
  "· pagar em <data>".

- [ ] **BOL-33 — Rolagem infinita**
  Esperado: com mais de uma página, carrega ao chegar perto do fim.

- [ ] **BOL-34 — FAB Importar**
  Esperado: só aparece para quem tem `bill:import`.

---

## 9. Quarentena e captura

> Depende de `Capture.Enabled = true`, `Graph.Enabled = true`,
> `Secrets.MasterKey` e `Storage.*` configurados. Sem isso, teste apenas os
> estados vazios.

### 9.1 Fontes de captura — `/bill-payment/capture-sources`

- [ ] **CAP-01 — Estado vazio**
  Esperado: "Nenhuma caixa conectada..." e FAB **Conectar caixa**.

- [ ] **CAP-02 — Passo 1 do assistente**
  Esperado: as 5 instruções do Entra ID, incluindo o alerta da *Application
  Access Policy*, e o aviso de que a credencial fica cifrada no cofre e
  nunca volta pela API.

- [ ] **CAP-03 — Campos obrigatórios**
  Passos: Conectar com os campos em branco.
  Esperado: "Campo obrigatório." em nome, endereço, Directory ID,
  Application ID e secret. **Pasta é opcional** (vazio = caixa de entrada).

- [ ] **CAP-04 — Credencial inválida**
  Passos: informe IDs/secret falsos.
  Esperado: a conexão **falha** com a mensagem do servidor — a prova de
  acesso precisa passar para a fonte existir.

- [ ] **CAP-05 — Conexão válida**
  Esperado: vai para o detalhe da fonte, credencial "Guardada no cofre".

- [ ] **CAP-06 — Caixa compartilhada**
  Passos: conecte, em outro cliente, uma caixa já monitorada.
  Esperado: snackbar "Esta caixa já é monitorada por outra conta." — **sem
  dizer quem**. Cada cliente tem cursor e credencial próprios.

- [ ] **CAP-07 — Pastas monitoradas**
  Passos: adicione uma pasta; remova outra.
  Esperado: o campo limpa após adicionar; a remoção só é oferecida quando
  existe mais de uma pasta.

- [ ] **CAP-08 — Sincronizar agora**
  Esperado: snackbar "N novos, M já conhecidos."; ou, se o provedor recusar,
  o rótulo do desfecho: "Acesso negado", "Releitura necessária" ou
  "Indisponível".

- [ ] **CAP-09 — Reler caixa inteira**
  Passos: **Reler caixa inteira** → confirmar.
  Esperado: diálogo explicando que nada duplica; snackbar "N cursores
  descartados...".

- [ ] **CAP-10 — Desativar / Reativar**
  Esperado: badge alterna Ativa ↔ Desativada; desativada não é varrida.

- [ ] **CAP-11 — Substituir credencial**
  Esperado: aceita os três campos novos; a fonte volta a sincronizar.

- [ ] **CAP-12 — Desconectar**
  Esperado: diálogo avisando que reconectar exige digitar a credencial de
  novo; ao confirmar, volta para a lista sem a fonte.

- [ ] **CAP-13 — Erro na última sincronização**
  Passos: quebre a credencial de propósito e sincronize.
  Esperado: na lista, badge **"Falha na última sincronização"**; no
  detalhe, a mensagem no lugar da data.

### 9.2 Itens em quarentena — `/bill-payment/capture-items`

- [ ] **QUA-01 — Abre na fila de reivindicação**
  Esperado: o filtro **Aguardando reivindicação** já vem selecionado.

- [ ] **QUA-02 — Filtros**
  Esperado: Aguardando reivindicação, Não reconhecidos, Protegidos por
  senha, Download falhou, Todos. Vazio → "Nada aqui — a fila está limpa."

- [ ] **QUA-03 — Linha do item**
  Esperado: assunto (ou "(sem assunto)"), remetente + data, badge de status
  e, quando houver, o método de leitura (Texto do PDF, Código na imagem,
  Leitura por IA, Manual, Corpo do e-mail).

- [ ] **QUA-04 — Detalhe**
  Esperado: seções **Mensagem** e **Desfecho**; quando o PDF foi aberto por
  senha derivada, a linha "Aberto pela senha derivada de: ...".

- [ ] **QUA-05 — Reivindicar (só "Aguardando reivindicação")**
  Passos: abra um item nesse status → Reivindicar → confirmar.
  Esperado: o diálogo explica que o documento passa a ser deste cliente; ao
  confirmar, **navega para o boleto criado**, com o check **Roteamento**
  indicando reivindicação.

- [ ] **QUA-06 — Reprocessar (Não reconhecido / Protegido / Download falhou)**
  Esperado: o diálogo avisa que o degrau de visão consome a cota diária; ao
  confirmar, snackbar "Item devolvido à fila — o processamento roda em
  segundo plano." e o item recarrega.

- [ ] **QUA-07 — Item terminal não oferece ação**
  Passos: abra um item **De outro pagador**, **Descartado** ou **Virou
  boleto**.
  Esperado: sem Reivindicar e sem Reprocessar. Em "Virou boleto" aparece
  **Abrir o boleto deste item**.

- [ ] **QUA-08 — Item de outro pagador não é reivindicável**
  Esperado: o botão **não** existe — é terminal por decisão de projeto.

---

## 10. Painel de contas — `/bill-payment/pending`

- [ ] **PAN-01 — Aviso de perfil ausente**
  Passos: abra o painel com o Perfil do Pagador ainda não cadastrado.
  Esperado: card destacado "Configure o perfil do pagador" com o botão
  **Configurar agora** levando à tela do perfil. Depois de cadastrado, o
  card some.

- [ ] **PAN-02 — Fila de aprovação**
  Esperado: "N boleto(s) esperando decisão."; **Ver fila** abre a lista de
  boletos. Com mais de uma página, o número vem com **`+`** — é um piso,
  não o total.

- [ ] **PAN-03 — Não chegaram**
  Esperado: lista com competência, vencimento previsto e o motivo; tocar
  abre a expectativa.

- [ ] **PAN-04 — Chegaram com problema**
  Esperado: tocar leva ao **item da quarentena** que travou o ciclo (e não
  à expectativa), quando existe item associado.

- [ ] **PAN-05 — Vencem em breve**
  Esperado: aparece com o selo de nível (Aviso / Atenção / Urgente /
  Vencido) e não pede ação.

- [ ] **PAN-06 — Tudo em dia**
  Esperado: sem pendências e sem fila, mostra "Nenhuma pendência — tudo em
  dia."

- [ ] **PAN-07 — Atualizar**
  Esperado: o botão de recarregar no topo refaz as três consultas.

---

## 11. Permissões — a matriz

Repita o roteiro-resumo abaixo com **cada um dos quatro usuários**. O que
importa é o que **não** aparece.

| Ação | viewer | operator | approver | admin |
|---|:--:|:--:|:--:|:--:|
| Ver todas as listas | ✅ | ✅ | ✅ | ✅ |
| FAB Importar boleto | ❌ | ✅ | ❌ | ✅ |
| Revalidar | ❌ | ✅ | ❌ | ✅ |
| Aprovar / Negar / Cancelar | ❌ | ❌ | ✅ | ✅ |
| Cadastrar/editar beneficiário, origem, expectativa | ❌ | ✅ | ❌ | ✅ |
| Conectar/gerenciar caixa | ❌ | ✅ | ❌ | ✅ |
| Sincronizar / Reler caixa | ❌ | ✅ | ❌ | ✅ |
| Reivindicar / Reprocessar item | ❌ | ✅ | ❌ | ✅ |
| Dispensar ciclo de expectativa | ❌ | ❌ | ✅ | ✅ |

- [ ] **PERM-01 — Viewer**
  Esperado: nenhum FAB, nenhum botão de ação, chips sem `x`, menu de ações
  das origens ausente. As telas continuam legíveis.

- [ ] **PERM-02 — Operator não decide**
  Esperado: no detalhe do boleto aparece **Revalidar**, mas **não** aparecem
  Aprovar, Negar nem Cancelar.

- [ ] **PERM-03 — Approver não cadastra**
  Esperado: decide boleto e dispensa ciclo, mas não vê FAB de cadastro nem
  ações de gestão de caixa.

- [ ] **PERM-04 — Rota protegida por URL (web)**
  Passos: como approver, digite `/bill-payment/payees/create` na barra de
  endereço.
  Esperado: é redirecionado para `/bill-payment/payees` — o guarda de rota
  não depende de o botão estar escondido.

- [ ] **PERM-05 — Tenant alheio (IDOR)**
  Passos: troque o `tenantId` na URL de qualquer chamada por outro GUID.
  Esperado: 403 do servidor e "Você não tem permissão para esta ação." na
  tela. Nenhum dado do outro cliente aparece.

---

## 12. Transversais

- [ ] **TRV-01 — Sessão expirada**
  Passos: deixe o app parado além da validade do access token e execute uma
  ação.
  Esperado: "Sua sessão expirou. Entre novamente." e o fluxo de
  reautenticação.

- [ ] **TRV-02 — API fora do ar**
  Passos: derrube o BillPayment.API e abra qualquer lista.
  Esperado: painel de erro com **Tentar novamente**, e o botão realmente
  refaz a chamada.

- [ ] **TRV-03 — Mensagem do domínio tem precedência**
  Esperado: quando o servidor recusa por regra de negócio, a tela mostra o
  texto dele (em português), não o texto genérico da tela.

- [ ] **TRV-04 — Ação em voo não duplica**
  Passos: em qualquer botão de ação (aprovar, sincronizar, adicionar chip),
  clique duas vezes rápido.
  Esperado: o botão desabilita enquanto a chamada está em voo — uma
  operação só.

- [ ] **TRV-05 — Layout responsivo**
  Passos: rode em celular, tablet e navegador largo.
  Esperado: as listas usam largura de desktop, os formulários e detalhes
  ficam centralizados na largura de tablet; nada corta na horizontal.

---

## 13. Fluxos ponta a ponta

- [ ] **E2E-01 — Do zero até aprovar (sem e-mail)**
  1. Cadastre o Perfil do Pagador com o CNPJ da empresa de teste.
  2. Cadastre o beneficiário do boleto, com política de valor coerente e o
     banco emissor entre os aceitos.
  3. Importe a linha digitável.
  4. Confira que os checks rodaram e nenhum bloqueante falhou.
  5. Aprove agendando para a data mínima oferecida.
  Esperado: status **Aprovado**, seção Decisão preenchida, o boleto sai da
  fila "Aguardando aprovação" e aparece em "Aprovados".
  *(Precisa da chave Asaas configurada — veja 0.4.)*

- [ ] **E2E-02 — Captura por e-mail até a decisão**
  1. Conecte a caixa que recebe boletos.
  2. Cadastre o domínio do fornecedor como origem **Confiável**.
  3. Envie/aguarde um e-mail com boleto em anexo e sincronize.
  4. Acompanhe: item na quarentena → roteado → vira boleto.
  5. Decida no detalhe do boleto.
  Esperado: o check **Origem** reconhece o remetente; o **Roteamento**
  registra como o boleto chegou a este cliente.

- [ ] **E2E-03 — A rede de segurança**
  1. Cadastre uma expectativa mensal para um beneficiário.
  2. Deixe passar a data de aviso sem que a conta chegue.
  3. Abra o Painel de contas.
  Esperado: a expectativa aparece em **Não chegaram** com o motivo; ao
  importar a conta à mão, o ciclo passa a **Recebido**.

- [ ] **E2E-04 — Item sem dono**
  1. Provoque um item **Aguardando reivindicação** (boleto sem pagador
     identificável).
  2. Reivindique.
  3. Decida o boleto criado.
  Esperado: o boleto nasce com roteamento **Reivindicada** e segue o fluxo
  normal de verificação.

---

## 14. Registro de defeitos

| # | Caso | O que aconteceu | Esperado | Ambiente | Severidade |
|---|---|---|---|---|---|
|  |  |  |  |  |  |

Severidade sugerida: **Crítica** (perda de dinheiro, vazamento de documento
ou acesso a outro cliente) · **Alta** (fluxo bloqueado sem contorno) ·
**Média** (contorno existe) · **Baixa** (cosmético).
