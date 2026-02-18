# Script de Deploy para Evolution API no Azure Container Apps
# Certifique-se de estar logado no Azure CLI: az login

param(
    [Parameter(Mandatory=$true)]
    [string]$ResourceGroup,
    
    [Parameter(Mandatory=$true)]
    [string]$EnvironmentName,
    
    [Parameter(Mandatory=$false)]
    [string]$Location = "eastus",
    
    [Parameter(Mandatory=$false)]
    [string]$ContainerAppName = "evolution-api"
)

Write-Host "Iniciando deploy do Evolution API..." -ForegroundColor Green

# Verifica se o Resource Group existe
$rgExists = az group exists --name $ResourceGroup
if ($rgExists -eq "false") {
    Write-Host "Criando Resource Group: $ResourceGroup" -ForegroundColor Yellow
    az group create --name $ResourceGroup --location $Location
}

# Verifica se o Container App Environment existe
Write-Host "Verificando Container App Environment..." -ForegroundColor Yellow
$envExists = az containerapp env show --name $EnvironmentName --resource-group $ResourceGroup 2>&1 | Out-String
$envExistsCheck = $LASTEXITCODE -eq 0

if (-not $envExistsCheck) {
    Write-Host "Container App Environment não encontrado. Criando: $EnvironmentName" -ForegroundColor Yellow
    az containerapp env create `
        --name $EnvironmentName `
        --resource-group $ResourceGroup `
        --location $Location
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Erro ao criar Container App Environment" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "Container App Environment já existe: $EnvironmentName" -ForegroundColor Green
}

# Lê as variáveis de ambiente do arquivo .env
Write-Host "Carregando variáveis de ambiente do arquivo .env..." -ForegroundColor Yellow
$envPath = Join-Path $PSScriptRoot ".env"
$envVars = @()

if (Test-Path $envPath) {
    Get-Content $envPath | ForEach-Object {
        $line = $_.Trim()
        if ($line -and -not $line.StartsWith("#")) {
            $parts = $line -split "=", 2
            if ($parts.Count -eq 2) {
                $key = $parts[0].Trim()
                $value = $parts[1].Trim()
                $envVars += "$key=$value"
            }
        }
    }
    Write-Host "Carregadas $($envVars.Count) variáveis de ambiente" -ForegroundColor Green
} else {
    Write-Host "Aviso: Arquivo .env não encontrado em $envPath" -ForegroundColor Yellow
}

# Cria ou atualiza o Container App
Write-Host "Criando/Atualizando Container App: $ContainerAppName" -ForegroundColor Yellow

# Executa o comando az containerapp create
Write-Host "Executando: az containerapp create..." -ForegroundColor Cyan

# Monta o comando base
$azCommand = @(
    'containerapp', 'create',
    '--name', $ContainerAppName,
    '--resource-group', $ResourceGroup,
    '--environment', $EnvironmentName,
    '--image', 'evoapicloud/evolution-api:v2.3.7',
    '--target-port', '8080',
    '--ingress', 'external',
    '--cpu', '0.5',
    '--memory', '1Gi',
    '--min-replicas', '1',
    '--max-replicas', '3'
)

# Adiciona todas as variáveis de ambiente de uma vez
if ($envVars.Count -gt 0) {
    $azCommand += '--env-vars'
    $azCommand += $envVars
}

# Executa o comando
& az $azCommand

if ($LASTEXITCODE -ne 0) {
    Write-Host "Erro ao criar/atualizar Container App" -ForegroundColor Red
    exit 1
}

# Obtém a URL do aplicativo
Write-Host "`nObtendo URL do aplicativo..." -ForegroundColor Yellow
$fqdn = az containerapp show `
    --name $ContainerAppName `
    --resource-group $ResourceGroup `
    --query "properties.configuration.ingress.fqdn" `
    --output tsv

Write-Host "`n✅ Deploy concluído com sucesso!" -ForegroundColor Green
Write-Host "URL da aplicação: https://$fqdn" -ForegroundColor Cyan
