using Avalonia.Controls;

namespace Cornflower.Sample.Pages;

/// <summary>A letter of the contacts: its key and its people, none for a letter no one's name starts with.</summary>
public sealed class ContactGroup(string key, IEnumerable<string> people) : List<string>(people)
{
    public string Key { get; } = key;
}

/// <summary>
/// SemanticZoom (#215): a contacts list grouped a to z, some letters empty, and its jump list: a tap on a letter's header
/// opens the tiles over the whole window, a tap on a tile lands the list on that letter, Back closes them.
/// </summary>
[SamplePage("semantic zoom", Order = 215)]
public partial class SemanticZoomPage : UserControl
{
    /// <summary>The contacts, by first name. No one's starts with e, i, q, u, v, x, y or z.</summary>
    private static readonly string[] People =
    [
        "Aaron Ayers", "Abby Ng", "Adam Ito", "Alice Ward", "Amir Khan", "Anna Berg",
        "Ben Cole", "Bianca Ruiz", "Boris Lind",
        "Carla Diaz", "Chen Wei", "Chris Park", "Cora Hale",
        "Dana Fox", "David Moss", "Dina Shah",
        "Fatima Noor", "Felix Grant", "Fiona Lake",
        "Gabe Stone", "Grace Lee",
        "Hana Sato", "Hugo Brandt",
        "Jade Kim", "James Hall", "Jonas Weber", "Julia Rossi",
        "Kai Tan", "Kara Webb",
        "Leo Marsh", "Lila Patel", "Luca Romano",
        "Maya Cruz", "Mia Wong", "Milo Hart",
        "Nadia Popov", "Noah Reed",
        "Olga Ivanova", "Omar Aziz",
        "Paul Young", "Priya Rao",
        "Rafael Silva", "Rosa Lopez", "Ryan Doyle",
        "Sara Holm", "Sofia Marin", "Sven Olsen",
        "Tara Quinn", "Theo Black", "Tom Price",
        "Wen Liu", "Will Shaw",
    ];

    public SemanticZoomPage()
    {
        InitializeComponent();
        DataContext = Contacts();
    }

    /// <summary>Every letter, a to z, with its people: the list shows those with some, the jump list all of them.</summary>
    internal static List<ContactGroup> Contacts() =>
        "abcdefghijklmnopqrstuvwxyz".Select(letter => new ContactGroup(letter.ToString(),
            People.Where(name => char.ToLowerInvariant(name[0]) == letter))).ToList();
}
