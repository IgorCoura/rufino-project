# ADR-006 — Captura de e-mail: só Microsoft Graph; Gmail entra por encaminhamento

**Status:** Aceito · **Data:** 2026-07-31 · **Revisado:** 2026-07-31 (Gmail é conta pessoal)

## Contexto

As caixas em uso são **um tenant Microsoft 365 próprio** (com admin) e **uma conta Gmail pessoal** (`@gmail.com`, sem Workspace). IMAP funcionaria nos dois com um adapter só.

O Gmail pessoal muda o cálculo. O escopo `gmail.readonly` é *restricted*: sem Google Workspace não existe app "Internal" nem domain-wide delegation, então o único caminho OAuth de produção é verificação com **avaliação de segurança CASA** — semanas, custo e **renovação anual**, para monitorar uma caixa. Desproporcional.

## Decisão

**Um único adapter: Microsoft Graph.** As mensagens do Gmail chegam ao sistema por **encaminhamento automático do Gmail para a caixa do Microsoft 365**, configurado uma vez nas opções do Gmail.

Sincronização incremental por `deltaLink` guardado em `CaptureSource.SyncCursor`.

No Microsoft 365, preferência por **client credentials com `Mail.Read`**, restrito por **Application Access Policy** a um grupo de segurança com apenas as caixas monitoradas — sem essa política, `Mail.Read` alcança todas as caixas do tenant.

## Razões

- **Elimina uma integração inteira.** Sem adapter Gmail, sem segundo fluxo OAuth, sem `historyId` e sem o resync que ele exige ao expirar. Uma plataforma a menos para manter.
- **Elimina o CASA.** Nenhuma verificação, nenhum custo recorrente, nenhuma renovação anual.
- **O encaminhamento preserva o `From:` original**, então `OriginTrust` continua funcionando sobre o remetente verdadeiro, não sobre o Gmail.
- **Cai exatamente no cenário já modelado.** Uma caixa passa a receber contas de dois pagadores — pessoais vindas do Gmail, da empresa direto. É o caso de fonte compartilhada que a escada de roteamento ([`07-multitenancy-and-routing.md`](../07-multitenancy-and-routing.md)) foi desenhada para resolver.
- **Segredo.** No Graph, client credentials não guardam senha de usuário e a Application Access Policy limita o alcance. IMAP exigiria guardar credencial de acesso total à caixa.

## Consequências

- **Não existe sprint de adapter Gmail.** O roadmap perde a antiga 2.3.
- **Não há prazo de aprovação a proteger.** O registro no Entra ID é autosserviço (o usuário é admin do tenant) e leva minutos — faz-se na sprint 2.1, não antes.
- **O encaminhamento é passo de onboarding**, não de código: ligar no Gmail, confirmar o código que chega no M365, e **adicionar o endereço Gmail aos remetentes seguros** do M365 — encaminhamento quebra SPF/DKIM e a mensagem pode cair no lixo eletrônico.
- **Encaminhar tudo, não filtrar no Gmail.** Filtro no Gmail é frágil e o que ele descarta você descobre tarde. A triagem é do sistema.
- **`CaptureSourceKind.GmailMailbox` sai do modelo** por ora. Volta se aparecer um Google Workspace, onde app Internal ou service account com domain-wide delegation dispensam verificação.
- **Fallback sancionado, se o encaminhamento não servir**: IMAP com App Password (funciona em conta pessoal com 2FA). Custo aceito e explícito — guarda credencial de acesso total, não expira sozinha, revogada só na troca da senha principal. É pior que o encaminhamento em todos os eixos; só use se houver razão concreta para manter as caixas separadas.
- Se um dia aparecer cliente com provedor fora do Graph, a porta `IMailboxReader` já isola o problema: um adapter novo é aditivo.

## Razões

- **Segredo.** IMAP exige guardar senha ou app password, que não expira e dá acesso total à caixa. OAuth dá escopo restrito (só leitura de mensagens), é revogável pelo cliente e não guarda senha. Numa aplicação que também move dinheiro, isso não é detalhe.
- **Ambos os provedores estão desligando autenticação básica no IMAP** — o caminho "simples" já exige OAuth de qualquer forma, e sobra só a desvantagem do protocolo.
- **Sincronização incremental de verdade.** Delta query e `history.list` entregam "o que mudou desde X" nativamente. Em IMAP isso vira controle manual de UID por pasta, que quebra quando o usuário move ou arquiva mensagem.
- **Metadados melhores** — id estável de mensagem para idempotência, e possivelmente resultado de autenticação da mensagem (relevante para o ADR-005).

## Consequências

- **Dois adapters, dois fluxos OAuth, duas telas de consentimento.** Custo real, aceito.
- **Aprovação de app é caminho crítico**: consentimento de admin no tenant Microsoft e verificação do app no Google levam tempo fora do nosso controle. Os pedidos precisam ser abertos **na fase 1**, não quando a fase 2 começar.
- Preferência no Microsoft 365 por **client credentials com `Mail.Read` restrito por Application Access Policy** — sem sessão de usuário e sem refresh token expirando. No Google, authorization code + refresh token, ou service account com domain-wide delegation.
- **`historyId` do Gmail expira** se o cursor ficar velho. O adapter precisa detectar e cair para resync completo limitado por data, senão a caixa para de sincronizar em silêncio.
- Tokens no cofre (`ISecretVault`), referenciados por `CredentialRef`. O Domain nunca vê segredo — `BLP.CPS01`.
- Se aparecer cliente com provedor fora dos dois, a porta `IMailboxReader` já isola o problema: um adapter IMAP é aditivo, não é reescrita.
