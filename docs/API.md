# API Reference

## Base URL
```
Local: https://localhost:7001/api
Production: https://your-domain.com/api
```

## Authentication
```http
Authorization: Bearer {jwt-token}
```

## Endpoints

### Sessions

#### Create Session
```http
POST /api/sessions
Content-Type: application/json

{
  "jobOffer": {
    "title": "Senior Developer",
    "description": "Desarrollador senior con experiencia en .NET",
    "requirements": ["C#", ".NET", "SQL Server"],
    "experience": "5+ años"
  }
}
```

**Response:**
```json
{
  "sessionId": "123e4567-e89b-12d3-a456-426614174000",
  "status": "created",
  "createdAt": "2024-01-15T10:30:00Z"
}
```

#### Get Session Status
```http
GET /api/sessions/{sessionId}/status
```

**Response:**
```json
{
  "sessionId": "123e4567-e89b-12d3-a456-426614174000",
  "status": "processing",
  "totalDocuments": 10,
  "processedDocuments": 7,
  "progress": 70,
  "estimatedTimeRemaining": "00:05:30"
}
```

### Documents

#### Upload Documents
```http
POST /api/sessions/{sessionId}/documents
Content-Type: multipart/form-data

files: [file1.pdf, file2.pdf, ...]
```

**Response:**
```json
{
  "sessionId": "123e4567-e89b-12d3-a456-426614174000",
  "uploadedDocuments": [
    {
      "documentId": "doc-uuid-1",
      "fileName": "cv1.pdf",
      "size": 1024000,
      "status": "uploaded"
    }
  ],
  "totalUploaded": 5
}
```

#### Get Document Details
```http
GET /api/sessions/{sessionId}/documents/{documentId}
```

**Response:**
```json
{
  "documentId": "doc-uuid-1",
  "fileName": "cv1.pdf",
  "status": "processed",
  "extractedData": {
    "personalInfo": {
      "name": "Juan Pérez",
      "email": "juan@email.com",
      "phone": "+1234567890"
    },
    "experience": [
      {
        "company": "Tech Corp",
        "position": "Senior Developer",
        "duration": "2020-2024",
        "responsibilities": ["Desarrollo .NET", "Liderazgo técnico"]
      }
    ],
    "skills": ["C#", ".NET", "SQL Server", "Azure"],
    "education": [
      {
        "institution": "Universidad XYZ",
        "degree": "Ingeniería en Sistemas",
        "year": 2019
      }
    ],
    "score": {
      "overall": 85,
      "experience": 90,
      "skills": 80,
      "education": 85
    }
  }
}
```

### Analysis

#### Get Comparison Matrix
```http
GET /api/sessions/{sessionId}/matrix
```

**Response:**
```json
{
  "sessionId": "123e4567-e89b-12d3-a456-426614174000",
  "matrix": {
    "candidates": [
      {
        "documentId": "doc-uuid-1",
        "name": "Juan Pérez",
        "overallScore": 85,
        "scores": {
          "experience": 90,
          "skills": 80,
          "education": 85,
          "jobMatch": 88
        },
        "ranking": 1
      },
      {
        "documentId": "doc-uuid-2",
        "name": "María García",
        "overallScore": 78,
        "scores": {
          "experience": 75,
          "skills": 85,
          "education": 80,
          "jobMatch": 72
        },
        "ranking": 2
      }
    ],
    "statistics": {
      "totalCandidates": 10,
      "averageScore": 75.5,
      "topScore": 85,
      "processingTime": "00:15:30"
    }
  }
}
```

#### Get Rankings
```http
GET /api/sessions/{sessionId}/rankings?sortBy=overall&order=desc&limit=10
```

**Query Parameters:**
- `sortBy`: overall, experience, skills, education, jobMatch
- `order`: asc, desc
- `limit`: number of results (default: 50)

### Job Offers

#### Create Job Offer
```http
POST /api/job-offers
Content-Type: application/json

{
  "title": "Senior .NET Developer",
  "description": "Buscamos desarrollador senior con experiencia en .NET",
  "requirements": [
    "5+ años experiencia en .NET",
    "Conocimiento en Azure",
    "Experiencia con SQL Server"
  ],
  "preferredSkills": ["Docker", "Kubernetes", "DevOps"],
  "location": "Madrid, España",
  "salaryRange": {
    "min": 50000,
    "max": 70000,
    "currency": "EUR"
  },
  "workMode": "hybrid"
}
```

#### Get Job Offer
```http
GET /api/job-offers/{jobOfferId}
```

### Health Check

#### API Health
```http
GET /api/health
```

**Response:**
```json
{
  "status": "Healthy",
  "checks": {
    "database": "Healthy",
    "openai": "Healthy",
    "storage": "Healthy"
  },
  "duration": "00:00:01.234"
}
```

## WebSocket Events (SignalR)

### Connection
```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/processingHub")
    .build();
```

### Events

#### Processing Progress
```javascript
connection.on("ProcessingProgress", (sessionId, progress) => {
    console.log(`Session ${sessionId}: ${progress}% complete`);
});
```

#### Document Processed
```javascript
connection.on("DocumentProcessed", (sessionId, documentId, result) => {
    console.log(`Document ${documentId} processed`);
});
```

#### Analysis Complete
```javascript
connection.on("AnalysisComplete", (sessionId, matrix) => {
    console.log("Analysis complete", matrix);
});
```

## Error Responses

### Standard Error Format
```json
{
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Invalid request data",
    "details": [
      {
        "field": "jobOffer.title",
        "message": "Title is required"
      }
    ],
    "traceId": "trace-id-123"
  }
}
```

### HTTP Status Codes
- `200` - Success
- `201` - Created
- `400` - Bad Request
- `401` - Unauthorized
- `403` - Forbidden
- `404` - Not Found
- `409` - Conflict
- `422` - Unprocessable Entity
- `429` - Too Many Requests
- `500` - Internal Server Error

## Rate Limiting

```http
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 95
X-RateLimit-Reset: 1642694400
```

## Pagination

```http
GET /api/sessions?page=1&pageSize=20
```

**Response Headers:**
```http
X-Total-Count: 150
X-Page-Count: 8
Link: <https://api.example.com/sessions?page=2>; rel="next"
```

## File Upload Limits

- **Max file size**: 10MB per file
- **Supported formats**: PDF, DOC, DOCX
- **Max files per batch**: 100
- **Total batch size**: 500MB