using Azure.Identity;
using IntuneManager.Core.Services;
using Microsoft.Graph.Beta;
using Microsoft.Graph.Beta.Models.ODataErrors;

namespace IntuneManager.Core.Tests.Integration;

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

    /// <summary>
    /// Retries an async operation with exponential backoff for transient Graph API failures.
    /// Useful for integration tests against live APIs that may experience intermittent issues.
    /// </summary>
    protected async Task<TResult> RetryOnTransientFailureAsync<TResult>(
        Func<Task<TResult>> operation,
        int maxAttempts = 3,
        int initialDelayMs = 1000)
    {
        var attempt = 0;
        Exception? lastException = null;

        while (attempt < maxAttempts)
        {
            try
            {
                var result = await operation();
                
                // If result is null and we haven't exhausted retries, treat as transient
                if (result == null && attempt < maxAttempts - 1)
                {
                    attempt++;
                    var delay = initialDelayMs * (int)Math.Pow(2, attempt - 1);
                    await Task.Delay(delay);
                    continue;
                }
                
                return result;
            }
            catch (ODataError ex) when (IsTransientError(ex) && attempt < maxAttempts - 1)
            {
                lastException = ex;
                attempt++;
                var delay = initialDelayMs * (int)Math.Pow(2, attempt - 1);
                await Task.Delay(delay);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Failed to update") && attempt < maxAttempts - 1)
            {
                // Catch our custom "Failed to update" exceptions from services
                lastException = ex;
                attempt++;
                var delay = initialDelayMs * (int)Math.Pow(2, attempt - 1);
                await Task.Delay(delay);
            }
        }

        // If we got here, all retries failed — throw the last exception
        throw lastException ?? new InvalidOperationException("Operation failed after all retries");
    }

    /// <summary>
    /// Determines if an ODataError is transient (retriable).
    /// </summary>
    private static bool IsTransientError(ODataError error)
    {
        // Check for 500 series errors (server-side issues)
        if (error.ResponseStatusCode >= 500 && error.ResponseStatusCode < 600)
            return true;

        // Check for specific transient error messages
        var message = error.Message?.ToLowerInvariant() ?? string.Empty;
        if (message.Contains("internal server error") ||
            message.Contains("service unavailable") ||
            message.Contains("timeout"))
            return true;

        return false;
    }
}
