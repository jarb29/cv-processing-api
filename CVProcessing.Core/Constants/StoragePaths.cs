namespace CVProcessing.Core.Constants;

/// <summary>
/// Rutas y estructura de almacenamiento del sistema
/// </summary>
public static class StoragePaths
{
    /// <summary>
    /// Directorio base para todas las sesiones
    /// </summary>
    public const string Sessions = "storage/sessions";
    
    /// <summary>
    /// Directorio para documentos dentro de una sesión
    /// </summary>
    public const string Documents = "documents";
    
    /// <summary>
    /// Directorio para archivos originales
    /// </summary>
    public const string Input = "input";
    
    /// <summary>
    /// Directorio para resultados procesados
    /// </summary>
    public const string Output = "output";
    
    /// <summary>
    /// Directorio para análisis y matrices
    /// </summary>
    public const string Analysis = "analysis";
    
    /// <summary>
    /// Directorio para logs del sistema
    /// </summary>
    public const string Logs = "logs";
    
    /// <summary>
    /// Archivo de metadatos de la sesión
    /// </summary>
    public const string SessionMetadata = "metadata.json";
    
    /// <summary>
    /// Archivo de oferta laboral
    /// </summary>
    public const string JobOfferFile = "job-offer.json";
    
    /// <summary>
    /// Archivo de matriz de comparación
    /// </summary>
    public const string ComparisonMatrix = "comparison-matrix.json";
}