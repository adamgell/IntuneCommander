using Azure.Identity;
using Intune.Commander.Core.Services;
using Microsoft.Graph.Beta;

namespace Intune.Commander.Core.Tests.Integration;

/// <summary>
/// Base class for Graph API integration tests.
/// Requires AZURE_TENANT_ID, AZURE_CLIENT_ID, and AZURE_CLIENT_SECRET environment variables.
/// Tests gracefully no-op when credentials are not available (local dev).
/// </summary>
[Trait("Category", "Integration")]
public abstract class GraphIntegrationTestBase
{
    protected GraphServiceClient? GraphClient { get; }
    protected bool HasCredentials { get; }

    protected GraphIntegrationTestBase()
    {
        var tenantId = Environment.GetEnvironmentVariable("AZURE_TENANT_ID");
        var clientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");
        var clientSecret = Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET");

        if (!string.IsNullOrEmpty(tenantId) &&
            !string.IsNullOrEmpty(clientId) &&
            !string.IsNullOrEmpty(clientSecret))
        {
            var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
            GraphClient = new GraphServiceClient(credential,
                scopes: ["https://graph.microsoft.com/.default"]);
            HasCredentials = true;
        }
    }

    /// <summary>
    /// Helper to skip tests when credentials are not available.
    /// Returns true if the test should be skipped.
    /// </summary>
    protected bool ShouldSkip()
    {
        if (!HasCredentials)
        {
            // No credentials — silently pass. Integration CI provides secrets.
            return true;
        }
        return false;
    }

    /// <summary>
    /// Creates a service instance using the Graph client. Skips if no credentials.
    /// </summary>
    protected T? CreateService<T>() where T : class
    {
        if (GraphClient == null) return null;
        return (T?)Activator.CreateInstance(typeof(T), GraphClient);
    }
}
