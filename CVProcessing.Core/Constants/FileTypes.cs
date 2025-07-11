namespace CVProcessing.Core.Constants;

/// <summary>
/// Tipos de archivo soportados y configuraciones relacionadas
/// </summary>
public static class FileTypes
{
    /// <summary>
    /// Extensiones de archivo soportadas para CVs
    /// </summary>
    public static readonly string[] SupportedExtensions = { ".pdf", ".doc", ".docx" };
    
    /// <summary>
    /// Tipos MIME soportados
    /// </summary>
    public static readonly string[] SupportedMimeTypes = 
    {
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    };
    
    /// <summary>
    /// Tamaño máximo de archivo en bytes (10MB)
    /// </summary>
    public const long MaxFileSize = 10 * 1024 * 1024;
    
    /// <summary>
    /// Número máximo de archivos por lote
    /// </summary>
    public const int MaxFilesPerBatch = 100;
    
    /// <summary>
    /// Tamaño máximo total del lote en bytes (500MB)
    /// </summary>
    public const long MaxBatchSize = 500 * 1024 * 1024;
}