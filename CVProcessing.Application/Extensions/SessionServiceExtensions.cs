using CVProcessing.Application.DTOs;
using CVProcessing.Core.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CVProcessing.Application.Extensions;

/// <summary>
/// Extension methods for ISessionService
/// </summary>
public static class SessionServiceExtensions
{
    /// <summary>
    /// Crear una nueva sesión a partir de un DTO
    /// </summary>
    /// <param name="sessionService">The session service</param>
    /// <param name="request">DTO con datos de la sesión</param>
    /// <returns>Respuesta con datos de la sesión creada</returns>
    public static async Task<CreateSessionResponse> CreateFromDtoAsync(this ISessionService sessionService, CreateSessionRequest request)
    {
        // Convert JobOfferDto to JobOffer
        var jobOffer = new CVProcessing.Core.Entities.JobOffer
        {
            Title = request.JobOffer.Title,
            Description = request.JobOffer.Description,
            RequiredSkills = request.JobOffer.RequiredSkills,
            PreferredSkills = request.JobOffer.PreferredSkills,
            MinExperienceYears = request.JobOffer.MinExperienceYears,
            EducationLevel = request.JobOffer.EducationLevel,
            Location = request.JobOffer.Location,
            WorkMode = request.JobOffer.WorkMode,
            SalaryRange = request.JobOffer.SalaryRange != null
                ? new CVProcessing.Core.Entities.SalaryRange
                {
                    Min = request.JobOffer.SalaryRange.Min,
                    Max = request.JobOffer.SalaryRange.Max,
                    Currency = request.JobOffer.SalaryRange.Currency
                }
                : null
        };

        var session = await sessionService.CreateAsync(jobOffer);

        return new CreateSessionResponse
        {
            SessionId = session.Id,
            Status = session.Status,
            CreatedAt = session.CreatedAt
        };
    }

    /// <summary>
    /// Obtener el estado de una sesión
    /// </summary>
    /// <param name="sessionService">The session service</param>
    /// <param name="sessionId">ID de la sesión</param>
    /// <returns>Estado de la sesión o null si no existe</returns>
    public static async Task<SessionStatusResponse?> GetStatusAsync(this ISessionService sessionService, Guid sessionId)
    {
        var session = await sessionService.GetByIdAsync(sessionId);
        if (session == null) return null;

        return new SessionStatusResponse
        {
            SessionId = session.Id,
            Status = session.Status,
            TotalDocuments = session.Documents.Count,
            ProcessedDocuments = session.Documents.Count(d =>
                d.Status == Core.Enums.DocumentStatus.Processed ||
                d.Status == Core.Enums.DocumentStatus.Failed),
            Progress = session.Progress,
            StatusMessage = session.StatusMessage
        };
    }
}
