using Microsoft.Graph.Beta;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.Core.Services;

public class UserService(GraphServiceClient graphClient) : IUserService
{
    private readonly GraphServiceClient _graphClient = graphClient;

    private static readonly string[] UserSelect =
        ["id", "displayName", "userPrincipalName", "mail", "jobTitle", "department"];

    public async Task<List<User>> SearchUsersAsync(string query, CancellationToken cancellationToken = default)
    {
        var result = new List<User>();
        if (string.IsNullOrWhiteSpace(query)) return result;

        var trimmed = query.Trim();

        // If the query looks like a UPN, try a direct lookup first
        if (trimmed.Contains('@'))
        {
            try
            {
                var user = await _graphClient.Users[trimmed]
                    .GetAsync(req => req.QueryParameters.Select = UserSelect, cancellationToken);
                if (user != null) result.Add(user);
                return result;
            }
            catch
            {
                // Fall through to displayName search
            }
        }

        // Search by displayName or userPrincipalName startsWith
        var escaped = trimmed.Replace("'", "''");
        var response = await _graphClient.Users.GetAsync(req =>
        {
            req.QueryParameters.Filter =
                $"startsWith(displayName,'{escaped}') or startsWith(userPrincipalName,'{escaped}')";
            req.QueryParameters.Select = UserSelect;
            req.QueryParameters.Top = 25;
            req.QueryParameters.Orderby = ["displayName"];
            req.Headers.Add("ConsistencyLevel", "eventual");
            req.QueryParameters.Count = true;
        }, cancellationToken);

        if (response?.Value != null)
            result.AddRange(response.Value);

        return result;
    }
}
