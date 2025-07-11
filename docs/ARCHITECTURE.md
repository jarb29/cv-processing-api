# Arquitectura del Sistema

## Visión General

Sistema de procesamiento de CVs basado en microservicios con arquitectura limpia, desarrollado en .NET 9 con integración OpenAI para análisis inteligente de candidatos.

## Componentes Principales

### 1. API Layer (CVProcessing.API)
- **ASP.NET Core 9** Web API
- **OpenAPI 3.0** con Swagger UI
- **Middleware personalizado** para logging
- **SignalR** para actualizaciones en tiempo real
- **Authentication/Authorization** (JWT)

### 2. Application Layer (CVProcessing.Application)
- **Services**: Lógica de negocio
- **DTOs**: Data Transfer Objects
- **Mappers**: AutoMapper configurations
- **Validators**: FluentValidation rules
- **Handlers**: Command/Query handlers

### 3. Core Layer (CVProcessing.Core)
- **Entities**: Domain models
- **Interfaces**: Contratos de servicios
- **Enums**: Tipos enumerados
- **Constants**: Constantes del sistema

### 4. Infrastructure Layer (CVProcessing.Infrastructure)
- **OCR Services**: Tesseract.NET, Azure Document Intelligence
- **OpenAI Integration**: GPT-4o-mini client
- **File Storage**: Local filesystem, Azure Blob
- **Logging**: Serilog structured logging
- **Background Services**: Procesamiento asíncrono

### 5. UI Layer (CVProcessing.UI)
- **Blazor Server/WASM**
- **MudBlazor** components
- **SignalR Client** para real-time updates
- **Chart.js** para visualizaciones

## Flujo de Datos

```
UI Upload → API Endpoint → Session Service → File Storage → Background Processor → OCR Service → OpenAI Service → JSON Parser → Comparison Engine → Matrix Generator → SignalR Hub → UI Update
```

## Patrones de Diseño

### Clean Architecture
- **Separation of Concerns**: Cada capa tiene responsabilidades específicas
- **Dependency Inversion**: Interfaces en Core, implementaciones en Infrastructure
- **SOLID Principles**: Aplicados en toda la arquitectura

### Repository Pattern
```csharp
public interface ISessionRepository
{
    Task<Session> CreateAsync(Session session);
    Task<Session> GetByIdAsync(Guid sessionId);
    Task UpdateAsync(Session session);
}
```

### Command Query Responsibility Segregation (CQRS)
```csharp
public record CreateSessionCommand(Guid SessionId, JobOffer JobOffer);
public record GetSessionQuery(Guid SessionId);
```

### Background Services
```csharp
public class CVProcessingService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Procesamiento asíncrono de CVs
    }
}
```

## Almacenamiento

### Estructura Local
```
storage/
├── sessions/
│   └── {session-id}/
│       ├── metadata.json
│       ├── job-offer.json
│       ├── documents/
│       │   └── {document-uuid}/
│       │       ├── input/
│       │       │   └── original.pdf
│       │       └── output/
│       │           ├── ocr-text.txt
│       │           ├── structured-data.json
│       │           └── openai-response.json
│       └── analysis/
│           ├── comparison-matrix.json
│           ├── rankings.json
│           └── statistics.json
├── logs/
│   └── app-{date}.log
└── temp/
    └── processing/
```

### Migración Azure
- **Azure Blob Storage**: Archivos y documentos
- **Azure Cosmos DB**: Metadata y JSON estructurado
- **Azure Service Bus**: Colas de procesamiento
- **Azure Application Insights**: Monitoring y logging

## Integración OpenAI

### Prompt Engineering
```json
{
  "system": "Eres un experto en análisis de CVs. Extrae información estructurada.",
  "user": "Analiza este CV considerando la siguiente oferta laboral: {jobOffer}",
  "response_format": "json_schema"
}
```

### JSON Schema Estandarizado
```json
{
  "personalInfo": {
    "name": "string",
    "email": "string",
    "phone": "string"
  },
  "experience": [
    {
      "company": "string",
      "position": "string",
      "duration": "string",
      "responsibilities": ["string"]
    }
  ],
  "skills": ["string"],
  "education": [
    {
      "institution": "string",
      "degree": "string",
      "year": "number"
    }
  ],
  "score": {
    "overall": "number",
    "experience": "number",
    "skills": "number",
    "education": "number"
  }
}
```

## Seguridad

### API Security
- **JWT Authentication**
- **Rate Limiting**
- **CORS Configuration**
- **Input Validation**
- **File Upload Restrictions**

### Data Protection
- **Encryption at Rest**
- **Secure API Keys Storage** (Azure Key Vault)
- **PII Data Handling**
- **GDPR Compliance**

## Escalabilidad

### Horizontal Scaling
- **Stateless Services**
- **Load Balancer Ready**
- **Database Sharding**
- **Microservices Architecture**

### Performance Optimization
- **Async/Await Pattern**
- **Connection Pooling**
- **Caching Strategy** (Redis)
- **Background Processing**

## Monitoring y Observabilidad

### Logging
- **Structured Logging** con Serilog
- **Correlation IDs**
- **Performance Metrics**
- **Error Tracking**

### Health Checks
```csharp
services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database")
    .AddCheck<OpenAIHealthCheck>("openai")
    .AddCheck<StorageHealthCheck>("storage");
```

### Metrics
- **Request/Response Times**
- **Processing Queue Length**
- **Success/Error Rates**
- **Resource Utilization**

## Deployment

### Containerización
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0
COPY . /app
WORKDIR /app
EXPOSE 80
ENTRYPOINT ["dotnet", "CVProcessing.API.dll"]
```

### CI/CD Pipeline
1. **Build & Test**
2. **Security Scan**
3. **Container Build**
4. **Deploy to Staging**
5. **Integration Tests**
6. **Deploy to Production**

## Consideraciones Futuras

### Mejoras Planificadas
- **LinkedIn Integration**
- **Multi-language Support**
- **Advanced Analytics**
- **Machine Learning Models**
- **Mobile App**

### Tecnologías Emergentes
- **Azure OpenAI Service**
- **Semantic Kernel**
- **Vector Databases**
- **GraphQL API**