using CVProcessing.Core.Enums;

namespace CVProcessing.Core.Entities;

/// <summary>
/// Representa un documento (CV) individual en el sistema
/// </summary>
public class Document
{
    /// <summary>
    /// Identificador único del documento
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();
    
    /// <summary>
    /// ID de la sesión a la que pertenece
    /// </summary>
    public required Guid SessionId { get; init; }
    
    /// <summary>
    /// Nombre original del archivo
    /// </summary>
    public required string FileName { get; init; }
    
    /// <summary>
    /// Ruta donde se almacena el archivo original
    /// </summary>
    public required string FilePath { get; init; }
    
    /// <summary>
    /// Tamaño del archivo en bytes
    /// </summary>
    public long FileSize { get; init; }
    
    /// <summary>
    /// Tipo MIME del archivo
    /// </summary>
    public required string ContentType { get; init; }
    
    /// <summary>
    /// Estado actual del procesamiento
    /// </summary>
    public DocumentStatus Status { get; set; } = DocumentStatus.Uploaded;
    
    /// <summary>
    /// Datos extraídos del CV (null hasta que se procese)
    /// </summary>
    public CVData? ExtractedData { get; set; }
    
    /// <summary>
    /// Texto extraído del documento (OCR o directo)
    /// </summary>
    public string? ExtractedText { get; set; }
    
    /// <summary>
    /// Mensaje de error si el procesamiento falló
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Fecha de subida del documento
    /// </summary>
    public DateTime UploadedAt { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Fecha de último procesamiento
    /// </summary>
    public DateTime? ProcessedAt { get; set; }
    
    /// <summary>
    /// Tiempo total de procesamiento en milisegundos
    /// </summary>
    public long? ProcessingTimeMs { get; set; }
}