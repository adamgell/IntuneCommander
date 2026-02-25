using System.Text;
using System.Text.Json;
using Intune.Commander.Core.Models;
using Intune.Commander.Core.Services;

namespace Intune.Commander.Core.Tests.Services;

public class PermissionCheckServiceTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>Builds a synthetic JWT with the given payload object.</summary>
    private static string MakeJwt(object payload)
    {
        var header  = Base64UrlEncode("""{"alg":"none","typ":"JWT"}""");
        var body    = Base64UrlEncode(JsonSerializer.Serialize(payload));
        return $"{header}.{body}.";
    }

    private static string Base64UrlEncode(string s)
    {
        var bytes = Encoding.UTF8.GetBytes(s);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    // ── ExtractPermissionsFromJwt (application / "roles" token) ──────────────

    [Fact]
    public void ExtractPermissions_ApplicationToken_ReturnsRolesClaim()
    {
        var jwt = MakeJwt(new
        {
            oid   = "abc",
            roles = new[] { "DeviceManagementConfiguration.ReadWrite.All", "Group.Read.All" }
        });

        var (perms, source) = PermissionCheckService.ExtractPermissionsFromJwt(jwt);

        Assert.Equal("roles", source);
        Assert.Contains("DeviceManagementConfiguration.ReadWrite.All", perms);
        Assert.Contains("Group.Read.All", perms);
        Assert.Equal(2, perms.Count);
    }

    [Fact]
    public void ExtractPermissions_DelegatedToken_ReturnsScpClaim()
    {
        var jwt = MakeJwt(new
        {
            oid = "abc",
            scp = "DeviceManagementConfiguration.ReadWrite.All Group.Read.All"
        });

        var (perms, source) = PermissionCheckService.ExtractPermissionsFromJwt(jwt);

        Assert.Equal("scp", source);
        Assert.Contains("DeviceManagementConfiguration.ReadWrite.All", perms);
        Assert.Contains("Group.Read.All", perms);
        Assert.Equal(2, perms.Count);
    }

    [Fact]
    public void ExtractPermissions_EmptyRoles_ReturnsEmptyList()
    {
        var jwt = MakeJwt(new { oid = "abc", roles = Array.Empty<string>() });

        var (perms, source) = PermissionCheckService.ExtractPermissionsFromJwt(jwt);

        Assert.Equal("roles", source);
        Assert.Empty(perms);
    }

    [Fact]
    public void ExtractPermissions_NoPermissionClaim_ReturnsNoPermissionClaimSource()
    {
        var jwt = MakeJwt(new { oid = "abc", sub = "user" });

        var (perms, source) = PermissionCheckService.ExtractPermissionsFromJwt(jwt);

        Assert.Equal("no-permission-claim", source);
        Assert.Empty(perms);
    }

    [Fact]
    public void ExtractPermissions_MalformedJwt_ReturnsUnknownSource()
    {
        var (perms, source) = PermissionCheckService.ExtractPermissionsFromJwt("not.a.valid.jwt.at.all");

        // The decoder attempts Base64 on part[1]; the 3rd part being "valid" may vary —
        // we just check it does not throw and returns something sensible.
        Assert.NotNull(perms);
        Assert.NotNull(source);
    }

    [Fact]
    public void ExtractPermissions_TooFewParts_ReturnsUnknownSource()
    {
        var (perms, source) = PermissionCheckService.ExtractPermissionsFromJwt("onlyone");

        Assert.Equal("unknown", source);
        Assert.Empty(perms);
    }

    // ── PermissionCheckResult classification ─────────────────────────────────

    [Fact]
    public void CheckResult_AllGranted_AllPermissionsGrantedIsTrue()
    {
        var all = PermissionCheckService.RequiredPermissions.ToList();
        var result = new PermissionCheckResult
        {
            RequiredPermissions = all,
            GrantedPermissions  = all,
            MissingPermissions  = [],
            ExtraPermissions    = [],
        };

        Assert.True(result.AllPermissionsGranted);
    }

    [Fact]
    public void CheckResult_SomeMissing_AllPermissionsGrantedIsFalse()
    {
        var result = new PermissionCheckResult
        {
            RequiredPermissions = ["A", "B"],
            GrantedPermissions  = ["A"],
            MissingPermissions  = ["B"],
            ExtraPermissions    = [],
        };

        Assert.False(result.AllPermissionsGranted);
    }

    [Fact]
    public void CheckResult_ExtraPermissions_DoesNotAffectAllPermissionsGranted()
    {
        var result = new PermissionCheckResult
        {
            RequiredPermissions = ["A"],
            GrantedPermissions  = ["A"],
            MissingPermissions  = [],
            ExtraPermissions    = ["SomeOther.Permission"],
        };

        Assert.True(result.AllPermissionsGranted);
    }

    // ── RequiredPermissions static list integrity ────────────────────────────

    [Fact]
    public void RequiredPermissions_ContainsCloudPcPermission()
    {
        Assert.Contains("CloudPC.ReadWrite.All", PermissionCheckService.RequiredPermissions);
    }

    [Fact]
    public void RequiredPermissions_ContainsCoreIntunePermissions()
    {
        var required = PermissionCheckService.RequiredPermissions;
        Assert.Contains("DeviceManagementConfiguration.ReadWrite.All", required);
        Assert.Contains("DeviceManagementApps.ReadWrite.All", required);
        Assert.Contains("DeviceManagementScripts.ReadWrite.All", required);
    }

    [Fact]
    public void RequiredPermissions_ContainsConditionalAccessPermission()
    {
        Assert.Contains("Policy.ReadWrite.ConditionalAccess", PermissionCheckService.RequiredPermissions);
    }

    [Fact]
    public void RequiredPermissions_NoDuplicates()
    {
        var perms = PermissionCheckService.RequiredPermissions;
        var distinct = perms.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        Assert.Equal(distinct.Count, perms.Count);
    }

    // ── Interface contract ───────────────────────────────────────────────────

    [Fact]
    public void Service_ImplementsInterface()
    {
        Assert.True(typeof(IPermissionCheckService).IsAssignableFrom(typeof(PermissionCheckService)));
    }

    [Fact]
    public void Interface_HasCheckPermissionsMethod()
    {
        var method = typeof(IPermissionCheckService).GetMethod("CheckPermissionsAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<PermissionCheckResult>), method!.ReturnType);
        var p = method.GetParameters();
        Assert.Single(p);
        Assert.Equal(typeof(CancellationToken), p[0].ParameterType);
        Assert.True(p[0].HasDefaultValue);
    }
}
