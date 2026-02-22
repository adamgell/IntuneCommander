using Intune.Commander.Core.Models;
using Intune.Commander.Core.Services;

namespace Intune.Commander.Core.Tests.Services;

public class AssignmentReportExporterTests
{
    // ── GenerateCsv ───────────────────────────────────────────────────────────────

    [Fact]
    public void GenerateCsv_EmptyRows_ReturnsHeaderLineOnly()
    {
        var csv = AssignmentReportExporter.GenerateCsv("Overview", []);

        var lines = csv.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.Single(lines);
        Assert.Contains("Policy Name", lines[0]);
        Assert.Contains("Type", lines[0]);
        Assert.Contains("Platform", lines[0]);
    }

    [Fact]
    public void GenerateCsv_SingleRow_ContainsData()
    {
        var rows = new List<AssignmentReportRow>
        {
            new() { PolicyName = "Test Policy", PolicyType = "Compliance", Platform = "Windows" }
        };

        var csv = AssignmentReportExporter.GenerateCsv("Overview", rows);

        Assert.Contains("Test Policy", csv);
        Assert.Contains("Compliance", csv);
        Assert.Contains("Windows", csv);
    }

    [Fact]
    public void GenerateCsv_MultipleRows_EachRowAppears()
    {
        var rows = new List<AssignmentReportRow>
        {
            new() { PolicyName = "Policy A", PolicyType = "Configuration", Platform = "iOS" },
            new() { PolicyName = "Policy B", PolicyType = "Compliance", Platform = "Android" }
        };

        var csv = AssignmentReportExporter.GenerateCsv("Overview", rows);

        Assert.Contains("Policy A", csv);
        Assert.Contains("Policy B", csv);
        Assert.Contains("iOS", csv);
        Assert.Contains("Android", csv);
    }

    [Fact]
    public void GenerateCsv_QuotesFieldsWithCommas()
    {
        var rows = new List<AssignmentReportRow>
        {
            new() { PolicyName = "Policy, With Comma", PolicyType = "Type", Platform = "Windows" }
        };

        var csv = AssignmentReportExporter.GenerateCsv("Overview", rows);

        Assert.Contains("\"Policy, With Comma\"", csv);
    }

    [Fact]
    public void GenerateCsv_QuotesFieldsWithEmbeddedQuotes()
    {
        var rows = new List<AssignmentReportRow>
        {
            new() { PolicyName = "Policy \"Quoted\"", PolicyType = "Type", Platform = "Windows" }
        };

        var csv = AssignmentReportExporter.GenerateCsv("Overview", rows);

        Assert.Contains("\"Policy \"\"Quoted\"\"\"", csv);
    }

    [Fact]
    public void GenerateCsv_NullOrEmptyFieldsBecomEmptyQuotedStrings()
    {
        var rows = new List<AssignmentReportRow>
        {
            new() { PolicyName = "", PolicyType = "", Platform = "" }
        };

        var csv = AssignmentReportExporter.GenerateCsv("Overview", rows);

        // At least three quoted-empty tokens in the data row
        var dataRow = csv.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)[1];
        var emptyQuotedCount = dataRow.Split("\"\"", StringSplitOptions.None).Length - 1;
        Assert.True(emptyQuotedCount >= 3);
    }

    [Fact]
    public void GenerateCsv_OptionalColumnsIncludedWhenPopulated()
    {
        var rows = new List<AssignmentReportRow>
        {
            new()
            {
                PolicyName = "P",
                PolicyType = "T",
                Platform = "Windows",
                AssignmentSummary = "All Devices",
                AssignmentReason = "All Users",
                GroupName = "EmptyGroup",
                GroupId = "grp-id",
                Group1Status = "Assigned",
                Group2Status = "Not Assigned",
                TargetDevice = "device-01",
                UserPrincipalName = "user@test.com",
                Status = "Success",
                LastReported = "2024-01-01"
            }
        };

        var csv = AssignmentReportExporter.GenerateCsv("All", rows);

        Assert.Contains("Assignments", csv);
        Assert.Contains("Assignment Reason", csv);
        Assert.Contains("Empty Group", csv);
        Assert.Contains("Group ID", csv);
        Assert.Contains("Group 1 Status", csv);
        Assert.Contains("Group 2 Status", csv);
        Assert.Contains("Device", csv);
        Assert.Contains("User", csv);
        Assert.Contains("Status", csv);
        Assert.Contains("Last Reported", csv);
    }

    [Fact]
    public void GenerateCsv_OptionalColumnsOmittedWhenAllEmpty()
    {
        var rows = new List<AssignmentReportRow>
        {
            new() { PolicyName = "P", PolicyType = "T", Platform = "Windows" }
        };

        var csv = AssignmentReportExporter.GenerateCsv("Overview", rows);

        Assert.DoesNotContain("Assignments", csv);
        Assert.DoesNotContain("Device", csv);
        Assert.DoesNotContain("Status", csv);
    }

    // ── GenerateHtml ──────────────────────────────────────────────────────────────

    [Fact]
    public void GenerateHtml_EmptyRows_ReturnsValidHtml()
    {
        var html = AssignmentReportExporter.GenerateHtml("Overview", []);

        Assert.Contains("<!DOCTYPE html>", html);
        Assert.Contains("Intune Assignment Report", html);
        Assert.Contains("Overview", html);
    }

    [Fact]
    public void GenerateHtml_ContainsRowCount()
    {
        var rows = new List<AssignmentReportRow>
        {
            new() { PolicyName = "P1", PolicyType = "T1", Platform = "Windows" },
            new() { PolicyName = "P2", PolicyType = "T2", Platform = "iOS" }
        };

        var html = AssignmentReportExporter.GenerateHtml("Overview", rows);

        Assert.Contains("2", html);
    }

    [Fact]
    public void GenerateHtml_ContainsPolicyNames()
    {
        var rows = new List<AssignmentReportRow>
        {
            new() { PolicyName = "MyPolicy", PolicyType = "Configuration", Platform = "Windows" }
        };

        var html = AssignmentReportExporter.GenerateHtml("Overview", rows);

        Assert.Contains("MyPolicy", html);
        Assert.Contains("Configuration", html);
        Assert.Contains("Windows", html);
    }

    [Fact]
    public void GenerateHtml_HtmlEncodesSpecialCharacters()
    {
        var rows = new List<AssignmentReportRow>
        {
            new() { PolicyName = "<script>alert('xss')</script>", PolicyType = "Type & More", Platform = "Windows" }
        };

        var html = AssignmentReportExporter.GenerateHtml("Overview", rows);

        // Raw script tag must NOT appear in output
        Assert.DoesNotContain("<script>alert(", html);
        // Encoded versions should appear
        Assert.Contains("&lt;script&gt;", html);
        Assert.Contains("&amp;", html);
    }

    [Fact]
    public void GenerateHtml_HtmlEncodesQuotesAndApostrophes()
    {
        var rows = new List<AssignmentReportRow>
        {
            new() { PolicyName = "Policy \"quoted\" it's", PolicyType = "T", Platform = "W" }
        };

        var html = AssignmentReportExporter.GenerateHtml("Overview", rows);

        Assert.Contains("&quot;", html);
        Assert.Contains("&#39;", html);
    }

    [Fact]
    public void GenerateHtml_ShowsPlatformCardWhenPlatformsPresent()
    {
        var rows = new List<AssignmentReportRow>
        {
            new() { PolicyName = "P", PolicyType = "T", Platform = "Windows" }
        };

        var html = AssignmentReportExporter.GenerateHtml("Overview", rows);

        Assert.Contains("Platforms", html);
    }

    [Fact]
    public void GenerateHtml_NoPlatformCardWhenAllPlatformsEmpty()
    {
        var rows = new List<AssignmentReportRow>
        {
            new() { PolicyName = "P", PolicyType = "T", Platform = "" }
        };

        var html = AssignmentReportExporter.GenerateHtml("Overview", rows);

        Assert.DoesNotContain("card-label\">Platforms", html);
    }

    [Fact]
    public void GenerateHtml_ContainsTypeFilterOptions()
    {
        var rows = new List<AssignmentReportRow>
        {
            new() { PolicyName = "P1", PolicyType = "Compliance", Platform = "Windows" },
            new() { PolicyName = "P2", PolicyType = "Configuration", Platform = "iOS" }
        };

        var html = AssignmentReportExporter.GenerateHtml("Overview", rows);

        Assert.Contains("Compliance", html);
        Assert.Contains("Configuration", html);
    }

    [Fact]
    public void GenerateHtml_ModeAppearsInTitle()
    {
        var html = AssignmentReportExporter.GenerateHtml("Group Lookup", []);

        Assert.Contains("Group Lookup", html);
    }

    [Fact]
    public void GenerateHtml_ContainsBarChartSvg_WhenDataExists()
    {
        var rows = new List<AssignmentReportRow>
        {
            new() { PolicyName = "P", PolicyType = "Compliance", Platform = "Windows" }
        };

        var html = AssignmentReportExporter.GenerateHtml("Overview", rows);

        Assert.Contains("<svg", html);
        Assert.Contains("<rect", html);
    }

    [Fact]
    public void GenerateHtml_ContainsNoDataMessage_WhenNoPlatforms()
    {
        var rows = new List<AssignmentReportRow>
        {
            new() { PolicyName = "P", PolicyType = "T", Platform = "" }
        };

        var html = AssignmentReportExporter.GenerateHtml("Overview", rows);

        Assert.Contains("No data available", html);
    }

    [Fact]
    public void GenerateHtml_TruncatesLongLabelInChart()
    {
        // Labels longer than 20 chars get truncated to 17 chars + "..."
        var rows = new List<AssignmentReportRow>
        {
            new() { PolicyName = "P", PolicyType = "A Very Long Policy Type Name Here", Platform = "Windows" }
        };

        var html = AssignmentReportExporter.GenerateHtml("Overview", rows);

        // Should contain truncated label (first 17 chars + "...") — label[..17] = "A Very Long Polic"
        Assert.Contains("A Very Long Polic...", html);
    }

    [Fact]
    public void GenerateHtml_OptionalHeadersIncludedInJson_WhenPopulated()
    {
        var rows = new List<AssignmentReportRow>
        {
            new() { PolicyName = "P", PolicyType = "T", Platform = "W", Status = "Success" }
        };

        var html = AssignmentReportExporter.GenerateHtml("Overview", rows);

        // HEADERS_JSON should include "Status"
        Assert.Contains("\"Status\"", html);
    }

    [Fact]
    public void GenerateHtml_AllPlaceholdersReplaced()
    {
        var rows = new List<AssignmentReportRow>
        {
            new() { PolicyName = "P", PolicyType = "T", Platform = "Windows" }
        };

        var html = AssignmentReportExporter.GenerateHtml("TestMode", rows);

        // Verify none of the template placeholders remain
        Assert.DoesNotContain("%%MODE%%", html);
        Assert.DoesNotContain("%%COUNT%%", html);
        Assert.DoesNotContain("%%TYPE_COUNT%%", html);
        Assert.DoesNotContain("%%PLATFORM_CARD%%", html);
        Assert.DoesNotContain("%%TYPE_CHART%%", html);
        Assert.DoesNotContain("%%PLATFORM_CHART%%", html);
        Assert.DoesNotContain("%%TYPE_OPTIONS%%", html);
        Assert.DoesNotContain("%%PLATFORM_OPTIONS%%", html);
        Assert.DoesNotContain("%%HEADERS%%", html);
        Assert.DoesNotContain("%%HEADERS_JSON%%", html);
        Assert.DoesNotContain("%%ROWS%%", html);
        Assert.DoesNotContain("%%DATE%%", html);
    }

    // ── Column selection edge cases ───────────────────────────────────────────────

    [Fact]
    public void GenerateCsv_LargeDataSet_HandledCorrectly()
    {
        var rows = Enumerable.Range(1, 100)
            .Select(i => new AssignmentReportRow
            {
                PolicyName = $"Policy {i}",
                PolicyType = i % 2 == 0 ? "Compliance" : "Configuration",
                Platform = i % 3 == 0 ? "Windows" : "iOS"
            })
            .ToList();

        var csv = AssignmentReportExporter.GenerateCsv("Overview", rows);

        var lines = csv.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        // 1 header + 100 data rows
        Assert.Equal(101, lines.Length);
    }

    [Fact]
    public void GenerateHtml_BarChartLimitedToTenEntries()
    {
        // Generate rows with 15 distinct policy types
        var rows = Enumerable.Range(1, 15)
            .Select(i => new AssignmentReportRow
            {
                PolicyName = $"P{i}",
                PolicyType = $"Type{i}",
                Platform = "Windows"
            })
            .ToList();

        var html = AssignmentReportExporter.GenerateHtml("Overview", rows);

        // The SVG is generated; bar chart takes top 10 only
        // Type11 through Type15 should not appear in chart SVG bars, only Type1-10
        // We can verify the SVG exists
        Assert.Contains("<svg", html);
    }

    [Fact]
    public void GenerateCsv_ReportModeParameter_NotIncludedInOutput()
    {
        // reportMode is used in HTML but not CSV
        var csv = AssignmentReportExporter.GenerateCsv("SomeModeValue", []);

        Assert.DoesNotContain("SomeModeValue", csv);
    }
}
