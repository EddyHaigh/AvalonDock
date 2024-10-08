using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MVVM.CommunityToolkit.Demo.Converters;

/// <summary>
/// Source: http://stackoverflow.com/questions/534575/how-do-i-invert-booleantovisibilityconverter
/// 
/// Implements a Boolean to Visibility converter
/// Use ConverterParameter=true to negate the visibility - boolean interpretation.
/// </summary>
[ValueConversion(typeof(bool), typeof(Visibility))]
public sealed class BoolToVisibilityConverter : IValueConverter
{
    /// <summary>
    /// Converts a <seealso cref="bool"/> value
    /// into a <seealso cref="Visibility"/> value.
    /// </summary>
    /// <param name="value">The value to convert back to a <see cref="Visibility"/>.</param>
    /// <param name="targetType">The target type.</param>
    /// <param name="parameter">The parameter.</param>
    /// <param name="culture">The culture information.</param>
    /// <returns>The visibility type.</returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isInverted = parameter != null && (bool)parameter;
        bool isVisible = value != null && (bool)value;

        if (isVisible)
        {
            return isInverted ? Visibility.Hidden : Visibility.Visible;
        }
        else
        {
            return isInverted ? Visibility.Visible : Visibility.Hidden;
        }
    }

    /// <summary>
    /// Converts a <seealso cref="Visibility"/> value
    /// into a <seealso cref="Boolean"/> value.
    /// </summary>
    /// <param name="value">The value to convert back to a <see cref="bool"/>.</param>
    /// <param name="targetType">The target type.</param>
    /// <param name="parameter">The parameter.</param>
    /// <param name="culture">The culture information.</param>
    /// <returns>The converted visibility.</returns>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        Visibility visiblility = value == null ? Visibility.Hidden : (Visibility)value;
        bool isInverted = parameter != null && (bool)parameter;

        return (visiblility == Visibility.Visible) != isInverted;
    }
}
