using CVProcessing.Core.Entities;
using CVProcessing.Core.Enums;

namespace CVProcessing.Core.Interfaces;

/// <summary>
/// Servicio para gestión de sesiones de procesamiento de CVs
/// </summary>
public interface ISessionService
{
    /// <summary>
    /// Crear una nueva sesión con una oferta laboral
    /// </summary>
    /// <param name="jobOffer">Oferta laboral de referencia</param>
    /// <returns>Sesión creada</returns>
    Task<Session> CreateAsync(JobOffer jobOffer);

    // This method is implemented in the Application layer
    // Task<CreateSessionResponse> CreateFromDtoAsync(CreateSessionRequest request);

    /// <summary>
    /// Obtener una sesión por su ID
    /// </summary>
    /// <param name="sessionId">ID de la sesión</param>
    /// <returns>Sesión encontrada o null</returns>
    Task<Session?> GetByIdAsync(Guid sessionId);

    // This method is implemented in the Application layer
    // Task<SessionStatusResponse?> GetStatusAsync(Guid sessionId);

    /// <summary>
    /// Actualizar el estado de una sesión
    /// </summary>
    /// <param name="sessionId">ID de la sesión</param>
    /// <param name="status">Nuevo estado</param>
    /// <param name="message">Mensaje de estado opcional</param>
    Task UpdateStatusAsync(Guid sessionId, SessionStatus status, string? message = null);

    /// <summary>
    /// Actualizar el progreso de una sesión
    /// </summary>
    /// <param name="sessionId">ID de la sesión</param>
    /// <param name="progress">Progreso (0-100)</param>
    Task UpdateProgressAsync(Guid sessionId, int progress);

    /// <summary>
    /// Obtener todas las sesiones (con paginación)
    /// </summary>
    /// <param name="page">Página (base 1)</param>
    /// <param name="pageSize">Tamaño de página</param>
    /// <returns>Lista paginada de sesiones</returns>
    Task<(List<Session> Sessions, int TotalCount)> GetAllAsync(int page = 1, int pageSize = 20);

    /// <summary>
    /// Eliminar una sesión y todos sus datos
    /// </summary>
    /// <param name="sessionId">ID de la sesión</param>
    Task DeleteAsync(Guid sessionId);

    /// <summary>
    /// Verificar si una sesión existe
    /// </summary>
    /// <param name="sessionId">ID de la sesión</param>
    /// <returns>True si existe</returns>
    Task<bool> ExistsAsync(Guid sessionId);
}
