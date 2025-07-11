using CVProcessing.Application.DTOs;
using CVProcessing.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CVProcessing.Application.Extensions;

/// <summary>
/// Extension methods for IDocumentService
/// </summary>
public static class DocumentServiceExtensions
{
    /// <summary>
    /// Subir documentos desde IFormFile
    /// </summary>
    /// <param name="documentService">The document service</param>
    /// <param name="sessionId">ID de la sesión</param>
    /// <param name="files">Colección de archivos del formulario</param>
    /// <returns>Respuesta con resultados de la subida</returns>
    public static async Task<UploadDocumentResponse> UploadFromFormAsync(this IDocumentService documentService, Guid sessionId, IFormFileCollection files)
    {
        var fileData = new List<(string FileName, Stream Content, string ContentType)>();

        foreach (var file in files)
        {
            fileData.Add((file.FileName, file.OpenReadStream(), file.ContentType));
        }

        var documents = await documentService.UploadBatchAsync(sessionId, fileData);

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
            TotalUploaded = results.Count(r => r.Status == Core.Enums.DocumentStatus.Uploaded)
        };
    }
}
