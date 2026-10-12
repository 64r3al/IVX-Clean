using System.Threading.Tasks;
using IvxClean.Core.Quarantine;

namespace IvxClean.App.Services;

/// <summary>Shows confirmation and chooser dialogs. Abstracted so view models stay testable.</summary>
public interface IDialogService
{
    /// <summary>Shows a confirmation dialog. Returns true if the user confirmed.</summary>
    Task<bool> ConfirmAsync(string title, string message, string confirmText, string cancelText);

    /// <summary>Shows an informational dialog.</summary>
    Task InfoAsync(string title, string message);

    /// <summary>Asks how to resolve a restore conflict for the given original path.</summary>
    Task<RestoreConflictResolution> AskRestoreConflictAsync(string originalPath);

    /// <summary>Prompts for a path to save a text file; returns null if cancelled.</summary>
    string? AskSaveTextPath(string suggestedFileName);
}
