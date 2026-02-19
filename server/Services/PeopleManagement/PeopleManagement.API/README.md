# People Management API

API de gerenciamento de pessoas desenvolvida em .NET 8 para o projeto Rufino. Esta aplicação fornece funcionalidades completas para gerenciamento de colaboradores, contratos e documentação digital com assinatura eletrônica.

## 📋 Índice

- [Sobre o Projeto](#sobre-o-projeto)
- [Tecnologias Utilizadas](#tecnologias-utilizadas)
- [Arquitetura](#arquitetura)
- [Pré-requisitos](#pré-requisitos)
- [Configuração do Ambiente](#configuração-do-ambiente)
- [Executando o Projeto](#executando-o-projeto)
- [Variáveis de Ambiente](#variáveis-de-ambiente)
- [Deploy](#deploy)
- [Endpoints](#endpoints)
- [Hangfire Dashboard](#hangfire-dashboard)
- [Testes](#testes)
- [Licença](#licença)

## 🎯 Sobre o Projeto

A **People Management API** é uma solução robusta para gerenciamento de pessoas e documentos, integrada com sistemas de autenticação (Keycloak), assinatura digital (ZapSign) e processamento em background (Hangfire). O projeto foi desenvolvido seguindo os princípios de Clean Architecture e Domain-Driven Design (DDD).

### Principais Funcionalidades

- ✅ Gerenciamento de colaboradores
- ✅ Controle de contratos e documentos
- ✅ Assinatura digital de documentos via ZapSign
- ✅ Autenticação e autorização via Keycloak
- ✅ Processamento de jobs em background com Hangfire
- ✅ Geração de documentos PDF
- ✅ Armazenamento de arquivos no Azure Blob Storage
- ✅ Notificações de expiração de documentos

## 🚀 Tecnologias Utilizadas

- **.NET 8** - Framework principal
- **C# 12.0** - Linguagem de programação
- **PostgreSQL** - Banco de dados relacional
- **Entity Framework Core** - ORM
- **Keycloak** - Autenticação e autorização
- **Hangfire** - Agendamento e execução de jobs em background
- **Azure Blob Storage** - Armazenamento de arquivos
- **ZapSign** - Plataforma de assinatura digital
- **PuppeteerSharp** - Geração de PDFs
- **MediatR** - Implementação de CQRS e mediator pattern
- **Docker** - Containerização
- **Azure Container Apps** - Hospedagem em produção

## 🏗️ Arquitetura

O projeto segue uma arquitetura em camadas baseada em Clean Architecture:

```
PeopleManagement/
├── PeopleManagement.API/          # Camada de apresentação (Controllers, Filters)
├── PeopleManagement.Application/   # Camada de aplicação (Commands, Queries, DTOs)
├── PeopleManagement.Domain/        # Camada de domínio (Entidades, Value Objects)
├── PeopleManagement.Infra/         # Camada de infraestrutura (Repositórios, Context)
├── PeopleManagement.Services/      # Serviços externos e integrações
├── PeopleManagement.UnitTests/     # Testes unitários
└── PeopleManagement.IntegrationTests/ # Testes de integração
```

## 📋 Pré-requisitos

Antes de começar, você precisa ter instalado em sua máquina:

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [PostgreSQL 15+](https://www.postgresql.org/download/)
- [Docker](https://www.docker.com/get-started) (opcional, para containerização)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) ou [VS Code](https://code.visualstudio.com/)

### Serviços Externos Necessários

- **Keycloak**: Servidor de autenticação configurado com o realm `rufino`
- **Azure Blob Storage**: Conta de storage para armazenamento de arquivos
- **ZapSign**: Conta e token de API para assinatura de documentos

## ⚙️ Configuração do Ambiente

### 1. Clone o Repositório

```bash
git clone https://github.com/IgorCoura/rufino-project.git
cd rufino-project/server/Services/PeopleManagement
```

### 2. Configure o Banco de Dados

Crie os bancos de dados no PostgreSQL:

```sql
CREATE DATABASE "PeopleManagementDb";
CREATE DATABASE "hangfiredb";
```

### 3. Configure o arquivo `appsettings.Development.json`

Crie ou edite o arquivo em `PeopleManagement.API/appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Information"
    }
  },
  "ConnectionStrings": {
    "BlobStorage": "DefaultEndpointsProtocol=https;AccountName=SEU_STORAGE;AccountKey=SUA_KEY;",
    "Postgresql": "Server=localhost;Port=5432;Database=PeopleManagementDb;UserId=seu_usuario;Password=sua_senha",
    "HangfireConnection": "Server=localhost;Port=5432;Database=hangfiredb;UserId=seu_usuario;Password=sua_senha"
  },
  "Keycloak": {
    "realm": "rufino",
    "AuthServerUrl": "https://seu-keycloak.com/",
    "SslRequired": "none",
    "Resource": "people-management-api",
    "VerifyTokenAudience": true,
    "Credentials": {
      "secret": "seu_client_secret"
    }
  },
  "ZapSign": {
    "URI": "https://api.zapsign.com.br/api/v1/",
    "Token": "seu_token_zapsign"
  },
  "DocumentTemplatesOptions": {
    "SourceDirectory": "app_files/templates",
    "MaxHoursWorkload": 8
  },
  "AuthorizationOptions": {
    "KeycloakUrl": "https://seu-keycloak.com/realms/rufino/protocol/openid-connect/token",
    "ClientId": "zapsign",
    "ClientSecret": "seu_client_secret"
  },
  "SignOptions": {
    "WeebHookUrl": "https://localhost:8041/api/v1/document/webhook"
  },
  "HangfireDashboard": {
    "Login": "admin",
    "Password": "sua_senha_segura"
  }
}
```

### 4. Execute as Migrations

```bash
cd PeopleManagement.API
dotnet ef database update --project ../PeopleManagement.Infra
```

## 🎮 Executando o Projeto

### Modo Desenvolvimento

#### Via .NET CLI

```bash
cd PeopleManagement.API
dotnet run
```

A API estará disponível em:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`
- Swagger: `https://localhost:5001/swagger`
- Hangfire Dashboard: `https://localhost:5001/hangfire`

#### Via Visual Studio

1. Abra a solution `PeopleManagement.sln`
2. Defina `PeopleManagement.API` como projeto de inicialização
3. Pressione `F5` ou clique em "Run"

### Modo Docker

#### Build da Imagem

```bash
cd server
docker build -t people-management-api:latest -f Services/PeopleManagement/PeopleManagement.API/Dockerfile .
```

#### Executar Container

```bash
docker run -d -p 8080:80 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e ConnectionStrings__Postgresql="Server=host.docker.internal;Port=5432;Database=PeopleManagementDb;UserId=admin;Password=senha" \
  -e ConnectionStrings__HangfireConnection="Server=host.docker.internal;Port=5432;Database=hangfiredb;UserId=admin;Password=senha" \
  people-management-api:latest
```

## 🌍 Variáveis de Ambiente

### Essenciais

| Variável | Descrição | Exemplo |
|----------|-----------|---------|
| `ASPNETCORE_ENVIRONMENT` | Ambiente de execução | `Development`, `Production` |
| `ConnectionStrings__Postgresql` | String de conexão do PostgreSQL | `Server=localhost;Port=5432;Database=PeopleManagementDb;...` |
| `ConnectionStrings__HangfireConnection` | String de conexão do Hangfire | `Server=localhost;Port=5432;Database=hangfiredb;...` |
| `ConnectionStrings__BlobStorage` | String de conexão do Azure Blob Storage | `DefaultEndpointsProtocol=https;AccountName=...` |
| `Keycloak__realm` | Realm do Keycloak | `rufino` |
| `Keycloak__AuthServerUrl` | URL do servidor Keycloak | `https://keycloak.example.com/` |
| `Keycloak__Resource` | Client ID no Keycloak | `people-management-api` |
| `Keycloak__Credentials__secret` | Client secret do Keycloak | `seu-secret` |
| `ZapSign__Token` | Token de API do ZapSign | `seu-token` |
| `HangfireDashboard__Login` | Login do dashboard Hangfire | `admin` |
| `HangfireDashboard__Password` | Senha do dashboard Hangfire | `senha-segura` |

### Opcionais

| Variável | Descrição | Padrão |
|----------|-----------|--------|
| `PUPPETEER_EXECUTABLE_PATH` | Caminho do executável do Chrome | `/usr/bin/google-chrome-unstable` |
| `DocumentTemplatesOptions__SourceDirectory` | Diretório dos templates | `app_files/templates` |
| `DocumentOptions__WarningDaysBeforeDocumentExpiration` | Dias antes de notificar expiração | `30` |

## 🚢 Deploy

### Processo de CI/CD

O deploy é automatizado via **GitHub Actions** e ocorre quando há push na branch `main` com alterações na pasta `server/Services/PeopleManagement/**`.

#### Fluxo de Deploy

1. **Build**: Construção da imagem Docker
2. **Push**: Envio para Magalu Cloud Container Registry
3. **Deploy**: Implantação no Azure Container Apps

#### Configuração Necessária no GitHub

**Secrets:**
- `MAGALU_CR_USERNAME`: Usuário do Container Registry
- `MAGALU_CR_PASSWORD`: Senha do Container Registry
- `AZURE_CREDENTIALS`: Credenciais do Azure no formato JSON
- `CONNECTION_BLOBSTORAGE`: String de conexão do Blob Storage
- `CONNECTION_POSTGRESQL`: String de conexão do PostgreSQL
- `CONNECTION_HANGFIRE`: String de conexão do Hangfire
- `KEYCLOAK_SECRET`: Secret do client Keycloak
- `ZAPSIGN_TOKEN`: Token da API ZapSign
- `AUTHORIZATION_CLIENTSECRET`: Client secret para autorização
- `HANGFIRE_PASSWORD`: Senha do dashboard Hangfire

**Variables:**
- `KEYCLOAK_REALM`: Realm do Keycloak
- `KEYCLOAK_AUTHSERVERURL`: URL do servidor Keycloak
- `KEYCLOAK_RESOURCE`: Resource/Client ID do Keycloak

### Deploy Manual

```bash
# Login no Container Registry
docker login container-registry.br-se1.magalu.cloud

# Build
docker build -t container-registry.br-se1.magalu.cloud/rufino-project/people-management-api:latest \
  -f ./server/Services/PeopleManagement/PeopleManagement.API/Dockerfile ./server

# Push
docker push container-registry.br-se1.magalu.cloud/rufino-project/people-management-api:latest

# Deploy via Azure CLI
az containerapp update \
  --name people-management-api \
  --resource-group RufinoGroup \
  --image container-registry.br-se1.magalu.cloud/rufino-project/people-management-api:latest
```

## 📡 Endpoints

### Swagger UI

Acesse a documentação interativa dos endpoints:
- **Desenvolvimento**: `https://localhost:5001/swagger`
- **Produção**: `https://people-management-api.bravemoss-5385f69a.eastus.azurecontainerapps.io/swagger`

### Principais Grupos de Endpoints

- `/api/v1/people` - Gerenciamento de pessoas
- `/api/v1/document` - Gerenciamento de documentos
- `/api/v1/document/webhook` - Webhook do ZapSign

## 📊 Hangfire Dashboard

O Hangfire Dashboard permite monitorar e gerenciar jobs em background.

### Acesso

- **URL Local**: `https://localhost:5001/hangfire`
- **URL Produção**: `https://people-management-api.bravemoss-5385f69a.eastus.azurecontainerapps.io/hangfire`

### Credenciais

Configuradas via `HangfireDashboard:Login` e `HangfireDashboard:Password` no appsettings.

### Jobs Recorrentes

O sistema registra automaticamente jobs recorrentes para:
- Verificação de expiração de documentos
- Envio de notificações
- Sincronização com sistemas externos

## 🧪 Testes

### Executar Testes Unitários

```bash
cd PeopleManagement.UnitTests
dotnet test
```

### Executar Testes de Integração

```bash
cd PeopleManagement.IntegrationTests
dotnet test
```

### Executar Todos os Testes

```bash
dotnet test
```

### Cobertura de Código

```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

## 📝 Estrutura de Pastas de Templates

Os templates de documentos devem ser colocados em:

```
app_files/
└── templates/
    ├── contrato-template.html
    ├── termo-template.html
    └── ...
```

## 🔒 Segurança

- ✅ Autenticação via JWT (Keycloak)
- ✅ Autorização baseada em roles e policies
- ✅ HTTPS obrigatório em produção
- ✅ Secrets gerenciados via Azure Key Vault
- ✅ CORS configurado
- ✅ Rate limiting via Kestrel

## 📄 Licença

Este projeto é privado e pertence ao projeto Rufino.

## 👥 Autores

- **Igor Coura** - [GitHub](https://github.com/IgorCoura)

## 📞 Suporte

Para questões e suporte, abra uma issue no repositório do GitHub.

---

⭐ Desenvolvido com .NET 8 e ❤️ para o Projeto Rufino
