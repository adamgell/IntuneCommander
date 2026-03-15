using Intune.Commander.Core.Auth;
using Intune.Commander.Core.Extensions;
using Intune.Commander.Core.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;

namespace Intune.Commander.Core.Tests.Extensions;

public class ServiceCollectionExtensionsTests : IDisposable
{
    private readonly ServiceCollection _services;
    private readonly ServiceProvider _provider;

    public ServiceCollectionExtensionsTests()
    {
        _services = [];
        _services.AddIntuneCommanderCore();
        _provider = _services.BuildServiceProvider();
    }

    public void Dispose()
    {
        _provider.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void AddIntuneCommanderCore_Returns_Same_ServiceCollection()
    {
        var services = new ServiceCollection();
        var result = services.AddIntuneCommanderCore();
        Assert.Same(services, result);
    }

    [Fact]
    public void Resolves_IDataProtectionProvider()
    {
        var dp = _provider.GetService<IDataProtectionProvider>();
        Assert.NotNull(dp);
    }

    [Fact]
    public void Resolves_IProfileEncryptionService_As_Singleton()
    {
        var svc1 = _provider.GetService<IProfileEncryptionService>();
        var svc2 = _provider.GetService<IProfileEncryptionService>();
        Assert.NotNull(svc1);
        Assert.IsType<ProfileEncryptionService>(svc1);
        Assert.Same(svc1, svc2);
    }

    [Fact]
    public void Resolves_IAuthenticationProvider_As_Singleton()
    {
        var svc1 = _provider.GetService<IAuthenticationProvider>();
        var svc2 = _provider.GetService<IAuthenticationProvider>();
        Assert.NotNull(svc1);
        Assert.IsType<InteractiveBrowserAuthProvider>(svc1);
        Assert.Same(svc1, svc2);
    }

    [Fact]
    public void Resolves_IntuneGraphClientFactory_As_Singleton()
    {
        var svc1 = _provider.GetService<IntuneGraphClientFactory>();
        var svc2 = _provider.GetService<IntuneGraphClientFactory>();
        Assert.NotNull(svc1);
        Assert.Same(svc1, svc2);
    }

    [Fact]
    public void Resolves_ProfileService_As_Singleton()
    {
        var svc1 = _provider.GetService<ProfileService>();
        var svc2 = _provider.GetService<ProfileService>();
        Assert.NotNull(svc1);
        Assert.Same(svc1, svc2);
    }

    [Fact]
    public void Resolves_IExportService_As_Transient()
    {
        var svc1 = _provider.GetService<IExportService>();
        var svc2 = _provider.GetService<IExportService>();
        Assert.NotNull(svc1);
        Assert.IsType<ExportService>(svc1);
        Assert.NotSame(svc1, svc2);
    }

    [Fact]
    public void Registers_ICacheService_As_Singleton()
    {
        var descriptor = Assert.Single(_services.Where(d => d.ServiceType == typeof(ICacheService)));
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
        Assert.NotNull(descriptor.ImplementationFactory);
    }

    [Fact]
    public void All_Expected_Services_Are_Registered()
    {
        // Verify every service registered by AddIntuneCommanderCore has a descriptor
        Assert.Contains(_services, d => d.ServiceType == typeof(IDataProtectionProvider));
        Assert.Contains(_services, d => d.ServiceType == typeof(IProfileEncryptionService));
        Assert.Contains(_services, d => d.ServiceType == typeof(IAuthenticationProvider));
        Assert.Contains(_services, d => d.ServiceType == typeof(IntuneGraphClientFactory));
        Assert.Contains(_services, d => d.ServiceType == typeof(ProfileService));
        Assert.Contains(_services, d => d.ServiceType == typeof(IExportService));
        Assert.Contains(_services, d => d.ServiceType == typeof(ICacheService));
    }
}
