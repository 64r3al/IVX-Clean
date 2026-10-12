using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

namespace IvxClean.App.Services;

/// <summary>Switches the shell's current page view model.</summary>
public interface INavigationService
{
    /// <summary>The view model currently shown in the shell content area.</summary>
    ObservableObject? Current { get; }

    /// <summary>Navigates to a freshly-resolved view model of the given type.</summary>
    TViewModel NavigateTo<TViewModel>() where TViewModel : ObservableObject;

    /// <summary>Shows an already-constructed view model (e.g. one carrying scan results).</summary>
    void Show(ObservableObject viewModel);
}

/// <inheritdoc cref="INavigationService"/>
public sealed partial class NavigationService(IServiceProvider services) : ObservableObject, INavigationService
{
    [ObservableProperty]
    private ObservableObject? _current;

    /// <inheritdoc />
    public TViewModel NavigateTo<TViewModel>() where TViewModel : ObservableObject
    {
        var vm = services.GetRequiredService<TViewModel>();
        Current = vm;
        return vm;
    }

    /// <inheritdoc />
    public void Show(ObservableObject viewModel) => Current = viewModel;
}
