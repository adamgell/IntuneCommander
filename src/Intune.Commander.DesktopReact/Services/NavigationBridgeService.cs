namespace Intune.Commander.DesktopReact.Services;

public class NavigationBridgeService
{
    public object GetCategories() => new
    {
        groups = new object[]
        {
            new
            {
                name = "Devices", icon = "laptop",
                children = new object[]
                {
                    new { name = "Device Configurations", icon = "cog" },
                    new { name = "Compliance Policies", icon = "check-circle-outline" },
                    new { name = "Settings Catalog", icon = "cog-outline" },
                    new { name = "Administrative Templates", icon = "receipt-outline" },
                    new { name = "Endpoint Security", icon = "shield-outline" },
                    new { name = "Device Categories", icon = "folder-outline" },
                    new { name = "Device Health Scripts", icon = "stethoscope" },
                    new { name = "Compliance Scripts", icon = "check-all" },
                    new { name = "Feature Updates", icon = "microsoft-windows" },
                    new { name = "Device Management Scripts", icon = "script-text-outline" },
                    new { name = "Device Shell Scripts", icon = "console" },
                }
            },
            new
            {
                name = "Applications", icon = "package-variant-closed",
                children = new object[]
                {
                    new { name = "Applications", icon = "package-variant" },
                    new { name = "Application Assignments", icon = "clipboard-text-outline" },
                    new { name = "App Protection Policies", icon = "lock-outline" },
                    new { name = "Managed Device App Configurations", icon = "cellphone-cog" },
                    new { name = "Targeted Managed App Configurations", icon = "target" },
                    new { name = "VPP Tokens", icon = "ticket-outline" },
                }
            },
            new
            {
                name = "Enrollment", icon = "card-account-details-outline",
                children = new object[]
                {
                    new { name = "Enrollment Configurations", icon = "card-account-details" },
                    new { name = "Autopilot Profiles", icon = "rocket-launch-outline" },
                    new { name = "Apple DEP", icon = "apple" },
                    new { name = "Cloud PC Provisioning Policies", icon = "desktop-classic" },
                }
            },
            new
            {
                name = "Identity & Access", icon = "lock-check-outline",
                children = new object[]
                {
                    new { name = "Conditional Access", icon = "lock-check" },
                    new { name = "Named Locations", icon = "map-marker-outline" },
                    new { name = "Authentication Strengths", icon = "shield-key-outline" },
                    new { name = "Authentication Contexts", icon = "tag-outline" },
                    new { name = "Terms of Use", icon = "file-document-outline" },
                }
            },
            new
            {
                name = "Tenant Admin", icon = "domain",
                children = new object[]
                {
                    new { name = "Scope Tags", icon = "tag-multiple-outline" },
                    new { name = "Role Definitions", icon = "briefcase-outline" },
                    new { name = "Role Assignments", icon = "key-outline" },
                    new { name = "Assignment Filters", icon = "filter-outline" },
                    new { name = "Policy Sets", icon = "folder-multiple-outline" },
                    new { name = "Intune Branding", icon = "palette-outline" },
                    new { name = "Azure Branding", icon = "microsoft-azure" },
                    new { name = "Terms and Conditions", icon = "script-outline" },
                    new { name = "Cloud PC User Settings", icon = "account-cog-outline" },
                    new { name = "ADMX Files", icon = "folder-zip-outline" },
                    new { name = "Reusable Policy Settings", icon = "link-variant" },
                }
            },
            new
            {
                name = "Groups & Monitoring", icon = "account-group-outline",
                children = new object[]
                {
                    new { name = "Dynamic Groups", icon = "account-convert-outline" },
                    new { name = "Assigned Groups", icon = "account-multiple-outline" },
                    new { name = "Mac Custom Attributes", icon = "apple-keyboard-command" },
                    new { name = "Notification Templates", icon = "bell-outline" },
                }
            }
        }
    };
}
