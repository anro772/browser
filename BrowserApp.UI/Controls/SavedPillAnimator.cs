using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace BrowserApp.UI.Controls;

/// <summary>
/// Fades a "Saved ✓" pill in for 150ms, holds for 1.2s, fades out over 250ms.
/// Consumers call Pulse(border) from a property-changed handler tied to an
/// incrementing counter so each save shows fresh feedback.
/// </summary>
public static class SavedPillAnimator
{
    public static void Pulse(FrameworkElement? target)
    {
        if (target == null) return;

        var sb = new Storyboard();
        var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(150));
        var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(250))
        {
            BeginTime = TimeSpan.FromMilliseconds(1350)
        };
        Storyboard.SetTarget(fadeIn, target);
        Storyboard.SetTargetProperty(fadeIn, new PropertyPath(UIElement.OpacityProperty));
        Storyboard.SetTarget(fadeOut, target);
        Storyboard.SetTargetProperty(fadeOut, new PropertyPath(UIElement.OpacityProperty));
        sb.Children.Add(fadeIn);
        sb.Children.Add(fadeOut);
        sb.Begin();
    }
}
