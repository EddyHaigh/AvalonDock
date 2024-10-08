using System.Windows.Data;

using MVVM.CommunityToolkit.Demo.ViewModels;

namespace MVVM.CommunityToolkit.Demo.Converters;

/// <summary>
/// Converts the active document value to a <see cref="FileViewModel"/> object.
/// </summary>
public sealed class ActiveDocumentConverter : IValueConverter
{
    /// <summary>
    /// Converts the active document value to a <see cref="FileViewModel"/> object.
    /// </summary>
    /// <param name="value">The value to be converted.</param>
    /// <param name="targetType">The type of the binding target property.</param>
    /// <param name="parameter">The converter parameter.</param>
    /// <param name="culture">The culture to use in the converter.</param>
    /// <returns>The converted value if it is of type <see cref="FileViewModel"/>, otherwise <see cref="Binding.DoNothing"/>.</returns>
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        return value is FileViewModel ? value : Binding.DoNothing;
    }

    /// <summary>
    /// Converts the active document value back to a <see cref="FileViewModel"/> object.
    /// </summary>
    /// <param name="value">The value to be converted back.</param>
    /// <param name="targetType">The type of the binding target property.</param>
    /// <param name="parameter">The converter parameter.</param>
    /// <param name="culture">The culture to use in the converter.</param>
    /// <returns>The converted value if it is of type <see cref="FileViewModel"/>, otherwise <see cref="Binding.DoNothing"/>.</returns>
    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        return value is FileViewModel ? value : Binding.DoNothing;
    }
}
