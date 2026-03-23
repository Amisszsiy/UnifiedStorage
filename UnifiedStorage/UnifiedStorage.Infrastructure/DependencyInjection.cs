using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using UnifiedStorage.Application.Common.Interfaces;
using UnifiedStorage.Infrastructure.OAuth;
using UnifiedStorage.Infrastructure.Persistence;
using UnifiedStorage.Infrastructure.Providers;
using UnifiedStorage.Infrastructure.Repositories;
using UnifiedStorage.Infrastructure.Security;

namespace UnifiedStorage.Infrastructure;

public static class DependencyInjection
{
    public static void AddInfrastructureServices(this IHostApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("UnifiedStorage")
            ?? throw new InvalidOperationException("Connection string 'UnifiedStorage' is not configured.");

        builder.Services.AddDbContext<UnifiedStorageDbContext>(options =>
            options.UseSqlServer(connectionString));

        builder.Services.Configure<OAuthSettings>(
            builder.Configuration.GetSection(OAuthSettings.SectionName));

        builder.Services.AddHttpClient<IOAuthService, OAuthService>();

        builder.Services.AddScoped<IStorageConnectionRepository, StorageConnectionRepository>();
        builder.Services.AddSingleton<ITokenEncryptionService, TokenEncryptionService>();

        // Register all provider services
        builder.Services.AddScoped<IStorageProviderService, GoogleDriveService>();
        builder.Services.AddScoped<IStorageProviderService, OneDriveService>();
        builder.Services.AddScoped<IStorageProviderService, DropboxService>();
    }
}
