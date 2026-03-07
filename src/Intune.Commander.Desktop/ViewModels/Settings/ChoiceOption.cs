namespace Intune.Commander.Desktop.ViewModels.Settings;

public sealed record ChoiceOption(string ItemId, string? DisplayName, string? Description)
{
    public override string ToString() => DisplayName ?? ItemId;
}
