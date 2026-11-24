using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;

namespace Cornflower.Sample.Pages;

/// <summary>The eighth plain themes, part b (#241): NotificationCard of each type on the page, and WindowNotificationManager's banners.</summary>
[SamplePage("plain notifications", Order = 242)]
public partial class PlainNotificationsPage : UserControl
{
    /// <summary>One manager per window, made on the first show: Avalonia's puts itself back in the window's adorner layer whenever the window's template is applied again, so one taken out would come back.</summary>
    private static readonly ConditionalWeakTable<TopLevel, WindowNotificationManager> Managers = new();

    private int _shown;

    public PlainNotificationsPage()
    {
        InitializeComponent();

        Information.Content = new Notification("Synced", "Your notes are up to date on this phone.");
        Success.Content = new Notification("Saved", "Your changes are in.", NotificationType.Success);
        Warning.Content = new Notification("Battery saver", "Background sync is off until you charge.", NotificationType.Warning);
        Error.Content = new Notification("Not sent", "The message couldn't be sent. Check your connection and try again, or save it as a draft.", NotificationType.Error);
        LightInformation.Content = new Notification("Synced", "Your notes are up to date on this phone.");
        LightError.Content = new Notification("Not sent", "The message couldn't be sent.", NotificationType.Error);

        ShowOne.Click += (_, _) => Show(new Notification("Message " + ++_shown, "A banner at the top, under the status bar.", expiration: TimeSpan.Zero));
        ShowError.Click += (_, _) => Show(new Notification("Not sent", "The message couldn't be sent.", NotificationType.Error, TimeSpan.Zero));
    }

    private WindowNotificationManager? Manager() =>
        TopLevel.GetTopLevel(this) is { } top
            ? Managers.GetValue(top, window =>
            {
                var manager = new WindowNotificationManager(window) { MaxItems = 3 };
                // Avalonia's manager drops a card shown before its template is applied.
                manager.ApplyTemplate();
                return manager;
            })
            : null;

    private void Show(Notification notification) => Manager()?.Show(notification);

    // The banners are this page's: they go with it.
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (e.RootVisual is TopLevel top && Managers.TryGetValue(top, out var manager))
            manager.CloseAll();
    }
}
