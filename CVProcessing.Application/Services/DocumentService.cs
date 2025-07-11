using CVProcessing.Application.DTOs;
using CVProcessing.Core.Constants;
using CVProcessing.Core.Entities;
using CVProcessing.Core.Enums;
using CVProcessing.Core.Interfaces;
using CVProcessing.Infrastructure.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CVProcessing.Application.Services;

/// <summary>
/// Servicio de aplicación para gestión de documentos
/// </summary>
public class DocumentService : IDocumentService
{
    private readonly SessionRepository _sessionRepository;
    private readonly IFileStorage _fileStorage;
    private readonly IOpenAIService _openAIService;
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(
        SessionRepository sessionRepository,
        IFileStorage fileStorage,
        IOpenAIService openAIService,
        ILogger<DocumentService> logger)
    {
        _sessionRepository = sessionRepository;
        _fileStorage = fileStorage;
        _openAIService = openAIService;
        _logger = logger;
    }

    public async Task<Document> UploadAsync(Guid sessionId, string fileName, Stream fileContent, string contentType)
    {
        _logger.LogInformation("Uploading document {FileName} to session {SessionId}", fileName, sessionId);

        // Validar sesión existe
        var session = await _sessionRepository.GetByIdAsync(sessionId);
        if (session == null)
            throw new InvalidOperationException($"Session {sessionId} not found");

        // Validar archivo
        await ValidateFileAsync(fileName, fileContent.Length, contentType);

        // Crear documento con ID generado
        var documentId = Guid.NewGuid();

        // Calcular rutas
        var documentPath = GetDocumentPath(sessionId, documentId);
        await _fileStorage.CreateDirectoryAsync(documentPath);

        var inputPath = Path.Combine(documentPath, StoragePaths.Input);
        await _fileStorage.CreateDirectoryAsync(inputPath);

        var filePath = Path.Combine(inputPath, fileName);

        // Guardar archivo físico
        await _fileStorage.SaveFileAsync(filePath, fileContent);

        // Crear documento con la ruta ya establecida
        var document = new Document
        {
            Id = documentId,
            SessionId = sessionId,
            FileName = fileName,
            FileSize = fileContent.Length,
            ContentType = contentType,
            FilePath = filePath,
            Status = DocumentStatus.Uploaded
        };

        // Agregar documento a la sesión
        session.Documents.Add(document);
        await _sessionRepository.UpdateAsync(session);

        _logger.LogInformation("Document uploaded successfully: {DocumentId}", document.Id);
        return document;
    }

    public async Task<List<Document>> UploadBatchAsync(Guid sessionId, List<(string FileName, Stream Content, string ContentType)> files)
    {
        _logger.LogInformation("Uploading batch of {Count} documents to session {SessionId}", files.Count, sessionId);

        var documents = new List<Document>();
        var totalSize = files.Sum(f => f.Content.Length);

        // Validar límites de lote
        if (files.Count > FileTypes.MaxFilesPerBatch)
            throw new InvalidOperationException($"Batch exceeds maximum files limit: {FileTypes.MaxFilesPerBatch}");

        if (totalSize > FileTypes.MaxBatchSize)
            throw new InvalidOperationException($"Batch exceeds maximum size limit: {FileTypes.MaxBatchSize} bytes");

        // Subir cada archivo
        foreach (var file in files)
        {
            try
            {
                var document = await UploadAsync(sessionId, file.FileName, file.Content, file.ContentType);
                documents.Add(document);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload file {FileName}", file.FileName);

                // Crear documento con estado de error
                var errorDocumentId = Guid.NewGuid();
                var errorFilePath = Path.Combine(
                    StoragePaths.Sessions,
                    sessionId.ToString(),
                    StoragePaths.Documents,
                    errorDocumentId.ToString(),
                    "error.txt");

                var errorDocument = new Document
                {
                    Id = errorDocumentId,
                    SessionId = sessionId,
                    FileName = file.FileName,
                    FileSize = file.Content.Length,
                    ContentType = file.ContentType,
                    FilePath = errorFilePath,
                    Status = DocumentStatus.Failed,
                    ErrorMessage = ex.Message
                };
                documents.Add(errorDocument);
            }
        }

        return documents;
    }

    public async Task<Document?> GetByIdAsync(Guid documentId)
    {
        _logger.LogDebug("Retrieving document: {DocumentId}", documentId);

        var allSessions = await _sessionRepository.GetAllAsync();

        foreach (var session in allSessions)
        {
            var document = session.Documents.FirstOrDefault(d => d.Id == documentId);
            if (document != null) return document;
        }

        return null;
    }

    public async Task<List<Document>> GetBySessionIdAsync(Guid sessionId)
    {
        _logger.LogDebug("Retrieving documents for session: {SessionId}", sessionId);

        var session = await _sessionRepository.GetByIdAsync(sessionId);
        return session?.Documents ?? new List<Document>();
    }

    public async Task<CVData> ProcessAsync(Guid documentId, JobOffer jobOffer)
    {
        _logger.LogInformation("Processing document: {DocumentId}", documentId);

        var document = await GetByIdAsync(documentId);
        if (document == null)
            throw new InvalidOperationException($"Document {documentId} not found");

        try
        {
            // Actualizar estado
            document.Status = DocumentStatus.Analyzing;
            await UpdateDocumentInSession(document);

            // Leer contenido del archivo
            // Aqui va el OCR SE SUSTITUYE ESTA LINEA
            var fileContent = await _fileStorage.ReadTextAsync(document.FilePath);


            // Procesar con OpenAI
            var cvData = await _openAIService.ExtractCVDataAsync(fileContent, jobOffer);

            // Guardar datos extraídos
            document.ExtractedData = cvData;
            document.Status = DocumentStatus.Processed;
            document.ProcessedAt = DateTime.UtcNow;

            // Guardar resultado en archivo
            var outputPath = Path.Combine(GetDocumentPath(document.SessionId, document.Id), StoragePaths.Output);
            await _fileStorage.CreateDirectoryAsync(outputPath);

            var dataPath = Path.Combine(outputPath, "extracted-data.json");
            var jsonData = System.Text.Json.JsonSerializer.Serialize(cvData, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            await _fileStorage.SaveTextAsync(dataPath, jsonData);

            await UpdateDocumentInSession(document);

            _logger.LogInformation("Document processed successfully: {DocumentId}", documentId);
            return cvData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process document: {DocumentId}", documentId);

            document.Status = DocumentStatus.Failed;
            document.ErrorMessage = ex.Message;
            await UpdateDocumentInSession(document);

            throw;
        }
    }

    public async Task<List<CVData>> ProcessSessionDocumentsAsync(Guid sessionId)
    {
        _logger.LogInformation("Processing all documents for session: {SessionId}", sessionId);

        var session = await _sessionRepository.GetByIdAsync(sessionId);
        if (session == null)
            throw new InvalidOperationException($"Session {sessionId} not found");

        var results = new List<CVData>();
        var uploadedDocuments = session.Documents.Where(d => d.Status == DocumentStatus.Uploaded).ToList();

        foreach (var document in uploadedDocuments)
        {
            try
            {
                var cvData = await ProcessAsync(document.Id, session.JobOffer);
                results.Add(cvData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process document {DocumentId} in session {SessionId}", document.Id, sessionId);
            }
        }

        return results;
    }

    public async Task DeleteAsync(Guid documentId)
    {
        _logger.LogInformation("Deleting document: {DocumentId}", documentId);

        var document = await GetByIdAsync(documentId);
        if (document == null) return;

        // Eliminar archivos físicos
        var documentPath = GetDocumentPath(document.SessionId, document.Id);
        await _fileStorage.DeleteDirectoryAsync(documentPath);

        // Remover de la sesión
        var session = await _sessionRepository.GetByIdAsync(document.SessionId);
        if (session != null)
        {
            session.Documents.RemoveAll(d => d.Id == documentId);
            await _sessionRepository.UpdateAsync(session);
        }
    }

    public async Task<bool> ValidateFileAsync(string fileName, long fileSize, string contentType)
    {
        // Validar extensión
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!FileTypes.SupportedExtensions.Contains(extension))
            throw new InvalidOperationException($"File type not supported: {extension}");

        // Validar tipo MIME
        if (!FileTypes.SupportedMimeTypes.Contains(contentType))
            throw new InvalidOperationException($"MIME type not supported: {contentType}");

        // Validar tamaño
        if (fileSize > FileTypes.MaxFileSize)
            throw new InvalidOperationException($"File size exceeds limit: {fileSize} bytes");

        if (fileSize == 0)
            throw new InvalidOperationException("File is empty");

        return true;
    }

    /// <summary>
    /// Subir documentos desde IFormFile
    /// </summary>
    public async Task<UploadDocumentResponse> UploadFromFormAsync(Guid sessionId, IFormFileCollection files)
    {
        var fileData = new List<(string FileName, Stream Content, string ContentType)>();

        foreach (var file in files)
        {
            fileData.Add((file.FileName, file.OpenReadStream(), file.ContentType));
        }

        var documents = await UploadBatchAsync(sessionId, fileData);

        var results = documents.Select(d => new DocumentUploadResult
        {
            DocumentId = d.Id,
            FileName = d.FileName,
            Size = d.FileSize,
            Status = d.Status,
            ErrorMessage = d.ErrorMessage
        }).ToList();

        return new UploadDocumentResponse
        {
            SessionId = sessionId,
            UploadedDocuments = results,
            TotalUploaded = results.Count(r => r.Status == DocumentStatus.Uploaded)
        };
    }

    private async Task UpdateDocumentInSession(Document document)
    {
        var session = await _sessionRepository.GetByIdAsync(document.SessionId);
        if (session != null)
        {
            var existingDoc = session.Documents.FirstOrDefault(d => d.Id == document.Id);
            if (existingDoc != null)
            {
                var index = session.Documents.IndexOf(existingDoc);
                session.Documents[index] = document;
                await _sessionRepository.UpdateAsync(session);
            }
        }
    }

    private static string GetDocumentPath(Guid sessionId, Guid documentId)
    {
        return Path.Combine(StoragePaths.Sessions, sessionId.ToString(), StoragePaths.Documents, documentId.ToString());
    }
}
