using IntuneManager.Core.Services;
using Microsoft.Graph.Beta.Models;

namespace IntuneManager.Core.Tests.Services;

public class GroupServiceTests
{
    [Fact]
    public void InferGroupType_SecurityEnabledOnly_ReturnsSecurity()
    {
        var group = new Group
        {
            SecurityEnabled = true,
            MailEnabled = false,
            GroupTypes = []
        };

        var result = GroupService.InferGroupType(group);
        Assert.Equal("Security", result);
    }

    [Fact]
    public void InferGroupType_MailEnabledSecurity_ReturnsMailEnabledSecurity()
    {
        var group = new Group
        {
            SecurityEnabled = true,
            MailEnabled = true,
            GroupTypes = []
        };

        var result = GroupService.InferGroupType(group);
        Assert.Equal("Mail-enabled Security", result);
    }

    [Fact]
    public void InferGroupType_DistributionGroup_ReturnsDistribution()
    {
        var group = new Group
        {
            SecurityEnabled = false,
            MailEnabled = true,
            GroupTypes = []
        };

        var result = GroupService.InferGroupType(group);
        Assert.Equal("Distribution", result);
    }

    [Fact]
    public void InferGroupType_UnifiedGroup_ReturnsMicrosoft365()
    {
        var group = new Group
        {
            SecurityEnabled = true,
            MailEnabled = false,
            GroupTypes = ["Unified"]
        };

        var result = GroupService.InferGroupType(group);
        Assert.Equal("Microsoft 365", result);
    }

    [Fact]
    public void InferGroupType_NoFlags_ReturnsSecurity()
    {
        var group = new Group
        {
            SecurityEnabled = false,
            MailEnabled = false,
            GroupTypes = []
        };

        var result = GroupService.InferGroupType(group);

        Assert.Equal("Security", result);
    }

    [Fact]
    public void InferGroupType_DynamicSecurityGroup_ReturnsSecurity()
    {
        var group = new Group
        {
            SecurityEnabled = true,
            MailEnabled = false,
            GroupTypes = ["DynamicMembership"]
        };

        var result = GroupService.InferGroupType(group);

        // A dynamic group that is security-enabled but not Unified → Security
        Assert.Equal("Security", result);
    }

    [Fact]
    public void InferGroupType_DynamicM365Group_ReturnsMicrosoft365()
    {
        var group = new Group
        {
            SecurityEnabled = false,
            MailEnabled = true,
            GroupTypes = ["Unified", "DynamicMembership"]
        };

        var result = GroupService.InferGroupType(group);

        Assert.Equal("Microsoft 365", result);
    }

    [Fact]
    public void InferGroupType_NullGroupTypes_ReturnsSecurity()
    {
        var group = new Group
        {
            SecurityEnabled = true,
            MailEnabled = false,
            GroupTypes = null
        };

        var result = GroupService.InferGroupType(group);

        Assert.Equal("Security", result);
    }

    [Fact]
    public void GroupMemberCounts_RecordProperties()
    {
        var counts = new GroupMemberCounts(10, 5, 2, 17);

        Assert.Equal(10, counts.Users);
        Assert.Equal(5, counts.Devices);
        Assert.Equal(2, counts.NestedGroups);
        Assert.Equal(17, counts.Total);
    }

    [Fact]
    public async Task SearchGroupsAsync_EmptyQuery_ReturnsEmptyWithoutGraphCall()
    {
        var sut = new GroupService(null!);

        var result = await sut.SearchGroupsAsync("   ");

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetGroupAssignmentsAsync_MatchesIncludeAndExclude_AndSortsResults()
    {
        var groupId = "group-1";
        var config = new DeviceConfiguration { Id = "cfg-1", DisplayName = "Z Config", OdataType = "#microsoft.graph.windows10GeneralConfiguration" };
        var policy = new DeviceCompliancePolicy { Id = "pol-1", DisplayName = "A Policy", OdataType = "#microsoft.graph.iosCompliancePolicy" };
        var app = new MobileApp { Id = "app-1", DisplayName = "M App", OdataType = "#microsoft.graph.win32LobApp" };

        var configService = new GroupTestConfigService(
            [config],
            new Dictionary<string, List<DeviceConfigurationAssignment>>
            {
                ["cfg-1"] =
                [
                    new DeviceConfigurationAssignment { Target = new GroupAssignmentTarget { GroupId = groupId } },
                    new DeviceConfigurationAssignment { Target = new GroupAssignmentTarget { GroupId = "other" } }
                ]
            });

        var policyService = new GroupTestComplianceService(
            [policy],
            new Dictionary<string, List<DeviceCompliancePolicyAssignment>>
            {
                ["pol-1"] =
                [
                    new DeviceCompliancePolicyAssignment { Target = new ExclusionGroupAssignmentTarget { GroupId = groupId } }
                ]
            });

        var appService = new GroupTestApplicationService(
            [app],
            new Dictionary<string, List<MobileAppAssignment>>
            {
                ["app-1"] =
                [
                    new MobileAppAssignment { Target = new GroupAssignmentTarget { GroupId = groupId }, Intent = InstallIntent.Available }
                ]
            });

        var progress = new List<string>();
        var sut = new GroupService(null!);

        var results = await sut.GetGroupAssignmentsAsync(
            groupId,
            configService,
            policyService,
            appService,
            m => progress.Add(m));

        Assert.Equal(3, results.Count);
        Assert.Collection(results,
            r => Assert.Equal("Application", r.Category),
            r => Assert.Equal("Compliance Policy", r.Category),
            r => Assert.Equal("Device Configuration", r.Category));

        Assert.Contains(results, r => r.Category == "Compliance Policy" && r.IsExclusion && r.AssignmentIntent == "Exclude");
        Assert.Contains(results, r => r.Category == "Application" && !r.IsExclusion && r.AssignmentIntent == "Available");
        Assert.Contains(results, r => r.Category == "Device Configuration" && !r.IsExclusion && r.AssignmentIntent == "Include");

        Assert.Contains(progress, p => p.StartsWith("Scanning device configurations..."));
        Assert.Contains(progress, p => p.StartsWith("Scanning compliance policies..."));
        Assert.Contains(progress, p => p.StartsWith("Scanning applications..."));
        Assert.Contains(progress, p => p.StartsWith("Found 3 assignment(s)"));
    }

    private sealed class GroupTestConfigService : IConfigurationProfileService
    {
        private readonly List<DeviceConfiguration> _configs;
        private readonly Dictionary<string, List<DeviceConfigurationAssignment>> _assignments;

        public GroupTestConfigService(List<DeviceConfiguration> configs, Dictionary<string, List<DeviceConfigurationAssignment>> assignments)
        {
            _configs = configs;
            _assignments = assignments;
        }

        public Task<List<DeviceConfiguration>> ListDeviceConfigurationsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(_configs);

        public Task<DeviceConfiguration?> GetDeviceConfigurationAsync(string id, CancellationToken cancellationToken = default)
            => Task.FromResult<DeviceConfiguration?>(_configs.FirstOrDefault(c => c.Id == id));

        public Task<DeviceConfiguration> CreateDeviceConfigurationAsync(DeviceConfiguration config, CancellationToken cancellationToken = default)
            => Task.FromResult(config);

        public Task<DeviceConfiguration> UpdateDeviceConfigurationAsync(DeviceConfiguration config, CancellationToken cancellationToken = default)
            => Task.FromResult(config);

        public Task DeleteDeviceConfigurationAsync(string id, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<List<DeviceConfigurationAssignment>> GetAssignmentsAsync(string configId, CancellationToken cancellationToken = default)
            => Task.FromResult(_assignments.TryGetValue(configId, out var value) ? value : []);
    }

    private sealed class GroupTestComplianceService : ICompliancePolicyService
    {
        private readonly List<DeviceCompliancePolicy> _policies;
        private readonly Dictionary<string, List<DeviceCompliancePolicyAssignment>> _assignments;

        public GroupTestComplianceService(List<DeviceCompliancePolicy> policies, Dictionary<string, List<DeviceCompliancePolicyAssignment>> assignments)
        {
            _policies = policies;
            _assignments = assignments;
        }

        public Task<List<DeviceCompliancePolicy>> ListCompliancePoliciesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(_policies);

        public Task<DeviceCompliancePolicy?> GetCompliancePolicyAsync(string id, CancellationToken cancellationToken = default)
            => Task.FromResult<DeviceCompliancePolicy?>(_policies.FirstOrDefault(p => p.Id == id));

        public Task<DeviceCompliancePolicy> CreateCompliancePolicyAsync(DeviceCompliancePolicy policy, CancellationToken cancellationToken = default)
            => Task.FromResult(policy);

        public Task<DeviceCompliancePolicy> UpdateCompliancePolicyAsync(DeviceCompliancePolicy policy, CancellationToken cancellationToken = default)
            => Task.FromResult(policy);

        public Task DeleteCompliancePolicyAsync(string id, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<List<DeviceCompliancePolicyAssignment>> GetAssignmentsAsync(string policyId, CancellationToken cancellationToken = default)
            => Task.FromResult(_assignments.TryGetValue(policyId, out var value) ? value : []);

        public Task AssignPolicyAsync(string policyId, List<DeviceCompliancePolicyAssignment> assignments, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class GroupTestApplicationService : IApplicationService
    {
        private readonly List<MobileApp> _apps;
        private readonly Dictionary<string, List<MobileAppAssignment>> _assignments;

        public GroupTestApplicationService(List<MobileApp> apps, Dictionary<string, List<MobileAppAssignment>> assignments)
        {
            _apps = apps;
            _assignments = assignments;
        }

        public Task<List<MobileApp>> ListApplicationsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(_apps);

        public Task<MobileApp?> GetApplicationAsync(string id, CancellationToken cancellationToken = default)
            => Task.FromResult<MobileApp?>(_apps.FirstOrDefault(a => a.Id == id));

        public Task<List<MobileAppAssignment>> GetAssignmentsAsync(string appId, CancellationToken cancellationToken = default)
            => Task.FromResult(_assignments.TryGetValue(appId, out var value) ? value : []);
    }
}
