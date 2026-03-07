using Intune.Commander.Core.Models;

namespace Intune.Commander.CLI.Models;

public sealed class ImportCommandOutput
{
    public required CommandResult Result { get; init; }
    public required MigrationTable MigrationTable { get; init; }
    public required ImportSummary Summary { get; init; }
    public required IReadOnlyList<ImportValidationError> ValidationErrors { get; init; }
}

public sealed class ImportSummary
{
    public required int Total { get; init; }
    public required Dictionary<string, int> PerTypeCounts { get; init; }
    public required int ValidationErrorCount { get; init; }
}

public sealed class ImportValidationError
{
    public required string SummaryKey { get; init; }
    public required string RelativePath { get; init; }
    public required string ErrorType { get; init; }
    public required string Message { get; init; }
}
