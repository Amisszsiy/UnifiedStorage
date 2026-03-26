# Unified Storage – Requirements

## 1. Overview
A .NET 8 Web API that acts as a unified proxy/orchestrator for multiple cloud storage providers (Google Drive, OneDrive, Dropbox). Users connect their cloud storage accounts via OAuth 2.0 once and can then list, upload, download, and delete files across all connected drives through a single API.

---

## 2. Supported Providers

| Provider     | API               | Auth         |
|--------------|-------------------|--------------|
| Google Drive | Drive REST API v3 | OAuth 2.0    |
| OneDrive     | Microsoft Graph   | OAuth 2.0    |
| Dropbox      | Dropbox API v2    | OAuth 2.0    |

> **Not supported:** iCloud Drive (no public API available from Apple)

---

## 3. Architecture

**Pattern:** Clean Architecture + CQRS (MediatR) + Code First (EF Core)

```
UnifiedStorage.WebAPI          ← HTTP controllers, DI wiring, Program.cs
UnifiedStorage.Application     ← CQRS commands/queries, interfaces, DTOs
UnifiedStorage.Infrastructure  ← EF Core, repositories, OAuth, provider services
UnifiedStorage.Domain          ← Entities, enums, domain exceptions, models
```

### Dependency Rule
- Domain has no external dependencies
- Application depends only on Domain
- Infrastructure depends on Application + Domain
- WebAPI depends on Application + Infrastructure

---

## 4. Domain Model

### `StorageConnection` (Entity)
Stored in the database. Represents one provider account linked to a user.

| Field                  | Type             | Description                              |
|------------------------|------------------|------------------------------------------|
| Id                     | Guid             | Primary key                              |
| UserId                 | string           | User identifier (from auth system)       |
| Provider               | StorageProvider  | GoogleDrive / OneDrive / Dropbox         |
| EncryptedAccessToken   | string           | AES-256 encrypted access token           |
| EncryptedRefreshToken  | string           | AES-256 encrypted refresh token          |
| AccessTokenExpiresAt   | DateTime (UTC)   | When the access token expires            |
| Status                 | ConnectionStatus | Active / Expired / Revoked               |
| LastUsedAt             | DateTime (UTC)   | Last API call made with this connection  |
| CreatedAt              | DateTime (UTC)   | When the connection was created          |

**Unique constraint:** `(UserId, Provider)`

### `CloudFile` (Model — not persisted)
Unified representation of a file from any provider.

| Field       | Type            | Description                          |
|-------------|-----------------|--------------------------------------|
| Id          | string          | Provider-native file ID              |
| Name        | string          | File/folder name                     |
| SizeBytes   | long            | File size in bytes (0 for folders)   |
| ModifiedAt  | DateTime?       | Last modified timestamp              |
| Provider    | StorageProvider | Which provider this file belongs to  |
| DownloadUrl | string?         | Direct download URL (if available)   |
| IsFolder    | bool            | True if this is a folder/directory   |
| MimeType    | string?         | MIME type                            |
| ParentId    | string?         | Parent folder ID                     |

### Enums

**`StorageProvider`:** `GoogleDrive`, `OneDrive`, `Dropbox`

**`ConnectionStatus`:** `Active`, `Expired`, `Revoked`

---

## 5. API Endpoints

### OAuth Flow

| Method | Path                            | Description                                             |
|--------|---------------------------------|---------------------------------------------------------|
| GET    | `/api/oauth/connect/{provider}` | Returns the OAuth authorization URL for the provider    |
| GET    | `/api/oauth/callback/{provider}`| Handles provider redirect; exchanges code for tokens    |

**OAuth Flow:**
1. Client calls `GET /api/oauth/connect/googledrive`
2. API returns `{ "authorizationUrl": "https://accounts.google.com/o/oauth2/v2/auth?..." }`
3. Client redirects user to that URL
4. User consents; provider redirects to `GET /api/oauth/callback/googledrive?code=xxx&state=yyy`
5. API exchanges code for tokens, encrypts them, stores the `StorageConnection`
6. Returns `{ "message": "GoogleDrive connected successfully." }`

### Storage Connections

| Method | Path                       | Description                                     |
|--------|----------------------------|-------------------------------------------------|
| GET    | `/api/connections`         | List all connected providers for current user   |
| DELETE | `/api/connections/{provider}` | Disconnect (remove) a provider connection    |

### Files

| Method | Path                                   | Description                                          |
|--------|----------------------------------------|------------------------------------------------------|
| GET    | `/api/files`                           | List files from all connected providers              |
| GET    | `/api/files?provider=GoogleDrive`      | List files from a specific provider                  |
| GET    | `/api/files?folderId=xxx&provider=xxx` | List files in a specific folder                      |
| GET    | `/api/files/{provider}/{fileId}/download` | Download a file (streamed response)               |
| POST   | `/api/files/upload?provider=xxx`       | Upload a file (`multipart/form-data`)                |
| DELETE | `/api/files/{provider}/{fileId}`       | Delete a file                                        |

---

## 6. CQRS Structure (MediatR)

### Queries
| Query                    | Returns                          | Description                      |
|--------------------------|----------------------------------|----------------------------------|
| `GetConnectionsQuery`    | `IReadOnlyList<StorageConnectionDto>` | All connections for current user |
| `GetFilesQuery`          | `IReadOnlyList<CloudFileDto>`    | Files from one or all providers  |
| `DownloadFileQuery`      | `DownloadFileResult`             | File stream + metadata           |
| `GetAuthorizationUrlQuery` | `string`                       | OAuth authorization URL          |

### Commands
| Command                    | Returns        | Description                                  |
|----------------------------|----------------|----------------------------------------------|
| `DisconnectStorageCommand` | `Unit`         | Removes a storage connection                 |
| `UploadFileCommand`        | `CloudFileDto` | Uploads a file to a provider                 |
| `DeleteFileCommand`        | `Unit`         | Deletes a file from a provider               |
| `ExchangeOAuthCodeCommand` | `Unit`         | Exchanges OAuth code, stores tokens          |

---

## 7. Token Lifecycle Management

- Access tokens are short-lived (~1 hour). Before calling a provider API, the system checks if the token expires within **5 minutes**.
- If expiring soon: silently refreshes using the refresh token and updates the stored connection.
- If refresh fails (revoked/expired refresh token): connection is marked as `Expired` and a `ReAuthRequiredException` is thrown.
- The API returns `HTTP 401` with `{ "error": "...", "provider": "..." }` when re-auth is required.
- **Dropbox**: rotates the refresh token on each use — the latest refresh token is always saved.

---

## 8. Security

### Token Encryption
- Access and refresh tokens are encrypted at rest using **AES-256** before storing in the database.
- The encryption key is a 32-byte (256-bit) Base64-encoded value stored in `Encryption:Key` configuration.
- **Production:** use Azure Key Vault or .NET Data Protection API for key management.

### User Identity
- `ICurrentUserService` resolves the user ID from JWT `sub` claim (production) or `X-User-Id` request header (development fallback).
- **Production requirement:** configure proper JWT bearer authentication.

### OAuth State Parameter
- The `state` parameter encodes `userId:nonce` as Base64 to bind the callback to the initiating user.
- **Production recommendation:** use a signed JWT or server-side nonce store for state validation.

---

## 9. Database

- **Database:** Microsoft SQL Server (via Docker Compose)
- **ORM:** Entity Framework Core 8 with Code First
- **Connection string key:** `ConnectionStrings:UnifiedStorage`

### Running the database (Docker)
```bash
docker-compose up -d
```

### Creating the first migration
```bash
cd UnifiedStorage
dotnet ef migrations add InitialCreate --project UnifiedStorage.Infrastructure --startup-project UnifiedStorage.WebAPI
dotnet ef database update --project UnifiedStorage.Infrastructure --startup-project UnifiedStorage.WebAPI
```

Migrations are also applied automatically on startup in the Development environment.

---

## 10. Configuration (`appsettings.json`)

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

**Generating an encryption key (PowerShell):**
```powershell
[Convert]::ToBase64String((1..32 | ForEach-Object { [byte](Get-Random -Max 256) }))
```

---

## 11. Provider SDK Integration

All three provider service classes are fully implemented.

| Provider     | NuGet Package          | Version       | Status          |
|--------------|------------------------|---------------|-----------------|
| Google Drive | `Google.Apis.Drive.v3` | 1.73.0.4081   | Implemented ✅  |
| OneDrive     | `Microsoft.Graph`      | 5.103.0       | Implemented ✅  |
| Dropbox      | `Dropbox.Api`          | 7.0.0         | Implemented ✅  |

### Implementation Notes

**Google Drive (`GoogleDriveService`)**
- Uses `GoogleCredential.FromAccessToken(accessToken)` to build a `DriveService` per request.
- `ListFilesAsync`: queries `files(id,name,size,modifiedTime,mimeType,parents,webContentLink)`, defaults to `'root' in parents`, excludes trashed items.
- `UploadFileAsync`: uses `Files.Create` with resumable upload (`UploadAsync`).
- `DownloadFileAsync`: Google Docs native formats (e.g. Docs, Sheets) are exported as PDF; binary files are downloaded directly.

**OneDrive (`OneDriveService`)**
- Uses `BaseBearerTokenAuthenticationProvider` with a file-scoped `StaticAccessTokenProvider` wrapper.
- All item operations go through `client.Drives[driveId].Items[...]` (Graph SDK v5+ pattern — `Me.Drive` is only used to resolve the drive ID).
- `ListFilesAsync`: paginates via `OdataNextLink`.
- `UploadFileAsync`: uses `Items[parentId].ItemWithPath(fileName).Content.PutAsync()`.

**Dropbox (`DropboxService`)**
- Creates a `DropboxClient(accessToken)` per request.
- `folderId` / `fileId` are Dropbox **paths** (e.g. `""` = root, `"/folder/sub"`), not opaque IDs.
- `ListFilesAsync`: paginates via `ListFolderContinueAsync` while `HasMore` is true.
- `UploadFileAsync`: uses `WriteMode.Overwrite`.
- `DownloadFileAsync`: copies response stream to `MemoryStream` before disposing the client.

---

## 12. Error Responses

| HTTP Status | Scenario                                        |
|-------------|-------------------------------------------------|
| 200 OK      | Success                                         |
| 201 Created | File uploaded successfully                      |
| 204 No Content | Disconnected or deleted successfully         |
| 400 Bad Request | Missing or invalid parameters               |
| 401 Unauthorized | User not authenticated, or re-auth needed |
| 404 Not Found | Connection or file not found                 |
| 500 Internal Server Error | Unexpected error                  |

---

## 13. Out of Scope (MVP)

- iCloud Drive (no public API)
- File preview/thumbnail generation
- Cross-provider file move (download from one + upload to another)
- Search across all drives
- Frontend application (API-only backend)
- Full JWT authentication setup (placeholder `X-User-Id` header for dev)
- Background token refresh service (proactive refresh before expiry)
