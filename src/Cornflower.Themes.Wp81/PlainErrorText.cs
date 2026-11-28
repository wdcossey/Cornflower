using System.Globalization;
using System.Reflection;
using Avalonia.Data.Converters;

namespace Cornflower.Themes.Wp81;

/// <summary>
/// The text of an error line in the plain DataValidationErrors (#262): an exception's &lt;c&gt;Message&lt;/c&gt;, any other error
/// as itself. Avalonia passes a &lt;c&gt;DataValidationException&lt;/c&gt;'s &lt;c&gt;ErrorData&lt;/c&gt; on as the error, but any other exception
/// a binding catches (a setter's own, or a &lt;c&gt;TargetException&lt;/c&gt;) as the exception, whose &lt;c&gt;ToString()&lt;/c&gt; is its whole
/// stack: Simple and Fluent show that, in a tooltip or a line; a line of small print shows the message, a
/// &lt;c&gt;TargetInvocationException&lt;/c&gt;'s inner one's, as its own says nothing.
/// &lt;/summary&gt;
internal sealed class PlainErrorText : IValueConverter
{
    public static readonly PlainErrorText Converter = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is Exception exception ? Unwrap(exception).Message : value?.ToString();

    private static Exception Unwrap(Exception exception)
    {
        while (exception is TargetInvocationException { InnerException: { } inner })
            exception = inner;
        return exception;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
