using System.Threading.Tasks;
using IvxClean.Core.Quarantine;
using Microsoft.Win32;
using Wpf.Ui.Controls;

namespace IvxClean.App.Services;

/// <summary>Themed confirmation/chooser dialogs backed by WPF-UI.</summary>
public sealed class DialogService : IDialogService
{
    /// <inheritdoc />
    public async Task<bool> ConfirmAsync(string title, string message, string confirmText, string cancelText)
    {
        var box = new Wpf.Ui.Controls.MessageBox
        {
            Title = title,
            Content = message,
            PrimaryButtonText = confirmText,
            CloseButtonText = cancelText,
        };
        MessageBoxResult result = await box.ShowDialogAsync().ConfigureAwait(true);
        return result == MessageBoxResult.Primary;
    }

    /// <inheritdoc />
    public async Task InfoAsync(string title, string message)
    {
        var box = new Wpf.Ui.Controls.MessageBox
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
        };
        _ = await box.ShowDialogAsync().ConfigureAwait(true);
    }

    /// <inheritdoc />
    public async Task<RestoreConflictResolution> AskRestoreConflictAsync(string originalPath)
    {
        var box = new Wpf.Ui.Controls.MessageBox
        {
            Title = "A file already exists",
            Content = $"{originalPath} already exists. Keep both, or skip restoring this item?",
            PrimaryButtonText = "Keep both",
            CloseButtonText = "Skip",
        };
        MessageBoxResult result = await box.ShowDialogAsync().ConfigureAwait(true);
        return result == MessageBoxResult.Primary ? RestoreConflictResolution.KeepBoth : RestoreConflictResolution.Skip;
    }

    /// <inheritdoc />
    public string? AskSaveTextPath(string suggestedFileName)
    {
        var dialog = new SaveFileDialog
        {
            FileName = suggestedFileName,
            Filter = "Text file (*.txt)|*.txt",
            DefaultExt = ".txt",
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
