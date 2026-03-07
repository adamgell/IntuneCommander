using System.Text.Json;
using Intune.Commander.Core.Models;
using Intune.Commander.Core.Services;
using Intune.Commander.Desktop.ViewModels;
using Microsoft.Graph.Beta.Models;
using NSubstitute;

namespace Intune.Commander.Desktop.Tests.ViewModels;

public class BaselineViewModelTests
{
    private readonly IBaselineService _baselineService = Substitute.For<IBaselineService>();
    private readonly ISettingsCatalogService _scService = Substitute.For<ISettingsCatalogService>();
    private readonly IEndpointSecurityService _esService = Substitute.For<IEndpointSecurityService>();
    private readonly ICompliancePolicyService _compService = Substitute.For<ICompliancePolicyService>();
    private readonly IGroupService _groupService = Substitute.For<IGroupService>();

    private BaselineViewModel CreateVm() =>
        new(_baselineService, _scService, _esService, _compService, _groupService);

    [Fact]
    public void Constructor_LoadsBaselinesForDefaultType()
    {
        _baselineService.GetCategories(BaselinePolicyType.SettingsCatalog)
            .Returns(new List<string> { "Security", "Identity" });
        _baselineService.GetBaselinesByType(BaselinePolicyType.SettingsCatalog)
            .Returns(new List<BaselinePolicy>());

        var vm = CreateVm();

        Assert.Contains("All", vm.Categories);
        Assert.Contains("Security", vm.Categories);
        Assert.Contains("Identity", vm.Categories);
    }

    [Fact]
    public void ActiveBaselineType_Change_ReloadsBaselines()
    {
        _baselineService.GetCategories(Arg.Any<BaselinePolicyType>())
            .Returns(new List<string>());
        _baselineService.GetBaselinesByType(Arg.Any<BaselinePolicyType>())
            .Returns(new List<BaselinePolicy>());

        var vm = CreateVm();

        vm.ActiveBaselineType = BaselinePolicyType.EndpointSecurity;

        _baselineService.Received().GetBaselinesByType(BaselinePolicyType.EndpointSecurity);
    }

    [Fact]
    public void CategoryFilter_Change_FiltersBaselines()
    {
        _baselineService.GetCategories(Arg.Any<BaselinePolicyType>())
            .Returns(new List<string> { "Security" });
        _baselineService.GetBaselinesByType(Arg.Any<BaselinePolicyType>())
            .Returns(new List<BaselinePolicy>());
        _baselineService.GetBaselinesByCategory("Security", Arg.Any<BaselinePolicyType>())
            .Returns(new List<BaselinePolicy>
            {
                new() { Name = "TestPolicy", Category = "Security" }
            });

        var vm = CreateVm();

        vm.CategoryFilter = "Security";

        Assert.Single(vm.Baselines);
        Assert.Equal("TestPolicy", vm.Baselines[0].Name);
    }

    [Theory]
    [InlineData(BaselinePolicyType.SettingsCatalog, true)]
    [InlineData(BaselinePolicyType.EndpointSecurity, false)]
    [InlineData(BaselinePolicyType.Compliance, false)]
    public void IsCompareAvailable_DependsOnActiveType(BaselinePolicyType type, bool expected)
    {
        _baselineService.GetCategories(Arg.Any<BaselinePolicyType>())
            .Returns(new List<string>());
        _baselineService.GetBaselinesByType(Arg.Any<BaselinePolicyType>())
            .Returns(new List<BaselinePolicy>());

        var vm = CreateVm();
        vm.ActiveBaselineType = type;

        Assert.Equal(expected, vm.IsCompareAvailable);
    }

    [Fact]
    public void RadioButtonProperties_TrackActiveBaselineType()
    {
        _baselineService.GetCategories(Arg.Any<BaselinePolicyType>())
            .Returns(new List<string>());
        _baselineService.GetBaselinesByType(Arg.Any<BaselinePolicyType>())
            .Returns(new List<BaselinePolicy>());

        var vm = CreateVm();

        Assert.True(vm.IsSettingsCatalogSelected);
        Assert.False(vm.IsEndpointSecuritySelected);
        Assert.False(vm.IsComplianceSelected);

        vm.ActiveBaselineType = BaselinePolicyType.EndpointSecurity;

        Assert.False(vm.IsSettingsCatalogSelected);
        Assert.True(vm.IsEndpointSecuritySelected);
        Assert.False(vm.IsComplianceSelected);
    }

    [Fact]
    public void ActiveBaselineType_Change_ClearsComparisonResult()
    {
        _baselineService.GetCategories(Arg.Any<BaselinePolicyType>())
            .Returns(new List<string>());
        _baselineService.GetBaselinesByType(Arg.Any<BaselinePolicyType>())
            .Returns(new List<BaselinePolicy>());

        var vm = CreateVm();
        vm.ComparisonResult = new BaselineComparisonResult { BaselineName = "test" };

        vm.ActiveBaselineType = BaselinePolicyType.Compliance;

        Assert.Null(vm.ComparisonResult);
    }

    [Fact]
    public async Task DeployAsNew_NoSelectedBaseline_DoesNothing()
    {
        _baselineService.GetCategories(Arg.Any<BaselinePolicyType>())
            .Returns(new List<string>());
        _baselineService.GetBaselinesByType(Arg.Any<BaselinePolicyType>())
            .Returns(new List<BaselinePolicy>());

        var vm = CreateVm();
        vm.SelectedBaseline = null;

        await vm.DeployAsNewCommand.ExecuteAsync(null);

        await _scService.DidNotReceive().CreateSettingsCatalogPolicyAsync(
            Arg.Any<DeviceManagementConfigurationPolicy>(), Arg.Any<CancellationToken>());
    }
}
