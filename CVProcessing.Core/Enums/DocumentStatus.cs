namespace CVProcessing.Core.Enums;

/// <summary>
/// Estados posibles de un documento (CV) durante el procesamiento
/// </summary>
public enum DocumentStatus
{
    /// <summary>
    /// Documento subido, esperando procesamiento
    /// </summary>
    Uploaded = 0,
    
    /// <summary>
    /// Extrayendo texto del documento (OCR)
    /// </summary>
    Extracting = 1,
    
    /// <summary>
    /// Analizando con OpenAI
    /// </summary>
    Analyzing = 2,
    
    /// <summary>
    /// Procesamiento completado exitosamente
    /// </summary>
    Processed = 3,
    
    /// <summary>
    /// Error durante el procesamiento
    /// </summary>
    Failed = 4,
    
    /// <summary>
    /// Documento rechazado (formato no válido)
    /// </summary>
    Rejected = 5
}