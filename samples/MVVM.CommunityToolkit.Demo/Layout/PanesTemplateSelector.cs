using System.Windows;
using System.Windows.Controls;

using MVVM.CommunityToolkit.Demo.ViewModels;

namespace MVVM.CommunityToolkit.Demo.Layout;

/// <summary>
/// A <see cref="DataTemplateSelector"/> that selects the appropriate <see cref="DataTemplate"/> based on the type of the item.
/// </summary>
public class PanesTemplateSelector : DataTemplateSelector
{
    /// <summary>
    /// Gets or sets the <see cref="DataTemplate"/> for the <see cref="FileViewModel"/>.
    /// </summary>
    public DataTemplate FileViewTemplate { get; set; } = default!;

    /// <summary>
    /// Gets or sets the <see cref="DataTemplate"/> for the <see cref="FileStatsViewModel"/>.
    /// </summary>
    public DataTemplate FileStatsViewTemplate { get; set; } = default!;

    /// <inheritdoc/>
    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
        return item switch
        {
            FileViewModel => FileViewTemplate,
            FileStatsViewModel => FileStatsViewTemplate,
            _ => base.SelectTemplate(item, container)
        };
    }
}