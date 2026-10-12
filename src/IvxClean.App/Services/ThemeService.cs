using System;
using System.Windows;
using Microsoft.Win32;
using Wpf.Ui.Appearance;

namespace IvxClean.App.Services;

/// <summary>Applies the IVX Clean color tokens and the matching window chrome for the chosen theme.</summary>
public sealed class ThemeService
{
    private const string DarkDict = "pack://application:,,,/Resources/Colors.Dark.xaml";
    private const string LightDict = "pack://application:,,,/Resources/Colors.Light.xaml";

    private ResourceDictionary? _current;

    /// <summary>Applies the given theme (resolving System against the Windows setting).</summary>
    public void Apply(AppTheme theme)
    {
        bool dark = theme switch
        {
            AppTheme.Dark => true,
            AppTheme.Light => false,
            _ => IsWindowsDark(),
        };

        var dict = new ResourceDictionary { Source = new Uri(dark ? DarkDict : LightDict) };
        Application.Current.Resources.MergedDictionaries.Remove(_current);
        Application.Current.Resources.MergedDictionaries.Add(dict);
        _current = dict;

        ApplicationThemeManager.Apply(
            dark ? ApplicationTheme.Dark : ApplicationTheme.Light,
            Wpf.Ui.Controls.WindowBackdropType.Mica,
            updateAccent: false);
    }

    private static bool IsWindowsDark()
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is int v && v == 0;
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException)
        {
            return true; // default to dark
        }
    }
}
