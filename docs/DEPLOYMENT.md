# Deployment Guide

## Requisitos Previos

### Local Development
- .NET 9 SDK
- Docker Desktop
- OpenAI API Key
- Visual Studio 2024 o VS Code

### Azure Production
- Azure Subscription
- Azure CLI
- Docker
- OpenAI API Key

## Configuración Local

### 1. Clonar Repositorio
```bash
git clone <repository-url>
cd endpoint-hr
```

### 2. Configurar Variables de Entorno
```bash
# .env file
OPENAI_API_KEY=your-openai-api-key
ASPNETCORE_ENVIRONMENT=Development
STORAGE_PATH=./storage
LOGS_PATH=./logs
```

### 3. Configurar appsettings.json
```json
{
  "OpenAI": {
    "ApiKey": "${OPENAI_API_KEY}",
    "Model": "gpt-4o-mini",
    "MaxTokens": 4000
  },
  "Storage": {
    "Type": "Local",
    "Path": "${STORAGE_PATH}"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    },
    "Serilog": {
      "WriteTo": [
        {
          "Name": "File",
          "Args": {
            "path": "${LOGS_PATH}/app-.log",
            "rollingInterval": "Day"
          }
        }
      ]
    }
  }
}
```

### 4. Ejecutar Aplicación
```bash
# Restaurar dependencias
dotnet restore

# Compilar
dotnet build

# Ejecutar API
dotnet run --project CVProcessing.API

# Ejecutar UI (en otra terminal)
dotnet run --project CVProcessing.UI
```

## Docker Local

### 1. Docker Compose
```yaml
# docker-compose.yml
version: '3.8'

services:
  api:
    build:
      context: .
      dockerfile: CVProcessing.API/Dockerfile
    ports:
      - "7001:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - OPENAI_API_KEY=${OPENAI_API_KEY}
    volumes:
      - ./storage:/app/storage
      - ./logs:/app/logs

  ui:
    build:
      context: .
      dockerfile: CVProcessing.UI/Dockerfile
    ports:
      - "7002:80"
    depends_on:
      - api
    environment:
      - API_BASE_URL=http://api:80
```

### 2. Ejecutar con Docker
```bash
# Construir y ejecutar
docker-compose up --build

# Ejecutar en background
docker-compose up -d

# Ver logs
docker-compose logs -f

# Detener
docker-compose down
```

## Deployment Azure

### 1. Preparar Recursos Azure

#### Crear Resource Group
```bash
az group create --name rg-cv-processing --location eastus
```

#### Crear App Service Plan
```bash
az appservice plan create \
  --name asp-cv-processing \
  --resource-group rg-cv-processing \
  --sku B1 \
  --is-linux
```

#### Crear Web Apps
```bash
# API
az webapp create \
  --name cv-processing-api \
  --resource-group rg-cv-processing \
  --plan asp-cv-processing \
  --runtime "DOTNETCORE:9.0"

# UI
az webapp create \
  --name cv-processing-ui \
  --resource-group rg-cv-processing \
  --plan asp-cv-processing \
  --runtime "DOTNETCORE:9.0"
```

#### Crear Storage Account
```bash
az storage account create \
  --name stcvprocessing \
  --resource-group rg-cv-processing \
  --location eastus \
  --sku Standard_LRS
```

#### Crear Key Vault
```bash
az keyvault create \
  --name kv-cv-processing \
  --resource-group rg-cv-processing \
  --location eastus
```

### 2. Configurar Secrets
```bash
# Agregar OpenAI API Key
az keyvault secret set \
  --vault-name kv-cv-processing \
  --name "OpenAI-ApiKey" \
  --value "your-openai-api-key"

# Agregar Storage Connection String
az keyvault secret set \
  --vault-name kv-cv-processing \
  --name "Storage-ConnectionString" \
  --value "your-storage-connection-string"
```

### 3. Configurar App Settings
```bash
# API App Settings
az webapp config appsettings set \
  --name cv-processing-api \
  --resource-group rg-cv-processing \
  --settings \
    "OpenAI__ApiKey=@Microsoft.KeyVault(VaultName=kv-cv-processing;SecretName=OpenAI-ApiKey)" \
    "Storage__Type=Azure" \
    "Storage__ConnectionString=@Microsoft.KeyVault(VaultName=kv-cv-processing;SecretName=Storage-ConnectionString)"
```

### 4. Deploy con GitHub Actions

#### Workflow File (.github/workflows/deploy.yml)
```yaml
name: Deploy to Azure

on:
  push:
    branches: [ main ]

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '9.0.x'
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore --configuration Release
    
    - name: Test
      run: dotnet test --no-build --verbosity normal
    
    - name: Publish API
      run: dotnet publish CVProcessing.API -c Release -o ./api-publish
    
    - name: Publish UI
      run: dotnet publish CVProcessing.UI -c Release -o ./ui-publish
    
    - name: Deploy API to Azure
      uses: azure/webapps-deploy@v2
      with:
        app-name: cv-processing-api
        publish-profile: ${{ secrets.AZURE_API_PUBLISH_PROFILE }}
        package: ./api-publish
    
    - name: Deploy UI to Azure
      uses: azure/webapps-deploy@v2
      with:
        app-name: cv-processing-ui
        publish-profile: ${{ secrets.AZURE_UI_PUBLISH_PROFILE }}
        package: ./ui-publish
```

### 5. Configurar Monitoring

#### Application Insights
```bash
az monitor app-insights component create \
  --app cv-processing-insights \
  --location eastus \
  --resource-group rg-cv-processing
```

#### Log Analytics Workspace
```bash
az monitor log-analytics workspace create \
  --workspace-name cv-processing-logs \
  --resource-group rg-cv-processing \
  --location eastus
```

## Configuración de Producción

### 1. appsettings.Production.json
```json
{
  "OpenAI": {
    "ApiKey": "#{OpenAI.ApiKey}#",
    "Model": "gpt-4o-mini",
    "MaxTokens": 4000,
    "Temperature": 0.1
  },
  "Storage": {
    "Type": "Azure",
    "ConnectionString": "#{Storage.ConnectionString}#",
    "ContainerName": "cv-documents"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "CVProcessing": "Information"
    },
    "ApplicationInsights": {
      "LogLevel": {
        "Default": "Information"
      }
    }
  },
  "AllowedHosts": "*",
  "CORS": {
    "AllowedOrigins": [
      "https://cv-processing-ui.azurewebsites.net"
    ]
  }
}
```

### 2. Health Checks
```csharp
// Program.cs
builder.Services.AddHealthChecks()
    .AddCheck<OpenAIHealthCheck>("openai")
    .AddCheck<StorageHealthCheck>("storage")
    .AddCheck<DatabaseHealthCheck>("database");

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
```

### 3. Security Headers
```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    await next();
});
```

## Troubleshooting

### Common Issues

#### 1. OpenAI API Errors
```bash
# Check API key configuration
az webapp config appsettings list --name cv-processing-api --resource-group rg-cv-processing

# Check logs
az webapp log tail --name cv-processing-api --resource-group rg-cv-processing
```

#### 2. Storage Issues
```bash
# Test storage connection
az storage blob list --account-name stcvprocessing --container-name cv-documents
```

#### 3. Performance Issues
```bash
# Scale up App Service
az appservice plan update --name asp-cv-processing --resource-group rg-cv-processing --sku P1V2

# Enable auto-scaling
az monitor autoscale create --resource-group rg-cv-processing --resource cv-processing-api
```

### Monitoring Commands
```bash
# View application logs
az webapp log tail --name cv-processing-api --resource-group rg-cv-processing

# Check health endpoint
curl https://cv-processing-api.azurewebsites.net/health

# Monitor metrics
az monitor metrics list --resource cv-processing-api --metric-names "Http2xx,Http4xx,Http5xx"
```

## Backup y Recovery

### 1. Database Backup
```bash
# Automated backup (if using SQL Database)
az sql db export --server cv-processing-sql --database cv-processing-db --storage-uri "https://stcvprocessing.blob.core.windows.net/backups/db-backup.bacpac"
```

### 2. Storage Backup
```bash
# Sync to backup storage
az storage blob sync --source "https://stcvprocessing.blob.core.windows.net/cv-documents" --destination "https://stcvprocessingbackup.blob.core.windows.net/cv-documents"
```

### 3. Configuration Backup
```bash
# Export app settings
az webapp config appsettings list --name cv-processing-api --resource-group rg-cv-processing > app-settings-backup.json
```