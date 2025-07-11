# CVProcessing.Core

## Propósito
Contiene los **modelos de dominio** y **contratos** fundamentales del sistema. Es el corazón del negocio sin dependencias externas.

## Estructura

### `/Entities`
Modelos de dominio que representan los conceptos principales:
- **Session**: Sesión de procesamiento de CVs
- **Document**: CV individual a procesar
- **JobOffer**: Oferta laboral de referencia
- **CVData**: Información extraída del CV
- **ComparisonMatrix**: Matriz de comparación entre candidatos

### `/Interfaces`
Contratos de servicios que definen QUÉ hace cada componente:
- **ISessionService**: Gestión de sesiones
- **IDocumentService**: Procesamiento de documentos
- **IOpenAIService**: Integración con OpenAI
- **IFileStorage**: Almacenamiento de archivos
- **IAnalysisService**: Análisis y comparación

### `/Enums`
Estados y tipos enumerados del sistema:
- **SessionStatus**: Estados de una sesión
- **DocumentStatus**: Estados de un documento
- **ProcessingStatus**: Estados del procesamiento

### `/Constants`
Valores constantes utilizados en todo el sistema:
- **StoragePaths**: Rutas de almacenamiento
- **OpenAIPrompts**: Templates de prompts
- **FileTypes**: Tipos de archivo soportados

## Principios
- **Sin dependencias externas**: Solo referencias a System.*
- **Inmutable cuando sea posible**: Records y propiedades readonly
- **Expresivo**: Nombres claros que reflejen el dominio del negocio
- **Validación básica**: Reglas de negocio fundamentales