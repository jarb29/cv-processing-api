using CVProcessing.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace CVProcessing.Infrastructure.Storage;

/// <summary>
/// Implementación de almacenamiento en sistema de archivos local
/// </summary>
public class LocalFileStorage : IFileStorage
{
    private readonly ILogger<LocalFileStorage> _logger;
    private readonly string _basePath;

    public LocalFileStorage(ILogger<LocalFileStorage> logger, string basePath = "storage")
    {
        _logger = logger;
        _basePath = Path.GetFullPath(basePath);
        
        // Crear directorio base si no existe
        Directory.CreateDirectory(_basePath);
    }

    public async Task<string> SaveFileAsync(string path, Stream content)
    {
        var fullPath = GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath)!;
        
        Directory.CreateDirectory(directory);
        
        using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
        await content.CopyToAsync(fileStream);
        
        _logger.LogInformation("File saved: {Path}", fullPath);
        return fullPath;
    }

    public async Task<string> SaveTextAsync(string path, string content)
    {
        var fullPath = GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath)!;
        
        Directory.CreateDirectory(directory);
        
        await File.WriteAllTextAsync(fullPath, content);
        
        _logger.LogInformation("Text file saved: {Path}", fullPath);
        return fullPath;
    }

    public async Task<Stream> ReadFileAsync(string path)
    {
        var fullPath = GetFullPath(path);
        
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"File not found: {fullPath}");
        
        var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
        return await Task.FromResult(fileStream);
    }

    public async Task<string> ReadTextAsync(string path)
    {
        var fullPath = GetFullPath(path);
        
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"File not found: {fullPath}");
        
        return await File.ReadAllTextAsync(fullPath);
    }

    public Task<bool> ExistsAsync(string path)
    {
        var fullPath = GetFullPath(path);
        return Task.FromResult(File.Exists(fullPath));
    }

    public Task DeleteFileAsync(string path)
    {
        var fullPath = GetFullPath(path);
        
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            _logger.LogInformation("File deleted: {Path}", fullPath);
        }
        
        return Task.CompletedTask;
    }

    public Task DeleteDirectoryAsync(string path)
    {
        var fullPath = GetFullPath(path);
        
        if (Directory.Exists(fullPath))
        {
            Directory.Delete(fullPath, recursive: true);
            _logger.LogInformation("Directory deleted: {Path}", fullPath);
        }
        
        return Task.CompletedTask;
    }

    public Task CreateDirectoryAsync(string path)
    {
        var fullPath = GetFullPath(path);
        Directory.CreateDirectory(fullPath);
        return Task.CompletedTask;
    }

    public Task<long> GetFileSizeAsync(string path)
    {
        var fullPath = GetFullPath(path);
        
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"File not found: {fullPath}");
        
        var fileInfo = new FileInfo(fullPath);
        return Task.FromResult(fileInfo.Length);
    }

    public Task<List<string>> ListFilesAsync(string path, string? pattern = null)
    {
        var fullPath = GetFullPath(path);
        
        if (!Directory.Exists(fullPath))
            return Task.FromResult(new List<string>());
        
        var searchPattern = pattern ?? "*";
        var files = Directory.GetFiles(fullPath, searchPattern, SearchOption.TopDirectoryOnly)
            .Select(f => Path.GetRelativePath(_basePath, f))
            .ToList();
        
        return Task.FromResult(files);
    }

    private string GetFullPath(string relativePath)
    {
        return Path.Combine(_basePath, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }
}