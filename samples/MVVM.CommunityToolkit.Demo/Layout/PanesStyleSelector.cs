using System.Windows;
using System.Windows.Controls;

using MVVM.CommunityToolkit.Demo.ViewModels;

namespace MVVM.CommunityToolkit.Demo.Layout;

/// <summary>
/// A style selector for panes in the application.
/// </summary>
public class PanesStyleSelector : StyleSelector
{
    /// <summary>
    /// Gets or sets the style for tool view models.
    /// </summary>
    public Style ToolStyle { get; set; } = default!;

    /// <summary>
    /// Gets or sets the style for file view models.
    /// </summary>
    public Style FileStyle { get; set; } = default!;

    /// <inheritdoc/>
    public override Style SelectStyle(object item, DependencyObject container)
    {
        return item switch
        {
            ToolViewModel => ToolStyle,
            FileViewModel => FileStyle,
            _ => base.SelectStyle(item, container)
        };
    }
}