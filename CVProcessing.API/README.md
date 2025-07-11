# CVProcessing.API

## Propósito
API REST que expone los endpoints para el sistema de procesamiento de CVs con documentación OpenAPI 3.0.

## Estructura

### `/Controllers`
Controladores REST que exponen la funcionalidad:
- **SessionsController**: Gestión de sesiones de procesamiento
- **DocumentsController**: Upload y gestión de documentos
- **AnalysisController**: Análisis y comparación de candidatos
- **HealthController**: Health checks del sistema

### `/Middleware`
Middleware personalizado para la aplicación:
- **ExceptionHandlingMiddleware**: Manejo global de excepciones
- **ValidationMiddleware**: Validación de requests
- **CorsMiddleware**: Configuración de CORS

## Características

### **OpenAPI 3.0**
- Documentación automática con Swagger
- Schemas detallados para requests/responses
- Ejemplos de uso para cada endpoint
- Autenticación JWT documentada

### **Error Handling**
- Respuestas de error estandarizadas
- Logging de excepciones con correlation IDs
- Status codes HTTP apropiados
- Detalles de validación específicos

### **File Upload**
- Soporte para multipart/form-data
- Validación de tipos y tamaños de archivo
- Upload por lotes optimizado
- Progress tracking para uploads grandes

### **Real-time Updates**
- SignalR hubs para notificaciones
- Updates de progreso en tiempo real
- Notificaciones de procesamiento completado

## Endpoints Principales

- `POST /api/sessions` - Crear sesión
- `GET /api/sessions/{id}` - Obtener sesión
- `POST /api/sessions/{id}/documents` - Upload documentos
- `GET /api/sessions/{id}/status` - Estado de procesamiento
- `GET /api/sessions/{id}/matrix` - Matriz de comparación
- `GET /api/health` - Health check

## Configuración
- **CORS**: Configurado para desarrollo y producción
- **Rate Limiting**: Límites por IP y endpoint
- **Compression**: Gzip para responses grandes
- **Caching**: Headers apropiados para recursos estáticos