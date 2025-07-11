using CVProcessing.Core.Entities;

namespace CVProcessing.Core.Interfaces;

/// <summary>
/// Servicio para gestión y procesamiento de documentos (CVs)
/// </summary>
public interface IDocumentService
{
    /// <summary>
    /// Subir un documento a una sesión
    /// </summary>
    /// <param name="sessionId">ID de la sesión</param>
    /// <param name="fileName">Nombre del archivo</param>
    /// <param name="fileContent">Contenido del archivo</param>
    /// <param name="contentType">Tipo MIME del archivo</param>
    /// <returns>Documento creado</returns>
    Task<Document> UploadAsync(Guid sessionId, string fileName, Stream fileContent, string contentType);

    /// <summary>
    /// Subir múltiples documentos en lote
    /// </summary>
    /// <param name="sessionId">ID de la sesión</param>
    /// <param name="files">Lista de archivos a subir</param>
    /// <returns>Lista de documentos creados</returns>
    Task<List<Document>> UploadBatchAsync(Guid sessionId, List<(string FileName, Stream Content, string ContentType)> files);

    /// <summary>
    /// Obtener un documento por su ID
    /// </summary>
    /// <param name="documentId">ID del documento</param>
    /// <returns>Documento encontrado o null</returns>
    Task<Document?> GetByIdAsync(Guid documentId);

    /// <summary>
    /// Obtener todos los documentos de una sesión
    /// </summary>
    /// <param name="sessionId">ID de la sesión</param>
    /// <returns>Lista de documentos</returns>
    Task<List<Document>> GetBySessionIdAsync(Guid sessionId);

    /// <summary>
    /// Procesar un documento (extraer texto y analizar con IA)
    /// </summary>
    /// <param name="documentId">ID del documento</param>
    /// <param name="jobOffer">Oferta laboral de referencia</param>
    /// <returns>Datos extraídos del CV</returns>
    Task<CVData> ProcessAsync(Guid documentId, JobOffer jobOffer);

    /// <summary>
    /// Procesar todos los documentos de una sesión
    /// </summary>
    /// <param name="sessionId">ID de la sesión</param>
    /// <returns>Lista de datos extraídos</returns>
    Task<List<CVData>> ProcessSessionDocumentsAsync(Guid sessionId);

    /// <summary>
    /// Eliminar un documento
    /// </summary>
    /// <param name="documentId">ID del documento</param>
    Task DeleteAsync(Guid documentId);

    /// <summary>
    /// Validar si un archivo es válido para procesamiento
    /// </summary>
    /// <param name="fileName">Nombre del archivo</param>
    /// <param name="fileSize">Tamaño del archivo</param>
    /// <param name="contentType">Tipo MIME</param>
    /// <returns>True si es válido, excepción si no</returns>
    Task<bool> ValidateFileAsync(string fileName, long fileSize, string contentType);

    // This method is implemented in the Application layer
    // Task<UploadDocumentResponse> UploadFromFormAsync(Guid sessionId, IFormFileCollection files);
}
