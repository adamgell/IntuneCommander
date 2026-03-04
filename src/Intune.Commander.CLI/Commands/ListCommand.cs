using Intune.Commander.CLI.Helpers;
using Intune.Commander.Core.Auth;
using Intune.Commander.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;
using System.CommandLine.Invocation;

namespace Intune.Commander.CLI.Commands;

public static class ListCommand
{
    private static readonly string[] AllTypes =
    [
        "configurations", "compliance", "applications", "endpoint-security", "administrative-templates",
        "settings-catalog", "enrollment-configurations", "app-protection", "managed-device-app-configurations",
        "targeted-managed-app-configurations", "terms-and-conditions", "scope-tags", "role-definitions",
        "intune-branding", "azure-branding", "autopilot", "device-health-scripts", "mac-custom-attributes",
        "feature-updates", "named-locations", "authentication-strengths", "authentication-contexts",
        "terms-of-use", "device-management-scripts", "device-shell-scripts", "compliance-scripts",
        "quality-updates", "driver-updates"
    ];

    public static Command Build()
    {
        var command = new Command("list", "List Intune objects of the given type");

        var typeArgument = new Argument<string>("type", "The object type to list (tab-complete to see all options)");
        typeArgument.AddCompletions(AllTypes);
        var profile = new Option<string?>("--profile");
        var tenantId = new Option<string?>("--tenant-id");
        var clientId = new Option<string?>("--client-id");
        var secret = new Option<string?>("--secret");
        var cloud = new Option<string?>("--cloud");
        var format = new Option<string>("--format", () => "table");

        command.AddArgument(typeArgument);
        command.AddOption(profile);
        command.AddOption(tenantId);
        command.AddOption(clientId);
        command.AddOption(secret);
        command.AddOption(cloud);
        command.AddOption(format);

        command.SetHandler(async context =>
        {
            await ExecuteAsync(
                context.ParseResult.GetValueForArgument(typeArgument),
                context.ParseResult.GetValueForOption(profile),
                context.ParseResult.GetValueForOption(tenantId),
                context.ParseResult.GetValueForOption(clientId),
                context.ParseResult.GetValueForOption(secret),
                context.ParseResult.GetValueForOption(cloud),
                context.ParseResult.GetValueForOption(format) ?? "table",
                context.GetCancellationToken());
        });
        return command;
    }

    private static async Task ExecuteAsync(
        string type,
        string? profile,
        string? tenantId,
        string? clientId,
        string? secret,
        string? cloud,
        string format,
        CancellationToken cancellationToken)
    {
        using var provider = CliServices.CreateServiceProvider();
        var profileService = provider.GetRequiredService<ProfileService>();
        var graphClientFactory = provider.GetRequiredService<IntuneGraphClientFactory>();

        var resolvedProfile = await ProfileResolver.ResolveAsync(profileService, profile, tenantId, clientId, secret, cloud, cancellationToken);
        var graphClient = await graphClientFactory.CreateClientAsync(resolvedProfile, AuthHelper.DeviceCodeToStderr, cancellationToken);

        var normalizedType = type.Trim().ToLowerInvariant();
        object result = normalizedType switch
        {
            "configurations"                      => await new ConfigurationProfileService(graphClient).ListDeviceConfigurationsAsync(cancellationToken),
            "compliance"                          => await new CompliancePolicyService(graphClient).ListCompliancePoliciesAsync(cancellationToken),
            "applications"                        => await new ApplicationService(graphClient).ListApplicationsAsync(cancellationToken),
            "endpoint-security"                   => await new EndpointSecurityService(graphClient).ListEndpointSecurityIntentsAsync(cancellationToken),
            "administrative-templates"            => await new AdministrativeTemplateService(graphClient).ListAdministrativeTemplatesAsync(cancellationToken),
            "settings-catalog"                    => await new SettingsCatalogService(graphClient).ListSettingsCatalogPoliciesAsync(cancellationToken),
            "enrollment-configurations"           => await new EnrollmentConfigurationService(graphClient).ListEnrollmentConfigurationsAsync(cancellationToken),
            "app-protection"                      => await new AppProtectionPolicyService(graphClient).ListAppProtectionPoliciesAsync(cancellationToken),
            "managed-device-app-configurations"   => await new ManagedAppConfigurationService(graphClient).ListManagedDeviceAppConfigurationsAsync(cancellationToken),
            "targeted-managed-app-configurations" => await new ManagedAppConfigurationService(graphClient).ListTargetedManagedAppConfigurationsAsync(cancellationToken),
            "terms-and-conditions"                => await new TermsAndConditionsService(graphClient).ListTermsAndConditionsAsync(cancellationToken),
            "scope-tags"                          => await new ScopeTagService(graphClient).ListScopeTagsAsync(cancellationToken),
            "role-definitions"                    => await new RoleDefinitionService(graphClient).ListRoleDefinitionsAsync(cancellationToken),
            "intune-branding"                     => await new IntuneBrandingService(graphClient).ListIntuneBrandingProfilesAsync(cancellationToken),
            "azure-branding"                      => await new AzureBrandingService(graphClient).ListBrandingLocalizationsAsync(cancellationToken),
            "autopilot"                           => await new AutopilotService(graphClient).ListAutopilotProfilesAsync(cancellationToken),
            "device-health-scripts"               => await new DeviceHealthScriptService(graphClient).ListDeviceHealthScriptsAsync(cancellationToken),
            "mac-custom-attributes"               => await new MacCustomAttributeService(graphClient).ListMacCustomAttributesAsync(cancellationToken),
            "feature-updates"                     => await new FeatureUpdateProfileService(graphClient).ListFeatureUpdateProfilesAsync(cancellationToken),
            "named-locations"                     => await new NamedLocationService(graphClient).ListNamedLocationsAsync(cancellationToken),
            "authentication-strengths"            => await new AuthenticationStrengthService(graphClient).ListAuthenticationStrengthPoliciesAsync(cancellationToken),
            "authentication-contexts"             => await new AuthenticationContextService(graphClient).ListAuthenticationContextsAsync(cancellationToken),
            "terms-of-use"                        => await new TermsOfUseService(graphClient).ListTermsOfUseAgreementsAsync(cancellationToken),
            "device-management-scripts"           => await new DeviceManagementScriptService(graphClient).ListDeviceManagementScriptsAsync(cancellationToken),
            "device-shell-scripts"                => await new DeviceShellScriptService(graphClient).ListDeviceShellScriptsAsync(cancellationToken),
            "compliance-scripts"                  => await new ComplianceScriptService(graphClient).ListComplianceScriptsAsync(cancellationToken),
            "quality-updates"                     => await new QualityUpdateProfileService(graphClient).ListQualityUpdateProfilesAsync(cancellationToken),
            "driver-updates"                      => await new DriverUpdateProfileService(graphClient).ListDriverUpdateProfilesAsync(cancellationToken),
            _ => throw new InvalidOperationException(
                $"Unsupported list type \"{type}\". Supported: {string.Join(", ", AllTypes)}")
        };

        if (string.Equals(format, "json", StringComparison.OrdinalIgnoreCase))
        {
            OutputFormatter.WriteJsonToStdout(result);
            return;
        }

        if (!string.Equals(format, "table", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Format must be \"table\" or \"json\".");

        if (result is not System.Collections.IEnumerable enumerable)
        {
            OutputFormatter.WriteJsonToStdout(result);
            return;
        }

        var rows = new List<string[]>();
        foreach (var item in enumerable)
        {
            var itemType = item?.GetType();
            rows.Add([
                itemType?.GetProperty("DisplayName")?.GetValue(item)?.ToString()
                    ?? itemType?.GetProperty("Name")?.GetValue(item)?.ToString()
                    ?? itemType?.GetProperty("ProfileName")?.GetValue(item)?.ToString()
                    ?? string.Empty,
                itemType?.GetProperty("Id")?.GetValue(item)?.ToString() ?? string.Empty,
                itemType?.GetProperty("OdataType")?.GetValue(item)?.ToString() ?? string.Empty
            ]);
        }

        OutputFormatter.WriteTable(["DisplayName", "Id", "ODataType"], rows);
    }
}
