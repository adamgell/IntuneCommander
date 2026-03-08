namespace Intune.Commander.Desktop.Models;

/// <summary>
/// A display model for policy settings in the UI. Supports richer Settings Catalog
/// metadata while remaining compatible with simpler label/value usages elsewhere.
/// </summary>
public sealed record SettingItem(
    string Label,
    string Value,
    string? Description = null,
    string? Category = null,
    string? DefinitionId = null);
