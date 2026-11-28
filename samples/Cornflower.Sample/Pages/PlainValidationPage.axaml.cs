using Avalonia.Controls;
using Avalonia.Data;

namespace Cornflower.Sample.Pages;

/// <summary>The eighth plain themes, part a (#241): DataValidationErrors round Wp81's TextBox, PasswordBox and ComboBox, and the light theme.</summary>
[SamplePage("plain validation", Order = 241)]
public partial class PlainValidationPage : UserControl
{
    // Held here: a binding holds its Source weakly, so a Person only the binding held went at the first GC, and the
    // setter then threw a TargetException for its missing target (#262).
    private readonly Person _person = new();
    private readonly Person _lightPerson = new();

    public PlainValidationPage()
    {
        InitializeComponent();

        // A binding's own validation: the name's error comes and goes as it is typed.
        NameBox.Bind(TextBox.TextProperty, new Binding(nameof(Person.Name)) { Source = _person, Mode = BindingMode.TwoWay });
        LightName.Bind(TextBox.TextProperty, new Binding(nameof(Person.Name)) { Source = _lightPerson, Mode = BindingMode.TwoWay });

        DataValidationErrors.SetErrors(Code, ["Too short.", "A code is six digits: the two you have, and four more from the card's back."]);
        DataValidationErrors.SetErrors(Password, ["At least eight characters."]);
        DataValidationErrors.SetErrors(Size, ["Choose a size."]);
        DataValidationErrors.SetErrors(LightSize, ["Choose a size."]);
    }

    /// <summary>A name that can't be empty: a binding reports the setter's exception as the field's error.</summary>
    private sealed class Person
    {
        private string _name = "Ada";

        public string Name
        {
            get => _name;
            set => _name = string.IsNullOrWhiteSpace(value) ? throw new DataValidationException("Enter a name.") : value;
        }
    }
}
