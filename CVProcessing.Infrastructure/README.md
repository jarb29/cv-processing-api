# CVProcessing.Infrastructure

## Propósito
Implementa los servicios de infraestructura que conectan el dominio con tecnologías externas como almacenamiento, OpenAI y logging.

## Estructura

### `/Storage`
Implementaciones de almacenamiento de archivos:
- **LocalFileStorage**: Almacenamiento en sistema de archivos local
- **SessionRepository**: Persistencia de sesiones en JSON
- **DocumentRepository**: Gestión de documentos y metadata

### `/OpenAI`
Integración con OpenAI para análisis de CVs:
- **OpenAIClient**: Cliente para comunicación con API de OpenAI
- **PromptTemplates**: Templates de prompts para extracción de datos
- **ResponseParsers**: Parsers para respuestas JSON de OpenAI

### `/Logging`
Configuración de logging estructurado:
- **SerilogConfiguration**: Setup de Serilog
- **LoggingMiddleware**: Middleware para captura de requests/responses
- **StructuredLogging**: Helpers para logging consistente

## Dependencias
- **OpenAI .NET SDK**: Para integración con OpenAI
- **Serilog**: Para logging estructurado
- **System.Text.Json**: Para serialización JSON
- **Microsoft.Extensions.DependencyInjection**: Para IoC

## Principios
- **Implementación de interfaces Core**: Respeta contratos definidos
- **Configuración externa**: Settings desde appsettings.json
- **Error handling robusto**: Manejo de excepciones y retry logic
- **Performance**: Operaciones asíncronas y eficientes