using Intune.Commander.Core.Services;
using Microsoft.Graph.Beta;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.Core.Tests.Services;

public class NotificationTemplateServiceTests
{
    [Fact]
    public void Service_ImplementsInterface()
    {
        Assert.True(typeof(INotificationTemplateService).IsAssignableFrom(typeof(NotificationTemplateService)));
    }

    [Fact]
    public void Service_HasGraphClientConstructor()
    {
        var constructor = typeof(NotificationTemplateService).GetConstructor([typeof(GraphServiceClient)]);
        Assert.NotNull(constructor);
    }

    [Fact]
    public void Interface_DefinesListMethod()
    {
        var method = typeof(INotificationTemplateService).GetMethod("ListNotificationTemplatesAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<List<NotificationMessageTemplate>>), method.ReturnType);
    }

    [Fact]
    public void Interface_DefinesGetMethod()
    {
        var method = typeof(INotificationTemplateService).GetMethod("GetNotificationTemplateAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<NotificationMessageTemplate?>), method.ReturnType);
        var parameters = method.GetParameters();
        Assert.Equal(typeof(string), parameters[0].ParameterType);
    }

    [Fact]
    public void Interface_DefinesCreateMethod()
    {
        var method = typeof(INotificationTemplateService).GetMethod("CreateNotificationTemplateAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<NotificationMessageTemplate>), method.ReturnType);
        var parameters = method.GetParameters();
        Assert.Equal(typeof(NotificationMessageTemplate), parameters[0].ParameterType);
        Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);
    }

    [Fact]
    public void Interface_DefinesUpdateMethod()
    {
        var method = typeof(INotificationTemplateService).GetMethod("UpdateNotificationTemplateAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<NotificationMessageTemplate>), method.ReturnType);
        var parameters = method.GetParameters();
        Assert.Equal(typeof(NotificationMessageTemplate), parameters[0].ParameterType);
    }

    [Fact]
    public void Interface_DefinesDeleteMethod()
    {
        var method = typeof(INotificationTemplateService).GetMethod("DeleteNotificationTemplateAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task), method.ReturnType);
        var parameters = method.GetParameters();
        Assert.Equal(typeof(string), parameters[0].ParameterType);
    }

    [Fact]
    public void Interface_AllMethodsAcceptCancellationToken()
    {
        var methods = typeof(INotificationTemplateService).GetMethods();
        foreach (var method in methods)
        {
            var parameters = method.GetParameters();
            var lastParam = parameters[^1];
            Assert.Equal(typeof(CancellationToken), lastParam.ParameterType);
            Assert.True(lastParam.HasDefaultValue);
        }
    }

    [Fact]
    public void Interface_HasFiveMethods()
    {
        var methods = typeof(INotificationTemplateService).GetMethods();
        Assert.Equal(5, methods.Length);
    }
}
