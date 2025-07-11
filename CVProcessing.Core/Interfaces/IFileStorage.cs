namespace CVProcessing.Core.Interfaces;

/// <summary>
/// Servicio para almacenamiento de archivos (local o cloud)
/// </summary>
public interface IFileStorage
{
    /// <summary>
    /// Guardar un archivo
    /// </summary>
    /// <param name="path">Ruta relativa donde guardar</param>
    /// <param name="content">Contenido del archivo</param>
    /// <returns>Ruta completa del archivo guardado</returns>
    Task<string> SaveFileAsync(string path, Stream content);
    
    /// <summary>
    /// Guardar contenido de texto
    /// </summary>
    /// <param name="path">Ruta relativa donde guardar</param>
    /// <param name="content">Contenido de texto</param>
    /// <returns>Ruta completa del archivo guardado</returns>
    Task<string> SaveTextAsync(string path, string content);
    
    /// <summary>
    /// Leer un archivo como stream
    /// </summary>
    /// <param name="path">Ruta del archivo</param>
    /// <returns>Stream del archivo</returns>
    Task<Stream> ReadFileAsync(string path);
    
    /// <summary>
    /// Leer un archivo como texto
    /// </summary>
    /// <param name="path">Ruta del archivo</param>
    /// <returns>Contenido del archivo</returns>
    Task<string> ReadTextAsync(string path);
    
    /// <summary>
    /// Verificar si un archivo existe
    /// </summary>
    /// <param name="path">Ruta del archivo</param>
    /// <returns>True si existe</returns>
    Task<bool> ExistsAsync(string path);
    
    /// <summary>
    /// Eliminar un archivo
    /// </summary>
    /// <param name="path">Ruta del archivo</param>
    Task DeleteFileAsync(string path);
    
    /// <summary>
    /// Eliminar un directorio y todo su contenido
    /// </summary>
    /// <param name="path">Ruta del directorio</param>
    Task DeleteDirectoryAsync(string path);
    
    /// <summary>
    /// Crear un directorio si no existe
    /// </summary>
    /// <param name="path">Ruta del directorio</param>
    Task CreateDirectoryAsync(string path);
    
    /// <summary>
    /// Obtener el tamaño de un archivo
    /// </summary>
    /// <param name="path">Ruta del archivo</param>
    /// <returns>Tamaño en bytes</returns>
    Task<long> GetFileSizeAsync(string path);
    
    /// <summary>
    /// Listar archivos en un directorio
    /// </summary>
    /// <param name="path">Ruta del directorio</param>
    /// <param name="pattern">Patrón de búsqueda (opcional)</param>
    /// <returns>Lista de rutas de archivos</returns>
    Task<List<string>> ListFilesAsync(string path, string? pattern = null);
}