namespace CVProcessing.Core.Enums;

/// <summary>
/// Estados posibles de una sesión de procesamiento de CVs
/// </summary>
public enum SessionStatus
{
    /// <summary>
    /// Sesión creada, esperando documentos
    /// </summary>
    Created = 0,
    
    /// <summary>
    /// Procesando documentos cargados
    /// </summary>
    Processing = 1,
    
    /// <summary>
    /// Procesamiento completado exitosamente
    /// </summary>
    Completed = 2,
    
    /// <summary>
    /// Error durante el procesamiento
    /// </summary>
    Failed = 3,
    
    /// <summary>
    /// Sesión cancelada por el usuario
    /// </summary>
    Cancelled = 4
}