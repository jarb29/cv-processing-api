# CV Processing System

Sistema de procesamiento y análisis de CVs con IA para reclutamiento, desarrollado en .NET 9 con integración OpenAI.

## 🚀 Características

- **API REST** con OpenAPI 3.0 y Swagger
- **Procesamiento por lotes** de CVs
- **Extracción inteligente** con OpenAI GPT-4o-mini
- **Análisis comparativo** y ranking automático
- **UI interactiva** con Blazor
- **Procesamiento asíncrono** con actualizaciones en tiempo real

## 🏗️ Arquitectura

```
CVProcessingSystem/
├── CVProcessing.API/              # Web API + Swagger
├── CVProcessing.Core/             # Domain models, interfaces
├── CVProcessing.Application/      # Business logic, OpenAI integration
├── CVProcessing.Infrastructure/   # OCR, File handling, Storage
└── CVProcessing.Tests/           # Testing
```

## 📋 Requisitos

- .NET 9 SDK
- OpenAI API Key
- Docker (opcional)

## 🛠️ Instalación

```bash
git clone <repository-url>
cd endpoint-hr
dotnet restore
dotnet build
```

## ⚙️ Configuración

1. Configurar OpenAI API Key en `appsettings.json`:

```json
{
  "OpenAI": {
    "ApiKey": "your-openai-api-key"
  }
}
```

2. Ejecutar la aplicación:

```bash
export ASPNETCORE_ENVIRONMENT=Development
dotnet run --project CVProcessing.API
```

## 📚 API Endpoints

- `POST /api/sessions` - Crear sesión de procesamiento
- `POST /api/sessions/{sessionId}/documents` - Subir CVs por lotes
- `GET /api/sessions/{sessionId}/status` - Estado del procesamiento
- `GET /api/sessions/{sessionId}/matrix` - Matriz de comparación
- `POST /api/job-offers` - Definir oferta laboral

## 🎯 Uso

1. **Definir oferta laboral** con requisitos específicos
2. **Subir CVs** individualmente o por lotes
3. **Monitorear procesamiento** en tiempo real
4. **Analizar resultados** en matriz comparativa
5. **Ordenar candidatos** por puntuación

## 🔄 Flujo de Procesamiento

```
CV Upload → OCR/Direct → OpenAI Analysis → JSON Structure → Comparison Matrix → Ranking
```

## 📁 Estructura de Almacenamiento

```
storage/
├── sessions/{session-id}/
│   ├── job-offer.json
│   ├── documents/{document-uuid}/
│   │   ├── input/original.pdf
│   │   └── output/structured-data.json
│   └── analysis/comparison-matrix.json
```

## 🚀 Deployment

### Local

```bash
docker-compose up
```

### Azure

- App Service para API y UI
- Blob Storage para documentos
- Cosmos DB para metadata

## 🧪 Testing

```bash
dotnet test
```

## 📖 Documentación

- [Arquitectura](docs/ARCHITECTURE.md)
- [API Reference](docs/API.md)
- [Deployment Guide](docs/DEPLOYMENT.md)

## 🤝 Contribución

1. Fork el proyecto
2. Crear feature branch
3. Commit cambios
4. Push al branch
5. Crear Pull Request

## 📄 Licencia

Este proyecto está bajo la Licencia MIT - ver [LICENSE](LICENSE) para detalles.
