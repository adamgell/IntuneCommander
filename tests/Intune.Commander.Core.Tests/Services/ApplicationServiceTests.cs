using System.Reflection;
using Intune.Commander.Core.Models;
using Intune.Commander.Core.Services;
using Microsoft.Graph.Beta;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.Core.Tests.Services;

public class ApplicationServiceTests
{
    [Fact]
    public void Service_ImplementsInterface()
    {
        Assert.True(typeof(IApplicationService).IsAssignableFrom(typeof(ApplicationService)));
    }

    [Fact]
    public void Service_HasGraphClientConstructor()
    {
        var constructor = typeof(ApplicationService).GetConstructor([typeof(GraphServiceClient)]);
        Assert.NotNull(constructor);
    }

    [Fact]
    public void Interface_DefinesListMethod()
    {
        var method = typeof(IApplicationService).GetMethod("ListApplicationsAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<List<MobileApp>>), method.ReturnType);
    }

    [Fact]
    public void Interface_DefinesGetMethod()
    {
        var method = typeof(IApplicationService).GetMethod("GetApplicationAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<MobileApp?>), method.ReturnType);
        var parameters = method.GetParameters();
        Assert.Equal(typeof(string), parameters[0].ParameterType);
    }

    [Fact]
    public void Interface_DefinesGetAssignmentsMethod()
    {
        var method = typeof(IApplicationService).GetMethod("GetAssignmentsAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<List<MobileAppAssignment>>), method.ReturnType);
        var parameters = method.GetParameters();
        Assert.Equal(typeof(string), parameters[0].ParameterType);
    }

    [Fact]
    public void Interface_AllMethodsAcceptCancellationToken()
    {
        var methods = typeof(IApplicationService).GetMethods();
        foreach (var method in methods)
        {
            var parameters = method.GetParameters();
            var lastParam = parameters[^1];
            Assert.Equal(typeof(CancellationToken), lastParam.ParameterType);
            Assert.True(lastParam.HasDefaultValue);
        }
    }

    [Fact]
    public void Interface_HasThreeMethods()
    {
        var methods = typeof(IApplicationService).GetMethods();
        Assert.Equal(3, methods.Length);
    }

    [Fact]
    public void ExportModel_HasRequiredProperties()
    {
        var export = new ApplicationExport
        {
            Application = new MobileApp { Id = "test", DisplayName = "Test App" }
        };

        Assert.NotNull(export.Application);
        Assert.Empty(export.Assignments);
    }

    [Fact]
    public void ExportModel_AssignmentsDefaultsToEmptyList()
    {
        var export = new ApplicationExport
        {
            Application = new MobileApp()
        };

        Assert.NotNull(export.Assignments);
        Assert.IsType<List<MobileAppAssignment>>(export.Assignments);
    }

    [Fact]
    public void ExportModel_CanSetAssignments()
    {
        var assignments = new List<MobileAppAssignment>
        {
            new() { Id = "a1" },
            new() { Id = "a2" }
        };

        var export = new ApplicationExport
        {
            Application = new MobileApp(),
            Assignments = assignments
        };

        Assert.Equal(2, export.Assignments.Count);
    }
}

/// <summary>
/// Tests for the private EnsureOdataType helper via reflection.
/// </summary>
public class ApplicationServiceEnsureOdataTypeTests
{
    private static readonly MethodInfo EnsureOdataType =
        typeof(ApplicationService).GetMethod(
            "EnsureOdataType",
            BindingFlags.NonPublic | BindingFlags.Static)!;

    private static void Invoke(MobileApp app) =>
        EnsureOdataType.Invoke(null, [app]);

    [Fact]
    public void EnsureOdataType_WhenOdataTypeAlreadySet_DoesNotOverwrite()
    {
        var app = new MobileApp { OdataType = "#microsoft.graph.mobileApp" };

        Invoke(app);

        Assert.Equal("#microsoft.graph.mobileApp", app.OdataType);
    }

    [Fact]
    public void EnsureOdataType_WhenConcreteSubclass_SetsOdataTypeFromTypeName()
    {
        // Win32LobApp is a concrete Graph SDK subclass of MobileApp
        var app = new Win32LobApp { OdataType = null };

        Invoke(app);

        // Should derive from type name: win32LobApp
        Assert.Equal("#microsoft.graph.win32LobApp", app.OdataType);
    }

    [Fact]
    public void EnsureOdataType_WhenBaseMobileAppWithOdataInAdditionalData_SetsFromAdditionalData()
    {
        var app = new MobileApp
        {
            OdataType = null,
            AdditionalData = new Dictionary<string, object>
            {
                ["@odata.type"] = "#microsoft.graph.androidForWorkApp"
            }
        };

        Invoke(app);

        Assert.Equal("#microsoft.graph.androidForWorkApp", app.OdataType);
    }

    [Fact]
    public void EnsureOdataType_WhenBaseMobileAppWithNoAdditionalData_LeavesOdataTypeNull()
    {
        var app = new MobileApp { OdataType = null, AdditionalData = null };

        Invoke(app);

        Assert.Null(app.OdataType);
    }

    [Fact]
    public void EnsureOdataType_WhenBaseMobileAppWithEmptyOdataInAdditionalData_LeavesOdataTypeNull()
    {
        var app = new MobileApp
        {
            OdataType = null,
            AdditionalData = new Dictionary<string, object>
            {
                ["@odata.type"] = ""
            }
        };

        Invoke(app);

        Assert.Null(app.OdataType);
    }

    [Fact]
    public void EnsureOdataType_WhenAdditionalDataDoesNotContainOdataType_LeavesNull()
    {
        var app = new MobileApp
        {
            OdataType = null,
            AdditionalData = new Dictionary<string, object>
            {
                ["someOtherKey"] = "someValue"
            }
        };

        Invoke(app);

        Assert.Null(app.OdataType);
    }

    [Fact]
    public void EnsureOdataType_MethodExists_ViaReflection()
    {
        Assert.NotNull(EnsureOdataType);
    }

    [Fact]
    public void EnsureOdataType_WhenWindowsStoreApp_SetsCorrectOdataType()
    {
        var app = new WindowsStoreApp { OdataType = null };

        Invoke(app);

        Assert.Equal("#microsoft.graph.windowsStoreApp", app.OdataType);
    }

    [Fact]
    public void EnsureOdataType_OdataTypeDerivation_LowercasesFirstChar()
    {
        // The implementation does: char.ToLowerInvariant(typeName[0]) + typeName[1..]
        // All concrete Graph types start with uppercase; result should be lowercase-first
        var app = new Win32LobApp { OdataType = null };

        Invoke(app);

        var result = app.OdataType!;
        Assert.StartsWith("#microsoft.graph.", result);
        var typeNamePart = result["#microsoft.graph.".Length..];
        Assert.True(char.IsLower(typeNamePart[0]));
    }
}
