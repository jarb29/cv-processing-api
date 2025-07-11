using CVProcessing.Application.DTOs;
using CVProcessing.Application.Extensions;
using CVProcessing.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CVProcessing.API.Controllers;

/// <summary>
/// Controlador para gestión de sesiones de procesamiento de CVs
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class SessionsController : ControllerBase
{
    private readonly ISessionService _sessionService;
    private readonly ILogger<SessionsController> _logger;

    public SessionsController(ISessionService sessionService, ILogger<SessionsController> logger)
    {
        _sessionService = sessionService;
        _logger = logger;
    }

    /// <summary>
    /// Crear una nueva sesión de procesamiento
    /// </summary>
    /// <param name="request">Datos de la sesión a crear</param>
    /// <returns>Sesión creada</returns>
    [HttpPost]
    [ProducesResponseType(typeof(CreateSessionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateSessionResponse>> CreateSession([FromBody] CreateSessionRequest request)
    {
        _logger.LogInformation("Creating new session for job: {JobTitle}", request.JobOffer.Title);

        var response = await _sessionService.CreateFromDtoAsync(request);

        return CreatedAtAction(
            nameof(GetSession),
            new { id = response.SessionId },
            response);
    }

    /// <summary>
    /// Obtener una sesión por ID
    /// </summary>
    /// <param name="id">ID de la sesión</param>
    /// <returns>Datos de la sesión</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SessionStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SessionStatusResponse>> GetSession(Guid id)
    {
        _logger.LogDebug("Getting session: {SessionId}", id);

        var session = await _sessionService.GetStatusAsync(id);
        if (session == null)
            return NotFound($"Session {id} not found");

        return Ok(session);
    }

    /// <summary>
    /// Obtener estado de procesamiento de una sesión
    /// </summary>
    /// <param name="id">ID de la sesión</param>
    /// <returns>Estado actual del procesamiento</returns>
    [HttpGet("{id:guid}/status")]
    [ProducesResponseType(typeof(SessionStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SessionStatusResponse>> GetSessionStatus(Guid id)
    {
        _logger.LogDebug("Getting session status: {SessionId}", id);

        var status = await _sessionService.GetStatusAsync(id);
        if (status == null)
            return NotFound($"Session {id} not found");

        return Ok(status);
    }

    /// <summary>
    /// Obtener lista paginada de sesiones
    /// </summary>
    /// <param name="page">Número de página (base 1)</param>
    /// <param name="pageSize">Tamaño de página</param>
    /// <returns>Lista paginada de sesiones</returns>
    [HttpGet]
    [ProducesResponseType(typeof(SessionListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<SessionListResponse>> GetSessions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        _logger.LogDebug("Getting sessions page {Page}, size {PageSize}", page, pageSize);

        var (sessions, totalCount) = await _sessionService.GetAllAsync(page, pageSize);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var sessionSummaries = sessions.Select(s => new SessionSummaryDto
        {
            Id = s.Id,
            JobTitle = s.JobOffer.Title,
            Status = s.Status,
            DocumentCount = s.Documents.Count,
            Progress = s.Progress,
            CreatedAt = s.CreatedAt,
            CompletedAt = s.CompletedAt
        }).ToList();

        var response = new SessionListResponse
        {
            Sessions = sessionSummaries,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages
        };

        return Ok(response);
    }

    /// <summary>
    /// Eliminar una sesión
    /// </summary>
    /// <param name="id">ID de la sesión</param>
    /// <returns>Confirmación de eliminación</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSession(Guid id)
    {
        _logger.LogInformation("Deleting session: {SessionId}", id);

        var exists = await _sessionService.ExistsAsync(id);
        if (!exists)
            return NotFound($"Session {id} not found");

        await _sessionService.DeleteAsync(id);
        return NoContent();
    }
}
