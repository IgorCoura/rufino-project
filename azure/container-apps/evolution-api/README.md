# Evolution API - Azure Container Apps

Este diretório contém os arquivos de configuração para deploy do Evolution API no Azure Container Apps.

## Pré-requisitos

- Azure CLI instalado
- Conta Azure ativa
- Arquivo `.env` configurado em `.env`

## Estrutura de Arquivos

- `main.bicep` - Template Bicep para infraestrutura como código
- `container-app.yaml` - Configuração declarativa do Container App
- `deploy.ps1` - Script PowerShell para deploy automatizado

## Deploy Manual via Azure CLI

### 1. Login no Azure
```bash
az login
az account set --subscription <sua-subscription-id>
```

### 2. Criar Resource Group (se necessário)
```bash
az group create --name rg-rufino-prod --location eastus
```

### 3. Criar Container App Environment
```bash
az containerapp env create \
  --name cae-rufino-prod \
  --resource-group rg-rufino-prod \
  --location eastus
```

### 4. Deploy do Evolution API
```bash
az containerapp create --name evolution-api --resource-group RufinoGroup --environment managedEnvironment-RufinoGroup-a0bb --image evoapicloud/evolution-api:v2.3.7 --target-port 8080 --ingress external --cpu 0.5 --memory 1Gi --min-replicas 1 --max-replicas 3 --env-vars $(cat .env | xargs)
```

## Deploy Automatizado com PowerShell

Execute o script `deploy.ps1`:

```powershell
.\deploy.ps1 `
  -ResourceGroup "RufinoGroup `
  -EnvironmentName "managedEnvironment-RufinoGroup-a0bb" `
  -Location "eastus" `
  -ContainerAppName "evolution-api"
```

## Deploy com Bicep

```bash
az deployment group create \
  --resource-group rg-rufino-prod \
  --template-file main.bicep \
  --parameters environmentId=<environment-id>
```

## Configuração de Variáveis de Ambiente

As variáveis de ambiente são carregadas do arquivo `.env`. Para adicionar ou modificar variáveis:

1. Edite o arquivo `.env`
2. Execute o script de deploy novamente, ou
3. Atualize manualmente via Azure CLI:

```bash
az containerapp update \
  --name evolution-api \
  --resource-group rg-rufino-prod \
  --set-env-vars KEY=VALUE
```

## Volumes Persistentes

Para configurar volumes Azure Files para `evolution_store` e `evolution_instances`:

1. Crie um Azure Storage Account
2. Crie File Shares
3. Configure os volumes no Container App Environment
4. Monte os volumes no container

```bash
# Criar storage account
az storage account create \
  --name storufino \
  --resource-group rg-rufino-prod \
  --location eastus \
  --sku Standard_LRS

# Criar file shares
az storage share create --name evolution-store --account-name storufino
az storage share create --name evolution-instances --account-name storufino

# Adicionar storage ao environment
az containerapp env storage set \
  --name cae-rufino-prod \
  --resource-group rg-rufino-prod \
  --storage-name evolution-store \
  --azure-file-account-name storufino \
  --azure-file-account-key <storage-key> \
  --azure-file-share-name evolution-store \
  --access-mode ReadWrite
```

## Monitoramento

Visualize logs em tempo real:

```bash
az containerapp logs show \
  --name evolution-api \
  --resource-group rg-rufino-prod \
  --follow
```

## Comandos Úteis

```bash
# Ver status do container
az containerapp show --name evolution-api --resource-group rg-rufino-prod

# Escalar o container
az containerapp update \
  --name evolution-api \
  --resource-group rg-rufino-prod \
  --min-replicas 2 \
  --max-replicas 5

# Deletar o container app
az containerapp delete --name evolution-api --resource-group rg-rufino-prod
```
