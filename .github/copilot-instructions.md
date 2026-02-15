# Copilot Instructions

## Project Guidelines
- User is building an Intune management GUI in Avalonia specifically to avoid UI deadlocks/freezing issues they experience with PowerShell. Async responsiveness is a priority.
- The UI startup must NEVER block or wait on any async operation. All data loading (profiles, services, etc.) must happen asynchronously after the window is already visible. No `.GetAwaiter().GetResult()`, `.Wait()`, or `.Result` calls on the UI thread — ever.