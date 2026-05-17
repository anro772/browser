using System;
using System.Windows;

namespace BrowserApp.UI.Services;

/// <summary>
/// Thin wrapper over <see cref="Application.Current"/>'s Dispatcher.
/// Marshals to the UI thread when one exists; runs inline otherwise.
///
/// Why: VMs need to mutate ObservableCollections back on the UI thread after
/// async work, but unit tests have no WPF Application — calling
/// <c>Application.Current.Dispatcher.Invoke</c> throws NullReferenceException
/// in that case. This helper makes those call sites test-safe without changing
/// runtime semantics (in production, Application.Current is always set, so we
/// take the dispatcher path).
/// </summary>
internal static class UiThread
{
    public static void Invoke(Action action)
    {
        if (action == null) return;
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher != null && !dispatcher.CheckAccess())
        {
            dispatcher.Invoke(action);
        }
        else
        {
            action();
        }
    }
}
