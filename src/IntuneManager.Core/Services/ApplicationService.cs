using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace IntuneManager.Core.Services;

public class ApplicationService : IApplicationService
{
    private readonly GraphServiceClient _graphClient;

    public ApplicationService(GraphServiceClient graphClient)
    {
        _graphClient = graphClient;
    }

    public async Task<List<MobileApp>> ListApplicationsAsync(CancellationToken cancellationToken = default)
    {
        var result = new List<MobileApp>();

        var response = await _graphClient.DeviceAppManagement.MobileApps
            .GetAsync(cancellationToken: cancellationToken);

        if (response != null)
        {
            // PageIterator handles first page + all subsequent pages.
            // Do NOT also AddRange(response.Value) — that would duplicate the first page.
            var pageIterator = PageIterator<MobileApp, MobileAppCollectionResponse>
                .CreatePageIterator(_graphClient, response, item =>
                {
                    result.Add(item);
                    return true;
                });

            await pageIterator.IterateAsync(cancellationToken);
        }

        // Ensure OdataType is populated — the Graph SDK sometimes deserializes into
        // the correct concrete type but leaves OdataType null.
        foreach (var app in result)
            EnsureOdataType(app);

        return result;
    }

    public async Task<MobileApp?> GetApplicationAsync(string id, CancellationToken cancellationToken = default)
    {
        var app = await _graphClient.DeviceAppManagement.MobileApps[id]
            .GetAsync(cancellationToken: cancellationToken);
        if (app != null)
            EnsureOdataType(app);
        return app;
    }

    /// <summary>
    /// If the Graph SDK deserialized the app into a concrete subclass but left
    /// <see cref="MobileApp.OdataType"/> null, derive it from the runtime type.
    /// </summary>
    private static void EnsureOdataType(MobileApp app)
    {
        if (!string.IsNullOrEmpty(app.OdataType)) return;

        // 1. If the SDK deserialized into a concrete subclass, derive from the type name.
        var typeName = app.GetType().Name;
        if (typeName != nameof(MobileApp))
        {
            app.OdataType = $"#microsoft.graph.{char.ToLowerInvariant(typeName[0])}{typeName[1..]}";
            return;
        }

        // 2. Some items land as base MobileApp but still carry @odata.type in AdditionalData.
        if (app.AdditionalData?.TryGetValue("@odata.type", out var val) == true
            && val is string odataStr && !string.IsNullOrEmpty(odataStr))
        {
            app.OdataType = odataStr;
        }
    }

    public async Task<List<MobileAppAssignment>> GetAssignmentsAsync(string appId, CancellationToken cancellationToken = default)
    {
        var response = await _graphClient.DeviceAppManagement.MobileApps[appId]
            .Assignments.GetAsync(cancellationToken: cancellationToken);

        return response?.Value ?? [];
    }
}
