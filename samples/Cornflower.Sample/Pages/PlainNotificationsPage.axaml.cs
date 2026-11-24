using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Primitives;

namespace Cornflower.Sample.Pages;

/// <summary>The eighth plain themes, part b (#241): NotificationCard of each type on the page, and WindowNotificationManager's banners.</summary>
[SamplePage("plain notifications", Order = 242)]
public partial class PlainNotificationsPage : UserControl
{
    private WindowNotificationManager? _notifications;
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

    private void Show(Notification notification)
    {
        _notifications ??= new WindowNotificationManager(TopLevel.GetTopLevel(this)) { MaxItems = 3 };
        _notifications.Show(notification);
    }

    // The manager lives in the window's adorner layer: take it out as the page goes.
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (_notifications?.Parent is AdornerLayer layer)
            layer.Children.Remove(_notifications);
        _notifications = null;
    }
}
