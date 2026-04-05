namespace Intune.Commander.DesktopReact.Models;

public record ExportResult(int ExportedCount, string OutputPath);

public record ImportPreviewItem(string ObjectType, string Name, string FileName);

public record ImportPreview(ImportPreviewItem[] Items, int TotalCount, string[] ObjectTypes);

public record ImportResultItem(string ObjectType, string Name, bool Success, string? Error);

public record ImportResult(ImportResultItem[] Items, int SuccessCount, int FailureCount);
