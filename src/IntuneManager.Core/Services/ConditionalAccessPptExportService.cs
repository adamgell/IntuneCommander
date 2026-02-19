using Microsoft.Graph.Beta.Models;
using SyncPresentation = Syncfusion.Presentation;

namespace IntuneManager.Core.Services;

/// <summary>
/// Service for exporting Conditional Access policies to PowerPoint format.
/// Generates a comprehensive deck with policy summaries, conditions, grants, and assignments.
/// </summary>
public class ConditionalAccessPptExportService : IConditionalAccessPptExportService
{
    private readonly IConditionalAccessPolicyService _caPolicyService;
    private readonly INamedLocationService _namedLocationService;
    private readonly IAuthenticationStrengthService _authStrengthService;
    private readonly IAuthenticationContextService _authContextService;
    private readonly IApplicationService _applicationService;

    public ConditionalAccessPptExportService(
        IConditionalAccessPolicyService caPolicyService,
        INamedLocationService namedLocationService,
        IAuthenticationStrengthService authStrengthService,
        IAuthenticationContextService authContextService,
        IApplicationService applicationService)
    {
        _caPolicyService = caPolicyService;
        _namedLocationService = namedLocationService;
        _authStrengthService = authStrengthService;
        _authContextService = authContextService;
        _applicationService = applicationService;
    }

    public async Task ExportAsync(
        string outputPath,
        string tenantName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentException("Output path must not be null, empty, or whitespace.", nameof(outputPath));
        }

        if (string.IsNullOrWhiteSpace(tenantName))
        {
            throw new ArgumentException("Tenant name must not be null, empty, or whitespace.", nameof(tenantName));
        }

        // Load all required data
        var policies = await _caPolicyService.ListPoliciesAsync(cancellationToken);
        var namedLocations = await _namedLocationService.ListNamedLocationsAsync(cancellationToken);
        var authStrengths = await _authStrengthService.ListAuthenticationStrengthPoliciesAsync(cancellationToken);
        var authContexts = await _authContextService.ListAuthenticationContextsAsync(cancellationToken);
        var applications = await _applicationService.ListApplicationsAsync(cancellationToken);

        // Create presentation
        using var presentation = SyncPresentation.Presentation.Create();

        // Generate slides
        AddCoverSlide(presentation, tenantName);
        AddTenantSummarySlide(presentation, tenantName, policies.Count);
        AddPolicyInventorySlide(presentation, policies);
        
        // Add detail slides for each policy
        foreach (var policy in policies.OrderBy(p => p.DisplayName))
        {
            cancellationToken.ThrowIfCancellationRequested();
            AddPolicyDetailSlide(presentation, policy);
        }

        // Ensure output directory exists
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Save presentation
        using var fileStream = File.Create(outputPath);
        presentation.Save(fileStream);
    }

    private void AddCoverSlide(SyncPresentation.IPresentation presentation, string tenantName)
    {
        var slide = presentation.Slides.Add(SyncPresentation.SlideLayoutType.Blank);
        
        // Add title
        var titleShape = slide.Shapes.AddTextBox(50, 150, 600, 100);
        var titleParagraph = titleShape.TextBody.AddParagraph();
        titleParagraph.Text = "Conditional Access Policy Report";
        titleParagraph.Font.FontSize = 44;
        titleParagraph.Font.Bold = true;
        titleParagraph.HorizontalAlignment = SyncPresentation.HorizontalAlignmentType.Center;

        // Add tenant name
        var tenantShape = slide.Shapes.AddTextBox(50, 270, 600, 60);
        var tenantParagraph = tenantShape.TextBody.AddParagraph();
        tenantParagraph.Text = tenantName;
        tenantParagraph.Font.FontSize = 28;
        tenantParagraph.HorizontalAlignment = SyncPresentation.HorizontalAlignmentType.Center;

        // Add timestamp
        var timestampShape = slide.Shapes.AddTextBox(50, 400, 600, 40);
        var timestampParagraph = timestampShape.TextBody.AddParagraph();
        timestampParagraph.Text = $"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC";
        timestampParagraph.Font.FontSize = 16;
        timestampParagraph.HorizontalAlignment = SyncPresentation.HorizontalAlignmentType.Center;
    }

    private void AddTenantSummarySlide(SyncPresentation.IPresentation presentation, string tenantName, int policyCount)
    {
        var slide = presentation.Slides.Add(SyncPresentation.SlideLayoutType.Blank);
        
        // Add title
        var titleShape = slide.Shapes.AddTextBox(50, 50, 600, 60);
        var titleParagraph = titleShape.TextBody.AddParagraph();
        titleParagraph.Text = "Tenant Summary";
        titleParagraph.Font.FontSize = 32;
        titleParagraph.Font.Bold = true;

        // Add summary content
        var contentShape = slide.Shapes.AddTextBox(50, 130, 600, 300);
        
        var tenantPara = contentShape.TextBody.AddParagraph();
        tenantPara.Text = $"Tenant: {tenantName}";
        tenantPara.Font.FontSize = 20;
        
        var policyPara = contentShape.TextBody.AddParagraph();
        policyPara.Text = $"Total Policies: {policyCount}";
        policyPara.Font.FontSize = 20;
        
        var exportPara = contentShape.TextBody.AddParagraph();
        exportPara.Text = $"Export Date: {DateTime.UtcNow:yyyy-MM-dd}";
        exportPara.Font.FontSize = 20;
    }

    private void AddPolicyInventorySlide(SyncPresentation.IPresentation presentation, List<ConditionalAccessPolicy> policies)
    {
        var slide = presentation.Slides.Add(SyncPresentation.SlideLayoutType.Blank);
        
        // Add title
        var titleShape = slide.Shapes.AddTextBox(50, 50, 600, 60);
        var titleParagraph = titleShape.TextBody.AddParagraph();
        titleParagraph.Text = "Policy Inventory";
        titleParagraph.Font.FontSize = 32;
        titleParagraph.Font.Bold = true;

        // Add policies table
        var table = slide.Shapes.AddTable(2, 3, 50, 130, 600, 350);
        
        // Header row
        table.Rows[0].Cells[0].TextBody.AddParagraph("Policy Name");
        table.Rows[0].Cells[1].TextBody.AddParagraph("State");
        table.Rows[0].Cells[2].TextBody.AddParagraph("Created");
        
        // Make header bold
        for (int i = 0; i < 3; i++)
        {
            table.Rows[0].Cells[i].TextBody.Paragraphs[0].Font.Bold = true;
        }

        // Add policy rows for all policies
        var displayPolicies = policies.OrderBy(p => p.DisplayName).ToList();
        for (int i = 0; i < displayPolicies.Count; i++)
        {
            if (i + 1 >= table.Rows.Count)
                table.Rows.Add();
                
            var policy = displayPolicies[i];
            table.Rows[i + 1].Cells[0].TextBody.AddParagraph(policy.DisplayName ?? "Unnamed");
            table.Rows[i + 1].Cells[1].TextBody.AddParagraph(policy.State?.ToString() ?? "Unknown");
            table.Rows[i + 1].Cells[2].TextBody.AddParagraph(policy.CreatedDateTime?.ToString("yyyy-MM-dd") ?? "N/A");
        }
    }

    private void AddPolicyDetailSlide(
        SyncPresentation.IPresentation presentation,
        ConditionalAccessPolicy policy)
    {
        var slide = presentation.Slides.Add(SyncPresentation.SlideLayoutType.Blank);
        
        // Add title
        var titleShape = slide.Shapes.AddTextBox(50, 30, 600, 50);
        var titleParagraph = titleShape.TextBody.AddParagraph();
        titleParagraph.Text = policy.DisplayName ?? "Unnamed Policy";
        titleParagraph.Font.FontSize = 28;
        titleParagraph.Font.Bold = true;

        // Add policy state
        var stateShape = slide.Shapes.AddTextBox(50, 90, 600, 30);
        var stateParagraph = stateShape.TextBody.AddParagraph();
        stateParagraph.Text = $"State: {policy.State?.ToString() ?? "Unknown"}";
        stateParagraph.Font.FontSize = 16;

        // Add conditions summary
        var conditionsShape = slide.Shapes.AddTextBox(50, 130, 300, 300);
        var conditionsTitle = conditionsShape.TextBody.AddParagraph();
        conditionsTitle.Text = "Conditions";
        conditionsTitle.Font.FontSize = 18;
        conditionsTitle.Font.Bold = true;
        
        AddConditionsSummary(conditionsShape, policy);

        // Add grant controls summary
        var grantsShape = slide.Shapes.AddTextBox(370, 130, 300, 300);
        var grantsTitle = grantsShape.TextBody.AddParagraph();
        grantsTitle.Text = "Grant Controls";
        grantsTitle.Font.FontSize = 18;
        grantsTitle.Font.Bold = true;
        
        AddGrantControlsSummary(grantsShape, policy);
    }

    private void AddConditionsSummary(SyncPresentation.IShape shape, ConditionalAccessPolicy policy)
    {
        var conditions = policy.Conditions;
        if (conditions == null) return;

        // Users
        if (conditions.Users != null)
        {
            var para = shape.TextBody.AddParagraph();
            var includeUsers = conditions.Users.IncludeUsers?.Count ?? 0;
            var excludeUsers = conditions.Users.ExcludeUsers?.Count ?? 0;
            para.Text = $"Users: +{includeUsers} -{excludeUsers}";
            para.Font.FontSize = 14;
        }

        // Applications
        if (conditions.Applications != null)
        {
            var para = shape.TextBody.AddParagraph();
            var includeApps = conditions.Applications.IncludeApplications?.Count ?? 0;
            var excludeApps = conditions.Applications.ExcludeApplications?.Count ?? 0;
            para.Text = $"Apps: +{includeApps} -{excludeApps}";
            para.Font.FontSize = 14;
        }

        // Platforms
        if (conditions.Platforms != null)
        {
            var para = shape.TextBody.AddParagraph();
            var includePlatforms = conditions.Platforms.IncludePlatforms?.Count ?? 0;
            para.Text = $"Platforms: {includePlatforms} selected";
            para.Font.FontSize = 14;
        }

        // Locations
        if (conditions.Locations != null)
        {
            var para = shape.TextBody.AddParagraph();
            var includeLocations = conditions.Locations.IncludeLocations?.Count ?? 0;
            var excludeLocations = conditions.Locations.ExcludeLocations?.Count ?? 0;
            para.Text = $"Locations: +{includeLocations} -{excludeLocations}";
            para.Font.FontSize = 14;
        }
    }

    private void AddGrantControlsSummary(SyncPresentation.IShape shape, ConditionalAccessPolicy policy)
    {
        var grantControls = policy.GrantControls;
        if (grantControls == null)
        {
            var para = shape.TextBody.AddParagraph();
            para.Text = "No grant controls";
            para.Font.FontSize = 14;
            return;
        }

        // Operator
        var opPara = shape.TextBody.AddParagraph();
        opPara.Text = $"Operator: {grantControls.Operator ?? "N/A"}";
        opPara.Font.FontSize = 14;

        // Built-in controls
        if (grantControls.BuiltInControls != null && grantControls.BuiltInControls.Any())
        {
            var controlsPara = shape.TextBody.AddParagraph();
            controlsPara.Text = $"Controls: {string.Join(", ", grantControls.BuiltInControls)}";
            controlsPara.Font.FontSize = 14;
        }

        // Terms of Use
        if (grantControls.TermsOfUse != null && grantControls.TermsOfUse.Any())
        {
            var touPara = shape.TextBody.AddParagraph();
            touPara.Text = $"Terms of Use: {grantControls.TermsOfUse.Count} required";
            touPara.Font.FontSize = 14;
        }

        // Custom controls
        if (grantControls.CustomAuthenticationFactors != null && grantControls.CustomAuthenticationFactors.Any())
        {
            var customPara = shape.TextBody.AddParagraph();
            customPara.Text = $"Custom Controls: {grantControls.CustomAuthenticationFactors.Count}";
            customPara.Font.FontSize = 14;
        }
    }
}
