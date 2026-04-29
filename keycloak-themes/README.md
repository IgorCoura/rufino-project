# Rufino — Keycloak Themes

Temas customizados do Keycloak para combinar com a identidade visual do app Rufino.

```
rufino/
├── login/      → Telas de login, registro, esqueci a senha, OTP, termos, etc.
├── account/    → Account Console v3 (autoatendimento do usuário)
├── email/      → E-mails enviados pelo Keycloak (verificação, reset, etc.)
├── admin/      → Console administrativo
└── welcome/    → Página inicial em /
```

Cor primária: **`#00695C`** (mesma `AppColors.seed` em `lib/core/theme/app_colors.dart`).
Os CSS sobrescrevem **apenas cores e backgrounds** — layout, espaçamentos e tipografia continuam idênticos ao Keycloak padrão.

---

## Convenções

| Variável | Significa |
|---|---|
| `<USER>` | usuário SSH do VPS Hostinger (geralmente `root` ou um usuário com sudo) |
| `<IP>` | IP público do VPS (ex.: `203.0.113.45`) |
| `<KC>` | nome ou ID do container Keycloak no Dokploy (ex.: `dokploy-keycloak-7f3a`) |
| `<HOST>` | domínio do Keycloak em produção (ex.: `keycloak.couratechsafety.cloud`) |

> **UID 1000** — o container `keycloak/keycloak:26.3.1` roda como UID `1000`. Os arquivos no host precisam ser legíveis por esse UID, senão o tema não aparece no dropdown do Admin Console.

---

## 1. Antes de subir — opcional: adicione um logo

Se quiser substituir o nome textual "Rufino" por uma imagem, coloque o arquivo em:

```
rufino/login/resources/img/logo.png
```

Sem logo, o título textual padrão é exibido. (Como removemos overrides de layout, o tamanho e posição do logo seguem o padrão do `keycloak.v2`.)

---

## 2. Conectar no VPS via SSH

Da sua máquina local (Windows / macOS / Linux):

```bash
# Login interativo
ssh <USER>@<IP>

# Ex.: ssh root@203.0.113.45
```

Se SSH pedir senha toda vez, configure chave pública (recomendado):

```bash
# Local — gera chave (uma vez, se ainda não tem)
ssh-keygen -t ed25519 -C "rufino-deploy"

# Local — copia chave pública para o VPS
ssh-copy-id <USER>@<IP>

# Próximos logins entram sem senha:
ssh <USER>@<IP>
```

> No Windows, use Git Bash, WSL ou PowerShell 7+ — todos têm `ssh`/`scp`/`ssh-keygen` nativos.

---

## 3. Primeiro deploy — configurar do zero

Faça **uma única vez**, da sua máquina local:

```bash
# 3.1. Crie o diretório destino no VPS (precisa de sudo se for fora do home)
ssh <USER>@<IP> "sudo mkdir -p /etc/keycloak-themes"

# 3.2. Copie a pasta `rufino/` para o VPS
scp -r keycloak-themes/rufino <USER>@<IP>:/tmp/rufino-upload

# 3.3. Mova para o destino final com sudo (caso não tenha permissão direta)
ssh <USER>@<IP> "sudo mv /tmp/rufino-upload /etc/keycloak-themes/rufino"

# 3.4. Ajuste ownership e permissões para o UID 1000 do Keycloak
ssh <USER>@<IP> "sudo chown -R 1000:1000 /etc/keycloak-themes/rufino && sudo chmod -R u=rX,g=rX,o=rX /etc/keycloak-themes/rufino"
```

> O `chmod` usa **`X` maiúsculo** de propósito: significa "execute apenas em diretórios". Sem isso ninguém consegue *entrar* nas pastas, mesmo podendo ler os arquivos.

### 3.5 — Monte o volume no Dokploy

No painel do Dokploy:

1. Abra a aplicação **Keycloak**.
2. Aba **Advanced** → seção **Volumes** (ou **Mounts**).
3. **Add Volume / Bind Mount**:

   | Campo | Valor |
   |-------|-------|
   | **Type** | `Bind Mount` |
   | **Host Path** | `/etc/keycloak-themes/rufino` |
   | **Container Path** | `/opt/keycloak/themes/rufino` |
   | **Read Only** | ✅ marcar |

4. **Save** + **Redeploy** o container.

> Equivalente em `docker-compose.yml`:
> ```yaml
> services:
>   keycloak:
>     image: keycloak/keycloak:26.3.1
>     volumes:
>       - /etc/keycloak-themes/rufino:/opt/keycloak/themes/rufino:ro
> ```

### 3.6 — Confirme que o container vê os arquivos

```bash
# Descobre o nome/ID do container
ssh <USER>@<IP> "docker ps --filter 'ancestor=keycloak/keycloak:26.3.1' --format '{{.Names}}'"

# Lista os temas como o Keycloak vê
ssh <USER>@<IP> "docker exec <KC> ls -la /opt/keycloak/themes/rufino"

# Lê um arquivo concreto (deve imprimir o conteúdo, NÃO 'Permission denied')
ssh <USER>@<IP> "docker exec <KC> cat /opt/keycloak/themes/rufino/login/theme.properties"
```

Se algum desses falhar com `Permission denied`, repita o passo 3.4.

### 3.7 — (Opcional) Desligue o cache durante a customização ativa

Para iterar nas cores sem reiniciar o container a cada CSS, no **painel Dokploy → Environment Variables**, adicione:

```
KC_SPI_THEME_CACHE_THEMES=false
KC_SPI_THEME_CACHE_TEMPLATES=false
KC_SPI_THEME_STATIC_MAX_AGE=-1
```

**Save → Redeploy**. Quando o tema estiver finalizado, **remova as três variáveis e redeploy** para reativar o cache em produção (performance).

### 3.8 — Ative o tema no realm

No Admin Console (`https://<HOST>/admin`):

1. Selecione o realm **rufino** (canto superior esquerdo).
2. **Realm settings** → aba **Themes**:

   | Campo | Valor |
   |-------|-------|
   | **Login theme** | `rufino` |
   | **Account theme** | `rufino` |
   | **Admin theme** | `rufino` *(opcional — afeta apenas este realm)* |
   | **Email theme** | `rufino` |

3. **Save**.
4. Aba **Localization** → **Internationalization Enabled = On** → **Supported locales** = adicione `pt-BR` (e `en` se quiser bilíngue) → **Default locale** = `pt-BR` → **Save**.

### 3.9 — Validação

| Tela | URL |
|---|---|
| Login | `https://<HOST>/realms/rufino/account/` (clique "Sign in") |
| Esqueci senha | login → "Esqueci minha senha" |
| Account Console | `https://<HOST>/realms/rufino/account/` (já logado) |
| Admin Console | `https://<HOST>/admin/rufino/console/` |
| Welcome | `https://<HOST>/` |

**Sempre use Ctrl+Shift+R** (hard reload) ou janela anônima para evitar cache do navegador mascarando as mudanças.

---

## 4. Atualizar o tema (depois do primeiro deploy)

Sequência completa para qualquer mudança em CSS, `theme.properties` ou `messages_*.properties`:

```bash
# 4.1. Sincroniza arquivos novos no VPS
#      (--delete remove arquivos que você apagou localmente)
rsync -avz --delete -e ssh \
  keycloak-themes/rufino/ \
  <USER>@<IP>:/etc/keycloak-themes/rufino/

# Alternativa sem rsync (sobrescreve mas não remove arquivos antigos):
# scp -r keycloak-themes/rufino/* <USER>@<IP>:/etc/keycloak-themes/rufino/

# 4.2. Reaplica permissões (necessário sempre que cria arquivos novos)
ssh <USER>@<IP> "sudo chown -R 1000:1000 /etc/keycloak-themes/rufino && sudo chmod -R u=rX,g=rX,o=rX /etc/keycloak-themes/rufino"

# 4.3. Restart do container — OBRIGATÓRIO se cache de tema está LIGADO
ssh <USER>@<IP> "docker restart <KC>"

# 4.4. Acompanhe o restart
ssh <USER>@<IP> "docker logs --tail 50 -f <KC>"
# Ctrl+C quando ver "Listening on: http://0.0.0.0:8080"
```

> **Se o cache está DESLIGADO** (passo 3.7 aplicado), pule o **4.3**. Basta editar o arquivo, sincronizar e dar **Ctrl+Shift+R** no navegador. Edição de `messages_pt_BR.properties` ainda assim costuma exigir restart porque o bundle de mensagens é cacheado em outro nível.

### 4.5 — Atalho: script único

Salve como `deploy-theme.sh` na sua máquina local:

```bash
#!/usr/bin/env bash
set -euo pipefail

USER="root"                                  # ajuste
IP="203.0.113.45"                            # ajuste
KC="dokploy-keycloak-7f3a"                   # ajuste
LOCAL_DIR="$(dirname "$0")/keycloak-themes/rufino"
REMOTE_DIR="/etc/keycloak-themes/rufino"

echo "→ Sincronizando arquivos…"
rsync -avz --delete -e ssh "$LOCAL_DIR/" "$USER@$IP:$REMOTE_DIR/"

echo "→ Ajustando permissões (UID 1000)…"
ssh "$USER@$IP" "sudo chown -R 1000:1000 $REMOTE_DIR && sudo chmod -R u=rX,g=rX,o=rX $REMOTE_DIR"

echo "→ Reiniciando container Keycloak…"
ssh "$USER@$IP" "docker restart $KC"

echo "→ Aguardando Keycloak voltar (até 60s)…"
ssh "$USER@$IP" "timeout 60 bash -c 'until docker logs --tail 5 $KC 2>&1 | grep -q \"Listening on:\"; do sleep 2; done'"

echo "✓ Pronto. Faça Ctrl+Shift+R no navegador."
```

Torne executável e use:

```bash
chmod +x deploy-theme.sh
./deploy-theme.sh
```

---

## 5. Diagnóstico rápido

Se algo não funcionar, rode estes comandos e analise:

```bash
# 5.1. O Dokploy está com o bind mount certo?
ssh <USER>@<IP> "docker inspect <KC> --format '{{range .Mounts}}{{.Source}} -> {{.Destination}} ({{.Mode}}){{println}}{{end}}'"
# Deve aparecer: /etc/keycloak-themes/rufino -> /opt/keycloak/themes/rufino (ro)

# 5.2. Os arquivos têm o ownership correto?
ssh <USER>@<IP> "ls -la /etc/keycloak-themes/rufino"
# Coluna user:group deve ser '1000 1000', modo 'drwxr-xr-x'

# 5.3. O container consegue ler de fato?
ssh <USER>@<IP> "docker exec <KC> cat /opt/keycloak/themes/rufino/login/theme.properties"
# Tem que imprimir o conteúdo, sem 'Permission denied'

# 5.4. O Keycloak detectou o tema sem erro de parse?
ssh <USER>@<IP> "docker logs <KC> 2>&1 | grep -iE 'theme|rufino|error' | tail -30"

# 5.5. Os 5 theme.properties estão lá?
ssh <USER>@<IP> "docker exec <KC> find /opt/keycloak/themes/rufino -name theme.properties"
# Tem que retornar 5 caminhos (login/account/email/admin/welcome)
```

---

## 6. Voltar para o tema padrão (rollback)

No Admin Console → **Realm settings → Themes** → escolha `keycloak.v2` (ou deixe em branco) em todos os campos → **Save**. Não precisa mexer nos arquivos do volume.

Para remover totalmente:

```bash
# Desfaz o bind mount no Dokploy → Volumes → remove a entrada → Redeploy.
# Depois, opcionalmente:
ssh <USER>@<IP> "sudo rm -rf /etc/keycloak-themes/rufino"
```

---

## 7. Estrutura interna

| Arquivo | Função |
|---------|--------|
| `theme.properties` | Aponta o `parent`, o(s) stylesheet(s) e os locales suportados. **Atenção**: `styles=` SUBSTITUI a lista do parent — para login/welcome precisamos re-listar o stylesheet base antes do nosso `override.css`. |
| `resources/css/override.css` | Sobrescreve apenas as variáveis CSS PatternFly v5 (`--pf-v5-global--*`) — colors e backgrounds, nada de layout. |
| `resources/img/*` | Imagens (logo, ícones). |
| `messages/messages_pt_BR.properties` | Traduções pt-BR. Sobrescrevem o bundle padrão. |

> **Cascade de assets**: o Keycloak procura no tema atual primeiro, e se não achar sobe pelo `parent`. Por isso só precisamos colocar o que muda.

---

## 8. Cuidados

- **PatternFly v5** é o que Keycloak 26.x usa (`--pf-v5-*`). Se em uma futura versão o Red Hat migrar para PF6 (`--pf-v6-*`), os overrides aqui deixarão de pegar e precisarão ser revistos.
- **`styles=` substitui, não acrescenta.** No login e welcome, sempre re-liste o stylesheet do parent (`css/styles.css`/`css/welcome.css`) antes do `override.css`. No account/admin (SPAs), basta o `override.css` porque o bundle é injetado por outro mecanismo.
- **Não edite `.ftl` aqui.** Mudar HTML/template significa copiar do JAR do Keycloak (`/opt/keycloak/lib/lib/main/org.keycloak.keycloak-themes-26.3.1.jar`) e mantê-los manualmente a cada upgrade do Keycloak.
- **Cache do navegador** pode mascarar mudanças mesmo com cache de tema desligado no Keycloak. Use Ctrl+Shift+R ou janela anônima para validar.

---

## 9. Referências

- Keycloak 26.3 Server Developer Guide → Themes: https://www.keycloak.org/docs/26.3.1/server_development/#_themes
- PatternFly v5 design tokens: https://www.patternfly.org/v5/tokens
- Account Console v3: https://www.keycloak.org/docs/26.3.1/server_admin/#_account-service
