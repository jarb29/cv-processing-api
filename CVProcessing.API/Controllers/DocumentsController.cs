using CVProcessing.Application.DTOs;
using CVProcessing.Application.Extensions;
using CVProcessing.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CVProcessing.API.Controllers;

/// <summary>
/// Controlador para gestión de documentos (CVs)
/// </summary>
[ApiController]
[Route("api/sessions/{sessionId:guid}/[controller]")]
[Produces("application/json")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly ISessionService _sessionService;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(
        IDocumentService documentService,
        ISessionService sessionService,
        ILogger<DocumentsController> logger)
    {
        _documentService = documentService;
        _sessionService = sessionService;
        _logger = logger;
    }

    /// <summary>
    /// Subir documentos (CVs) a una sesión
    /// </summary>
    /// <param name="sessionId">ID de la sesión</param>
    /// <param name="files">Archivos a subir</param>
    /// <returns>Resultado del upload</returns>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(UploadDocumentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [RequestSizeLimit(500_000_000)] // 500MB limit
    public async Task<ActionResult<UploadDocumentResponse>> UploadDocuments(
        Guid sessionId,
        [FromForm] IFormFileCollection files)
    {
        _logger.LogInformation("Uploading {Count} documents to session {SessionId}", files.Count, sessionId);

        // Verificar que la sesión existe
        var sessionExists = await _sessionService.ExistsAsync(sessionId);
        if (!sessionExists)
            return NotFound($"Session {sessionId} not found");

        // Validar que se enviaron archivos
        if (!files.Any())
            return BadRequest("No files provided");

        try
        {
            var response = await _documentService.UploadFromFormAsync(sessionId, files);
            return CreatedAtAction(
                nameof(GetSessionDocuments),
                new { sessionId },
                response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid upload request for session {SessionId}", sessionId);
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Obtener todos los documentos de una sesión
    /// </summary>
    /// <param name="sessionId">ID de la sesión</param>
    /// <returns>Lista de documentos</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<DocumentDetailsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<DocumentDetailsResponse>>> GetSessionDocuments(Guid sessionId)
    {
        _logger.LogDebug("Getting documents for session: {SessionId}", sessionId);

        var sessionExists = await _sessionService.ExistsAsync(sessionId);
        if (!sessionExists)
            return NotFound($"Session {sessionId} not found");

        var documents = await _documentService.GetBySessionIdAsync(sessionId);

        var response = documents.Select(d => new DocumentDetailsResponse
        {
            DocumentId = d.Id,
            FileName = d.FileName,
            Status = d.Status,
            FileSize = d.FileSize,
            UploadedAt = d.UploadedAt,
            ProcessedAt = d.ProcessedAt,
            ProcessingTimeMs = d.ProcessingTimeMs,
            ExtractedData = MapCVDataToDto(d.ExtractedData),
            ErrorMessage = d.ErrorMessage
        }).ToList();

        return Ok(response);
    }

    /// <summary>
    /// Obtener detalles de un documento específico
    /// </summary>
    /// <param name="sessionId">ID de la sesión</param>
    /// <param name="documentId">ID del documento</param>
    /// <returns>Detalles del documento</returns>
    [HttpGet("{documentId:guid}")]
    [ProducesResponseType(typeof(DocumentDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentDetailsResponse>> GetDocument(Guid sessionId, Guid documentId)
    {
        _logger.LogDebug("Getting document {DocumentId} from session {SessionId}", documentId, sessionId);

        var document = await _documentService.GetByIdAsync(documentId);
        if (document == null || document.SessionId != sessionId)
            return NotFound($"Document {documentId} not found in session {sessionId}");

        var response = new DocumentDetailsResponse
        {
            DocumentId = document.Id,
            FileName = document.FileName,
            Status = document.Status,
            FileSize = document.FileSize,
            UploadedAt = document.UploadedAt,
            ProcessedAt = document.ProcessedAt,
            ProcessingTimeMs = document.ProcessingTimeMs,
            ExtractedData = MapCVDataToDto(document.ExtractedData),
            ErrorMessage = document.ErrorMessage
        };

        return Ok(response);
    }

    /// <summary>
    /// Eliminar un documento
    /// </summary>
    /// <param name="sessionId">ID de la sesión</param>
    /// <param name="documentId">ID del documento</param>
    /// <returns>Confirmación de eliminación</returns>
    [HttpDelete("{documentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDocument(Guid sessionId, Guid documentId)
    {
        _logger.LogInformation("Deleting document {DocumentId} from session {SessionId}", documentId, sessionId);

        var document = await _documentService.GetByIdAsync(documentId);
        if (document == null || document.SessionId != sessionId)
            return NotFound($"Document {documentId} not found in session {sessionId}");

        await _documentService.DeleteAsync(documentId);
        return NoContent();
    }

    /// <summary>
    /// Procesar un documento específico
    /// </summary>
    /// <param name="sessionId">ID de la sesión</param>
    /// <param name="documentId">ID del documento</param>
    /// <returns>Datos extraídos del CV</returns>
    [HttpPost("{documentId:guid}/process")]
    [ProducesResponseType(typeof(CVDataDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CVDataDto>> ProcessDocument(Guid sessionId, Guid documentId)
    {
        _logger.LogInformation("Processing document {DocumentId} from session {SessionId}", documentId, sessionId);

        var session = await _sessionService.GetByIdAsync(sessionId);
        if (session == null)
            return NotFound($"Session {sessionId} not found");

        var document = await _documentService.GetByIdAsync(documentId);
        if (document == null || document.SessionId != sessionId)
            return NotFound($"Document {documentId} not found in session {sessionId}");

        try
        {
            var cvData = await _documentService.ProcessAsync(documentId, session.JobOffer);
            var response = MapCVDataToDto(cvData);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to process document {DocumentId}", documentId);
            return BadRequest(ex.Message);
        }
    }

    private static CVDataDto? MapCVDataToDto(Core.Entities.CVData? cvData)
    {
        if (cvData == null) return null;

        return new CVDataDto
        {
            PersonalInfo = new PersonalInfoDto
            {
                Name = cvData.PersonalInfo.Name,
                Email = cvData.PersonalInfo.Email,
                Phone = cvData.PersonalInfo.Phone,
                Location = cvData.PersonalInfo.Location,
                LinkedIn = cvData.PersonalInfo.LinkedIn,
                Summary = cvData.PersonalInfo.Summary
            },
            Experience = cvData.Experience.Select(e => new ExperienceDto
            {
                Company = e.Company,
                Position = e.Position,
                Duration = e.Duration,
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                Responsibilities = e.Responsibilities,
                Technologies = e.Technologies,
                IsCurrent = e.IsCurrent
            }).ToList(),
            Skills = cvData.Skills,
            Education = cvData.Education.Select(e => new EducationDto
            {
                Institution = e.Institution,
                Degree = e.Degree,
                Field = e.Field,
                Year = e.Year,
                Grade = e.Grade
            }).ToList(),
            Certifications = cvData.Certifications.Select(c => new CertificationDto
            {
                Name = c.Name,
                Issuer = c.Issuer,
                IssueDate = c.IssueDate,
                ExpiryDate = c.ExpiryDate,
                CredentialId = c.CredentialId
            }).ToList(),
            Languages = cvData.Languages.Select(l => new LanguageDto
            {
                Name = l.Name,
                Level = l.Level
            }).ToList(),
            Score = new CVScoreDto
            {
                Overall = cvData.Score.Overall,
                Experience = cvData.Score.Experience,
                Skills = cvData.Score.Skills,
                Education = cvData.Score.Education,
                JobMatch = cvData.Score.JobMatch
            }
        };
    }
}
