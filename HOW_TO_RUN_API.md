# How to Run the CV Processing API

This guide explains how to set up and run the CV Processing API endpoints locally.

## Prerequisites

- .NET 9 SDK installed
- OpenAI API Key (for AI-powered CV analysis)
- Git (to clone the repository)

## Setup Steps

### 1. Clone the Repository

```bash
git clone <repository-url>
cd endpoint-hr
```

### 2. Configure OpenAI API Key

Create or modify the `appsettings.json` file in the `CVProcessing.API` directory:

```json
{
  "OpenAI": {
    "ApiKey": "your-openai-api-key"
  }
}
```

Alternatively, you can set it as an environment variable:

```bash
# For Windows
set OPENAI_API_KEY=your-openai-api-key

# For macOS/Linux
export OPENAI_API_KEY=your-openai-api-key
```

### 3. Build and Run the API

```bash
# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run the API
dotnet run --project CVProcessing.API
```

The API will start running at `https://localhost:7001/api`.

### 4. Access Swagger Documentation

Once the API is running, you can access the Swagger UI to explore and test the endpoints:

```
https://localhost:7001/swagger
```

## Using the API

### Main Endpoints

1. **Create a Session**
   ```http
   POST https://localhost:7001/api/sessions
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

2. **Upload Documents to a Session**
   ```http
   POST https://localhost:7001/api/sessions/{sessionId}/documents
   Content-Type: multipart/form-data

   files: [file1.pdf, file2.pdf, ...]
   ```

3. **Check Session Status**
   ```http
   GET https://localhost:7001/api/sessions/{sessionId}/status
   ```

4. **Get Comparison Matrix**
   ```http
   GET https://localhost:7001/api/sessions/{sessionId}/matrix
   ```

5. **Health Check**
   ```http
   GET https://localhost:7001/api/health
   ```

## Running with Docker (Alternative)

If you prefer to use Docker:

```bash
# Build and run with Docker Compose
docker-compose up --build
```

This will start both the API and UI services. The API will be available at `http://localhost:7001/api`.

## Troubleshooting

- **API Key Issues**: Ensure your OpenAI API key is correctly set in the configuration.
- **Port Conflicts**: If port 7001 is already in use, you can modify the port in `CVProcessing.API/Properties/launchSettings.json`.
- **Database Errors**: The application uses in-memory storage by default. No additional database setup is required for local development.

## Next Steps

- To run the UI application, use: `dotnet run --project CVProcessing.UI`
- For more detailed API documentation, refer to the [API Reference](docs/API.md)
- For deployment options, see the [Deployment Guide](docs/DEPLOYMENT.md)