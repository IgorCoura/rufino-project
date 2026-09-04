# ADR-009 — Segredos em variáveis de ambiente; User Secrets nos testes

**Status:** Aceito · **Data:** 2026-07-31 · **Revisado:** 2026-07-31 (decisão do usuário: sem cofre por enquanto)

## Contexto

O BC guarda segredos de duas naturezas:

| | Segredo de **infraestrutura** | Segredo **por tenant** |
|---|---|---|
| Exemplos | senha do Postgres, chave da conta-plataforma Asaas, token do webhook, chave da API Claude, KEK | refresh token OAuth de cada caixa, chave da subconta Asaas de cada tenant, credencial de portal, senha de PDF aprendida |
| Quantidade | dezenas, fixas | centenas, cresce com o cliente |
| Quando muda | em deploy | em runtime, sem deploy |

Ambiente: **Dokploy** em servidor próprio, só software open source, custo perto de zero.

## Decisão

**Sem cofre dedicado por enquanto.** Duas camadas, ambas sem serviço novo:

### Camada 1 — segredos de infraestrutura: variáveis de ambiente

Injetadas pelo Dokploy (produção) e por `dotnet user-secrets` / `secrets.json` (desenvolvimento e testes). Nada em `appsettings.json`, nada versionado.

```powershell
# uma vez por projeto
dotnet user-secrets init --project BillPayment.API
dotnet user-secrets set "Asaas:PlatformApiKey" "<chave>" --project BillPayment.API
dotnet user-secrets set "Secrets:MasterKey" "<base64 de 32 bytes>" --project BillPayment.API
```

`secrets.json` vive fora do repositório (em `%APPDATA%\Microsoft\UserSecrets\<id>\` no Windows) e é lido automaticamente pelo `IConfiguration` em `Development`. Os testes de integração leem do mesmo lugar via `IntegrationTestWebAppFactory`.

### Camada 2 — segredos por tenant: envelope encryption no próprio Postgres

Continuam **cifrados na tabela `tenant_secrets`**, exatamente como antes:

- DEK aleatório de 256 bits por segredo; payload cifrado com `AES-256-GCM` (`System.Security.Cryptography.AesGcm`, built-in do .NET); nonce de 96 bits por operação, nunca reutilizado; AAD carrega `TenantId` + tipo do segredo.
- O DEK é envelopado pela **master key**, que vem da camada 1 (variável de ambiente) e fica só em memória.
- `kek_version` na linha desde o primeiro dia, mesmo com uma versão só.

`ISecretVault` continua sendo a porta; muda apenas **de onde a master key vem**. O Domain segue vendo só `CredentialRef`.

## Razões

- **Cofre não é banco de credencial de cliente.** Mesmo com um cofre disponível, tokens OAuth e chaves de subconta por tenant pertencem ao banco cifrado — cofres são feitos para configuração de aplicação, não para milhares de segredos criados em runtime. Ou seja: **a camada 2 não muda quando o cofre entrar**, e é onde mora o risco real.
- **A camada 1 é a única que muda**, e muda em uma linha de configuração. Adiar o cofre não cria dívida estrutural, só operacional.
- **Zero serviço novo** no Dokploy — menos coisa para manter, atualizar e fazer backup numa fase que ainda não movimenta dinheiro.
- `dotnet user-secrets` é o mecanismo nativo do .NET para exatamente este caso e já é o que os testes esperam.

## Consequências — e o que isso custa

O que se perde em relação a um cofre, explicitamente:

- **Sem trilha de auditoria** de quem leu qual segredo.
- **Sem rotação versionada** da master key — trocar exige deploy coordenado e re-envelopar os DEKs.
- **O segredo é visível para qualquer pessoa com acesso ao painel do Dokploy.**
- **Backup manual.** Perder a master key é perder todos os tokens OAuth e chaves de subconta — recuperável (reconectar caixas, reemitir chaves), mas é incidente com todos os clientes. **Guarde uma cópia cifrada fora do host** (`age` ou `gpg` num pendrive/cofre físico serve). Isso não é opcional.

> ⚠️ **Fato descoberto depois desta decisão:** a consulta oficial do Asaas (boleto e Pix) exige chave com **permissão de saque** ([`ADR-001`](ADR-001-asaas-como-provedor.md) → "Achado de campo"). Não existe fase com credencial inofensiva — desde a Fase 1 o segredo guardado é capaz de pagar contas. Isso encurta a distância até o gatilho abaixo e torna a whitelist de IP no Asaas uma mitigação obrigatória, não opcional.

**Gatilho para reabrir este ADR:** o primeiro cliente externo com dinheiro real no sistema. Até lá é dívida aceitável; a partir daí é exposição de terceiros. Quando reabrir, a decisão anterior continua válida — **Infisical self-hosted** (MIT, Docker Compose no Dokploy, gratuito, identidades de máquina, SDK .NET) guardando a master key, escolhido sobre OpenBao por causa do *unseal*, que sem KMS externo exige intervenção manual a cada restart. HashiCorp Vault segue descartado (BUSL-1.1, não é open source desde 2023).

### Regras que valem já

- **Log nunca imprime segredo, `CredentialRef` resolvido, senha de PDF ou master key.** Redação explícita no logging estruturado; não confiar em disciplina.
- Tabela `tenant_secrets` é infraestrutura, mapeada na Infra, sem Aggregate.
- Teste unitário obrigatório: dois `Encrypt` do mesmo payload produzem ciphertexts diferentes (prova que o nonce não é fixo).
- **Nenhum segredo entra no repositório.** Inclui `appsettings.Development.json` — ele é versionado.
