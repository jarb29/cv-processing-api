using CVProcessing.Core.Constants;
using CVProcessing.Core.Entities;
using CVProcessing.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CVProcessing.Infrastructure.Storage;

/// <summary>
/// Repositorio para persistencia de sesiones en JSON
/// </summary>
public class SessionRepository
{
    private readonly IFileStorage _fileStorage;
    private readonly ILogger<SessionRepository> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public SessionRepository(IFileStorage fileStorage, ILogger<SessionRepository> logger)
    {
        _fileStorage = fileStorage;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
    }

    public async Task<Session> SaveAsync(Session session)
    {
        var sessionPath = GetSessionPath(session.Id);
        await _fileStorage.CreateDirectoryAsync(sessionPath);
        
        // Guardar metadata de la sesión
        var metadataPath = Path.Combine(sessionPath, StoragePaths.SessionMetadata);
        var sessionJson = JsonSerializer.Serialize(session, _jsonOptions);
        await _fileStorage.SaveTextAsync(metadataPath, sessionJson);
        
        // Guardar oferta laboral por separado
        var jobOfferPath = Path.Combine(sessionPath, StoragePaths.JobOfferFile);
        var jobOfferJson = JsonSerializer.Serialize(session.JobOffer, _jsonOptions);
        await _fileStorage.SaveTextAsync(jobOfferPath, jobOfferJson);
        
        _logger.LogInformation("Session saved: {SessionId}", session.Id);
        return session;
    }

    public async Task<Session?> GetByIdAsync(Guid sessionId)
    {
        try
        {
            var sessionPath = GetSessionPath(sessionId);
            var metadataPath = Path.Combine(sessionPath, StoragePaths.SessionMetadata);
            
            if (!await _fileStorage.ExistsAsync(metadataPath))
                return null;
            
            var sessionJson = await _fileStorage.ReadTextAsync(metadataPath);
            var session = JsonSerializer.Deserialize<Session>(sessionJson, _jsonOptions);
            
            return session;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading session: {SessionId}", sessionId);
            return null;
        }
    }

    public async Task<List<Session>> GetAllAsync()
    {
        var sessions = new List<Session>();
        
        try
        {
            if (!await _fileStorage.ExistsAsync(StoragePaths.Sessions))
                return sessions;
            
            var sessionDirs = await _fileStorage.ListFilesAsync(StoragePaths.Sessions);
            
            foreach (var sessionDir in sessionDirs)
            {
                if (Guid.TryParse(Path.GetFileName(sessionDir), out var sessionId))
                {
                    var session = await GetByIdAsync(sessionId);
                    if (session != null)
                        sessions.Add(session);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading all sessions");
        }
        
        return sessions;
    }

    public async Task UpdateAsync(Session session)
    {
        session.UpdatedAt = DateTime.UtcNow;
        await SaveAsync(session);
        _logger.LogInformation("Session updated: {SessionId}", session.Id);
    }

    public async Task DeleteAsync(Guid sessionId)
    {
        var sessionPath = GetSessionPath(sessionId);
        await _fileStorage.DeleteDirectoryAsync(sessionPath);
        _logger.LogInformation("Session deleted: {SessionId}", sessionId);
    }

    public async Task<bool> ExistsAsync(Guid sessionId)
    {
        var sessionPath = GetSessionPath(sessionId);
        var metadataPath = Path.Combine(sessionPath, StoragePaths.SessionMetadata);
        return await _fileStorage.ExistsAsync(metadataPath);
    }

    private static string GetSessionPath(Guid sessionId)
    {
        return Path.Combine(StoragePaths.Sessions, sessionId.ToString());
    }
}