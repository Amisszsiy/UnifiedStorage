# UnifiedStorage

A .NET 8 Web API that acts as a unified proxy for multiple cloud storage providers. Users connect their accounts via OAuth 2.0 once and can then list, upload, download, and delete files across all connected drives through a single API.

## Supported Providers

| Provider     | API                   | SDK Package            | Version     |
|--------------|-----------------------|------------------------|-------------|
| Google Drive | Drive REST API v3     | `Google.Apis.Drive.v3` | 1.73.0.4081 |
| OneDrive     | Microsoft Graph API   | `Microsoft.Graph`      | 5.103.0     |
| Dropbox      | Dropbox API v2        | `Dropbox.Api`          | 7.0.0       |

> iCloud Drive is not supported (no public API).

## Architecture

Clean Architecture + CQRS (MediatR) + EF Core Code First.

```
UnifiedStorage.WebAPI          ← HTTP controllers, DI wiring, Program.cs
UnifiedStorage.Application     ← CQRS commands/queries, interfaces, DTOs
UnifiedStorage.Infrastructure  ← EF Core, repositories, OAuth, provider services
UnifiedStorage.Domain          ← Entities, enums, domain exceptions, models
```

## Getting Started

### Prerequisites

- .NET 8 SDK
- Docker (for SQL Server)
- OAuth app credentials for each provider you want to use

### 1. Start the database

```bash
docker-compose up -d
```

### 2. Configure `appsettings.json`

```json
{
  "ConnectionStrings": {
    "UnifiedStorage": "Server=localhost,1433;Database=UnifiedStorageDb;User Id=sa;Password=P@ssw0rd123;TrustServerCertificate=True;"
  },
  "Encryption": {
    "Key": "<32-byte Base64-encoded AES key>"
  },
  "OAuth": {
    "GoogleDrive": { "ClientId": "", "ClientSecret": "" },
    "OneDrive":    { "ClientId": "", "ClientSecret": "" },
    "Dropbox":     { "ClientId": "", "ClientSecret": "" }
  }
}
```

Generate an encryption key (PowerShell):

```powershell
[Convert]::ToBase64String((1..32 | ForEach-Object { [byte](Get-Random -Max 256) }))
```

### 3. Apply migrations

Migrations run automatically in the Development environment. To apply manually:

```bash
cd UnifiedStorage
dotnet ef database update --project UnifiedStorage.Infrastructure --startup-project UnifiedStorage.WebAPI
```

### 4. Run the API

```bash
dotnet run --project UnifiedStorage/UnifiedStorage.WebAPI
```

## API Endpoints

### OAuth Flow

| Method | Path                             | Description                                          |
|--------|----------------------------------|------------------------------------------------------|
| GET    | `/api/oauth/connect/{provider}`  | Returns the OAuth authorization URL for the provider |
| GET    | `/api/oauth/callback/{provider}` | Exchanges OAuth code for tokens, stores connection   |

**Providers:** `googledrive`, `onedrive`, `dropbox`

### Storage Connections

| Method | Path                            | Description                                   |
|--------|---------------------------------|-----------------------------------------------|
| GET    | `/api/connections`              | List all connected providers for current user |
| DELETE | `/api/connections/{provider}`   | Disconnect a provider                         |

### Files

| Method | Path                                      | Description                           |
|--------|-------------------------------------------|---------------------------------------|
| GET    | `/api/files`                              | List files from all connected providers |
| GET    | `/api/files?provider=GoogleDrive`         | List files from a specific provider   |
| GET    | `/api/files?folderId=xxx&provider=xxx`    | List files in a specific folder       |
| GET    | `/api/files/{provider}/{fileId}/download` | Download a file (streamed)            |
| POST   | `/api/files/upload?provider=xxx`          | Upload a file (`multipart/form-data`) |
| DELETE | `/api/files/{provider}/{fileId}`          | Delete a file                         |

## Authentication (Development)

Pass the user ID via the `X-User-Id` request header. In production, configure JWT bearer authentication and the `sub` claim will be used automatically.

## Token Lifecycle

- Access tokens are checked before each API call. If expiring within **5 minutes**, they are silently refreshed.
- If the refresh token is expired or revoked, the connection is marked `Expired` and the API returns `HTTP 401` with a `ReAuthRequiredException` payload.
- Dropbox rotates its refresh token on each use — the latest token is always persisted.

## Security

- Access and refresh tokens are encrypted at rest with **AES-256**.
- Tokens are never returned to the client.
- OAuth `state` parameter encodes `userId:nonce` as Base64 to prevent CSRF.

## Project Structure

```
UnifiedStorage/
├── UnifiedStorage.Domain/
│   ├── Entities/          StorageConnection.cs
│   ├── Enums/             StorageProvider.cs, ConnectionStatus.cs
│   ├── Exceptions/        DomainException, ReAuthRequiredException, ...
│   └── Models/            CloudFile.cs
├── UnifiedStorage.Application/
│   ├── Common/Interfaces/ IStorageProviderService, IOAuthService, ...
│   ├── Files/             GetFiles, DownloadFile, UploadFile, DeleteFile
│   ├── OAuth/             GetAuthorizationUrl, ExchangeOAuthCode
│   └── StorageConnections/ GetConnections, DisconnectStorage
├── UnifiedStorage.Infrastructure/
│   ├── OAuth/             OAuthService.cs
│   ├── Persistence/       UnifiedStorageDbContext, Migrations
│   ├── Providers/         GoogleDriveService, OneDriveService, DropboxService
│   ├── Repositories/      StorageConnectionRepository
│   └── Security/          TokenEncryptionService
└── UnifiedStorage.WebAPI/
    ├── Controllers/       OAuthController, FilesController, StorageConnectionsController
    └── Services/          CurrentUserService
```
