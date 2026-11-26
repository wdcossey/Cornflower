using Avalonia.Controls;
using Avalonia.Threading;

namespace Cornflower.Sample.Pages;

/// <summary>The eighth plain themes, part c (#241): pull-to-refresh round a list, with a button that refreshes too.</summary>
[SamplePage("plain refresh", Order = 243)]
public partial class PlainRefreshPage : UserControl
{
    /// <summary>How long the sample's refresh takes before it completes by itself.</summary>
    internal static readonly TimeSpan RefreshTime = TimeSpan.FromSeconds(2);

    private int _refreshes;

    private bool _refreshing;

    public PlainRefreshPage()
    {
        InitializeComponent();

        Rows.ItemsSource = Enumerable.Range(1, 30).Select(row => "row " + row).ToArray();
        // So the ring can be seen without a touch screen.
        // A tap while refreshing would start a second refresh over the first, so it waits for the first to end.
        Refresh.Click += (_, _) =>
        {
            if (!_refreshing)
                Container.RequestRefresh();
        };
        Container.RefreshRequested += OnRefreshRequested;
    }

    private void OnRefreshRequested(object? sender, RefreshRequestedEventArgs e)
    {
        var deferral = e.GetDeferral();
        _refreshing = true;
        DispatcherTimer.RunOnce(() =>
        {
            _refreshing = false;
            _refreshes++;
            Status.Text = _refreshes == 1 ? "refreshed once" : $"refreshed {_refreshes} times";
            deferral.Complete();
        }, RefreshTime);
    }
}
