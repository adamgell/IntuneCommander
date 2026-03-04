namespace Intune.Commander.Desktop.ViewModels;

/// <summary>
/// Represents a single localized message entry from a notification template.
/// </summary>
public record LocalizedNotificationMessageItem(string Locale, string Subject, string MessageTemplate);
