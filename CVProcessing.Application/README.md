# CVProcessing.Application

## Propósito
Contiene la **lógica de negocio** y **servicios de aplicación** que orquestan las operaciones del sistema.

## Estructura

### `/Services`
Servicios de aplicación que implementan casos de uso:
- **SessionService**: Gestión completa de sesiones de procesamiento
- **DocumentService**: Upload, validación y procesamiento de CVs
- **AnalysisService**: Comparación, ranking y estadísticas de candidatos

### `/DTOs`
Data Transfer Objects para comunicación con la API:
- **CreateSessionRequest**: Request para crear sesión
- **UploadDocumentRequest**: Request para subir documentos
- **SessionStatusResponse**: Response con estado de sesión
- **ComparisonMatrixResponse**: Response con matriz de comparación

### `/Mappers`
Mapeo entre entidades de dominio y DTOs:
- **SessionMapper**: Conversiones Session <-> DTOs
- **DocumentMapper**: Conversiones Document <-> DTOs
- **CVDataMapper**: Conversiones CVData <-> DTOs

### `/Validators`
Validación de requests y reglas de negocio:
- **CreateSessionValidator**: Validación de creación de sesiones
- **FileUploadValidator**: Validación de archivos subidos
- **JobOfferValidator**: Validación de ofertas laborales

## Responsabilidades
- **Orquestación**: Coordina operaciones entre Core e Infrastructure
- **Validación**: Aplica reglas de negocio y validaciones
- **Transformación**: Convierte entre DTOs y entidades de dominio
- **Transacciones**: Maneja operaciones complejas multi-paso

## Principios
- **Single Responsibility**: Cada servicio tiene una responsabilidad clara
- **Dependency Inversion**: Depende de interfaces, no implementaciones
- **Fail Fast**: Validación temprana de inputs
- **Async/Await**: Operaciones asíncronas para mejor performance