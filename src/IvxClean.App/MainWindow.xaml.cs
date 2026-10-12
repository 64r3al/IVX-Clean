using System.Windows;
using IvxClean.Core;

namespace IvxClean.App;

/// <summary>
/// The application shell window. The full navigation UI (§7) is built in the UI phase;
/// this placeholder confirms the app launches and renders on the target framework.
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>Initializes the window and shows the resolved product version.</summary>
    public MainWindow()
    {
        InitializeComponent();
        VersionText.Text = $"Version {AppInfo.Version} · Made by {AppInfo.MadeBy}";
    }
}
