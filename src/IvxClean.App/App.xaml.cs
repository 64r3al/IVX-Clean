using System;
using System.Threading;
using System.Windows;
using IvxClean.App.Services;
using IvxClean.App.ViewModels;
using IvxClean.Core.Abstractions;
using IvxClean.Core.Cleaning;
using IvxClean.Core.Paths;
using IvxClean.Core.Rules;
using IvxClean.Core.Quarantine;
using IvxClean.Core.Safety;
using IvxClean.Core.Scanning;
using IvxClean.Core.Software;
using IvxClean.Core.SystemOps;
using Microsoft.Extensions.DependencyInjection;

namespace IvxClean.App;

/// <summary>Application entry point and composition root. Single-instance via a named mutex.</summary>
public partial class App : Application
{
    private const string SingleInstanceMutexName = @"Global\IVXClean.SingleInstance.9E2C1A77";
    private static Mutex? _singleInstanceMutex;

    /// <summary>The composed service provider.</summary>
    public IServiceProvider Services { get; private set; } = null!;

    /// <inheritdoc />
    protected override void OnStartup(StartupEventArgs e)
    {
        _singleInstanceMutex = new Mutex(initiallyOwned: true, SingleInstanceMutexName, out bool createdNew);
        if (!createdNew)
        {
            Shutdown();
            return;
        }

        base.OnStartup(e);

        Services = BuildServices();

        // Recover any services a previous crash left stopped; purge expired quarantine.
        var settings = Services.GetRequiredService<ISettingsService>();
        Services.GetRequiredService<IServicesManager>().RecoverIfCrashed();
        Services.GetRequiredService<IQuarantineStore>().PurgeExpired(settings.Current.QuarantineDays);

        Services.GetRequiredService<ThemeService>().Apply(settings.Current.Theme);

        var window = new MainWindow { DataContext = Services.GetRequiredService<MainWindowViewModel>() };
        window.Show();
    }

    private static ServiceProvider BuildServices()
    {
        var s = new ServiceCollection();

        // Core singletons.
        s.AddSingleton<IClock, SystemClock>();
        s.AddSingleton<IPathRoots, ProductionPathRoots>();
        s.AddSingleton<IProcessInspector, ProcessInspector>();
        s.AddSingleton(_ => RuleCatalog.LoadEmbedded());
        s.AddSingleton(_ => SoftwareClassifier.LoadEmbedded());
        s.AddSingleton<IServiceController, WindowsServiceController>();
        s.AddSingleton<IServicesManager, ServicesManager>();
        s.AddSingleton<IRestorePointService, WmiRestorePointService>();
        s.AddSingleton<IRecycleBin, ShellRecycleBin>();
        s.AddSingleton<IEventLogService, WindowsEventLogService>();
        s.AddSingleton<IDismRunner, DismRunner>();
        s.AddSingleton<IRebootState, RegistryRebootState>();
        s.AddSingleton<IInstalledSoftwareSource, RegistryInstalledSoftwareSource>();
        s.AddSingleton<IInstalledSoftwareSource, AppxInstalledSoftwareSource>();
        s.AddSingleton<SoftwareInventory>();
        s.AddSingleton<ISoftwareUninstaller, ProcessSoftwareUninstaller>();

        // App services.
        s.AddSingleton<ISettingsService, SettingsService>();
        s.AddSingleton<ThemeService>();
        s.AddSingleton<IDialogService, DialogService>();
        s.AddSingleton<INavigationService, NavigationService>();
        s.AddSingleton<MainWindowViewModel>();

        // Safety-sensitive transients, rebuilt from current settings each time.
        s.AddTransient(sp => SafetyPolicy.CreateProduction(sp.GetRequiredService<ISettingsService>().Current.ProtectedFolders));
        s.AddTransient<ISafetyGuard>(sp => new SafetyGuard(
            sp.GetRequiredService<IPathRoots>(), sp.GetRequiredService<IClock>(), sp.GetRequiredService<SafetyPolicy>()));
        s.AddTransient<IScanner>(sp => new Scanner(
            sp.GetRequiredService<RuleCatalog>(), sp.GetRequiredService<IPathRoots>(),
            sp.GetRequiredService<ISafetyGuard>(), sp.GetRequiredService<IProcessInspector>()));
        s.AddTransient<IQuarantineStore>(sp => new QuarantineStore(
            sp.GetRequiredService<IPathRoots>(), sp.GetRequiredService<IClock>(),
            sp.GetRequiredService<ISettingsService>().Current.QuarantineDays));
        s.AddTransient<ICleaner>(sp => new Cleaner(
            sp.GetRequiredService<ISafetyGuard>(), sp.GetRequiredService<IQuarantineStore>(), sp.GetRequiredService<IClock>()));

        // Page view models.
        s.AddTransient<DashboardViewModel>();
        s.AddTransient<ScanViewModel>();
        s.AddTransient<QuarantineViewModel>();
        s.AddTransient<SoftwareViewModel>();
        s.AddTransient<SettingsViewModel>();
        s.AddTransient<AboutViewModel>();

        return s.BuildServiceProvider();
    }

    /// <inheritdoc />
    protected override void OnExit(ExitEventArgs e)
    {
        (Services as IDisposable)?.Dispose();
        _singleInstanceMutex?.Dispose();
        _singleInstanceMutex = null;
        base.OnExit(e);
    }
}
