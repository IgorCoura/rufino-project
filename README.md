<div align="center">

# 🏢 Rufino Project

### Plataforma de Gestão de Pessoas e Documentos para Empresas Brasileiras

![Version](https://img.shields.io/badge/version-3.0-blue)
![Backend](https://img.shields.io/badge/backend-.NET%208-purple)
![Frontend](https://img.shields.io/badge/frontend-Flutter-02569B)
![Database](https://img.shields.io/badge/database-PostgreSQL%2016-336791)
![Deploy](https://img.shields.io/badge/deploy-Azure%20Container%20Apps-0078D4)

</div>

---

## 📋 Sobre o Projeto

O **Rufino** é uma plataforma completa de **Gestão de Pessoas (RH)** projetada para empresas brasileiras. O sistema abrange todo o ciclo de vida do colaborador — desde a admissão até a gestão documental, estrutura organizacional, assinaturas eletrônicas e notificações automatizadas via WhatsApp.

A solução opera em um modelo **multi-empresa**, onde cada empresa pode gerenciar seus departamentos, locais de trabalho, cargos, funções, colaboradores e toda a documentação regulatória exigida pela legislação trabalhista brasileira (como a **NR-01** — Programa de Gerenciamento de Riscos).

---

## ✨ Funcionalidades Principais

| Funcionalidade | Descrição |
|---|---|
| **Estrutura Organizacional** | Gestão de empresas, departamentos, locais de trabalho, cargos e funções |
| **Ciclo de Vida do Colaborador** | Admissão, dados pessoais (CPF, RG, título de eleitor, reservista, CNH, dependentes), contratos de trabalho, exames admissionais |
| **Automação de Documentos** | Geração automática de documentos a partir de templates (NR-01, contratos, etc.), controle de validade, documentos recorrentes e obrigatórios por cargo/evento |
| **Assinatura Eletrônica** | Integração com **ZapSign** para assinatura digital de documentos com rastreamento de status via webhook |
| **Notificações WhatsApp** | Integração com **Evolution API** para envio de lembretes, solicitações de assinatura e notificações |
| **Armazenamento de Arquivos** | Gestão de arquivos e documentos via Azure Blob Storage com categorização |
| **Autenticação & Autorização** | Controle de acesso granular via **Keycloak** com permissões baseadas em recursos |
| **Jobs Automáticos** | Depreciação de documentos, lembretes de assinatura, geração recorrente de documentos, conclusão de admissões |

---

## 🏗️ Arquitetura

O projeto segue uma arquitetura de **monorepo full-stack** com separação clara de responsabilidades:

```
rufino-project/
├── 📱 client/          → App Flutter (frontend multiplataforma)
├── 🖥️ server/          → API .NET 8 (backend com Clean Architecture + DDD)
├── ☁️ azure/           → Infraestrutura como Código (Bicep + Container Apps)
└── 🛠️ utils/           → Ferramentas auxiliares (geradores de docs, scripts, etc.)
```

### Backend — Clean Architecture + DDD + CQRS

O servidor é uma **API REST em .NET 8** estruturada em camadas seguindo **Clean Architecture** com padrões de **Domain-Driven Design (DDD)** e **CQRS** via MediatR:

```
Services/PeopleManagement/
├── PeopleManagement.API            → Controllers, Auth, Swagger, Injeção de Dependência
├── PeopleManagement.Application    → Commands, Queries, Validações (FluentValidation), DTOs
├── PeopleManagement.Domain         → Aggregates, Entities, Value Objects, Domain Events, Errors
├── PeopleManagement.Infra          → EF Core, Repositórios, Migrations, Serviços de Blob/PDF/HTML
├── PeopleManagement.Services       → Handlers de Eventos, Jobs (Hangfire), Integrações externas
├── PeopleManagement.UnitTests      → Testes unitários dos aggregates do domínio
└── PeopleManagement.IntegrationTests → Testes de integração com banco real
```

#### Modelo de Domínio (13 Aggregates)

| Aggregate | Responsabilidade |
|---|---|
| `Employee` | Colaborador com dados pessoais, contrato, CNH, dependentes, exame admissional |
| `Company` | Empresa com CNPJ, endereço, contato |
| `Document` | Documento com unidades e controle de status |
| `DocumentTemplate` | Template de documento com locais de assinatura e recuperação dinâmica de dados |
| `Role` | Função com CBO, remuneração e moeda |
| `Workplace` | Local de trabalho |
| `Department` | Departamento |
| `Position` | Cargo |
| `Archive` / `ArchiveCategory` | Gestão de arquivos armazenados |
| `RequireDocuments` | Regras de documentos obrigatórios por evento/associação |
| `DocumentGroup` | Agrupamento de documentos |
| `WebHook` | Webhooks para integrações externas |

### Frontend — Flutter com Modular + BLoC

O app Flutter utiliza **flutter_modular** para injeção de dependência e roteamento, e **BLoC** para gerenciamento de estado:

| Módulo | Rota | Função |
|---|---|---|
| `AuthModule` | `/` | Login via OAuth2 (Keycloak) |
| `HomeModule` | `/home` | Dashboard principal |
| `EmployeeModule` | `/employee` | CRUD de colaboradores |
| `CompanyModule` | `/company` | Seleção e edição de empresas |
| `WorkplaceModule` | `/workplace` | Gestão de locais de trabalho |
| `DepartmentModule` | `/department` | Departamentos, cargos e funções |

---

## 🛠️ Stack Tecnológica

### Backend
| Tecnologia | Uso |
|---|---|
| **.NET 8 / C#** | Framework principal da API |
| **Entity Framework Core 9** | ORM com PostgreSQL (Npgsql) |
| **MediatR 12** | Implementação de CQRS (Commands/Queries) |
| **FluentValidation** | Validação de entrada de dados |
| **Hangfire** | Agendamento de jobs em background |
| **JWT Bearer + Keycloak** | Autenticação e autorização |
| **Swashbuckle** | Documentação Swagger/OpenAPI |
| **Puppeteer (Headless Chrome)** | Geração de PDF a partir de HTML |
| **Azure.Storage.Blobs** | Armazenamento de arquivos |

### Frontend
| Tecnologia | Uso |
|---|---|
| **Flutter (Dart ^3.5.2)** | Framework UI multiplataforma |
| **flutter_modular** | Arquitetura modular (DI + Rotas) |
| **BLoC / flutter_bloc** | Gerenciamento de estado |
| **OAuth2** | Fluxo de autenticação |
| **flutter_secure_storage** | Armazenamento seguro de tokens |
| **infinite_scroll_pagination** | Listagens paginadas |
| **file_picker** | Upload de documentos |

### Infraestrutura & Serviços Externos
| Tecnologia | Uso |
|---|---|
| **PostgreSQL 16.4** | Banco de dados relacional |
| **Keycloak 25** | Identity Provider (IAM) |
| **Docker / Docker Compose** | Containerização e orquestração local |
| **Azure Container Apps** | Deploy em produção (serverless containers) |
| **Azure Bicep** | Infraestrutura como Código |
| **Azurite** | Emulador local do Azure Blob Storage |
| **Evolution API v2** | Envio de mensagens WhatsApp |
| **ZapSign** | Assinatura eletrônica de documentos |

---

## 🐳 Como Rodar o Projeto

### Pré-requisitos

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) instalado e rodando
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Flutter SDK](https://flutter.dev/docs/get-started/install) (^3.5.2)
- Uma instância do **Keycloak** configurada (ou usar a configuração Docker Compose)

### 1. Subindo a Infraestrutura (Docker)

```powershell
cd server
docker-compose up -d peoplemanagement.db blob-azure-storage
```

> Para subir o Keycloak e a Evolution API, descomente os serviços no `docker-compose.yml`.

### 2. Rodando o Backend

```powershell
cd server/Services/PeopleManagement/PeopleManagement.API
dotnet run
```

A API estará disponível em `https://localhost:8041` (HTTPS) ou `http://localhost:8040` (HTTP).

### 3. Rodando o Frontend (Flutter)

```powershell
cd client/rufino
flutter pub get
flutter run
```

> Configure os endpoints da API no arquivo `secrets/local_config.json`.

---

## 🧪 Testes

O projeto possui cobertura de testes em múltiplas camadas:

| Tipo | Projeto | Escopo |
|---|---|---|
| **Testes Unitários** | `PeopleManagement.UnitTests` | Aggregates do domínio (Employee, Company, Document, Archive, etc.) |
| **Testes de Integração** | `PeopleManagement.IntegrationTests` | Endpoints da API com banco de dados real e dados de teste |
| **Testes de Widget** | `client/rufino/test/` | Testes de componentes Flutter |

```powershell
# Testes do Backend
cd server
dotnet test

# Testes do Flutter
cd client/rufino
flutter test
```

---

## ☁️ Deploy (Azure)

O deploy em produção utiliza **Azure Container Apps** com templates **Bicep** para Infrastructure as Code:

```
azure/container-apps/
└── evolution-api/
    ├── main.bicep          → Template de infraestrutura
    ├── container-app.yaml  → Spec declarativo do container
    ├── deploy.ps1          → Script PowerShell de deploy
    └── template.env        → Variáveis de ambiente
```

- **Scaling**: Auto-scaling de 0 a 3 réplicas
- **Recursos**: 0.5 vCPU / 1 GiB de memória por réplica
- **Região**: East US
- **Persistência**: Azure Files para armazenamento de dados

---

## 🛠️ Utilitários

O repositório inclui diversas ferramentas auxiliares no diretório `utils/`:

| Ferramenta | Tecnologia | Descrição |
|---|---|---|
| **DocsGenerator** | C# | Geração de documentos a partir de dados CSV + templates HTML |
| **Templates/NR01** | HTML/CSS | Templates para documentos regulatórios NR-01 |
| **CombinePDFs** | — | Combinação de múltiplos PDFs |
| **NomeacaoDePDFs** | Python | Renomeação automática de PDFs |
| **NormalizandoCFe** | — | Normalização de Cupons Fiscais Eletrônicos |
| **ConversorMedicoesProjetos** | C# | Conversão de medições de projetos |
| **Transfer** | C# | Transferência de dados entre sistemas |
| **MaterialTheme** | Flutter | Tema Material Design do app Rufino |
| **SQLs** | SQL | Scripts para criação de dados de teste |

---

## 📁 Estrutura de Pastas

```
rufino-project/
│
├── client/rufino/             # App Flutter multiplataforma
│   ├── lib/
│   │   ├── configurations/    # Configurações HTTP/SSL
│   │   ├── domain/            # Modelos de domínio (Company, etc.)
│   │   ├── modules/           # Módulos da aplicação (Auth, Home, Employee, etc.)
│   │   └── shared/            # Componentes reutilizáveis, erros, utilitários
│   ├── assets/                # Imagens e áudios
│   ├── secrets/               # Configurações de ambiente (local/prod)
│   └── test/                  # Testes de widget
│
├── server/                    # Backend .NET 8
│   ├── Services/
│   │   └── PeopleManagement/  # Serviço principal
│   │       ├── *.API/         # Camada de apresentação (Controllers, Auth)
│   │       ├── *.Application/ # Camada de aplicação (CQRS, DTOs, Validações)
│   │       ├── *.Domain/      # Camada de domínio (Aggregates, Entities, VOs)
│   │       ├── *.Infra/       # Camada de infraestrutura (EF Core, Blob, PDF)
│   │       ├── *.Services/    # Camada de serviços (Jobs, Eventos, Integrações)
│   │       ├── *.UnitTests/   # Testes unitários
│   │       └── *.IntegrationTests/ # Testes de integração
│   └── docker-compose.yml     # Orquestração de containers
│
├── azure/                     # Infraestrutura de deploy
│   └── container-apps/        # Templates Azure Container Apps + Bicep
│
└── utils/                     # Ferramentas auxiliares
    ├── DocsGenerator/         # Gerador de documentos
    ├── Templates/NR01/        # Templates NR-01
    ├── CombinePDFs/           # Combinador de PDFs
    └── ...                    # Outras ferramentas
```

---

## 📄 Licença

Projeto privado — uso interno.

---

<div align="center">

**Rufino Project** — Gestão de Pessoas simplificada e automatizada 🚀

</div>
