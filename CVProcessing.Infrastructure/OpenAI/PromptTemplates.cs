using CVProcessing.Core.Entities;

namespace CVProcessing.Infrastructure.OpenAI;

/// <summary>
/// Templates de prompts para OpenAI
/// </summary>
public static class PromptTemplates
{
    /// <summary>
    /// Prompt para extraer datos estructurados de un CV
    /// </summary>
    public static string ExtractCVData(string cvText, JobOffer jobOffer) => $$"""
        Eres un experto en análisis de CVs y reclutamiento. Analiza el siguiente CV y extrae la información de forma estructurada.

        OFERTA LABORAL DE REFERENCIA:
        Título: {jobOffer.Title}
        Descripción: {jobOffer.Description}
        Habilidades requeridas: {string.Join(", ", jobOffer.RequiredSkills)}
        Experiencia mínima: {jobOffer.MinExperienceYears} años
        {(jobOffer.PreferredSkills.Count > 0 ? $"Habilidades deseables: {string.Join(", ", jobOffer.PreferredSkills)}" : "")}

        CV A ANALIZAR:
        {cvText}

        INSTRUCCIONES:
        1. Extrae toda la información personal, experiencia, habilidades, educación y certificaciones
        2. Calcula una puntuación de 0-100 para cada categoría basada en la oferta laboral
        3. Identifica años de experiencia relevante para el puesto
        4. Lista habilidades que coinciden y las que faltan
        5. Responde ÚNICAMENTE con un JSON válido siguiendo este esquema exacto:

        {
          "personalInfo": {
            "name": "string",
            "email": "string o null",
            "phone": "string o null",
            "location": "string o null",
            "linkedin": "string o null",
            "summary": "string o null"
          },
          "experience": [
            {
              "company": "string",
              "position": "string",
              "duration": "string",
              "startDate": "YYYY-MM-DD o null",
              "endDate": "YYYY-MM-DD o null",
              "responsibilities": ["string"],
              "technologies": ["string"],
              "isCurrent": boolean
            }
          ],
          "skills": ["string"],
          "education": [
            {
              "institution": "string",
              "degree": "string",
              "field": "string o null",
              "year": number o null,
              "grade": "string o null"
            }
          ],
          "certifications": [
            {
              "name": "string",
              "issuer": "string",
              "issueDate": "YYYY-MM-DD o null",
              "expiryDate": "YYYY-MM-DD o null",
              "credentialId": "string o null"
            }
          ],
          "languages": [
            {
              "name": "string",
              "level": "Básico|Intermedio|Avanzado|Nativo"
            }
          ],
          "score": {
            "overall": number (0-100),
            "experience": number (0-100),
            "skills": number (0-100),
            "education": number (0-100),
            "jobMatch": number (0-100)
          }
        }
        """;

    /// <summary>
    /// Prompt para generar comparación detallada
    /// </summary>
    public static string GenerateComparison(CVData cvData, JobOffer jobOffer) => $$"""
        Genera una comparación detallada entre este candidato y la oferta laboral.

        CANDIDATO:
        Nombre: {cvData.PersonalInfo.Name}
        Experiencia: {cvData.Experience.Count} trabajos
        Habilidades: {string.Join(", ", cvData.Skills)}
        Puntuación general: {cvData.Score.Overall}

        OFERTA LABORAL:
        {jobOffer.Title} - {jobOffer.Description}
        Habilidades requeridas: {string.Join(", ", jobOffer.RequiredSkills)}

        Proporciona:
        1. Habilidades que coinciden
        2. Habilidades que faltan
        3. Fortalezas del candidato
        4. Áreas de mejora
        5. Recomendación de contratación
        6. Años de experiencia relevante

        Responde en JSON con este formato:
        {
          "matchingSkills": ["string"],
          "missingSkills": ["string"],
          "strengths": ["string"],
          "weaknesses": ["string"],
          "recommendation": "HighlyRecommended|Recommended|Consider|NotRecommended",
          "relevantExperienceYears": number
        }
        """;

    /// <summary>
    /// Prompt para generar resumen ejecutivo
    /// </summary>
    public static string GenerateExecutiveSummary(CVData cvData, JobOffer jobOffer) => $$"""
        Genera un resumen ejecutivo profesional de este candidato para el puesto de {jobOffer.Title}.

        CANDIDATO: {cvData.PersonalInfo.Name}
        PUNTUACIÓN: {cvData.Score.Overall}/100
        EXPERIENCIA: {cvData.Experience.Count} posiciones
        HABILIDADES CLAVE: {string.Join(", ", cvData.Skills.Take(5))}

        Crea un resumen de 2-3 párrafos que incluya:
        1. Perfil profesional y experiencia relevante
        2. Fortalezas técnicas y soft skills
        3. Fit con la posición y recomendación

        Responde solo con el texto del resumen, sin formato JSON.
        """;
}
