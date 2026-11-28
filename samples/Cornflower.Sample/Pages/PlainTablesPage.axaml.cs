using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;

namespace Cornflower.Sample.Pages;

/// <summary>A row of the plain tables page: a song's title, artist and length.</summary>
public sealed record PlainTableSong(string Title, string Artist, string Time);

/// <summary>The ninth plain themes (#242): a TableView of twenty songs, one selected at load below the fold, and a light one with a height of its own.</summary>
[SamplePage("plain tables", Order = 245)]
public partial class PlainTablesPage : UserControl
{
    private static readonly string[] Titles =
    [
        "morning light", "harbour", "paper planes", "slow river", "north wind", "glass houses", "small hours",
        "lanterns", "salt and stone", "open road", "evergreen", "quiet storm", "copper sky", "the long way",
        "blue hour", "riverside", "wildflowers", "city lights", "last train", "homeward",
    ];

    private static readonly string[] Artists = ["the tides", "ada lane", "north & co", "mira"];

    public PlainTablesPage()
    {
        InitializeComponent();

        List<PlainTableSong> songs = Titles
            .Select((title, i) => new PlainTableSong(title, Artists[i % Artists.Length], $"{3 + i % 3}:{(i * 17) % 60:00}"))
            .ToList();
        Songs.ItemsSource = songs;
        LightSongs.ItemsSource = songs.Take(10).ToList();

        void Update() => Selection.Text = Songs.SelectedItem is PlainTableSong song ? "selected: " + song.Title : "nothing selected";
        Update();
        Songs.SelectionChanged += (_, _) => Update();
    }
}
